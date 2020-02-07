/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

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
using TMPro;

namespace Immersal.AR
{
	public class SpaceContainer
	{
        public int mapCount = 0;
		public Vector3 targetPosition = Vector3.zero;
		public Quaternion targetRotation = Quaternion.identity;
		public PoseFilter filter = new PoseFilter();
	}

    public class MapOffset
    {
        public Matrix4x4 offset;
        public SpaceContainer space;
    }

    public class ARLocalizer : MonoBehaviour
	{
		[Tooltip("Time between localization requests in seconds")]
		[SerializeField]
		private float m_LocalizationInterval = 2.0f;
		[Tooltip("Downsample image to HD resolution")]
		[SerializeField]
		private bool m_Downsample = false;
		[SerializeField]
		private TextMeshProUGUI m_DebugText = null;
		private ImmersalSDK m_Sdk;
		private bool m_bIsTracking = false;
		private bool m_bIsLocalizing = false;
		private bool m_bHighFrequencyMode = true;
		private float m_LastLocalizeTime = 0.0f;
		private LocalizerStats m_Stats = new LocalizerStats();
		private float m_WarpThresholdDistSq = 5.0f * 5.0f;
		private float m_WarpThresholdCosAngle = Mathf.Cos(20.0f * Mathf.PI / 180.0f);
        static private Dictionary<Transform, SpaceContainer> m_TransformToSpace = new Dictionary<Transform, SpaceContainer>();
        static private Dictionary<int, MapOffset> m_MapIdToOffset= new Dictionary<int, MapOffset>();

        public bool downsample
		{
			get { return m_Downsample; }
			set
			{
				m_Downsample = value;
				SetDownsample();
			}
		}

		public LocalizerStats stats
		{
			get { return m_Stats; }
		}

		private void ARSessionStateChanged(ARSessionStateChangedEventArgs args)
		{
			if (m_Sdk == null || m_Sdk.arSession == null)
				return;
			
			m_bIsTracking = (args.state == ARSessionState.SessionTracking && m_Sdk.arSession.subsystem.trackingState != TrackingState.None);
			if (!m_bIsTracking)
			{
				foreach (KeyValuePair<Transform, SpaceContainer> item in m_TransformToSpace)
					item.Value.filter.InvalidateHistory();
			}
		}

		void Start()
		{
			m_Sdk = ImmersalSDK.Instance;
#if !UNITY_EDITOR
			SetDownsample();
#endif
		}

		void OnEnable()
		{
#if !UNITY_EDITOR
			ARSession.stateChanged += ARSessionStateChanged;
#endif
		}

		void OnDisable()
		{
#if !UNITY_EDITOR
			ARSession.stateChanged -= ARSessionStateChanged;
#endif
		}

		void SetDownsample()
		{
			if (downsample)
			{
				Immersal.Core.SetInteger("LocalizationMaxPixels", 1280*720);
			}
		}

		void OnApplicationPause(bool pauseStatus)
		{
			foreach (KeyValuePair<Transform, SpaceContainer> item in m_TransformToSpace)
				item.Value.filter.ResetFiltering();
			
			if (!pauseStatus)
				m_bHighFrequencyMode = true;
		}

		void Update()
		{
			foreach (KeyValuePair<Transform, SpaceContainer> item in m_TransformToSpace)
			{
				float distSq = (item.Value.filter.position - item.Value.targetPosition).sqrMagnitude;
				float cosAngle = Quaternion.Dot(item.Value.filter.rotation, item.Value.targetRotation);
				if (item.Value.filter.SampleCount() == 1 || distSq > m_WarpThresholdDistSq || cosAngle < m_WarpThresholdCosAngle)
				{
					item.Value.targetPosition = item.Value.filter.position;
					item.Value.targetRotation = item.Value.filter.rotation;
				}
				else
				{
					float smoothing = 0.025f;
					float steps = Time.deltaTime / (1.0f / 60.0f);
					if (steps < 1.0f)
						steps = 1.0f;
					else if (steps > 6.0f)
						steps = 6.0f;
					float alpha = 1.0f - Mathf.Pow(1.0f - smoothing, steps);

					item.Value.targetRotation = Quaternion.Slerp(item.Value.targetRotation, item.Value.filter.rotation, alpha);
					item.Value.targetPosition = Vector3.Lerp(item.Value.targetPosition, item.Value.filter.position, alpha);
				}
				UpdateSpace(item.Value.targetPosition, item.Value.targetRotation, item.Key);
			}

			float curTime = Time.unscaledTime;
			if (m_bHighFrequencyMode)	// try to localize at max speed during app start/resume
			{
				if (!m_bIsLocalizing && m_bIsTracking)
				{
					float elapsedTime = Time.realtimeSinceStartup - m_LastLocalizeTime;
					m_bIsLocalizing = true;
					StartCoroutine(Localize());
					if (m_Stats.localizationSuccessCount == 10 || elapsedTime >= 15f)
					{
						m_bHighFrequencyMode = false;
					}
				}
			}

			if (!m_bIsLocalizing && m_bIsTracking && (curTime-m_LastLocalizeTime) >= m_LocalizationInterval)
			{
				m_LastLocalizeTime = curTime;
				m_bIsLocalizing = true;
				StartCoroutine(Localize());
			}
		}

