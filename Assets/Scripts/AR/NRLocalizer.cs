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
using System.Threading.Tasks;
using NRKernal;

namespace Immersal.AR.Nreal
{
    public class NRLocalizer : LocalizerBase
	{
		public event MapChanged OnMapChanged = null;
		public event PoseFound OnPoseFound = null;
		public delegate void MapChanged(int newMapId);
		public delegate void PoseFound(LocalizerPose newPose);

		private bool m_HasTexture = false;
		private byte[] m_PixelBuffer = null;

        public override void Start()
        {
			NRYuvCamera.OnCaptureUpdate += OnCaptureUpdate;
			NRYuvCamera.Play();
        }

		private void OnCaptureUpdate(byte[] buffer)
		{
			m_PixelBuffer = buffer;
			m_HasTexture = true;
		}

		public override void Update()
		{
            isTracking = NRFrame.SessionStatus == SessionState.Running;

            base.Update();
		}

        public Vector4 GetIntrinsics(float width, float height)
        {
            bool result = false;
            EyeProjectMatrixData data = NRFrame.GetEyeProjectMatrix(out result, m_Cam.nearClipPlane, m_Cam.farClipPlane);
            Vector4 intrinsics = Vector4.zero;

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

				/*intrinsics.x = 1198.24f;
				intrinsics.y = 1195.96f;
				intrinsics.z = 648.262f;
				intrinsics.w = 369.875f;*/
            }

            return intrinsics;
        }

        public override IEnumerator Localize()
		{
			while (!m_HasTexture)
			{
				yield return null;
			}

			if (m_PixelBuffer != null)
			{
				stats.localizationAttemptCount++;
				Vector3 camPos = m_Cam.transform.position;
				Quaternion camRot = m_Cam.transform.rotation;
				int width = NRYuvCamera.Resolution.width;
				int height = NRYuvCamera.Resolution.height;
                byte[] pixels = new byte[width * height];
				System.Buffer.BlockCopy(m_PixelBuffer, 0, pixels, 0, pixels.Length);
				Vector4 intrinsics = GetIntrinsics(width, height);
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;

				float startTime = Time.realtimeSinceStartup;

				Task<int> t = Task.Run(() =>
				{
					return Immersal.Core.LocalizeImage(out pos, out rot, width, height, ref intrinsics, pixels);
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

					rot *= Quaternion.Euler(0f, 0f, -90.0f);
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

			m_HasTexture = false;

			yield return StartCoroutine(base.Localize());
		}
	}
}
