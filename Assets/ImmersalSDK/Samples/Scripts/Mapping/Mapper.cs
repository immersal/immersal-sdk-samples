/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Immersal.Samples.Util;
using Immersal.Samples.Mapping;
using Immersal.REST;
using UnityEngine.XR.ARExtensions;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Experimental.XR;
using System.Runtime.InteropServices;

namespace Immersal.Samples.Mapping
{
	public class CoroutineJob
	{
		protected const string URL_FORMAT = "{0}/fcgi?{1}";

		public virtual IEnumerator RunJob()
		{
			yield return null;
		}
	}

	public class CoroutineJobClear : CoroutineJob
	{
		public string server;
		public string token;
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
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(URL_FORMAT, server, "9"), jsonString))
			{
				request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
				request.SetRequestHeader("Accept", "application/json");
				yield return request.SendWebRequest();

				Debug.Log("Response code: " + request.responseCode);

				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log(request.error);
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
		public string server;
		public string token;
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
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(URL_FORMAT, this.server, "2"), jsonString))
			{
				request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
				request.SetRequestHeader("Accept", "application/json");
				yield return request.SendWebRequest();

				Debug.Log("Response code: " + request.responseCode);

				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log(request.error);
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
		public string server;
		public string token;
		public int bank;
		public MapperStats stats;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobStatus ***************************");

			SDKStatusRequest r = new SDKStatusRequest();
			r.token = this.token;
			r.bank = this.bank;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(URL_FORMAT, this.server, "10"), jsonString))
			{
				request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
				request.SetRequestHeader("Accept", "application/json");
				yield return request.SendWebRequest();

				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log(request.error);
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
		public string server;
		public string token;
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

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobCapture ***************************");

			byte[] capture = new byte[4 * 1024 * 1024];    // should need less than 2 megs when reso is 1440p
			Task<string> t = Task.Run(() =>
			{
				int size = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
				return Convert.ToBase64String(capture, 0, size);
			});

			while (!t.IsCompleted)
			{
				yield return null;
			}

			string encodedImage = t.Result;

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

			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(URL_FORMAT, this.server, "1"), jsonString))
			{
				request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
				request.SetRequestHeader("Accept", "application/json");
				yield return request.SendWebRequest();

				Debug.Log("Response code: " + request.responseCode);

				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log(request.error);
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
				Matrix4x4 cloudSpace = Matrix4x4.TRS(pos, rot, new Vector3(1, 1, 1));
				Matrix4x4 trackerSpace = Matrix4x4.TRS(position, rotation, new Vector3(1, 1, 1));
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

	public class CoroutineJobListJobs : CoroutineJob
	{
		public string server;
		public string token;
		public int bank;
		public MappingUIManager mappingUIManager;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineJobListJobs ***************************");

			SDKJobsRequest r = new SDKJobsRequest();
			r.token = this.token;
			r.bank = this.bank;
			string jsonString = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(URL_FORMAT, this.server, "8"), jsonString))
			{
				request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
				request.SetRequestHeader("Accept", "application/json");
				yield return request.SendWebRequest();

				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log(request.error);
				}
				else if (request.responseCode == (long)HttpStatusCode.OK)
				{
					SDKJobsResult result = JsonUtility.FromJson<SDKJobsResult>(request.downloadHandler.text);

					if (result.error == "none" && result.count > 0)
					{
						this.mappingUIManager.SetSelectSlotData(result.jobs);
					}
				}
			}
		}
	}

	public class CoroutineJobLoadMap : CoroutineJob
	{
		public string server;
		public string token;
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
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(URL_FORMAT, this.server, "3"), jsonString2))
			{
				request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
				request.SetRequestHeader("Accept", "application/json");
				yield return request.SendWebRequest();

				//Debug.Log("Response code: " + request.responseCode);

				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log(request.error);
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
						pcr.Add(id, renderer);

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
		private MappingUIManager m_mappingUIManager;
		private AudioSource m_cameraShutterClick;
        private Dictionary<int, PointCloudRenderer> pcr = new Dictionary<int, PointCloudRenderer>();

        private IEnumerator m_updateJobList;

		public event LoggedOut OnLogOut;
		public delegate void LoggedOut();

		private void ImageRunUpdate()
		{
			long bin = System.DateTime.Now.ToBinary();
			uint data = (uint)bin ^ (uint)(bin >> 32);
			imageRun = (imageRun ^ data) * 16777619;
		}

		private void SessionSubsystem_TrackingStateChanged(SessionTrackingStateChangedEventArgs args)
		{
			ImageRunUpdate();

			var captureButton = m_mappingUIManager.captureButton.GetComponent<Button>();
			var localizeButton = m_mappingUIManager.localizeButton.GetComponent<Button>();
			captureButton.interactable = args.NewState != TrackingState.Unavailable;
			localizeButton.interactable = args.NewState != TrackingState.Unavailable;
		}

		void Awake()
		{
            m_cameraShutterClick = GetComponent<AudioSource>();
			m_mappingUIManager = GetComponent<MappingUIManager>();
			m_mappingUIManager.OnItemSelected += OnItemSelected;
			m_mappingUIManager.OnSelectorOpened += OnSelectorOpened;
			m_mappingUIManager.OnSelectorClosed += OnSelectorClosed;
			#if !UNITY_EDITOR
			ARSubsystemManager.sessionSubsystem.TrackingStateChanged += SessionSubsystem_TrackingStateChanged;
			#endif
			ImageRunUpdate();
		}

		void Start()
		{

		}

		void OnEnable()
		{
			sdk = ImmersalARCloudSDK.Instance;

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

		public void SetToken(string aToken = null)
		{
			this.token = (aToken != null) ? aToken: PlayerPrefs.GetString("token");
		}

		public void SetServer(string aServer = null)
		{
			this.server = (aServer != null) ? aServer : sdk.localizationServer;
		}

		private void OnItemSelected(SDKJob job)
		{
			LoadMap(job.id);
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

		public void ResetMapperPictures()
		{
			CoroutineJobClear j = new CoroutineJobClear();
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.anchor = false;
			jobs.Add(j);
		}

		public void ResetMapperAll()
		{
			CoroutineJobClear j = new CoroutineJobClear();
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.anchor = true;
			jobs.Add(j);
		}

		public void Construct()
		{
			CoroutineJobConstruct j = new CoroutineJobConstruct();
			j.server = this.server;
			j.token = this.token;
			j.bank = this.bank;
			j.name = m_mappingUIManager.newMapName.text;
			jobs.Add(j);
		}

		private static Vector4 GetIntrinsics(float width, float height)
		{
			Vector4 intrinsics = Vector4.zero;
			Matrix4x4 proj = Matrix4x4.identity;

            if (ARSubsystemManager.cameraSubsystem.TryGetProjectionMatrix(ref proj))
			{
                float fy = 0.5f * proj.m11 * width;

                float cx = 0.5f * (proj.m02 + 1.0f) * height;
                float cy = 0.5f * (proj.m12 + 1.0f) * width;

                intrinsics.x = intrinsics.y = fy;
                intrinsics.z = cy;
                intrinsics.w = cx;
            }

            return intrinsics;
		}

		private IEnumerator Capture(bool anchor)
		{
			yield return new WaitForSeconds(0.25f);

			var cameraSubsystem = ARSubsystemManager.cameraSubsystem;

			CameraImage image;
			if (cameraSubsystem.TryGetLatestImage(out image))
			{
				CoroutineJobCapture j = new CoroutineJobCapture();
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
				j.intrinsics = GetIntrinsics(image.width, image.height);
				j.width = image.width;
				j.height = image.height;

				if (rgbCapture)
				{
					var conversionParams = new CameraImageConversionParams
					{
						inputRect = new RectInt(0, 0, image.width, image.height),
						outputDimensions = new Vector2Int(image.width, image.height),
						outputFormat = TextureFormat.RGB24,
						transformation = CameraImageTransformation.None
					};
					int size = image.GetConvertedDataSize(conversionParams);
					j.pixels = new byte[size];
					j.channels = 3;
					GCHandle bufferHandle = GCHandle.Alloc(j.pixels, GCHandleType.Pinned);
					image.Convert(conversionParams, bufferHandle.AddrOfPinnedObject(), j.pixels.Length);
					bufferHandle.Free();
				}
				else
				{
					CameraImagePlane plane = image.GetPlane(0); // use the Y plane
					j.pixels = new byte[plane.data.Length];
					j.channels = 1;
					plane.data.CopyTo(j.pixels);
				}

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
			var cameraSubsystem = ARSubsystemManager.cameraSubsystem;

			CameraImage image;
			if (cameraSubsystem.TryGetLatestImage(out image))
			{
				CoroutineJobLocalize j = new CoroutineJobLocalize();
                Camera cam = Camera.main;
                j.rotation = cam.transform.rotation;
				j.position = cam.transform.position;
				j.intrinsics = GetIntrinsics(image.width, image.height);
				j.width = image.width;
				j.height = image.height;
				j.stats = this.stats;
                j.pcr = this.pcr;

				CameraImagePlane plane = image.GetPlane(0); // use the Y plane
				j.pixels = new byte[plane.data.Length];
				plane.data.CopyTo(j.pixels);
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
			j.mappingUIManager = this.m_mappingUIManager;
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
