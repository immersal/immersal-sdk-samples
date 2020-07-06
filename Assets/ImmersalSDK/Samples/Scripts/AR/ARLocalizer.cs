/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading.Tasks;

namespace Immersal.AR
{
    public class ARLocalizer : LocalizerBase
	{
		public event MapChanged OnMapChanged = null;
		public event PoseFound OnPoseFound = null;
		public delegate void MapChanged(int newMapId);
		public delegate void PoseFound(LocalizerPose newPose);

		private void ARSessionStateChanged(ARSessionStateChangedEventArgs args)
		{
			CheckTrackingState(args.state);
		}

		private void CheckTrackingState(ARSessionState newState)
		{
			isTracking = newState == ARSessionState.SessionTracking;

			if (!isTracking)
			{
				foreach (KeyValuePair<Transform, SpaceContainer> item in ARSpace.transformToSpace)
					item.Value.filter.InvalidateHistory();
			}
		}

		public override void OnEnable()
		{
			base.OnEnable();
#if !UNITY_EDITOR
			CheckTrackingState(ARSession.state);
			ARSession.stateChanged += ARSessionStateChanged;
#endif
		}

		public override void OnDisable()
		{
#if !UNITY_EDITOR
			ARSession.stateChanged -= ARSessionStateChanged;
#endif
			base.OnDisable();
		}

        public override IEnumerator Localize()
		{
			XRCpuImage image;
			ARCameraManager cameraManager = m_Sdk.cameraManager;
			var cameraSubsystem = cameraManager.subsystem;

			if (cameraSubsystem != null && cameraSubsystem.TryAcquireLatestCpuImage(out image))
			{
				stats.localizationAttemptCount++;
				Vector3 camPos = m_Cam.transform.position;
				Quaternion camRot = m_Cam.transform.rotation;
				byte[] pixels;
				Vector4 intrinsics = ARHelper.GetIntrinsics();
				ARHelper.GetPlaneData(out pixels, image);

				image.Dispose();

				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;

				float startTime = Time.realtimeSinceStartup;

				Task<int> t = Task.Run(() =>
				{
					return Immersal.Core.LocalizeImage(out pos, out rot, image.width, image.height, ref intrinsics, pixels);
				});

				while (!t.IsCompleted)
				{
					yield return null;
				}

                int mapId = t.Result;

                if (mapId > 0 && ARSpace.mapIdToOffset.ContainsKey(mapId))
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

					ARHelper.GetRotation(ref rot);
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