/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NRKernal;
using Immersal.REST;
using Unity.Collections.LowLevel.Unsafe;

namespace Immersal.AR.Nreal
{
    public class NRLocalizer : LocalizerBase
	{
		public event MapChanged OnMapChanged = null;
		public event PoseFound OnPoseFound = null;
		public delegate void MapChanged(int newMapHandle);
		public delegate void PoseFound(LocalizerPose newPose);

		private static NRLocalizer instance = null;

        private NRRGBCamTextureYUV YuvCamTexture { get; set; }

		public static NRLocalizer Instance
		{
			get
			{
#if UNITY_EDITOR
				if (instance == null && !Application.isPlaying)
				{
					instance = UnityEngine.Object.FindObjectOfType<NRLocalizer>();
				}
#endif
				if (instance == null)
				{
					Debug.LogError("No NRLocalizer instance found. Ensure one exists in the scene.");
				}
				return instance;
			}
		}

		void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			if (instance != this)
			{
				Debug.LogError("There must be only one NRLocalizer object in a scene.");
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}
		}

        public override void OnEnable()
        {
            base.OnEnable();
			m_Cam = GameObject.Find("CenterCamera").GetComponent<Camera>();
        }

        public override void Start()
        {
			base.Start();

			m_Sdk.Localizer = instance;
            YuvCamTexture = new NRRGBCamTextureYUV();
			YuvCamTexture.OnUpdate += OnCaptureUpdate;

			if (autoStart)
			{
	            YuvCamTexture.Play();
			}
        }

        public override void StartLocalizing()
        {
            YuvCamTexture.Play();
            base.StartLocalizing();
        }

		public override void StopLocalizing()
		{
			base.StopLocalizing();
			YuvCamTexture.Stop();
		}

        public override void Pause()
        {
            base.Pause();
			YuvCamTexture.Pause();
        }

		protected override void Update()
		{
            isTracking = NRFrame.SessionStatus == SessionState.Running;

            base.Update();
		}

        public override void OnDestroy()
        {
            YuvCamTexture.Stop();
			YuvCamTexture.OnUpdate -= OnCaptureUpdate;
			base.OnDestroy();
        }

