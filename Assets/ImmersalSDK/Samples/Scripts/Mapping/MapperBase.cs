/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Util;
using UnityEngine.Events;
using UnityEngine.Networking;
using TMPro;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace Immersal.Samples.Mapping
{
    public abstract class MapperBase : MonoBehaviour
    {
        protected const double DefaultRadius = 200.0;

        public UnityEvent onConnect = null;
        public UnityEvent onFailedToConnect = null;

        [HideInInspector]
        public MapperStats stats = new MapperStats();
        [HideInInspector]
        public Dictionary<int, PointCloudRenderer> pcr = new Dictionary<int, PointCloudRenderer>();
        [HideInInspector]
        public MappingUIManager mappingUIManager;
        [HideInInspector]
        public MapperSettings mapperSettings;
        [HideInInspector]
        public WorkspaceManager workspaceManager;
        [HideInInspector]
        public VisualizeManager visualizeManager;
        [HideInInspector]
        public LocalizerPose lastLocalizedPose = default;

        protected int m_ImageIndex = 0;
        protected uint m_ImageRun = 0;
        protected bool m_SessionFirstImage = true;
        protected bool m_IsTracking = false;
        protected List<Task> m_Jobs = new List<Task>();
        private int m_JobLock = 0;
        protected ImmersalSDK m_Sdk;
        protected double m_Latitude = 0.0;
        protected double m_Longitude = 0.0;
        protected double m_Altitude = 0.0;
        protected double m_Haccuracy = 0.0;
        protected double m_Vaccuracy = 0.0;
        protected double m_VLatitude = 0.0;
        protected double m_VLongitude = 0.0;
        protected double m_VAltitude = 0.0;
        protected float m_VBearing = 0f;
        protected bool m_bCaptureRunning = false;
		protected IntPtr m_PixelBuffer = IntPtr.Zero;

        private int m_Bank = 0;
        private AudioSource m_CameraShutterClick;
        private IEnumerator m_UpdateJobList;
        private Camera m_MainCamera = null;

        private static IDispatch Dispatch;

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

        public bool gpsOn
        {
            #if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
            get { return NativeBindings.LocationServicesEnabled(); }
            #else
            get { return Input.location.status == LocationServiceStatus.Running; }
            #endif
        }

        public int currentBank
        {
            get { return m_Bank; }
        }

        public string tempImagePath
        {
            get
            {
                return string.Format("{0}/Images", Application.persistentDataPath);
            }
        }

        #region Abstract methods

        protected abstract void Capture(bool anchor);
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

            DirectoryInfo dataDir = new DirectoryInfo(tempImagePath);
            if (dataDir.Exists)
            {
                dataDir.Delete(true);
            }

            Directory.CreateDirectory(tempImagePath);

            #if UNITY_IOS
            UnityEngine.iOS.Device.SetNoBackupFlag(tempImagePath);
            #endif
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
                RunJob(m_Jobs[0]);
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
            mappingUIManager = GetComponentInChildren<MappingUIManager>();
            mapperSettings = GetComponent<MapperSettings>();
            workspaceManager = mappingUIManager.workspaceManager;
            visualizeManager = mappingUIManager.visualizeManager;
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

            if (mapperSettings.downsampleWhenLocalizing)
            {
                Immersal.Core.SetInteger("LocalizationMaxPixels", 1280*720);
            }

            mappingUIManager.vLocationText.text = "No VGPS localizations";

            Invoke("StartGPS", 0.1f);
            StatusPoll();
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
            #if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
            NativeBindings.StopLocation();
            #else
            Input.location.Stop();
            #endif
            PlayerPrefs.SetInt("use_gps", 0);
            NotificationManager.Instance.GenerateNotification("Geolocation tracking stopped");
            mappingUIManager.locationText.text = "GPS not enabled";
        }

        private IEnumerator EnableLocationServices()
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                mappingUIManager.gpsToggle.SetIsOnWithoutNotify(false);
                mapperSettings.SetUseGPS(false);
                NotificationManager.Instance.GenerateNotification("Location services not enabled");
                Debug.Log("Location services not enabled");
                yield break;
            }

            // Start service before querying location
            #if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
            NativeBindings.StartLocation();
            #else
            Input.location.Start(0.001f, 0.001f);
            #endif

            // Wait until service initializes
            int maxWait = 20;
            #if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
            while (!NativeBindings.LocationServicesEnabled() && maxWait > 0)
            #else
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            #endif
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                mappingUIManager.gpsToggle.SetIsOnWithoutNotify(false);
                mapperSettings.SetUseGPS(false);
                NotificationManager.Instance.GenerateNotification("Location services timed out");
                Debug.Log("Timed out");
                yield break;
            }

            // Connection has failed
            #if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
            if (!NativeBindings.LocationServicesEnabled())
            #else
            if (Input.location.status == LocationServiceStatus.Failed)
            #endif
            {
                mappingUIManager.gpsToggle.SetIsOnWithoutNotify(false);
                mapperSettings.SetUseGPS(false);
                NotificationManager.Instance.GenerateNotification("Unable to determine device location");
                Debug.Log("Unable to determine device location");
                yield break;
            }

            #if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
            if (NativeBindings.LocationServicesEnabled())
            #else
            if (Input.location.status == LocationServiceStatus.Running)
            #endif
            {
                //PlayerPrefs.SetInt("use_gps", 1);
                mappingUIManager.gpsToggle.SetIsOnWithoutNotify(true);
                mapperSettings.SetUseGPS(true);
                NotificationManager.Instance.GenerateNotification("Tracking geolocation");
            }
        }

        public int SwitchBank(int max_banks)
        {
            m_Bank = (m_Bank + 1) % max_banks;
            m_SessionFirstImage = true;

            return m_Bank;
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

        async void StatusPoll()
        {
            JobStatusAsync j = new JobStatusAsync();
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKStatusResult result && result.error == "none")
                {
                    this.stats.imageCount = result.imageCount;
                }
            };

            await j.RunJobAsync();
            await Task.Delay(3000);

            if (Application.isPlaying)
            {
                StatusPoll();
            }
        }

        private async void RunJob(Task t)
        {
            await t;

            if (m_Jobs.Count > 0)
            {
                m_Jobs.RemoveAt(0);
            }
            m_JobLock = 0;
        }

        public MapperStats Stats()
        {
            return stats;
        }

        Matrix4x4 Rot(double angle, int axis)
        {
            float cang = (float)System.Math.Cos(angle * System.Math.PI / 180.0);
            float sang = (float)System.Math.Sin(angle * System.Math.PI / 180.0);

            Matrix4x4 R = Matrix4x4.identity;

            if (axis == 1)
            {
                R.m00 = 1;
                R.m01 = 0;
                R.m02 = 0;
                R.m10 = 0;
                R.m20 = 0;
                R.m11 = cang;
                R.m22 = cang;
                R.m12 = sang;
                R.m21 = -sang;
            }
            else if (axis == 2)
            {
                R.m01 = 0;
                R.m10 = 0;
                R.m11 = 1;
                R.m12 = 0;
                R.m21 = 0;
                R.m00 = cang;
                R.m22 = cang;
                R.m02 = -sang;
                R.m20 = sang;
            }
            else if (axis == 3)
            {
                R.m20 = 0;
                R.m21 = 0;
                R.m22 = 1;
                R.m02 = 0;
                R.m12 = 0;
                R.m00 = cang;
                R.m11 = cang;
                R.m10 = -sang;
                R.m01 = sang;
            }

            return R;
        }

        Matrix4x4 Rot3d(double reflat, double reflon)
        {
            Matrix4x4 R1 = Rot(90 + reflon, 3);
            Matrix4x4 R2 = Rot(90 - reflat, 1);
            return R2 * R1;
        }

        Vector2 CompassDir(Camera cam, Matrix4x4 trackerToMap, double[] mapToEcef)
        {
            Vector3 a = trackerToMap.MultiplyPoint(cam.transform.position);
            Vector3 b = trackerToMap.MultiplyPoint(cam.transform.position + cam.transform.forward);

            double[] aEcef = new double[3];
            int ra = Immersal.Core.PosMapToEcef(aEcef, a, lastLocalizedPose.mapToEcef);
            double[] bEcef = new double[3];
            int rb = Immersal.Core.PosMapToEcef(bEcef, b, lastLocalizedPose.mapToEcef);

            double[] wgs84 = new double[3];
            int rw = Immersal.Core.PosMapToWgs84(wgs84, a, mapToEcef);
            Matrix4x4 R = Rot3d(wgs84[0], wgs84[1]);

            Vector3 v = new Vector3((float)(bEcef[0] - aEcef[0]), (float)(bEcef[1] - aEcef[1]), (float)(bEcef[2] - aEcef[2]));
            Vector3 vt = R.MultiplyVector(v.normalized);

            Vector2 d = new Vector2(vt.x, vt.y);
            return d.normalized;
        }

        void UpdateLocation()
        {
            if (gpsOn)
            {
                #if (UNITY_IOS || PLATFORM_ANDROID) && !UNITY_EDITOR
                m_Latitude = NativeBindings.GetLatitude();
                m_Longitude = NativeBindings.GetLongitude();
                m_Altitude = NativeBindings.GetAltitude();
                m_Haccuracy = NativeBindings.GetHorizontalAccuracy();
                m_Vaccuracy = NativeBindings.GetVerticalAccuracy();
                #else
                m_Latitude = Input.location.lastData.latitude;
                m_Longitude = Input.location.lastData.longitude;
                m_Altitude = Input.location.lastData.altitude;
                m_Haccuracy = Input.location.lastData.horizontalAccuracy;
                m_Vaccuracy = Input.location.lastData.verticalAccuracy;
                #endif

                string txt = string.Format("Lat: {0}, Lon: {1}, Alt: {2}, HAcc: {3}, VAcc: {4}", 
                                m_Latitude.ToString("0.00000"), 
                                m_Longitude.ToString("0.00000"), 
                                m_Altitude.ToString("0.0"), 
                                m_Haccuracy.ToString("0.0"), 
                                m_Vaccuracy.ToString("0.0"));
                
                mappingUIManager.locationText.text = txt;
            }

            if (lastLocalizedPose.valid)
            {
                Vector2 cd = CompassDir(mainCamera, lastLocalizedPose.matrix, lastLocalizedPose.mapToEcef);
                float bearing = Mathf.Atan2(-cd.x, cd.y) * (180f / (float)Math.PI);
                if(bearing >= 0f)
                {
                    m_VBearing = bearing;
                }
                else
                {
                    m_VBearing = 360f - Mathf.Abs(bearing);
                }

                Matrix4x4 trackerSpace = Matrix4x4.TRS(mainCamera.transform.position, mainCamera.transform.rotation, Vector3.one);
                Matrix4x4 m = lastLocalizedPose.matrix * trackerSpace;
                Vector3 pos = m.GetColumn(3);

                double[] wgs84 = new double[3];
                int r = Immersal.Core.PosMapToWgs84(wgs84, pos, lastLocalizedPose.mapToEcef);
                m_VLatitude = wgs84[0];
                m_VLongitude = wgs84[1];
                m_VAltitude = wgs84[2];

                lastLocalizedPose.lastUpdatedPose.position = pos;
                lastLocalizedPose.lastUpdatedPose.rotation = m.rotation;
            }

            string txt2 = string.Format("VLat: {0}, VLon: {1}, VAlt: {2}, VBRG: {3}", 
                            m_VLatitude.ToString("0.000000"),
                            m_VLongitude.ToString("0.000000"),
                            m_VAltitude.ToString("0.0"),
                            m_VBearing.ToString("0.0"));
            
            mappingUIManager.vLocationText.text = txt2;
        }

        public void DeleteMap(int jobId)
        {
            JobDeleteMapAsync j = new JobDeleteMapAsync();
            j.id = jobId;
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKDeleteMapResult result && result.error == "none")
                {
                    Debug.Log(string.Format("Map {0} deleted successfully.", jobId));
                }
            };

            m_Jobs.Add(j.RunJobAsync());
        }

        public void RestoreMapImages(int jobId)
        {
            JobRestoreMapImagesAsync j = new JobRestoreMapImagesAsync();
            j.id = jobId;
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKRestoreMapImagesResult result && result.error == "none")
                {
                    Debug.Log(string.Format("Successfully restored images for map {0}", jobId));
                }
            };

            m_Jobs.Add(j.RunJobAsync());

            m_SessionFirstImage = true;
        }

        public void ResetMapperPictures(bool deleteAnchor)
        {
            JobClearAsync j = new JobClearAsync();
            j.anchor = deleteAnchor;
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKClearResult result && result.error == "none")
                {
                    Debug.Log("Workspace cleared successfully");
                }
            };

            m_Jobs.Add(j.RunJobAsync());

            m_SessionFirstImage = true;
        }

        public void Construct()
        {
            JobConstructAsync j = new JobConstructAsync();
            j.name = workspaceManager.newMapName.text;
            j.featureCount = mapperSettings.mapDetailLevel;
            j.preservePoses = mapperSettings.preservePoses;
            j.windowSize = mapperSettings.windowSize;
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKConstructResult result && result.error == "none")
                {
                    Debug.Log(string.Format("Started constructing a map width ID {0}, containing {1} images and detail level of {2}", result.id, result.size, j.featureCount));
                }
            };

            m_Jobs.Add(j.RunJobAsync());
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
                Capture(false);
            }
        }

        public void Anchor()
        {
            if (!m_bCaptureRunning)
            {
                m_CameraShutterClick.Play();
                Capture(true);
            }
        }

        public async void LoadMap(int jobId)
        {
            if (pcr.ContainsKey(jobId))
            {
                Task<int> t0 = Task.Run(() =>
                {
                    return Immersal.Core.FreeMap(pcr[jobId].mapHandle);
                });

                await t0;

                if (t0.Result == 1)
                {
                    PointCloudRenderer p = pcr[jobId];
                    p.ClearCloud();
                    pcr.Remove(jobId);
                }
                return;
            }

            JobLoadMapAsync j = new JobLoadMapAsync();
            j.id = jobId;

            j.OnStart += () =>
            {
                mappingUIManager.SetProgress(0);
                mappingUIManager.ShowProgressBar();
            };
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKMapResult result && result.error == "none")
                {
                    byte[] mapData = Convert.FromBase64String(result.b64);
                    Debug.Log(string.Format("Load map {0} ({1} bytes) ({2}/{3})", jobId, mapData.Length, CryptoUtil.SHA256(mapData), result.sha256_al));
                    CompleteMapLoad(jobId, mapData);
                }

                mappingUIManager.HideProgressBar();
            };
            j.Progress.ProgressChanged += (s, progress) =>
            {
                int value = (int)(100f * progress);
                mappingUIManager.SetProgress(value);
            };
            j.OnError += (HttpResponseMessage response) =>
            {
                mappingUIManager.HideProgressBar();
            };

            m_Jobs.Add(j.RunJobAsync());
        }

        async void CompleteMapLoad(int jobId, byte[] mapData)
        {
            Vector3[] vector3Array = new Vector3[ARMap.MAX_VERTICES];

            Task<int> t0 = Task.Run(() =>
            {
                return Immersal.Core.LoadMap(mapData);
            });

            await t0;

            int mapHandle = t0.Result;

            if (mapHandle >= 0)
            {
                Task<int> t1 = Task.Run(() =>
                {
                    return Immersal.Core.GetPointCloud(mapHandle, vector3Array);
                });

                await t1;

                int num = t1.Result;

                PointCloudRenderer renderer = gameObject.AddComponent<PointCloudRenderer>();
                renderer.CreateCloud(vector3Array, num);
                renderer.mapHandle = mapHandle;
                if (!pcr.ContainsKey(jobId)) {
                    pcr.Add(jobId, renderer);
                }
            }

            stats.locFail = 0;
            stats.locSucc = 0;

            VisualizeManager.loadJobs.Remove(jobId);
        }

        public void Jobs()
        {
            JobListJobsAsync j = new JobListJobsAsync();
            List<int> activeMaps = new List<int>();

            if (mapperSettings.listOnlyNearbyMaps)
            {
                j.useGPS = true;
                j.latitude = m_Latitude;
                j.longitude = m_Longitude;
                j.radius = DefaultRadius;
            }

            foreach (int id in pcr.Keys)
            {
                activeMaps.Add(id);
            }

            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKJobsResult result && result.error == "none")
                {
                    this.visualizeManager.SetMapListData(result.jobs, activeMaps);
                }
            };

            m_Jobs.Add(j.RunJobAsync());
        }

        public void Logout()
        {
            if (LoginManager.Instance != null)
                LoginManager.Instance.Logout();
        }
    }

    public class MapperStats
    {
        public int queueLen;
        public int imageCount;
        public int locFail;
        public int locSucc;
    }
}
