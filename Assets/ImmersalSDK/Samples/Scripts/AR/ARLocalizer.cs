/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARExtensions;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Experimental.XR;
using System.Threading.Tasks;
using TMPro;

namespace Immersal.AR
{
	public class SpaceContainer
	{
		public Vector3 targetPosition = Vector3.zero;
		public Quaternion targetRotation = Quaternion.identity;
		public PoseFilter filter = new PoseFilter();
		public List<Transform> nodes = new List<Transform>();
	}

	public class ARLocalizer : MonoBehaviour
	{
		[Tooltip("Time between localization requests in seconds")]
		[SerializeField]
		private float m_LocalizationDelay = 2.0f;
		[Tooltip("Downsample image to HD resolution")]
		[SerializeField]
		private bool m_Downsample = false;
		[SerializeField]
		private TextMeshProUGUI m_debugText = null;
		private bool m_bIsTracking = false;
		private bool m_bIsLocalizing = false;
		private float m_LastLocalizeTime = 0.0f;
		private LocalizerStats m_stats = new LocalizerStats();
		private float m_WarpThresholdDistSq = 5.0f * 5.0f;
		private float m_WarpThresholdCosAngle = Mathf.Cos(20.0f * Mathf.PI / 180.0f);
		static private Dictionary<int, SpaceContainer> m_Spaces = new Dictionary<int, SpaceContainer>();

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
			get { return m_stats; }
		}

		public Vector3 position
		{
			// TODO: handle differently
			get {
				if (m_Spaces.ContainsKey(0))
					return m_Spaces[0].filter.position;
				else
					return Vector3.zero;
			}
		}

		public Quaternion rotation
		{
			// TODO: handle differently
			get {
				if (m_Spaces.ContainsKey(0))
					return m_Spaces[0].filter.rotation;
				else
					return Quaternion.identity;
			}
		}

		private void SessionSubsystem_TrackingStateChanged(SessionTrackingStateChangedEventArgs args)
		{
			m_bIsTracking = args.NewState == TrackingState.Tracking;
			if (!m_bIsTracking)
			{
				foreach (KeyValuePair<int, SpaceContainer> item in m_Spaces)
					item.Value.filter.InvalidateHistory();
			}
		}

		void Start()
		{
#if !UNITY_EDITOR
			ARSubsystemManager.sessionSubsystem.TrackingStateChanged += SessionSubsystem_TrackingStateChanged;

			SetDownsample();
#endif
		}

		void SetDownsample()
		{
			if (downsample)
			{
				Native.icvSetInteger("LocalizationMaxPixels", 1280*720);
			}
		}

		void OnApplicationPause(bool pauseStatus)
		{
			foreach (KeyValuePair<int, SpaceContainer> item in m_Spaces)
				item.Value.filter.ResetFiltering();
		}

		void Update()
		{
			foreach (KeyValuePair<int, SpaceContainer> item in m_Spaces)
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
			if (!m_bIsLocalizing && m_bIsTracking && (curTime-m_LastLocalizeTime) >= m_LocalizationDelay)
			{
				m_LastLocalizeTime = curTime;
				m_bIsLocalizing = true;
				StartCoroutine(Localize());
			}
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

        private IEnumerator Localize()
		{
			var cameraSubsystem = ARSubsystemManager.cameraSubsystem;
			CameraImage image;
			if (cameraSubsystem.TryGetLatestImage(out image))
			{
				m_stats.localizationAttemptCount++;
				Camera cam = Camera.main;
				Vector3 camPos = cam.transform.position;
				Quaternion camRot = cam.transform.rotation;
				Vector4 intrinsics = GetIntrinsics(image.width, image.height);

				int width = image.width;
				int height = image.height;

				CameraImagePlane plane = image.GetPlane(0); // use the Y plane
				byte[] pixels = new byte[plane.data.Length];
				plane.data.CopyTo(pixels);
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

                int mapHandle = t.Result;

                if (mapHandle >= 0)
                {
                    float elapsedTime = Time.realtimeSinceStartup - startTime;
                    Debug.Log(string.Format("Relocalised in {0} seconds", elapsedTime));
                    m_stats.localizationSuccessCount++;
                    Matrix4x4 cloudSpace = Matrix4x4.TRS(pos, rot, new Vector3(1, 1, 1));
                    Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, new Vector3(1, 1, 1));
                    Matrix4x4 m = trackerSpace * (cloudSpace.inverse);
                    if (m_Spaces.ContainsKey(mapHandle))
                        m_Spaces[mapHandle].filter.RefinePose(m);
                }

                if (m_debugText != null)
				{
					m_debugText.text = string.Format("Localization status: {0}/{1}", m_stats.localizationSuccessCount, m_stats.localizationAttemptCount);
				}
				else
				{
					Debug.Log(string.Format("Localization status: {0}/{1}", m_stats.localizationSuccessCount, m_stats.localizationAttemptCount));
				}
			}
			m_bIsLocalizing = false;
		}

		#region ARSpace


		static public void RegisterSpace(Transform tr, int spaceId)
		{
			if (!m_Spaces.ContainsKey(spaceId))
				m_Spaces[spaceId] = new SpaceContainer();
			m_Spaces[spaceId].nodes.Add(tr);
		}

		static public void UnregisterSpace(Transform tr, int spaceId)
		{
			if (m_Spaces.ContainsKey(spaceId))
			{
				m_Spaces[spaceId].nodes.Remove(tr);
				if (m_Spaces[spaceId].nodes.Count == 0)
					m_Spaces.Remove(spaceId);
			}
		}

		private void UpdateSpace(Vector3 pos, Quaternion rot, int id)
		{
			if (m_Spaces.ContainsKey(id))
			{
				foreach (Transform tr in m_Spaces[id].nodes)
					tr.SetPositionAndRotation(pos, rot);
			}
		}
		#endregion
	}

	public class LocalizerStats
	{
		public int localizationAttemptCount = 0;
		public int localizationSuccessCount = 0;
	}
}