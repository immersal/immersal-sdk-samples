/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Immersal.REST;
using Immersal.Samples.Util;
using UnityEngine.Events;
using TMPro;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace Immersal.Samples.Mapping
{
    public abstract class BaseMapper : MonoBehaviour, IJobHost
    {
        protected const double DefaultRadius = 200.0;

        public event LoggedOut OnLogOut = null;
        public delegate void LoggedOut();
        public UnityEvent onConnect = null;
        public UnityEvent onFailedToConnect = null;

        [SerializeField]
        private TextMeshProUGUI m_LocationText = null;
        [SerializeField]
        private TextMeshProUGUI m_VLocationText = null;
        [SerializeField]
        private Toggle m_GpsToggle = null;
        [SerializeField]
        private GameObject m_LocationPanel = null;

        [HideInInspector]
        public MapperStats stats = new MapperStats();
        [HideInInspector]
        public double vlatitude = 0.0;
        [HideInInspector]
        public double vlongitude = 0.0;
        [HideInInspector]
        public double valtitude = 0.0;
        [HideInInspector]
        public Dictionary<int, PointCloudRenderer> pcr = new Dictionary<int, PointCloudRenderer>();
        [HideInInspector]
        public WorkspaceManager workspaceManager;
        [HideInInspector]
        public VisualizeManager visualizeManager;
        [HideInInspector]
        public (bool valid, double[] mapToEcef, Matrix4x4 Matrix, Pose LastUpdatedPose) lastLocalizedPose = (false, new double[13], Matrix4x4.identity, Pose.identity);

        protected bool m_RgbCapture = false;
        protected int m_ImageIndex = 0;
        protected uint m_ImageRun = 0;
        protected bool m_SessionFirstImage = true;
        protected bool m_IsTracking = false;
        protected List<CoroutineJob> m_Jobs = new List<CoroutineJob>();
        private int m_JobLock = 0;
        protected ImmersalSDK m_Sdk;
        protected double m_Latitude = 0.0;
        protected double m_Longitude = 0.0;
        protected double m_Altitude = 0.0;
        protected double m_Haccuracy = 0.0;
        protected double m_Vaccuracy = 0.0;
        protected bool m_bCaptureRunning = false;

        private string m_Server = null;
        private string m_Token = null;
        private int m_Bank = 0;
        private AudioSource m_CameraShutterClick;
        private IEnumerator m_UpdateJobList;
        private Camera m_MainCamera = null;
        
		private static IDispatch Dispatch;

        public string token
        {
            get
            {
                if (m_Token == null)
                {
                    m_Token = PlayerPrefs.GetString("token");
                    if (m_Token == null)
                        Debug.LogError("No valid developer token. Contact sdk@immersal.com.");
                }

                return m_Token;
            }
            set { m_Token = value; }
        }

        public string server
        {
            get
            {
                if (m_Server == null)
                {
                    m_Server = m_Sdk.localizationServer;
                }

                return m_Server;
            }
            set { m_Server = value; }
        }

        public Camera mainCamera
        {
            get
            {
                if (m_MainCamera == null)
                {
                    m_MainCamera = Camera.main;
                    if (m_MainCamera == null)
                        Debug.LogError("No Camera found");
                }

                return m_MainCamera;
            }
        }

        public bool useGPS
        {
            get { return Input.location.status == LocationServiceStatus.Running; }
        }

        public int currentBank
        {
            get { return m_Bank; }
        }

        #region Abstract methods

        protected abstract IEnumerator Capture(bool anchor);
        public abstract void Localize();
        public abstract void LocalizeServer();

        #endregion

        #region Virtual methods

        protected virtual void OnEnable()
        {
            stats.queueLen = 0;
            stats.imageCount = 0;
            stats.locFail = 0;
            stats.locSucc = 0;
        }

        protected virtual void OnDisable()
        {
            PlayerPrefs.DeleteKey("token");
            m_Sdk.developerToken = null;
        }

        public virtual void Update()
        {
            UpdateLocation();

            stats.queueLen = m_Jobs.Count;

            if (m_JobLock == 1)
                return;
            if (m_Jobs.Count > 0)
            {
                m_JobLock = 1;
                StartCoroutine(RunJob(m_Jobs[0]));
            }
        }

        #endregion

		public void OnGPSToggleChanged(bool value)
		{
            if (value)
            {
                Invoke("StartGPS", 0.1f);
            }
            else
            {
                Invoke("StopGPS", 0.1f);
            }
		}

        internal void ImageRunUpdate()
        {
            long bin = System.DateTime.Now.ToBinary();
            uint data = (uint)bin ^ (uint)(bin >> 32);
            m_ImageRun = (m_ImageRun ^ data) * 16777619;
        }

        void Awake()
        {
            Dispatch = new MainThreadDispatch();
            m_CameraShutterClick = GetComponent<AudioSource>();
            workspaceManager = GetComponentInChildren<WorkspaceManager>();
            visualizeManager = GetComponentInChildren<VisualizeManager>();
            visualizeManager.OnItemSelected += OnItemSelected;
            visualizeManager.OnItemDeleted += OnItemDeleted;
            visualizeManager.OnItemRestored += OnItemRestored;
            visualizeManager.OnSelectorOpened += OnSelectorOpened;
            visualizeManager.OnSelectorClosed += OnSelectorClosed;

            ImageRunUpdate();
        }

        void Start()
        {
            m_Sdk = ImmersalSDK.Instance;

            if ((PlayerPrefs.GetInt("use_gps", 0) == 1))
            {
                m_GpsToggle.isOn = true;
            }

            StartCoroutine(StatusPoll());
            Jobs();
        }

#if PLATFORM_ANDROID
		private IEnumerator WaitForLocationPermission()
		{
			while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
			{
				yield return null;
			}

			Debug.Log("Location permission OK");
			StartCoroutine(EnableLocationServices());
			yield return null;
		}
#endif

        public void StartGPS()
        {
            #if UNITY_IOS
            StartCoroutine(EnableLocationServices());
			#elif PLATFORM_ANDROID
			if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
			{
				Debug.Log("Location permission OK");
				StartCoroutine(EnableLocationServices());
			}
			else
			{
				Permission.RequestUserPermission(Permission.FineLocation);
				StartCoroutine(WaitForLocationPermission());
			}
			#endif
        }

        public void StopGPS()
        {
            Input.location.Stop();
            PlayerPrefs.SetInt("use_gps", 0);
            NotificationManager.Instance.GenerateNotification("Geolocation tracking stopped");
            m_LocationText.text = "";
            m_LocationPanel.SetActive(false);
        }

        private IEnumerator EnableLocationServices()
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                m_GpsToggle.SetIsOnWithoutNotify(false);
                NotificationManager.Instance.GenerateNotification("Location services not enabled");
                Debug.Log("Location services not enabled");
                yield break;
            }

            // Start service before querying location
            Input.location.Start(0.001f, 0.001f);

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                m_GpsToggle.SetIsOnWithoutNotify(false);
                NotificationManager.Instance.GenerateNotification("Location services timed out");
                Debug.Log("Timed out");
                yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                m_GpsToggle.SetIsOnWithoutNotify(false);
                NotificationManager.Instance.GenerateNotification("Unable to determine device location");
                Debug.Log("Unable to determine device location");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                PlayerPrefs.SetInt("use_gps", 1);
                m_LocationPanel.SetActive(true);
                NotificationManager.Instance.GenerateNotification("Tracking geolocation");
            }
        }

        public int SwitchBank(int max_banks)
        {
            m_Bank = (m_Bank + 1) % max_banks;
            m_SessionFirstImage = true;

            return m_Bank;
        }

        public void ToggleRGBCapture(Toggle toggle)
        {
            m_RgbCapture = toggle.isOn;
        }

        public void ToggleVisualization(Toggle toggle)
        {
            PointCloudRenderer.visible = toggle.isOn;
        }

        public void ToggleVisualization(bool active)
        {
            PointCloudRenderer.visible = active;
        }

        private void OnItemSelected(SDKJob job)
        {
            LoadMap(job.id);
        }

        private void OnItemDeleted(SDKJob job)
        {
            DeleteMap(job.id);
        }

        private void OnItemRestored(SDKJob job)
        {
            RestoreMapImages(job.id);
        }

        private void OnSelectorOpened()
        {
            if (m_UpdateJobList != null)
            {
                StopCoroutine(m_UpdateJobList);
            }

            m_UpdateJobList = UpdateJobList();
            StartCoroutine(m_UpdateJobList);
        }

        private void OnSelectorClosed()
        {
            if (m_UpdateJobList != null)
            {
                StopCoroutine(m_UpdateJobList);
            }
        }

        IEnumerator UpdateJobList()
        {
            while (true)
            {
                Jobs();
                yield return new WaitForSeconds(3f);
            }
        }

        IEnumerator StatusPoll()
        {
            CoroutineJobStatus j = new CoroutineJobStatus();
            j.host = this;

            yield return StartCoroutine(j.RunJob());
            yield return new WaitForSeconds(3);
            StartCoroutine(StatusPoll());
        }

        private IEnumerator RunJob(CoroutineJob j)
        {
            yield return StartCoroutine(j.RunJob());
            m_Jobs.RemoveAt(0);
            m_JobLock = 0;
        }

        public MapperStats Stats()
        {
            return stats;
        }

        void UpdateLocation()
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                m_Latitude = Input.location.lastData.latitude;
                m_Longitude = Input.location.lastData.longitude;
                m_Altitude = Input.location.lastData.altitude;
                m_Haccuracy = Input.location.lastData.horizontalAccuracy;
                m_Vaccuracy = Input.location.lastData.verticalAccuracy;

                string txt = string.Format("lat: {0}, lon: {1}, alt: {2}\nhacc: {3}, vacc: {4}", 
                                m_Latitude.ToString("0.00000"), 
                                m_Longitude.ToString("0.00000"), 
                                m_Altitude.ToString("0.00"), 
                                m_Haccuracy.ToString("0.00"), 
                                m_Vaccuracy.ToString("0.00"));
                m_LocationText.text = txt;
            }

            string txt2 = string.Format("VLat: {0}, VLon: {1}, VAlt: {2}", 
                            vlatitude.ToString("0.00000"),
                            vlongitude.ToString("0.00000"),
                            valtitude.ToString("0.00"));
            m_VLocationText.text = txt2;

            if (lastLocalizedPose.valid)
            {
                Camera cam = this.mainCamera;
                Matrix4x4 trackerSpace = Matrix4x4.TRS(cam.transform.position, cam.transform.rotation, Vector3.one);
                Matrix4x4 m = lastLocalizedPose.Matrix * trackerSpace;
                Vector3 pos = m.GetColumn(3);

                double[] wgs84 = new double[3];
                int r = Immersal.Core.PosMapToWgs84(wgs84, pos, lastLocalizedPose.mapToEcef);
                vlatitude = wgs84[0];
                vlongitude = wgs84[1];
                valtitude = wgs84[2];

                lastLocalizedPose.LastUpdatedPose.position = pos;
                lastLocalizedPose.LastUpdatedPose.rotation = m.rotation;
            }
        }

        public void DeleteMap(int mapId)
        {
            CoroutineJobDeleteMap j = new CoroutineJobDeleteMap();
            j.host = this;
            j.mapId = mapId;
            m_Jobs.Add(j);
        }

        public void RestoreMapImages(int mapId)
        {
            CoroutineJobRestoreMapImages j = new CoroutineJobRestoreMapImages();
            j.host = this;
            j.mapId = mapId;
            m_Jobs.Add(j);

            m_SessionFirstImage = true;
        }

        public void ResetMapperPictures(bool deleteAnchor)
        {
            CoroutineJobClear j = new CoroutineJobClear();
            j.host = this;
            j.anchor = deleteAnchor;
            m_Jobs.Add(j);

            m_SessionFirstImage = true;
        }

        public void Construct()
        {
            CoroutineJobConstruct j = new CoroutineJobConstruct();
            j.host = this;
            j.name = workspaceManager.newMapName.text;
            m_Jobs.Add(j);
        }

        public void NotifyIfConnected(icvCaptureInfo info)
        {
            Dispatch.Dispatch(() => {
                if (!m_SessionFirstImage)
                {
                    if (info.connected == 0)
                    {
                        this.onFailedToConnect?.Invoke();
                    }
                    else
                    {
                        this.onConnect?.Invoke();
                    }
                }
            });
        }

        public void Capture()
        {
            if (!m_bCaptureRunning)
            {
                var captureButton = workspaceManager.captureButton.GetComponent<Button>();
                captureButton.interactable = false;
                m_CameraShutterClick.Play();
                StartCoroutine(Capture(false));
            }
        }

        public void Anchor()
        {
            if (!m_bCaptureRunning)
            {
                m_CameraShutterClick.Play();
                StartCoroutine(Capture(true));
            }
        }

        public void LoadMap(int mapId)
        {
            if (pcr.ContainsKey(mapId))
            {
                CoroutineJobFreeMap jf = new CoroutineJobFreeMap();
                jf.host = this;
                jf.id = mapId;
                m_Jobs.Add(jf);
                return;
            }

            CoroutineJobLoadMap j = new CoroutineJobLoadMap();
            j.host = this;
            j.id = mapId;
            j.go = gameObject;
            m_Jobs.Add(j);
        }

        public void Jobs()
        {
            CoroutineJobListJobs j = new CoroutineJobListJobs();
            j.host = this;
            j.activeMaps = new List<int>();

            if (this.useGPS)
            {
                j.useGPS = true;
                j.latitude = m_Latitude;
                j.longitude = m_Longitude;
                j.radius = DefaultRadius;
            }

            foreach (int id in pcr.Keys)
            {
                j.activeMaps.Add(id);
            }

            m_Jobs.Add(j);
        }

        public void Logout()
        {
            OnLogOut?.Invoke();
        }
    }
}
