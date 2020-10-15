/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System;
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
		public delegate void MapChanged(int newMapHandle);
		public delegate void PoseFound(LocalizerPose newPose);

		private static NRLocalizer instance = null;

		private bool m_HasTexture = false;

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

        public override void Start()
        {
			base.Start();

			m_Sdk.Localizer = instance;
			StartLocalizing();
        }

		public override void StartLocalizing()
		{
			base.StartLocalizing();

			if (autoStart)
			{
				NRYuvCamera.OnCaptureUpdate += OnCaptureUpdate;
				NRYuvCamera.Play();
			}
		}

		public override void StopLocalizing()
		{
			if (NRYuvCamera.IsRGBCamPlaying)
				NRYuvCamera.Stop();
			
			base.StopLocalizing();
		}

		protected override void Update()
		{
            isTracking = NRFrame.SessionStatus == SessionState.Running;

            base.Update();
		}

        public override IEnumerator TryToLocalize()
		{
			while (!m_HasTexture)
			{
				yield return null;
			}

			if (m_PixelBuffer != IntPtr.Zero)
			{
				stats.localizationAttemptCount++;
				Vector3 camPos = m_Cam.transform.position;
				Quaternion camRot = m_Cam.transform.rotation;
				int width = NRYuvCamera.Resolution.width;
				int height = NRYuvCamera.Resolution.height;
				Vector4 intrinsics = GetIntrinsics(width, height);
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;

				float startTime = Time.realtimeSinceStartup;

				Task<int> t = Task.Run(() =>
				{
					return Immersal.Core.LocalizeImage(out pos, out rot, width, height, ref intrinsics, m_PixelBuffer);
				});

				while (!t.IsCompleted)
				{
					yield return null;
				}

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

					rot *= Quaternion.Euler(0f, 0f, -90.0f);
                    MapOffset mo = ARSpace.mapHandleToOffset[mapHandle];
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

					if (useFiltering)
						mo.space.filter.RefinePose(m);
					else
						ARSpace.UpdateSpace(mo.space, m.GetColumn(3), m.rotation);

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

			m_HasTexture = false;

			yield return StartCoroutine(base.TryToLocalize());
		}

		private void OnCaptureUpdate(IntPtr buffer)
		{
			m_PixelBuffer = buffer;
			m_HasTexture = true;
		}

        private Vector4 GetIntrinsics(float width, float height)
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
	}
}