        private IEnumerator Localize()
		{
			XRCameraImage image;
			ARCameraManager cameraManager = m_Sdk.cameraManager;
			var cameraSubsystem = cameraManager.subsystem;

			if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image))
			{
				Camera cam = Camera.main;
				m_Stats.localizationAttemptCount++;
				Vector3 camPos = cam.transform.position;
				Quaternion camRot = cam.transform.rotation;
				Vector4 intrinsics = ARHelper.GetIntrinsics(cameraManager);

				int width = image.width;
				int height = image.height;

				byte[] pixels;
				ARHelper.GetPlaneData(out pixels, image);
				image.Dispose();

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

                if (mapId >= 0 && m_MapIdToOffset.ContainsKey(mapId))
                {
					ARHelper.GetRotation(ref rot);
                    MapOffset mo = m_MapIdToOffset[mapId];
                    float elapsedTime = Time.realtimeSinceStartup - startTime;
                    Debug.Log(string.Format("Relocalised in {0} seconds", elapsedTime));
                    m_Stats.localizationSuccessCount++;
                    Matrix4x4 cloudSpace = mo.offset*Matrix4x4.TRS(pos, rot, Vector3.one);
                    Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                    Matrix4x4 m = trackerSpace * (cloudSpace.inverse);
                    mo.space.filter.RefinePose(m);
                }

                if (m_DebugText != null)
				{
					m_DebugText.text = string.Format("Successful localizations: {0}/{1}", m_Stats.localizationSuccessCount, m_Stats.localizationAttemptCount);
				}
				else
				{
					Debug.Log(string.Format("Successful localizations: {0}/{1}", m_Stats.localizationSuccessCount, m_Stats.localizationAttemptCount));
				}
			}
			m_bIsLocalizing = false;
		}

		#region ARSpace

        static public void RegisterSpace(Transform tr, int mapId, Matrix4x4 offset)
		{
            SpaceContainer sc;

            if (!m_TransformToSpace.ContainsKey(tr))
            {
                sc = new SpaceContainer();
                m_TransformToSpace[tr] = sc;
            }
            else
            {
                sc = m_TransformToSpace[tr];
            }

            sc.mapCount++;

            MapOffset mo = new MapOffset();
            mo.offset = offset;
            mo.space = sc;

            m_MapIdToOffset[mapId] = mo;
		}

        static public void RegisterSpace(Transform tr, int mapId)
        {
            RegisterSpace(tr, mapId, Matrix4x4.identity);
        }

        static public void UnregisterSpace(Transform tr, int spaceId)
		{
			if (m_TransformToSpace.ContainsKey(tr))
			{
				SpaceContainer sc = m_TransformToSpace[tr];
				if (--sc.mapCount == 0)
					m_TransformToSpace.Remove(tr);
				if (m_MapIdToOffset.ContainsKey(spaceId))
					m_MapIdToOffset.Remove(spaceId);
			}
		}

		private void UpdateSpace(Vector3 pos, Quaternion rot, Transform tr)
        {
    		tr.SetPositionAndRotation(pos, rot);
		}
		#endregion
	}

	public class LocalizerStats
	{
		public int localizationAttemptCount = 0;
		public int localizationSuccessCount = 0;
	}
}