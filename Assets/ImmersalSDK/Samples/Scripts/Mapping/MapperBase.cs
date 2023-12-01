/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Util;
using UnityEngine.Events;
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
        public UnityEvent onImageLimitExceeded = null;

        [HideInInspector]
        public MappingUIManager mappingUIManager;
        [HideInInspector]
        public MapperSettings mapperSettings;
        [HideInInspector]
        public WorkspaceManager workspaceManager;
        [HideInInspector]
        public VisualizeManager visualizeManager;

        public MapperStats stats { get; protected set; } = new MapperStats();

        protected int m_ImageIndex = 0;
        protected uint m_ImageRun = 0;
        protected bool m_SessionFirstImage = true;
        protected bool m_IsTracking = false;
        protected List<JobAsync> m_Jobs = new List<JobAsync>();
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

        private AudioSource m_CameraShutterClick;
        private IEnumerator m_UpdateJobList;
        private Camera m_MainCamera = null;

        private static IDispatch Dispatch;

        private bool m_enableStatusPolling;

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

        public string tempImagePath
        {
            get
            {
                return string.Format("{0}/Images", Application.persistentDataPath);
            }
        }

        #region Abstract methods

        protected abstract void Capture(bool anchor);

        #endregion

        #region Virtual methods

        protected virtual void OnEnable()
        {
            stats.queueLen = 0;
            stats.imageCount = 0;

            DirectoryInfo dataDir = new DirectoryInfo(tempImagePath);
            if (dataDir.Exists)
            {
                dataDir.Delete(true);
            }

            Directory.CreateDirectory(tempImagePath);

            #if UNITY_IOS
            UnityEngine.iOS.Device.SetNoBackupFlag(tempImagePath);
            #endif

            m_enableStatusPolling = true;
            
            mappingUIManager.vLocationText.text = "No VGPS localizations";

            if (mapperSettings.useGps)
            {
                Invoke("StartGPS", 0.1f);
            }

            StatusPoll();
            Jobs();
        }

        protected virtual void OnDisable()
        {
            bool deleteToken = true;
            if (PlayerPrefs.HasKey("rememberMe"))
            {
                if(bool.Parse(PlayerPrefs.GetString("rememberMe")))
                {
                    Debug.Log("Remember me feature is on - skipping token deletion");
                    deleteToken = false;
                }
            }
            if (deleteToken)
            {
                PlayerPrefs.DeleteKey("token");
                m_Sdk.developerToken = null;    
            }
            
            m_enableStatusPolling = false;
        }

        protected virtual SDKMapId[] GetActiveMapIds()
        {
            int n = ARSpace.mapIdToMap.Count;
            SDKMapId[] mapIds = new SDKMapId[n];

            int count = 0;
            foreach (int id in ARSpace.mapIdToMap.Keys)
            {
                SDKMapId mapId;
                mapId.id = id;
                mapIds[count++] = mapId;
            }

            return mapIds;
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
                Immersal.Core.SetInteger("LocalizationMaxPixels", 960*720);
            }
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
            mapperSettings.SetUseGPS(false);
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
                mappingUIManager.gpsToggle.SetIsOnWithoutNotify(true);
                mapperSettings.SetUseGPS(true);
                NotificationManager.Instance.GenerateNotification("Tracking geolocation");
            }
        }

        public void ToggleVisualization(Toggle toggle)
        {
            ARMap.pointCloudVisible = toggle.isOn;
        }

        public void ToggleVisualization(bool active)
        {
            ARMap.pointCloudVisible = active;
        }

        public void ToggleRenderPointsAs3D(Toggle toggle)
        {
            ARMap.renderAs3dPoints = toggle.isOn;
        }

        public void ToggleRenderPointsAs3D(bool renderAs3D)
        {
            ARMap.renderAs3dPoints = renderAs3D;
        }

        public void SetPointSize(Slider slider)
        {
            ARMap.pointSize = Mathf.Max(0f, slider.value);
        }

        public void SetPointSize(float pointSize)
        {
            ARMap.pointSize = Mathf.Max(0f, pointSize);
        }

        private void OnItemSelected(SDKJob job)
        {
            LoadMap(job);
        }

        private void OnItemDeleted(SDKJob job)
        {
            DeleteMap(job.id);
        }

        private void OnItemRestored(SDKJob job, bool clear)
        {
            RestoreMapImages(job.id, clear);
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
            int polledUserLevel = 0;
            
            JobStatusAsync j = new JobStatusAsync();
            j.OnResult += (SDKStatusResult result) =>
            {
                this.stats.imageCount = result.imageCount;
                this.stats.imageMax = result.imageMax;
                polledUserLevel = result.level;
            };

            await j.RunJobAsync();
            mapperSettings.UpdateLevelRestriction(polledUserLevel);
            
            await Task.Delay(3000);

            if (Application.isPlaying && m_enableStatusPolling)
            {
                StatusPoll();
            }
        }

        private async void RunJob(JobAsync j)
        {
            await j.RunJobAsync();

            if (m_Jobs.Count > 0)
            {
                m_Jobs.RemoveAt(0);
            }
            m_JobLock = 0;
        }

        Matrix4x4 RotX(double angle)
        {
            float c = (float)System.Math.Cos(angle * System.Math.PI / 180.0);
            float s = (float)System.Math.Sin(angle * System.Math.PI / 180.0);

            Matrix4x4 r = Matrix4x4.identity;

            r.m11 = c;
            r.m22 = c;
            r.m12 = s;
            r.m21 = -s;

            return r;
        }

        Matrix4x4 RotZ(double angle)
        {
            float c = (float)System.Math.Cos(angle * System.Math.PI / 180.0);
            float s = (float)System.Math.Sin(angle * System.Math.PI / 180.0);

            Matrix4x4 r = Matrix4x4.identity;

            r.m00 = c;
            r.m11 = c;
            r.m10 = -s;
            r.m01 = s;

            return r;
        }

        Matrix4x4 Rot3d(double lat, double lon)
        {
            Matrix4x4 rz = RotZ(90 + lon);
            Matrix4x4 rx = RotX(90 - lat);
            return rx * rz;
        }

        Vector2 CompassDir(Camera cam, Matrix4x4 trackerToMap, double[] mapToEcef)
        {
            Vector3 a = trackerToMap.MultiplyPoint(cam.transform.position);
            Vector3 b = trackerToMap.MultiplyPoint(cam.transform.position + cam.transform.forward);

            double[] aEcef = new double[3];
            int ra = Immersal.Core.PosMapToEcef(aEcef, ARHelper.SwitchHandedness(a), mapToEcef);
            double[] bEcef = new double[3];
            int rb = Immersal.Core.PosMapToEcef(bEcef, ARHelper.SwitchHandedness(b), mapToEcef);

            double[] wgs84 = new double[3];
            int rw = Immersal.Core.PosMapToWgs84(wgs84, ARHelper.SwitchHandedness(a), mapToEcef);
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

            LocalizerPose localizerPose = m_Sdk.Localizer.lastLocalizedPose;

            if (localizerPose.valid)
            {
                Vector2 cd = CompassDir(mainCamera, localizerPose.matrix, localizerPose.mapToEcef);
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
                Matrix4x4 m = localizerPose.matrix * trackerSpace;
                Vector3 pos = m.GetColumn(3);

                double[] wgs84 = new double[3];
                int r = Immersal.Core.PosMapToWgs84(wgs84, ARHelper.SwitchHandedness(pos), localizerPose.mapToEcef);
                m_VLatitude = wgs84[0];
                m_VLongitude = wgs84[1];
                m_VAltitude = wgs84[2];

                localizerPose.lastUpdatedPose.position = pos;
                localizerPose.lastUpdatedPose.rotation = m.rotation;
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
            j.OnResult += (SDKDeleteMapResult result) =>
            {
                Debug.LogFormat("Map {0} deleted successfully.", jobId);
            };

            m_Jobs.Add(j);
        }

        public void RestoreMapImages(int jobId, bool clear)
        {
            JobRestoreMapImagesAsync j = new JobRestoreMapImagesAsync();
            j.id = jobId;
            j.clear = clear;
            j.OnResult += (SDKRestoreMapImagesResult result) =>
            {
                Debug.LogFormat("Successfully restored images for map {0}", jobId);
            };

            m_Jobs.Add(j);

            m_SessionFirstImage = true;
        }

        public void ResetMapperPictures(bool deleteAnchor)
        {
            JobClearAsync j = new JobClearAsync();
            j.anchor = deleteAnchor;
            j.OnResult += (SDKClearResult result) =>
            {
                Debug.Log("Workspace cleared successfully");
            };

            m_Jobs.Add(j);

            m_SessionFirstImage = true;
        }

        public void Construct()
        {
            JobConstructAsync j = new JobConstructAsync();
            j.name = workspaceManager.newMapName.text;
            j.featureCount = mapperSettings.mapDetailLevel;
            j.preservePoses = mapperSettings.preservePoses;
            j.windowSize = mapperSettings.windowSize;
            j.mapTrim = mapperSettings.mapTrim;
            j.featureFilter = mapperSettings.featureFilter;
            j.compressionLevel = mapperSettings.compressionLevel;
            j.OnResult += (SDKConstructResult result) =>
            {
                Debug.LogFormat("Started constructing a map width ID {0}, containing {1} images and detail level of {2}", result.id, result.size, j.featureCount);
            };

            m_Jobs.Add(j);
        }

        public void NotifyIfConnected(CaptureInfo info)
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

        public void ImageLimitExceeded()
        {
            Dispatch.Dispatch(() => {
                Debug.Log("Account image limit exceeded, aborting image capture");
                this.onImageLimitExceeded?.Invoke();
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

        public void LoadMap(SDKJob job)
        {
            if (ARSpace.mapIdToMap.ContainsKey(job.id))
            {
                ARMap arMap = ARSpace.mapIdToMap[job.id];
                arMap.FreeMap(true);
                return;
            }

            JobLoadMapBinaryAsync j = new JobLoadMapBinaryAsync();
            j.id = job.id;
            j.sha256_al = job.sha256_al;

            j.OnStart += () =>
            {
                mappingUIManager.SetProgress(0);
                mappingUIManager.ShowProgressBar();
            };
            j.OnResult += async (SDKMapResult result) =>
            {
                Debug.LogFormat("Load map {0} ({1} bytes) ({2}/{3})", job.id, result.mapData.Length, CryptoUtil.SHA256(result.mapData), result.sha256_al);
    			Color pointCloudColor = ARMap.pointCloudColors[UnityEngine.Random.Range(0, ARMap.pointCloudColors.Length)];

                Transform root = null;
                if (!mapperSettings.useDifferentARSpaces)
                {
                    ARSpace[] arSpaces = GameObject.FindObjectsOfType<ARSpace>();
                    foreach (ARSpace space in arSpaces)
                    {
                        if (space.gameObject.name == "ARSpaceForAll")
                        {
                            root = space.transform;
                        }
                    }
                }

                bool applyAlignment = !mapperSettings.useDifferentARSpaces;

                await ARSpace.LoadAndInstantiateARMap(root, result, ARMap.RenderMode.EditorAndRuntime, pointCloudColor, applyAlignment);
                //await ARSpace.LoadAndInstantiateARMap(root, job, result.mapData, ARMap.RenderMode.EditorAndRuntime, pointCloudColor, applyAlignment);

                m_Sdk.Localizer.stats.localizationAttemptCount = 0;
                m_Sdk.Localizer.stats.localizationSuccessCount = 0;

                VisualizeManager.loadJobs.Remove(job.id);
                mappingUIManager.HideProgressBar();
            };
            j.Progress.ProgressChanged += (s, progress) =>
            {
                int value = (int)(100f * progress);
                mappingUIManager.SetProgress(value);
            };
            j.OnError += (e) =>
            {
                Debug.LogError(e);
                mappingUIManager.HideProgressBar();
            };

            m_Jobs.Add(j);
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

            foreach (int id in ARSpace.mapIdToMap.Keys)
            {
                activeMaps.Add(id);
            }

            j.OnResult += (SDKJobsResult result) =>
            {
                List<SDKJob> jobList = new List<SDKJob>();
                foreach (SDKJob job in result.jobs)
                {
                    if (job.type != (int)SDKJobType.Alignment)
                    {
                        jobList.Add(job);
                    }
                }

                this.visualizeManager.SetMapListData(jobList.ToArray(), activeMaps);
            };

            m_Jobs.Add(j);
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
        public int imageMax;
    }
}
