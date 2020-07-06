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
using TMPro;

namespace Immersal.AR
{
	public class LocalizerStats
	{
		public int localizationAttemptCount = 0;
		public int localizationSuccessCount = 0;
	}

	public struct LocalizerPose
	{
		public bool valid;
		public double[] mapToEcef;
		public Matrix4x4 matrix;
		public Pose lastUpdatedPose;
		public double vLatitude;
		public double vLongitude;
		public double vAltitude;
	}

    public abstract class LocalizerBase : MonoBehaviour
	{
		[Tooltip("Time between localization requests in seconds")]
		[SerializeField]
		protected float m_LocalizationInterval = 2.0f;
		[Tooltip("Downsample image to HD resolution")]
		[SerializeField]
		protected bool m_Downsample = false;
		[Tooltip("Reset localizer filtering when relocalized against a different map than the previous time")]
		[SerializeField]
		protected bool m_ResetOnMapChange = false;
		[SerializeField]
		protected TextMeshProUGUI m_DebugText = null;

		protected LocalizerStats m_Stats = new LocalizerStats();
		protected ImmersalSDK m_Sdk = null;
		protected float m_LastLocalizeTime = 0.0f;
		protected Camera m_Cam = null;
		protected float m_WarpThresholdDistSq = 5.0f * 5.0f;
		protected float m_WarpThresholdCosAngle = Mathf.Cos(20.0f * Mathf.PI / 180.0f);

		public int lastLocalizedMapId { get; protected set; }
        public bool isTracking { get; protected set; }
        public bool isLocalizing { get; protected set; }
        public bool highFrequencyMode { get; protected set; }

        private static LocalizerBase instance = null;
        public static LocalizerBase Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = FindObjectOfType<LocalizerBase>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No Localizer instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }

        public bool downsample
		{
			get { return m_Downsample; }
			set
			{
				m_Downsample = value;
				SetDownsample();
			}
		}

        public float localizationInterval
        {
            get { return m_LocalizationInterval; }
            set { m_LocalizationInterval = value; }
        }

		public bool resetOnMapChange
		{
			get { return m_ResetOnMapChange; }
			set { m_ResetOnMapChange = value; }
		}

		public LocalizerStats stats
		{
			get { return m_Stats; }
		}

        public virtual IEnumerator Localize()
		{
			if (m_DebugText != null)
			{
				m_DebugText.text = string.Format("Successful localizations: {0}/{1}", stats.localizationSuccessCount, stats.localizationAttemptCount);
			}
			else
			{
				Debug.Log(string.Format("Successful localizations: {0}/{1}", stats.localizationSuccessCount, stats.localizationAttemptCount));
			}

			isLocalizing = false;
			
			yield break;
		}

		public virtual void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			if (instance != this)
			{
				Debug.LogError("There must be only one Localizer object in a scene.");
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}
		}

		public virtual void Start()
		{
			m_Sdk = ImmersalSDK.Instance;
			lastLocalizedMapId = -1;
#if !UNITY_EDITOR
			SetDownsample();
#endif
		}

		public virtual void OnEnable()
		{
			m_Cam = Camera.main;
			highFrequencyMode = true;
		}

		public virtual void OnDisable()
		{
			isTracking = false;
		}

		public virtual void OnApplicationPause(bool pauseStatus)
		{
			foreach (KeyValuePair<Transform, SpaceContainer> item in ARSpace.transformToSpace)
				item.Value.filter.ResetFiltering();
			
			if (!pauseStatus)
				highFrequencyMode = true;
		}

		public virtual void Update()
		{
			foreach (KeyValuePair<Transform, SpaceContainer> item in ARSpace.transformToSpace)
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
			if (highFrequencyMode)	// try to localize at max speed during app start/resume
			{
				if (!isLocalizing && isTracking)
				{
					float elapsedTime = Time.realtimeSinceStartup - m_LastLocalizeTime;
					isLocalizing = true;
					StartCoroutine(Localize());
					if (stats.localizationSuccessCount == 10 || elapsedTime >= 15f)
					{
						highFrequencyMode = false;
					}
				}
			}

			if (!isLocalizing && isTracking && (curTime-m_LastLocalizeTime) >= m_LocalizationInterval)
			{
				m_LastLocalizeTime = curTime;
				isLocalizing = true;
				StartCoroutine(Localize());
			}
		}

		private void SetDownsample()
		{
			if (downsample)
			{
				Immersal.Core.SetInteger("LocalizationMaxPixels", 1280*720);
			}
		}

		public static void GetLocalizerPose(out LocalizerPose localizerPose, int mapId, Vector3 pos, Quaternion rot, Matrix4x4 m, double[] mapToEcef = null)
		{
			localizerPose = default;

			if (mapToEcef == null)
			{
				mapToEcef = new double[13];
				Immersal.Core.MapToEcefGet(mapToEcef, mapId);
			}

			double[] wgs84 = new double[3];
			int r = Immersal.Core.PosMapToWgs84(wgs84, pos, mapToEcef);

			if (r == 0)
			{
				localizerPose.valid = true;
				localizerPose.mapToEcef = mapToEcef;
				localizerPose.matrix = m;
				localizerPose.lastUpdatedPose = new Pose(pos, rot);
				localizerPose.vLatitude = wgs84[0];
				localizerPose.vLongitude = wgs84[1];
				localizerPose.vAltitude = wgs84[2];
			}
		}

		#region ARSpace

		protected void UpdateSpace(Vector3 pos, Quaternion rot, Transform tr)
        {
    		tr.SetPositionAndRotation(pos, rot);
		}

		#endregion
 	}
}