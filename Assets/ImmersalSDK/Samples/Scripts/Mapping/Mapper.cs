/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Util;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Runtime.InteropServices;
using UnityEngine.Events;
using TMPro;

namespace Immersal.Samples.Mapping
{
    public class CoroutineJob
    {
        public IJobHost host;

        public virtual IEnumerator RunJob()
        {
            yield return null;
        }
    }

    public class CoroutineJobClear : CoroutineJob
    {
        public bool anchor;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobClear ***************************");

            SDKClearRequest r = new SDKClearRequest();
            r.token = host.token;
            r.bank = (host as Mapper).currentBank;
            r.anchor = this.anchor;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.CLEAR_JOB), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }
        }
    }

    public class CoroutineJobConstruct : CoroutineJob
    {
        public string name;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobConstruct ***************************");

            SDKConstructRequest r = new SDKConstructRequest();
            r.token = host.token;
            r.bank = (host as Mapper).currentBank;
            r.name = this.name;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.CONSTRUCT_MAP), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKConstructResult result = JsonUtility.FromJson<SDKConstructResult>(request.downloadHandler.text);
                    if (result.error == "none")
                    {
                        Debug.Log(string.Format("Started constructing a map width ID {0}, containing {1} images", result.id, result.size));
                    }
                }
            }
        }
    }

    public class CoroutineJobRestoreMapImages : CoroutineJob
    {
        public int mapId;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobRestoreMapImages ***************************");

            SDKRestoreMapImagesRequest r = new SDKRestoreMapImagesRequest();
            r.token = host.token;
            r.id = this.mapId;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.RESTORE_MAP_IMAGES), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }
        }
    }

    public class CoroutineJobDeleteMap : CoroutineJob
    {
        public int mapId;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobDeleteMap ***************************");

            SDKDeleteMapRequest r = new SDKDeleteMapRequest();
            r.token = host.token;
            r.id = this.mapId;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, host.server, Endpoint.DELETE_MAP), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }
        }
    }

    public class CoroutineJobStatus : CoroutineJob
    {
        public override IEnumerator RunJob()
        {
//            Debug.Log("*************************** CoroutineJobStatus ***************************");

            Mapper mapper = host as Mapper;
            SDKStatusRequest r = new SDKStatusRequest();
            r.token = mapper.token;
            r.bank = mapper.currentBank;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.STATUS), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKStatusResult result = JsonUtility.FromJson<SDKStatusResult>(request.downloadHandler.text);
                    mapper.stats.imageCount = result.imageCount;
                }
            }
        }
    }

    public class CoroutineJobCapture : CoroutineJob
    {
        public int run;
        public int index;
        public bool anchor;
        public Vector4 intrinsics;
        public Matrix4x4 rotation;
        public Vector3 position;
        public double latitude;
        public double longitude;
        public double altitude;
        public string encodedImage;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobCapture ***************************");
            Mapper mapper = host as Mapper;

            SDKImageRequest imageRequest = new SDKImageRequest();
            imageRequest.token = mapper.token;
            imageRequest.run = this.run;
            imageRequest.bank = mapper.currentBank;
            imageRequest.index = this.index;
            imageRequest.anchor = this.anchor;
            imageRequest.px = position.x;
            imageRequest.py = position.y;
            imageRequest.pz = position.z;
            imageRequest.r00 = rotation.m00;
            imageRequest.r01 = rotation.m01;
            imageRequest.r02 = rotation.m02;
            imageRequest.r10 = rotation.m10;
            imageRequest.r11 = rotation.m11;
            imageRequest.r12 = rotation.m12;
            imageRequest.r20 = rotation.m20;
            imageRequest.r21 = rotation.m21;
            imageRequest.r22 = rotation.m22;
            imageRequest.fx = intrinsics.x;
            imageRequest.fy = intrinsics.y;
            imageRequest.ox = intrinsics.z;
            imageRequest.oy = intrinsics.w;
            imageRequest.latitude = latitude;
            imageRequest.longitude = longitude;
            imageRequest.altitude = altitude;
            imageRequest.b64 = encodedImage;

            string jsonString = JsonUtility.ToJson(imageRequest);

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.CAPTURE_IMAGE), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }
        }
    }

    public class CoroutineJobLocalize : CoroutineJob
    {
        public Vector4 intrinsics;
        public Quaternion rotation;
        public Vector3 position;
        public byte[] pixels;
        public int width;
        public int height;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLocalize ***************************");

            Mapper mapper = host as Mapper;
            Vector3 pos = new Vector3();
            Quaternion rot = new Quaternion();

            Task<int> t = Task.Run(() =>
            {
                return Immersal.Core.LocalizeImage(out pos, out rot, width, height, ref intrinsics, pixels);
            });

            while (!t.IsCompleted)
            {
                yield return null;
            }

            int mapId = t.Result;

            if (mapId >= 0)
            {
                mapper.stats.locSucc++;

                Debug.Log("*************************** Localization Succeeded ***************************");
                Matrix4x4 cloudSpace = Matrix4x4.TRS(pos, rot, Vector3.one);
                Matrix4x4 trackerSpace = Matrix4x4.TRS(position, rotation, Vector3.one);
                Debug.Log("id " + mapId + "\n" +
                          "fc 4x4\n" + cloudSpace + "\n" +
                          "ft 4x4\n" + trackerSpace);

                Matrix4x4 m = trackerSpace*(cloudSpace.inverse);

                double[] mapToecef = new double[13];
                double[] wgs84 = new double[3];
                Immersal.Core.MapToEcefGet(mapToecef, mapId);
                int r = Immersal.Core.PosMapToWgs84(wgs84, pos, mapToecef);
                mapper.vlatitude = wgs84[0];
                mapper.vlongitude = wgs84[1];
                mapper.valtitude = wgs84[2];

                if (r == 0)
                    mapper.lastLocalizedPose = (true, mapToecef, m.inverse, new Pose(pos, rot));

                foreach (PointCloudRenderer p in mapper.pcr.Values)
                {
                    if (p.mapId == mapId)
                    {
                        p.go.transform.position = m.GetColumn(3);
                        p.go.transform.rotation = m.rotation;
                        break;
                    }
                }
            }
            else
            {
                mapper.stats.locFail++;
                Debug.Log("*************************** Localization Failed ***************************");
            }
        }
    }

    public class CoroutineJobLocalizeServer : CoroutineJob
    {
        public Vector4 intrinsics;
        public Quaternion rotation;
        public Vector3 position;
        public byte[] pixels;
        public int width;
        public int height;
        public int channels;
        public double latitude = 0.0;
        public double longitude = 0.0;
        public double radius = 0.0;
        public bool useGPS = false;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLocalize On-Server ***************************");

            Mapper mapper = host as Mapper;
            byte[] capture = new byte[channels * width * height + 1024];
            Task<(string, icvCaptureInfo)> t = Task.Run(() =>
            {
                icvCaptureInfo info = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                return (Convert.ToBase64String(capture, 0, info.captureSize), info);
            });

            while (!t.IsCompleted)
            {
                yield return null;
            }

            string encodedImage = t.Result.Item1;
            icvCaptureInfo captureInfo = t.Result.Item2;

            SDKLocalizeRequest imageRequest = this.useGPS ? new SDKGeoLocalizeRequest() : new SDKLocalizeRequest();
            imageRequest.token = mapper.token;
            imageRequest.fx = intrinsics.x;
            imageRequest.fy = intrinsics.y;
            imageRequest.ox = intrinsics.z;
            imageRequest.oy = intrinsics.w;
            imageRequest.b64 = encodedImage;

            if (this.useGPS)
            {
                SDKGeoLocalizeRequest gr = imageRequest as SDKGeoLocalizeRequest;
                gr.latitude = this.latitude;
                gr.longitude = this.longitude;
                gr.radius = this.radius;
            }
            else
            {
                int n = mapper.pcr.Count;

                imageRequest.mapIds = new SDKMapId[n];

                int count = 0;
                foreach (int id in mapper.pcr.Keys)
                {
                    imageRequest.mapIds[count] = new SDKMapId();
                    imageRequest.mapIds[count++].id = id;
                }
            }

            string jsonString = JsonUtility.ToJson(imageRequest);
            string endpoint = this.useGPS ? Endpoint.SERVER_GEOLOCALIZE : Endpoint.SERVER_LOCALIZE;

            SDKLocalizeResult locResult = new SDKLocalizeResult();
            locResult.success = false;
            Matrix4x4 m = new Matrix4x4();
            Matrix4x4 cloudSpace = new Matrix4x4();

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, endpoint), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    locResult = JsonUtility.FromJson<SDKLocalizeResult>(request.downloadHandler.text);

                    if (locResult.success)
                    {
                        cloudSpace = Matrix4x4.identity;
                        cloudSpace.m00 = locResult.r00; cloudSpace.m01 = locResult.r01; cloudSpace.m02 = locResult.r02; cloudSpace.m03 = locResult.px;
                        cloudSpace.m10 = locResult.r10; cloudSpace.m11 = locResult.r11; cloudSpace.m12 = locResult.r12; cloudSpace.m13 = locResult.py;
                        cloudSpace.m20 = locResult.r20; cloudSpace.m21 = locResult.r21; cloudSpace.m22 = locResult.r22; cloudSpace.m23 = locResult.pz;
                        Matrix4x4 trackerSpace = Matrix4x4.TRS(position, rotation, Vector3.one);
                        mapper.stats.locSucc++;

                        Debug.Log("*************************** On-Server Localization Succeeded ***************************");
                        Debug.Log("fc 4x4\n" + cloudSpace + "\n" +
                                  "ft 4x4\n" + trackerSpace);

                        m = trackerSpace * (cloudSpace.inverse);

                        foreach (KeyValuePair<int, PointCloudRenderer> p in mapper.pcr)
                        {
                            if (p.Key == locResult.map)
                            {
                                p.Value.go.transform.position = m.GetColumn(3);
                                p.Value.go.transform.rotation = m.rotation;
                                break;
                            }
                        }

                        Debug.Log(locResult.error);
                    }
                    else
                    {
                        mapper.stats.locFail++;
                        Debug.Log("*************************** On-Server Localization Failed ***************************");
                    }
                }
            }

            if (locResult.success)
            {
                SDKEcefRequest ecefRequest = new SDKEcefRequest();
                ecefRequest.token = mapper.token;
                ecefRequest.id = locResult.map;

                using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.SERVER_ECEF), JsonUtility.ToJson(ecefRequest)))
                {
                    request.method = UnityWebRequest.kHttpVerbPOST;
                    request.useHttpContinue = false;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Accept", "application/json");
                    yield return request.SendWebRequest();

                    if (request.isNetworkError || request.isHttpError)
                    {
                        Debug.LogError(request.error);
                    }
                    else if (request.responseCode == (long)HttpStatusCode.OK)
                    {
                        SDKEcefResult result = JsonUtility.FromJson<SDKEcefResult>(request.downloadHandler.text);

                        Debug.Log(request.downloadHandler.text);
                                                
                        double[] wgs84 = new double[3];
                        Vector3 pos = cloudSpace.GetColumn(3);
                        Quaternion rot = cloudSpace.rotation;
                        int r = Immersal.Core.PosMapToWgs84(wgs84, pos, result.ecef);
                        mapper.vlatitude = wgs84[0];
                        mapper.vlongitude = wgs84[1];
                        mapper.valtitude = wgs84[2];

                        if (r == 0)
                            mapper.lastLocalizedPose = (true, result.ecef, m.inverse, new Pose(pos, rot));
                    }
                }
            }
        }
    }

    public class CoroutineJobListJobs : CoroutineJob
    {
        public double latitude = 0.0;
        public double longitude = 0.0;
        public double radius = 0.0;
        public bool useGPS = false;
        public List<int> activeMaps;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobListJobs ***************************");

            Mapper mapper = host as Mapper;
            SDKJobsRequest r = this.useGPS ? new SDKGeoJobsRequest() : new SDKJobsRequest();
            r.token = mapper.token;
            r.bank = mapper.currentBank;

            if (this.useGPS)
            {
                SDKGeoJobsRequest gr = r as SDKGeoJobsRequest;
                gr.latitude = this.latitude;
                gr.longitude = this.longitude;
                gr.radius = this.radius;
            }

            string jsonString = JsonUtility.ToJson(r);
            string endpoint = this.useGPS ? Endpoint.LIST_GEOJOBS : Endpoint.LIST_JOBS;

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, endpoint), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKJobsResult result = JsonUtility.FromJson<SDKJobsResult>(request.downloadHandler.text);
                    if (result.error == "none")
                    {
                        mapper.visualizeManager.SetSelectSlotData(result.jobs, activeMaps);
                    }
                }
            }
        }
    }

    public class CoroutineJobLoadMap : CoroutineJob
    {
        public int id;
        public GameObject go;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLoadMap ***************************");

            Mapper mapper = host as Mapper;
            SDKMapRequest r = new SDKMapRequest();
            r.token = mapper.token;
            r.id = this.id;

            string jsonString2 = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, mapper.server, Endpoint.LOAD_MAP), jsonString2))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.useHttpContinue = false;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                //Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKMapResult result = JsonUtility.FromJson<SDKMapResult>(request.downloadHandler.text);
                    if (result.error == "none")
                    {
                        byte[] mapData = Convert.FromBase64String(result.b64);
                        Debug.Log("Load map " + this.id + " (" + mapData.Length + " bytes)");

                        uint countMax = 16*1024;
                        Vector3[] vector3Array = new Vector3[countMax];

                        Task<int> t0 = Task.Run(() =>
                        {
                            return Immersal.Core.LoadMap(mapData);
                        });
                         
                        while (!t0.IsCompleted)
                        {
                            yield return null;
                        }

                        int mapId = t0.Result;

                        Debug.Log("mapId " + mapId);

                        Task<int> t1 = Task.Run(() =>
                        {
                            return Immersal.Core.GetPointCloud(mapId, vector3Array);
                        });

                        while (!t1.IsCompleted)
                        {
                            yield return null;
                        }

                        int num = t1.Result;

                        Debug.Log("map points: " + num);

                        PointCloudRenderer renderer = go.AddComponent<PointCloudRenderer>();
                        renderer.CreateCloud(vector3Array, num);
                        renderer.mapId = mapId;
                        if (!mapper.pcr.ContainsKey(id)) {
                            mapper.pcr.Add(id, renderer);
                        }

                        mapper.stats.locFail = 0;
                        mapper.stats.locSucc = 0;
                    }
                }
            }
        }
    }

    public class CoroutineJobFreeMap : CoroutineJob
    {
        public int id;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobFreeMap ***************************");

            Mapper mapper = host as Mapper;

            if (mapper.pcr.ContainsKey(id))
            {
                int mapId = mapper.pcr[id].mapId;

                Task<int> t0 = Task.Run(() =>
                {
                    return Immersal.Core.FreeMap(mapId);
                });

                while (!t0.IsCompleted)
                {
                    yield return null;
                }

                PointCloudRenderer p = mapper.pcr[id];
                p.ClearCloud();
                mapper.pcr.Remove(id);
            }
        }
    }

    public class MapperStats
    {
        public int queueLen;
        public int imageCount;
        public int locFail;
        public int locSucc;
    }

    public class Mapper : MonoBehaviour, IJobHost
    {
        private const double DefaultRadius = 200.0;

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

        private string m_Server = null;
        private string m_Token = null;
        private int m_Bank = 0;
        private bool m_RgbCapture = false;
        private int m_ImageIndex = 0;
        private uint m_ImageRun = 0;
        private bool m_SessionFirstImage = true;
        private List<CoroutineJob> m_Jobs = new List<CoroutineJob>();
        private int m_JobLock = 0;
        private ImmersalSDK m_Sdk;
        private AudioSource m_CameraShutterClick;
        private IEnumerator m_UpdateJobList;
        private Camera m_MainCamera = null;
        private double m_Latitude = 0.0;
        private double m_Longitude = 0.0;
        private double m_Altitude = 0.0;
        private double m_Haccuracy = 0.0;
        private double m_Vaccuracy = 0.0;
        private bool m_bCaptureRunning = false;
        
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
                    m_Server = sdk.localizationServer;
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

        public ImmersalSDK sdk
        {
            get { return m_Sdk; }
        }

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

        private void ImageRunUpdate()
        {
            long bin = System.DateTime.Now.ToBinary();
            uint data = (uint)bin ^ (uint)(bin >> 32);
            m_ImageRun = (m_ImageRun ^ data) * 16777619;
        }

        private void SessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            if (sdk.arSession == null)
                return;
            
            ImageRunUpdate();

            bool isTracking = (args.state == ARSessionState.SessionTracking && sdk.arSession.subsystem.trackingState != TrackingState.None);

            var captureButton = workspaceManager.captureButton.GetComponent<Button>();
            var localizeButton = visualizeManager.localizeButton.GetComponent<Button>();
            captureButton.interactable = isTracking;
            localizeButton.interactable = isTracking;
        }

        void Awake()
        {
            m_Sdk = ImmersalSDK.Instance;
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
            if ((PlayerPrefs.GetInt("use_gps", 0) == 1))
            {
                m_GpsToggle.isOn = true;
            }
        }

        public void StartGPS()
        {
            StartCoroutine(EnableLocationServices());
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

        void OnEnable()
        {
#if !UNITY_EDITOR
            ARSession.stateChanged += SessionStateChanged;
#endif

            stats.queueLen = 0;
            stats.imageCount = 0;
            stats.locFail = 0;
            stats.locSucc = 0;

            StartCoroutine(StatusPoll());
            Jobs();
        }

        void OnDisable()
        {
#if !UNITY_EDITOR
            ARSession.stateChanged -= SessionStateChanged;
#endif

            PlayerPrefs.DeleteKey("token");
            sdk.developerToken = null;
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

        public string GetVGPSData()
        {
            string data = null;

            if (this.useGPS && lastLocalizedPose.valid)
            {
                Vector3 vgpsCloudPos = lastLocalizedPose.LastUpdatedPose.position;
                double[] ecef = new double[13];
                double[] wgs84 = new double[3] { m_Latitude, m_Longitude, m_Altitude };
                int r = Immersal.Core.PosWgs84ToEcef(ecef, wgs84);
                Vector3 gpsCloudPos = Vector3.zero;
                int r2 = Immersal.Core.PosEcefToMap(out gpsCloudPos, ecef, lastLocalizedPose.mapToEcef);
                data = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", vgpsCloudPos.x.ToString("0.00000"), vgpsCloudPos.y.ToString("0.00000"), vgpsCloudPos.z.ToString("0.00000"),
                    gpsCloudPos.x.ToString("0.00000"), gpsCloudPos.y.ToString("0.00000"), gpsCloudPos.z.ToString("0.00000"), wgs84[0].ToString("0.00000"), wgs84[1].ToString("0.00000"), wgs84[2].ToString("0.00000"));
            }

            return data;
        }

        void Update()
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

        public void ResetMapperPictures()
        {
            CoroutineJobClear j = new CoroutineJobClear();
            j.host = this;
            j.anchor = false;
            m_Jobs.Add(j);

            m_SessionFirstImage = true;
        }

        public void ResetMapperAll()
        {
            CoroutineJobClear j = new CoroutineJobClear();
            j.host = this;
            j.anchor = true;
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

        private IEnumerator Capture(bool anchor)
        {
            m_bCaptureRunning = true;
//            yield return new WaitForSeconds(0.25f);

            XRCameraImage image;
            ARCameraManager cameraManager = sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image))
            {
                CoroutineJobCapture j = new CoroutineJobCapture();
                j.host = this;
                j.run = (int)(m_ImageRun & 0xEFFFFFFF);
                j.index = m_ImageIndex++;
                j.anchor = anchor;

                Camera cam = this.mainCamera;
                Quaternion _q = cam.transform.rotation;
                Matrix4x4 r = Matrix4x4.Rotate(new Quaternion(_q.x, _q.y, -_q.z, -_q.w));
                Vector3 _p = cam.transform.position;
                Vector3 p = new Vector3(_p.x, _p.y, -_p.z);
                j.rotation = r;
                j.position = p;
                j.intrinsics = ARHelper.GetIntrinsics(cameraManager);

                byte[] pixels;
                int channels = 0;

                if (m_RgbCapture)
                {
                    ARHelper.GetPlaneDataRGB(out pixels, image);
                    channels = 3;
                }
                else
                {
                    ARHelper.GetPlaneData(out pixels, image);
                    channels = 1;
                }

                byte[] capture = new byte[channels * image.width * image.height + 1024];

                Task<(string, icvCaptureInfo)> t = Task.Run(() =>
                {
                    icvCaptureInfo info = Core.CaptureImage(capture, capture.Length, pixels, image.width, image.height, channels);
                    return (Convert.ToBase64String(capture, 0, info.captureSize), info);
                });

                while (!t.IsCompleted)
                {
                    yield return null;
                }

                j.encodedImage = t.Result.Item1;
                NotifyIfConnected(t.Result.Item2);

                if (m_SessionFirstImage)
                    m_SessionFirstImage = false;

                if (useGPS)
                {
                    j.latitude = m_Latitude;
                    j.longitude = m_Longitude;
                    j.altitude = m_Altitude;
                }
                else
                {
                    j.latitude = j.longitude = j.altitude = 0.0;
                }

                m_Jobs.Add(j);
                image.Dispose();
            }

            m_bCaptureRunning = false;
            var captureButton = workspaceManager.captureButton.GetComponent<Button>();
            captureButton.interactable = true;
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

        public void Localize()
        {
            XRCameraImage image;
            ARCameraManager cameraManager = sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image))
            {
                CoroutineJobLocalize j = new CoroutineJobLocalize();
                Camera cam = this.mainCamera;
                j.rotation = cam.transform.rotation;
                j.position = cam.transform.position;
                j.intrinsics = ARHelper.GetIntrinsics(cameraManager);
                j.width = image.width;
                j.height = image.height;
                j.host = this;

                ARHelper.GetPlaneData(out j.pixels, image);
                m_Jobs.Add(j);
                image.Dispose();
            }
        }

        public void LocalizeServer()
        {
            bool rgb = false;   // enable for localization with RGB24 images

            ARCameraManager cameraManager = sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            XRCameraImage image;
            if (cameraSubsystem.TryGetLatestImage(out image))
            {
                CoroutineJobLocalizeServer j = new CoroutineJobLocalizeServer();
                j.host = this;

                if (this.useGPS)
                {
                    j.useGPS = true;
                    j.latitude = m_Latitude;
                    j.longitude = m_Longitude;
                    j.radius = DefaultRadius;
                }

                Camera cam = this.mainCamera;
                j.rotation = cam.transform.rotation;
                j.position = cam.transform.position;
                j.intrinsics = ARHelper.GetIntrinsics(cameraManager);
                j.width = image.width;
                j.height = image.height;

                if (rgb)
                {
                    ARHelper.GetPlaneDataRGB(out j.pixels, image);
                    j.channels = 3;
                }
                else
                {
                    ARHelper.GetPlaneData(out j.pixels, image);
                    j.channels = 1;
                }

                m_Jobs.Add(j);
                image.Dispose();
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
