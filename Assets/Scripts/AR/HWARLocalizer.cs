/*===============================================================================
Copyright (C) 2023 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using System;
using UnityEngine;
using HuaweiARUnitySDK;
using System.Threading.Tasks;
using Immersal.REST;

namespace Immersal.AR
{
    public class HWARLocalizer : LocalizerBase
	{
		private static HWARLocalizer instance = null;

		private bool m_localizationDebugLoggingEnabled = false;

		public bool LocalizationDebugLoggingEnabled
		{
			set => m_localizationDebugLoggingEnabled = value;
			get => m_localizationDebugLoggingEnabled;
		}

		private void LocalizerDebugLog(string message)
		{
			if (m_localizationDebugLoggingEnabled)
			{
				Debug.LogFormat("[HWARLocalizer]: {0}", message);
			}
		}

		protected override void Update()
		{
			if (!ARFrame.TextureIsAvailable())
				return;
			
			using (ARCamera camera = ARFrame.GetCamera())
			{
	            isTracking = camera.GetTrackingState() == ARTrackable.TrackingState.TRACKING;
			}

            base.Update();
		}

		public static HWARLocalizer Instance
		{
			get
			{
#if UNITY_EDITOR
				if (instance == null && !Application.isPlaying)
				{
					instance = UnityEngine.Object.FindObjectOfType<HWARLocalizer>();
				}
#endif
				if (instance == null)
				{
					Debug.LogError("No HWARLocalizer instance found. Ensure one exists in the scene.");
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
				Debug.LogError("There must be only one HWARLocalizer object in a scene.");
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}
		}

        public override void Start()
        {
            base.Start();
            m_Sdk.RegisterLocalizer(instance);
        }

        public override async void LocalizeServer(SDKMapId[] mapIds)
        {
			if (HWARHelper.TryGetCameraImageBytes(out ARCameraImageBytes image, out bool isHD))
			{
				stats.localizationAttemptCount++;

                JobLocalizeServerAsync j = new JobLocalizeServerAsync();

                byte[] pixels;
                Vector3 camPos = m_Cam.transform.position;
                Quaternion camRot = m_Cam.transform.rotation;
				Vector4 intrinsics;
                int channels = 1;
                int width = image.Width;
                int height = image.Height;

                HWARHelper.GetIntrinsics(out intrinsics, isHD, width, height);
				HWARHelper.GetPlaneData(out pixels, image);

				float startTime = Time.realtimeSinceStartup;

                Task<(byte[], CaptureInfo)> t = Task.Run(() =>
                {
                    byte[] capture = new byte[channels * width * height + 8192];
                    CaptureInfo info = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                    Array.Resize(ref capture, info.captureSize);
                    return (capture, info);
                });

                await t;

                j.image = t.Result.Item1;
				j.intrinsics = intrinsics;
				j.mapIds = mapIds;

                j.OnResult += (SDKLocalizeResult result) =>
                {
					float elapsedTime = Time.realtimeSinceStartup - startTime;

					if (result.success)
					{
						LocalizerDebugLog(string.Format("Relocalized in {0} seconds", elapsedTime));

						int mapId = result.map;

						if (mapId > 0 && ARSpace.mapIdToMap.ContainsKey(mapId))
						{
							ARMap map = ARSpace.mapIdToMap[mapId];

							if (mapId != lastLocalizedMapId)
							{
								if (resetOnMapChange)
								{
									Reset();
								}
								
								lastLocalizedMapId = mapId;
								OnMapChanged?.Invoke(mapId);
							}

							MapOffset mo = ARSpace.mapIdToOffset[mapId];
							stats.localizationSuccessCount++;
							
							Matrix4x4 responseMatrix = Matrix4x4.identity;
							responseMatrix.m00 = result.r00; responseMatrix.m01 = result.r01; responseMatrix.m02 = result.r02; responseMatrix.m03 = result.px;
							responseMatrix.m10 = result.r10; responseMatrix.m11 = result.r11; responseMatrix.m12 = result.r12; responseMatrix.m13 = result.py;
							responseMatrix.m20 = result.r20; responseMatrix.m21 = result.r21; responseMatrix.m22 = result.r22; responseMatrix.m23 = result.pz;
							
							Vector3 pos = responseMatrix.GetColumn(3);
							Quaternion rot = responseMatrix.rotation;
							ARHelper.GetRotation(ref rot);
							pos = ARHelper.SwitchHandedness(pos);
							rot = ARHelper.SwitchHandedness(rot);

							Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
							Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
							Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
							Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
							Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

							if (useFiltering)
								mo.space.filter.RefinePose(m);
							else
								ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

							double[] ecef = map.MapToEcefGet();
							LocalizerBase.GetLocalizerPose(out lastLocalizedPose, mapId, pos, rot, m.inverse, ecef);
							map.NotifySuccessfulLocalization(mapId);
							OnPoseFound?.Invoke(lastLocalizedPose);
						}
					}
					else
					{
						LocalizerDebugLog(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
					}
                };

				await j.RunJobAsync();

                image.Dispose();
            }

			base.LocalizeServer(mapIds);
        }

        public override async void LocalizeGeoPose(SDKMapId[] mapIds)
        {
			if (HWARHelper.TryGetCameraImageBytes(out ARCameraImageBytes image, out bool isHD))
            {
				stats.localizationAttemptCount++;

                JobGeoPoseAsync j = new JobGeoPoseAsync();

                byte[] pixels;
                Vector3 camPos = m_Cam.transform.position;
                Quaternion camRot = m_Cam.transform.rotation;
				Vector4 intrinsics;
                int channels = 1;
                int width = image.Width;
                int height = image.Height;

                j.mapIds = mapIds;

                HWARHelper.GetIntrinsics(out intrinsics, isHD, width, height);
				HWARHelper.GetPlaneData(out pixels, image);

				float startTime = Time.realtimeSinceStartup;

                Task<(byte[], CaptureInfo)> t = Task.Run(() =>
                {
                    byte[] capture = new byte[channels * width * height + 8192];
                    CaptureInfo info = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                    Array.Resize(ref capture, info.captureSize);
                    return (capture, info);
                });

                await t;

                j.image = t.Result.Item1;

                j.OnResult += (SDKGeoPoseResult result) =>
                {
					float elapsedTime = Time.realtimeSinceStartup - startTime;

                    if (result.success)
                    {
	                    LocalizerDebugLog(string.Format("Relocalized in {0} seconds", elapsedTime));

                        int mapId = result.map;
                        double latitude = result.latitude;
                        double longitude = result.longitude;
                        double ellipsoidHeight = result.ellipsoidHeight;
                        Quaternion rot = new Quaternion(result.quaternion[1], result.quaternion[2], result.quaternion[3], result.quaternion[0]);
                        LocalizerDebugLog(string.Format("GeoPose returned latitude: {0}, longitude: {1}, ellipsoidHeight: {2}, quaternion: {3}", latitude, longitude, ellipsoidHeight, rot));

                        double[] ecef = new double[3];
                        double[] wgs84 = new double[3] { latitude, longitude, ellipsoidHeight };
                        Core.PosWgs84ToEcef(ecef, wgs84);

						if (ARSpace.mapIdToMap.ContainsKey(mapId))
						{
							ARMap map = ARSpace.mapIdToMap[mapId];

							if (mapId != lastLocalizedMapId)
							{
								if (resetOnMapChange)
								{
									Reset();
								}
								
								lastLocalizedMapId = mapId;

								OnMapChanged?.Invoke(mapId);
							}

							MapOffset mo = ARSpace.mapIdToOffset[mapId];
							stats.localizationSuccessCount++;

							double[] mapToEcef = map.MapToEcefGet();
							Vector3 mapPos;
							Quaternion mapRot;
							Core.PosEcefToMap(out mapPos, ecef, mapToEcef);
							Core.RotEcefToMap(out mapRot, rot, mapToEcef);

							Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
							Vector3 scaledPos = Vector3.Scale(mapPos, mo.scale);
							Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, mapRot, Vector3.one);
							Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
							Matrix4x4 m = trackerSpace*(cloudSpace.inverse);
							
							if (useFiltering)
								mo.space.filter.RefinePose(m);
							else
								ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

							LocalizerBase.GetLocalizerPose(out lastLocalizedPose, mapId, cloudSpace.GetColumn(3), cloudSpace.rotation, m.inverse, mapToEcef);
							map.NotifySuccessfulLocalization(mapId);
							OnPoseFound?.Invoke(lastLocalizedPose);
						}
                    }
                    else
                    {
	                    LocalizerDebugLog(string.Format("GeoPose localization attempt failed after {0} seconds", elapsedTime));
                    }
                };

                await j.RunJobAsync();
                image.Dispose();
            }

			base.LocalizeGeoPose(mapIds);
        }

        public override async void Localize()
		{
			if (HWARHelper.TryGetCameraImageBytes(out ARCameraImageBytes image, out bool isHD))
			{
				stats.localizationAttemptCount++;
				Vector3 camPos = m_Cam.transform.position;
				Quaternion camRot = m_Cam.transform.rotation;
				Vector4 intrinsics;

				HWARHelper.GetIntrinsics(out intrinsics, isHD, image.Width, image.Height);
				HWARHelper.GetPlaneDataFast(ref m_PixelBuffer, image);
				float[] rotArray = new float[4];

				if (m_PixelBuffer != IntPtr.Zero)
				{
					float startTime = Time.realtimeSinceStartup;

					Task<LocalizeInfo> t = Task.Run(() =>
					{
						if (SolverType == SolverType.Lean)
							return Immersal.Core.LocalizeImage(image.Width, image.Height, ref intrinsics, m_PixelBuffer, rotArray);

						return Immersal.Core.LocalizeImage(image.Width, image.Height, ref intrinsics, m_PixelBuffer);
					});

					await t;
					
					LocalizeInfo locInfo = t.Result;

					Matrix4x4 resultMatrix = Matrix4x4.identity;
					resultMatrix.m00 = locInfo.r00; resultMatrix.m01 = locInfo.r01; resultMatrix.m02 = locInfo.r02; resultMatrix.m03 = locInfo.px;
					resultMatrix.m10 = locInfo.r10; resultMatrix.m11 = locInfo.r11; resultMatrix.m12 = locInfo.r12; resultMatrix.m13 = locInfo.py;
					resultMatrix.m20 = locInfo.r20; resultMatrix.m21 = locInfo.r21; resultMatrix.m22 = locInfo.r22; resultMatrix.m23 = locInfo.pz;

					Vector3 pos = resultMatrix.GetColumn(3);
					Quaternion rot = resultMatrix.rotation;
					
					int mapHandle = t.Result.handle;
					int mapId = ARMap.MapHandleToId(mapHandle);
					float elapsedTime = Time.realtimeSinceStartup - startTime;

					if (mapId > 0 && ARSpace.mapIdToMap.ContainsKey(mapId))
					{
						ARHelper.GetRotation(ref rot);
						pos = ARHelper.SwitchHandedness(pos);
						rot = ARHelper.SwitchHandedness(rot);

						LocalizerDebugLog(string.Format("Relocalized in {0} seconds", elapsedTime));
						stats.localizationSuccessCount++;

						if (mapId != lastLocalizedMapId)
						{
							if (resetOnMapChange)
							{
								Reset();
							}
							
							lastLocalizedMapId = mapId;
							OnMapChanged?.Invoke(mapId);
						}
						
						MapOffset mo = ARSpace.mapIdToOffset[mapId];
						Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
						Vector3 scaledPos = Vector3.Scale(pos, mo.scale);
						Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
						Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
						Matrix4x4 m = trackerSpace * (cloudSpace.inverse);
						
						if (useFiltering)
							mo.space.filter.RefinePose(m);
						else
							ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

						GetLocalizerPose(out lastLocalizedPose, mapId, pos, rot, m.inverse);
						OnPoseFound?.Invoke(lastLocalizedPose);
						
						ARMap map = ARSpace.mapIdToMap[mapId];
						map.NotifySuccessfulLocalization(mapId);
					}
					else
					{
						LocalizerDebugLog(string.Format("Localization attempt failed after {0} seconds", elapsedTime));
					}
				}

				image.Dispose();
			}

			base.Localize();
		}
	}
}
#endif