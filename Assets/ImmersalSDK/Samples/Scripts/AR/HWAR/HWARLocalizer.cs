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
		public event MapChanged OnMapChanged = null;
		public event PoseFound OnPoseFound = null;
		public delegate void MapChanged(int newMapId);
		public delegate void PoseFound(LocalizerPose newPose);

		public override void Update()
		{
            isTracking = ARFrame.GetTrackingState() == ARTrackable.TrackingState.TRACKING;

            base.Update();
		}

        public override IEnumerator Localize()
		{
			ARCameraImageBytes image = null;
			bool isHD = HWARHelper.TryGetCameraImageBytes(out image);

			if (image != null && image.IsAvailable)
			{
				stats.localizationAttemptCount++;
				Vector3 camPos = m_Cam.transform.position;
				Quaternion camRot = m_Cam.transform.rotation;
				byte[] pixels;
				Vector4 intrinsics = isHD ? HWARHelper.GetIntrinsics() : HWARHelper.GetIntrinsics(image.Width, image.Height);
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

						OnMapChanged?.Invoke(mapId);
					}

					HWARHelper.GetRotation(ref rot);
                    MapOffset mo = ARSpace.mapIdToOffset[mapId];
                    float elapsedTime = Time.realtimeSinceStartup - startTime;
                    Debug.Log(string.Format("Relocalised in {0} seconds", elapsedTime));
                    stats.localizationSuccessCount++;

					Matrix4x4 offsetNoScale = Matrix4x4.TRS(mo.position, mo.rotation, Vector3.one);
					Vector3 scaledPos = new Vector3
						(
							pos.x * mo.scale.x,
							pos.y * mo.scale.y,
							pos.z * mo.scale.z
						);
					Matrix4x4 cloudSpace = offsetNoScale * Matrix4x4.TRS(scaledPos, rot, Vector3.one);
					Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
					Matrix4x4 m = trackerSpace * (cloudSpace.inverse);
					mo.space.filter.RefinePose(m);
					
					LocalizerPose localizerPose;
					GetLocalizerPose(out localizerPose, mapId, pos, rot, m.inverse);
					OnPoseFound?.Invoke(localizerPose);
                }
			}

			yield return StartCoroutine(base.Localize());
		}
    }
}
#endif