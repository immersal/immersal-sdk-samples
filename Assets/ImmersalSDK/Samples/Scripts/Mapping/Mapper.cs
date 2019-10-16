/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.2.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
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

namespace Immersal.Samples.Mapping
{
	public class CoroutineJob
	{
		public string server;
		public string token;

		public virtual IEnumerator RunJob()
		{
			yield return null;
		}
	}

	public class CoroutineJobClear : CoroutineJob
	{
		public int bank;
		public bool anchor;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobClear ***************************");

			SDKClearRequest r = new SDKClearRequest();
			r.token = this.token;
			r.bank = this.bank;
			r.anchor = this.anchor;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, server, Endpoint.CLEAR_JOB), jsonString))
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
		public int bank;
		public string name;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobConstruct ***************************");

			SDKConstructRequest r = new SDKConstructRequest();
			r.token = this.token;
			r.bank = this.bank;
			r.name = this.name;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.CONSTRUCT_MAP), jsonString))
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

	public class CoroutineJobRestoreMapImages : CoroutineJob
	{
		public int mapId;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobRestoreMapImages ***************************");

			SDKRestoreMapImagesRequest r = new SDKRestoreMapImagesRequest();
			r.token = this.token;
			r.id = this.mapId;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.RESTORE_MAP_IMAGES), jsonString))
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
			r.token = this.token;
			r.id = this.mapId;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.DELETE_MAP), jsonString))
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
		public int bank;
		public MapperStats stats;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobStatus ***************************");

			SDKStatusRequest r = new SDKStatusRequest();
			r.token = this.token;
			r.bank = this.bank;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.STATUS), jsonString))
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
					stats.imageCount = result.imageCount;
				}
			}
		}
	}

	public class CoroutineJobCapture : CoroutineJob
	{
        public UnityEvent onConnect;
        public UnityEvent onFailedToConnect;
		public int bank;
		public int run;
		public int index;
		public bool anchor;
		public Vector4 intrinsics;
		public Matrix4x4 rotation;
		public Vector3 position;
		public byte[] pixels;

		public int width;
		public int height;
		public int channels;

        public bool sessionFirstImage;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobCapture ***************************");

            byte[] capture = new byte[channels * width * height + 1024];
            Task<(string, icvCaptureInfo)> t = Task.Run(() =>
            {
                icvCaptureInfo info = Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                return (Convert.ToBase64String(capture, 0, info.captureSize), info);
			});

			while (!t.IsCompleted)
			{
				yield return null;
			}

            string encodedImage = t.Result.Item1;
            icvCaptureInfo captureInfo = t.Result.Item2;

            if (!sessionFirstImage)
            {
                if (captureInfo.connected == 0)
                {
                    onFailedToConnect.Invoke();
                }
                else
                {
                    onConnect.Invoke();
                }
            }

            SDKImageRequest imageRequest = new SDKImageRequest();
			imageRequest.token = this.token;
			imageRequest.run = this.run;
			imageRequest.bank = this.bank;
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
			imageRequest.b64 = encodedImage;

			string jsonString = JsonUtility.ToJson(imageRequest);

			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.CAPTURE_IMAGE), jsonString))
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
		public MapperStats stats;
		public Transform transform;
        public Dictionary<int, PointCloudRenderer> pcr;

        public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobLocalize ***************************");

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

			int mapHandle = t.Result;

			if (mapHandle >= 0)
			{
				stats.locSucc++;

				Debug.Log("*************************** Localization Succeeded ***************************");
				Matrix4x4 cloudSpace = Matrix4x4.TRS(pos, rot, Vector3.one);
				Matrix4x4 trackerSpace = Matrix4x4.TRS(position, rotation, Vector3.one);
				Debug.Log("fc 4x4\n" + cloudSpace + "\n" +
						  "ft 4x4\n" + trackerSpace);

                Matrix4x4 m = trackerSpace*(cloudSpace.inverse);

                foreach (PointCloudRenderer p in pcr.Values)
                {
                    if (p.handle == mapHandle)
                    {
                        p.go.transform.position = m.GetColumn(3);
                        p.go.transform.rotation = m.rotation;
                        break;
                    }
                }
			}
			else
			{
				stats.locFail++;
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

        public Dictionary<int, PointCloudRenderer> pcr;
        public MapperStats stats;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobLocalize On-Server ***************************");

            byte[] capture = new byte[width * height + 1024];
            Task<(string, icvCaptureInfo)> t = Task.Run(() =>
            {
                icvCaptureInfo info = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, 1);
                return (Convert.ToBase64String(capture, 0, info.captureSize), info);
            });

            while (!t.IsCompleted)
            {
                yield return null;
            }

            string encodedImage = t.Result.Item1;
            icvCaptureInfo captureInfo = t.Result.Item2;

            SDKLocalizeRequest imageRequest = new SDKLocalizeRequest();
            imageRequest.token = this.token;
            imageRequest.fx = intrinsics.x;
            imageRequest.fy = intrinsics.y;
            imageRequest.ox = intrinsics.z;
            imageRequest.oy = intrinsics.w;
            imageRequest.b64 = encodedImage;

            int n = pcr.Count;

            imageRequest.mapIds = new SDKMapId[n];

            int count = 0;
            foreach (int id in pcr.Keys)
            {
                imageRequest.mapIds[count] = new SDKMapId();
                imageRequest.mapIds[count++].id = id;
            }

            string jsonString = JsonUtility.ToJson(imageRequest);

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.SERVER_LOCALIZE), jsonString))
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
                    SDKLocalizeResult result = JsonUtility.FromJson<SDKLocalizeResult>(request.downloadHandler.text);

                    if (result.success)
                    {
                        Matrix4x4 cloudSpace = Matrix4x4.identity;
                        cloudSpace.m00 = result.r00; cloudSpace.m01 = result.r01; cloudSpace.m02 = result.r02; cloudSpace.m03 = result.px;
                        cloudSpace.m10 = result.r10; cloudSpace.m11 = result.r11; cloudSpace.m12 = result.r12; cloudSpace.m13 = result.py;
                        cloudSpace.m20 = result.r20; cloudSpace.m21 = result.r21; cloudSpace.m22 = result.r22; cloudSpace.m23 = result.pz;
                        Matrix4x4 trackerSpace = Matrix4x4.TRS(position, rotation, Vector3.one);
                        stats.locSucc++;

                        Debug.Log("*************************** On-Server Localization Succeeded ***************************");
                        Debug.Log("fc 4x4\n" + cloudSpace + "\n" +
                                  "ft 4x4\n" + trackerSpace);

                        Matrix4x4 m = trackerSpace * (cloudSpace.inverse);


                        foreach (KeyValuePair<int, PointCloudRenderer> p in pcr)
                        {
                            if (p.Key == result.map)
                            {
                                p.Value.go.transform.position = m.GetColumn(3);
                                p.Value.go.transform.rotation = m.rotation;
                                break;
                            }
                        }

                        Debug.Log(result.error);
                    }
                    else
                    {
                        stats.locFail++;
                        Debug.Log("*************************** On-Server Localization Failed ***************************");
                    }
                }
            }
        }
    }

    public class CoroutineJobListJobs : CoroutineJob
	{
		public int bank;
		public VisualizeManager visualizeManager;
		public List<int> activeMaps;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobListJobs ***************************");

			SDKJobsRequest r = new SDKJobsRequest();
			r.token = this.token;
			r.bank = this.bank;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.LIST_JOBS), jsonString))
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

					if (result.error == "none" && result.count > 0)
					{
						this.visualizeManager.SetSelectSlotData(result.jobs, activeMaps);
					}
				}
			}
		}
	}

	public class CoroutineJobLoadMap : CoroutineJob
	{
		public int bank;
		public int id;
		public MapperStats stats;
        public GameObject go;
        public Dictionary<int, PointCloudRenderer> pcr;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobLoadMap ***************************");
			SDKMapRequest r = new SDKMapRequest();
			r.token = this.token;
			r.id = this.id;

			string jsonString2 = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.LOAD_MAP), jsonString2))
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

                        int handle = t0.Result;

                        Debug.Log("handle " + handle);

                        Task<int> t1 = Task.Run(() =>
                        {
                            return Immersal.Core.GetPointCloud(handle, vector3Array);
                        });

                        while (!t1.IsCompleted)
                        {
                            yield return null;
                        }

                        int num = t1.Result;

						Debug.Log("map points: " + num);

						PointCloudRenderer renderer = go.AddComponent<PointCloudRenderer>();
						renderer.CreateCloud(vector3Array, num);
						renderer.handle = handle;
						if (!pcr.ContainsKey(id)) {
							pcr.Add(id, renderer);
						}

                        stats.locFail = 0;
						stats.locSucc = 0;
					}
				}
			}

		}
	}

    public class CoroutineJobFreeMap : CoroutineJob
    {
        public int id;
        public Dictionary<int, PointCloudRenderer> pcr;

        public override IEnumerator RunJob()
        {
            Debug.Log("*************************** CoroutineJobFreeMap ***************************");

            if (pcr.ContainsKey(id))
            {
                int handle = pcr[id].handle;

                Task<int> t0 = Task.Run(() =>
                {
                    return Immersal.Core.FreeMap(handle);
                });

                while (!t0.IsCompleted)
                {
                    yield return null;
                }

                PointCloudRenderer p = pcr[id];
                p.ClearCloud();
                pcr.Remove(id);
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

	public class Mapper : MonoBehaviour
	{
        public UnityEvent onConnect;
        public UnityEvent onFailedToConnect;
		public MapperStats stats = new MapperStats();
		private bool rgbCapture = false;
		private int bank = 0;
		private int imageIndex = 0;
		private uint imageRun = 0;
		private string token = "";
		private string server = "";
		private List<CoroutineJob> jobs = new List<CoroutineJob>();
		private int jobLock = 0;
		private ImmersalARCloudSDK sdk;
		private WorkspaceManager m_workspaceManager;
        private VisualizeManager m_visualizeManager;
        private AudioSource m_cameraShutterClick;
        private Dictionary<int, PointCloudRenderer> pcr = new Dictionary<int, PointCloudRenderer>();
        private bool sessionFirstImage = true;

        private IEnumerator m_updateJobList;

		public event LoggedOut OnLogOut;
		public delegate void LoggedOut();

		private void ImageRunUpdate()
		{
			long bin = System.DateTime.Now.ToBinary();
			uint data = (uint)bin ^ (uint)(bin >> 32);
			imageRun = (imageRun ^ data) * 16777619;
		}

		private void SessionStateChanged(ARSessionStateChangedEventArgs args)
		{
			if (sdk.arSession == null)
				return;
			
			ImageRunUpdate();

			bool isTracking = (args.state == ARSessionState.SessionTracking && sdk.arSession.subsystem.trackingState != TrackingState.None);

			var captureButton = m_workspaceManager.captureButton.GetComponent<Button>();
			var localizeButton = m_visualizeManager.localizeButton.GetComponent<Button>();
			captureButton.interactable = isTracking;
			localizeButton.interactable = isTracking;
		}

		void Awake()
		{
			sdk = ImmersalARCloudSDK.Instance;
            m_cameraShutterClick = GetComponent<AudioSource>();
			m_workspaceManager = GetComponentInChildren<WorkspaceManager>();
            m_visualizeManager = GetComponentInChildren<VisualizeManager>();
            m_visualizeManager.OnItemSelected += OnItemSelected;
			m_visualizeManager.OnItemDeleted += OnItemDeleted;
			m_visualizeManager.OnItemRestored += OnItemRestored;
            m_visualizeManager.OnSelectorOpened += OnSelectorOpened;
            m_visualizeManager.OnSelectorClosed += OnSelectorClosed;
            ImageRunUpdate();
		}

		void Start()
		{

		}

        public int SwitchBank(int max_banks)
        {
            bank = (bank + 1) % max_banks;
            sessionFirstImage = true;

            return bank;
        }

        public int GetCurrentBank()
        {
            return bank;
        }

		void OnEnable()
		{
#if !UNITY_EDITOR
			ARSession.stateChanged += SessionStateChanged;
#endif

			SetServer();
			SetToken();

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
            rgbCapture = toggle.isOn;
        }

        public void ToggleVisualization(Toggle toggle)
		{
			PointCloudRenderer.visible = toggle.isOn;
		}

        public void ToggleVisualization(bool active)
        {
            PointCloudRenderer.visible = active;
        }

        public void SetToken(string aToken = null)
		{
			this.token = (aToken != null) ? aToken: PlayerPrefs.GetString("token");

			if (this.token == null)
			{
				Debug.LogError("No valid developer token. Contact sdk@immersal.com.");
			}
		}

		public void SetServer(string aServer = null)
		{
            this.server = (aServer != null) ? aServer : sdk.localizationServer;
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
			if (m_updateJobList != null)
			{
				StopCoroutine(m_updateJobList);
			}

			m_updateJobList = UpdateJobList();
			StartCoroutine(m_updateJobList);
		}

		private void OnSelectorClosed()
		{
			if (m_updateJobList != null)
			{
				StopCoroutine(m_updateJobList);
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
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.stats = this.stats;

			yield return StartCoroutine(j.RunJob());
			yield return new WaitForSeconds(3);
			StartCoroutine(StatusPoll());
		}

		private IEnumerator RunJob(CoroutineJob j)
		{
			yield return StartCoroutine(j.RunJob());
			jobs.RemoveAt(0);
			jobLock = 0;
		}

		public MapperStats Stats()
		{
			return stats;
		}

		void Update()
		{
			stats.queueLen = jobs.Count;

			if (jobLock == 1)
				return;
			if (jobs.Count > 0) {
				jobLock = 1;
				StartCoroutine(RunJob(jobs[0]));
			}
		}

		public void DeleteMap(int mapId)
		{
			CoroutineJobDeleteMap j = new CoroutineJobDeleteMap();
			j.server = this.server;
			j.token = this.token;
			j.mapId = mapId;
			jobs.Add(j);
		}

		public void RestoreMapImages(int mapId)
		{
			CoroutineJobRestoreMapImages j = new CoroutineJobRestoreMapImages();
			j.server = this.server;
			j.token = this.token;
			j.mapId = mapId;
			jobs.Add(j);
			sessionFirstImage = true;
		}

		public void ResetMapperPictures()
		{
			CoroutineJobClear j = new CoroutineJobClear();
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.anchor = false;
			jobs.Add(j);

            sessionFirstImage = true;
        }

		public void ResetMapperAll()
		{
			CoroutineJobClear j = new CoroutineJobClear();
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.anchor = true;
			jobs.Add(j);

            sessionFirstImage = true;
        }

		public void Construct()
		{
			CoroutineJobConstruct j = new CoroutineJobConstruct();
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.name = m_workspaceManager.newMapName.text;
			jobs.Add(j);
		}

		private IEnumerator Capture(bool anchor)
		{
			yield return new WaitForSeconds(0.25f);

			XRCameraImage image;
			ARCameraManager cameraManager = sdk.cameraManager;
			var cameraSubsystem = cameraManager.subsystem;

			if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image))
			{
				CoroutineJobCapture j = new CoroutineJobCapture();
                j.onConnect = onConnect;
                j.onFailedToConnect = onFailedToConnect;
                j.server = this.server;
				j.token = this.token;
				j.bank = this.bank;
				j.run = (int)(this.imageRun & 0xEFFFFFFF);
				j.index = this.imageIndex++;
				j.anchor = anchor;

				Camera cam = Camera.main;
				Quaternion _q = cam.transform.rotation;
				Matrix4x4 r = Matrix4x4.Rotate(new Quaternion(_q.x, _q.y, -_q.z, -_q.w));
				Vector3 _p = cam.transform.position;
				Vector3 p = new Vector3(_p.x, _p.y, -_p.z);
				j.rotation = r;
				j.position = p;
				j.intrinsics = ARHelper.GetIntrinsics(cameraManager);
				j.width = image.width;
				j.height = image.height;

				if (rgbCapture)
				{
					ARHelper.GetPlaneDataRGB(out j.pixels, image);
					j.channels = 3;
				}
				else
				{
					ARHelper.GetPlaneData(out j.pixels, image);
					j.channels = 1;
				}

                j.sessionFirstImage = sessionFirstImage;
                if (sessionFirstImage)
                    sessionFirstImage = false;

                jobs.Add(j);
				image.Dispose();

				m_cameraShutterClick.Play();
			}
		}

		public void Capture()
		{
			StartCoroutine(Capture(false));
		}

		public void Anchor()
		{
			StartCoroutine(Capture(true));
		}

		public void Localize()
		{
            XRCameraImage image;
			ARCameraManager cameraManager = sdk.cameraManager;
			var cameraSubsystem = cameraManager.subsystem;

			if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image))
			{
				CoroutineJobLocalize j = new CoroutineJobLocalize();
                Camera cam = Camera.main;
                j.rotation = cam.transform.rotation;
				j.position = cam.transform.position;
				j.intrinsics = ARHelper.GetIntrinsics(cameraManager);
				j.width = image.width;
				j.height = image.height;
				j.stats = this.stats;
                j.pcr = this.pcr;

				ARHelper.GetPlaneData(out j.pixels, image);
				jobs.Add(j);
				image.Dispose();
			}
		}

        public void LocalizeServer()
        {
            ARCameraManager cameraManager = sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            XRCameraImage image;
            if (cameraSubsystem.TryGetLatestImage(out image))
            {
                CoroutineJobLocalizeServer j = new CoroutineJobLocalizeServer();
                j.server = this.server;
                j.token = this.token;

                Camera cam = Camera.main;
                j.rotation = cam.transform.rotation;
                j.position = cam.transform.position;
                j.intrinsics = ARHelper.GetIntrinsics(cameraManager);
                j.width = image.width;
                j.height = image.height;
                j.stats = this.stats;
                j.pcr = this.pcr;

				ARHelper.GetPlaneData(out j.pixels, image);
                jobs.Add(j);
                image.Dispose();
            }
        }

        public void LoadMap(int mapId)
		{
            if (pcr.ContainsKey(mapId))
            {
                CoroutineJobFreeMap jf = new CoroutineJobFreeMap();
                jf.id = mapId;
                jf.pcr = pcr;
                jobs.Add(jf);
                return;
            }

            CoroutineJobLoadMap j = new CoroutineJobLoadMap();
			j.server = this.server;
			j.token = this.token;
			j.id = mapId;
			j.bank = this.bank;
			j.stats = this.stats;
            j.go = gameObject;
            j.pcr = pcr;
            jobs.Add(j);
		}

		public void Jobs()
		{
			CoroutineJobListJobs j = new CoroutineJobListJobs();
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.visualizeManager = this.m_visualizeManager;
			j.activeMaps = new List<int>();
			foreach (int id in pcr.Keys)
			{
				j.activeMaps.Add(id);
			}
			jobs.Add(j);
		}

		public void Logout()
		{
			if (OnLogOut != null)
			{
				OnLogOut();
			}
		}
	}
}