        public override async void Localize()
		{
			while (!YuvCamTexture.DidUpdateThisFrame)
			{
				await Task.Yield();
			}

			if (m_PixelBuffer != IntPtr.Zero)
			{
				stats.localizationAttemptCount++;
				Vector4 intrinsics;
				Vector3 camPos = m_Cam.transform.position;
				Quaternion camRot = m_Cam.transform.rotation;
				int width = YuvCamTexture.Width;
				int height = YuvCamTexture.Height;
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;
				GetIntrinsics(out intrinsics, width, height);

				float startTime = Time.realtimeSinceStartup;

				Task<int> t = Task.Run(() =>
				{
					return Immersal.Core.LocalizeImage(out pos, out rot, width, height, ref intrinsics, m_PixelBuffer);
				});

				await t;

                int mapHandle = t.Result;
                float elapsedTime = Time.realtimeSinceStartup - startTime;

                if (mapHandle >= 0 && ARSpace.mapHandleToOffset.ContainsKey(mapHandle))
                {
                    Debug.Log(string.Format("Relocalized in {0} seconds", elapsedTime));

					if (mapHandle != lastLocalizedMapHandle)
					{
						if (resetOnMapChange)
						{
							Reset();
						}
						
						lastLocalizedMapHandle = mapHandle;

						OnMapChanged?.Invoke(mapHandle);
					}

                    stats.localizationSuccessCount++;
					
					rot *= Quaternion.Euler(0f, 0f, -90.0f);
                    MapOffset mo = ARSpace.mapHandleToOffset[mapHandle];

					Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
					Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
					Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
					Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
					Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

					if (useFiltering)
						mo.space.filter.RefinePose(m);
					else
						ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

					Vector3 p = m.GetColumn(3);
					Vector3 euler = m.rotation.eulerAngles;

					LocalizerPose localizerPose;
					GetLocalizerPose(out localizerPose, mapHandle, pos, rot, m.inverse);
					OnPoseFound?.Invoke(localizerPose);

					if (ARSpace.mapHandleToMap.ContainsKey(mapHandle))
					{
						ARMap map = ARSpace.mapHandleToMap[mapHandle];
						map.NotifySuccessfulLocalization(mapHandle);
					}
                }
				else
				{
                    Debug.Log(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
				}
			}

			base.Localize();
		}

        public override async void LocalizeServer(SDKMapId[] mapIds)
        {
			while (!YuvCamTexture.DidUpdateThisFrame)
			{
				await Task.Yield();
			}

			if (m_PixelBuffer != IntPtr.Zero)
			{
				stats.localizationAttemptCount++;

                JobLocalizeServerAsync j = new JobLocalizeServerAsync();

                Vector3 camPos = m_Cam.transform.position;
                Quaternion camRot = m_Cam.transform.rotation;
				Vector4 intrinsics;
                int channels = 1;
				int width = YuvCamTexture.Width;
				int height = YuvCamTexture.Height;
                byte[] pixels = YuvCamTexture.GetTexture().YBuf;

				GetIntrinsics(out intrinsics, width, height);

				float startTime = Time.realtimeSinceStartup;

                Task<(byte[], icvCaptureInfo)> t = Task.Run(() =>
                {
                    byte[] capture = new byte[channels * width * height + 1024];
                    icvCaptureInfo info = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                    Array.Resize(ref capture, info.captureSize);
                    return (capture, info);
                });

                await t;

                j.image = t.Result.Item1;
				j.intrinsics = intrinsics;
				j.mapIds = mapIds;

                j.OnResult += (SDKResultBase r) =>
                {
					float elapsedTime = Time.realtimeSinceStartup - startTime;

					if (r is SDKLocalizeResult result && result.success)
					{
						Debug.Log("*************************** On-Server Localization Succeeded ***************************");
						Debug.Log(string.Format("Relocalized in {0} seconds", elapsedTime));

						int mapServerId = result.map;

						if (mapServerId > 0 && ARSpace.mapHandleToOffset.ContainsKey(mapServerId))
						{
							if (mapServerId != lastLocalizedMapHandle)
							{
								if (resetOnMapChange)
								{
									Reset();
								}
								
								lastLocalizedMapHandle = mapServerId;

								OnMapChanged?.Invoke(mapServerId);
							}

							MapOffset mo = ARSpace.mapHandleToOffset[mapServerId];
							stats.localizationSuccessCount++;
							
							Matrix4x4 responseMatrix = Matrix4x4.identity;
							responseMatrix.m00 = result.r00; responseMatrix.m01 = result.r01; responseMatrix.m02 = result.r02; responseMatrix.m03 = result.px;
							responseMatrix.m10 = result.r10; responseMatrix.m11 = result.r11; responseMatrix.m12 = result.r12; responseMatrix.m13 = result.py;
							responseMatrix.m20 = result.r20; responseMatrix.m21 = result.r21; responseMatrix.m22 = result.r22; responseMatrix.m23 = result.pz;

							Vector3 pos  = responseMatrix.GetColumn(3);
							Quaternion rot = responseMatrix.rotation;
							rot *= Quaternion.Euler(0f, 0f, -90f);

							Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
							Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
							Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
							Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
							Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

							if (useFiltering)
								mo.space.filter.RefinePose(m);
							else
								ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

							LocalizerPose localizerPose;
							GetLocalizerPose(out localizerPose, mapServerId, pos, rot, m.inverse);
							OnPoseFound?.Invoke(localizerPose);

							if (ARSpace.mapHandleToMap.ContainsKey(mapServerId))
							{
								ARMap map = ARSpace.mapHandleToMap[mapServerId];
								map.NotifySuccessfulLocalization(mapServerId);
							}
						}
						else
						{
							Debug.Log(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
						}
					}
					else
					{
						Debug.Log("*************************** On-Server Localization Failed ***************************");
					}
                };

				await j.RunJobAsync();
            }

			base.LocalizeServer(mapIds);
        }

        private void OnCaptureUpdate(NRRGBCamTextureYUV.YUVTextureFrame frame)
        {
			if (m_PixelBuffer == IntPtr.Zero)
			{
				unsafe
				{
					ulong handle;
					byte* ptr = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(frame.YBuf, out handle);
					m_PixelBuffer = (IntPtr)ptr;
					UnsafeUtility.ReleaseGCObject(handle);
				}
			}
        }

        private void GetIntrinsics(out Vector4 intrinsics, float width, float height)
        {
            bool result = false;
            intrinsics = Vector4.zero;
            EyeProjectMatrixData data = NRFrame.GetEyeProjectMatrix(out result, m_Cam.nearClipPlane, m_Cam.farClipPlane);

            if (result)
            {
                Matrix4x4 proj = data.RGBEyeMatrix;
                float fy = 0.5f * proj.m00 * width;
                float cx = 0.5f * (proj.m02 + 1.0f) * height;
                float cy = 0.5f * (proj.m12 + 1.0f) * width;
				cx -= 50f;	// adjust
                intrinsics.x = intrinsics.y = fy;
                intrinsics.z = cy;
                intrinsics.w = cx;
/*				intrinsics.x = 1198.24f;
				intrinsics.y = 1195.96f;
				intrinsics.z = 648.262f;
				intrinsics.w = 369.875f;*/
            }
        }
	}
}
