/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using HuaweiARUnitySDK;
using System.Threading.Tasks;

namespace Immersal.AR.HWAR
{
    public class HWARLocalizer : BaseLocalizer
	{
		public override void Update()
		{
            isTracking = ARFrame.GetTrackingState() == ARTrackable.TrackingState.TRACKING;

            base.Update();
		}

        public override IEnumerator Localize()
		{
			ARCameraImageBytes image = null;
			if (m_Sdk.androidResolution == ImmersalSDK.CameraResolution.Max)
			{
				try
				{
					image = ARFrame.AcquirPreviewImageBytes();
				}
				catch (NullReferenceException e)
				{
					Debug.LogError("Cannot acquire FullHD image: " + e.Message);

					image = ARFrame.AcquireCameraImageBytes();
				}
			}
			else
			{
				image = ARFrame.AcquireCameraImageBytes();
			}

			if (image != null && image.IsAvailable)
			{
				stats.localizationAttemptCount++;
				Vector3 camPos = m_Cam.transform.position;
				Quaternion camRot = m_Cam.transform.rotation;
				byte[] pixels;
				Vector4 intrinsics = HWARHelper.GetIntrinsics();
				HWARHelper.GetPlaneData(out pixels, image);

				image.Dispose();

				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;

				float startTime = Time.realtimeSinceStartup;

				Task<int> t = Task.Run(() =>
				{
					return Immersal.Core.LocalizeImage(out pos, out rot, image.Width, image.Height, ref intrinsics, pixels);
				});

				while (!t.IsCompleted)
				{
					yield return null;
				}

                int mapId = t.Result;

                if (mapId >= 0 && ARSpace.mapIdToOffset.ContainsKey(mapId))
                {
					if (mapId != lastLocalizedMapId)
					{
						if (m_ResetOnMapChange)
						{
							foreach (KeyValuePair<Transform, SpaceContainer> item in ARSpace.transformToSpace)
								item.Value.filter.ResetFiltering();
						}
						
						lastLocalizedMapId = mapId;
					}

					HWARHelper.GetRotation(ref rot);
                    MapOffset mo = ARSpace.mapIdToOffset[mapId];
                    float elapsedTime = Time.realtimeSinceStartup - startTime;
                    Debug.Log(string.Format("Relocalised in {0} seconds", elapsedTime));
                    stats.localizationSuccessCount++;
                    Matrix4x4 cloudSpace = mo.offset*Matrix4x4.TRS(pos, rot, Vector3.one);
                    Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                    Matrix4x4 m = trackerSpace * (cloudSpace.inverse);
                    mo.space.filter.RefinePose(m);
                }
			}

			yield return StartCoroutine(base.Localize());
		}
    }
}
#endif