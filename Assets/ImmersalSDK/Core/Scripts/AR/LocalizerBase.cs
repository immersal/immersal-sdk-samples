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
using TMPro;
using Immersal.REST;

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
		[Tooltip("Start localizing at app startup")]
		[SerializeField]
		protected bool m_AutoStart = true;
		[Tooltip("Time between localization requests in seconds")]
		public float localizationInterval = 2.0f;
		[Tooltip("Filter localizer poses for smoother results")]
		[SerializeField]
		protected bool m_UseFiltering = true;
		[Tooltip("Reset localizer filtering when relocalized against a different map than the previous time")]
		[SerializeField]
		protected bool m_ResetOnMapChange = false;
		[Tooltip("Downsample image to HD resolution")]
		[SerializeField]
		protected bool m_Downsample = true;
		[Tooltip("Try to localize at maximum speed at app startup / resume")]
		[SerializeField]
		protected bool m_BurstMode = true;
		[Tooltip("Use the on-server localizer")]
		[SerializeField]
		protected bool m_UseServerLocalizer = false;
		[Tooltip("Optional server map IDs when the on-server localizer is used")]
		[SerializeField]
		protected SDKMapId[] m_ServerMapIds = new SDKMapId[] { };

		public int lastLocalizedMapHandle { get; protected set; }
        public bool isTracking { get; protected set; }
        public bool isLocalizing { get; protected set; }

		protected LocalizerStats m_Stats = new LocalizerStats();
		protected ImmersalSDK m_Sdk = null;
		protected IntPtr m_PixelBuffer = IntPtr.Zero;
		protected float m_LastLocalizeTime = 0.0f;
		protected float m_BurstStartTime = 0.0f;
		protected bool m_BurstModeActive = false;
		protected bool m_LocalizeContinuously = false;
		protected Camera m_Cam = null;
		protected float m_WarpThresholdDistSq = 5.0f * 5.0f;
		protected float m_WarpThresholdCosAngle = Mathf.Cos(20.0f * Mathf.PI / 180.0f);

        public bool downsample
		{
			get { return m_Downsample; }
			set
			{
				m_Downsample = value;
				SetDownsample();
			}
		}

		public bool burstMode
		{
			get { return m_BurstMode; }
			set
			{
				SetBurstMode(value);
			}
		}

		public bool useFiltering
		{
			get { return m_UseFiltering; }
			set { m_UseFiltering = value; }
		}

		public bool resetOnMapChange
		{
			get { return m_ResetOnMapChange; }
			set { m_ResetOnMapChange = value; }
		}

		public bool autoStart
		{
			get { return m_AutoStart; }
			set
			{
				SetContinuousLocalization(value);
			}
		}

		public bool useServerLocalizer
		{
			get { return m_UseServerLocalizer; }
			set { m_UseServerLocalizer = value; }
		}

		public SDKMapId[] serverMapIds
		{
			get { return m_ServerMapIds; }
			set { m_ServerMapIds = value; }
		}

		public LocalizerStats stats
		{
			get { return m_Stats; }
		}

		public virtual void Start()
		{
			m_Sdk = ImmersalSDK.Instance;
			lastLocalizedMapHandle = -1;
#if !UNITY_EDITOR
			SetDownsample();
#endif
			SetBurstMode(burstMode);
			SetContinuousLocalization(autoStart);
		}

		public virtual void OnEnable()
		{
			m_Cam = Camera.main;
		}

		public virtual void OnDisable()
		{
			isTracking = false;
		}

		public virtual void OnDestroy()
		{
			m_PixelBuffer = IntPtr.Zero;
		}

		public virtual void OnApplicationPause(bool pauseStatus)
		{
			Reset();
			
			if (!pauseStatus)
				SetBurstMode(burstMode);
		}

		public virtual void Reset()
		{
			lastLocalizedMapHandle = -1;

			foreach (KeyValuePair<Transform, SpaceContainer> item in ARSpace.transformToSpace)
				item.Value.filter.ResetFiltering();
		}

		public virtual void StartLocalizing()
		{
			stats.localizationAttemptCount = stats.localizationSuccessCount = 0;

			Reset();
			SetBurstMode(burstMode);
			SetContinuousLocalization(autoStart);
		}

		public virtual void StopLocalizing()
		{
			SetContinuousLocalization(false);
			Reset();
		}

		public virtual void Pause()
		{
			SetContinuousLocalization(false);
		}

		public virtual void Resume()
		{
			SetContinuousLocalization(true);
		}

		public virtual void LocalizeServer(SDKMapId[] mapIds)
		{
			isLocalizing = false;
		}

		public virtual void Localize()
		{
			Debug.Log(string.Format("Successful localizations: {0}/{1}", stats.localizationSuccessCount, stats.localizationAttemptCount));
			isLocalizing = false;
		}

		protected virtual void Update()
		{
			if (!m_LocalizeContinuously)
				return;
			
			if (ARSpace.transformToSpace.Count == 0)
			{
				m_BurstStartTime = Time.unscaledTime;
				return;
			}
			
			if (useFiltering)
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
					ARSpace.UpdateSpace(item.Value, item.Value.targetPosition, item.Value.targetRotation);
				}
			}

			float curTime = Time.unscaledTime;
			if (m_BurstModeActive)	// try to localize at max speed during app start/resume
			{
				if (!isLocalizing && isTracking)
				{
					float elapsedTime = curTime - m_BurstStartTime;
					isLocalizing = true;
					if (useServerLocalizer && serverMapIds.Length > 0)
					{
						LocalizeServer(serverMapIds);
					}
					else
					{
						Localize();
					}
					if (stats.localizationSuccessCount == 10 || elapsedTime >= 15f)
					{
						m_BurstModeActive = false;
					}
				}
			}

			if (!isLocalizing && isTracking && (curTime - m_LastLocalizeTime) >= localizationInterval)
			{
				m_LastLocalizeTime = curTime;
				isLocalizing = true;
				if (useServerLocalizer && serverMapIds.Length > 0)
				{
					LocalizeServer(serverMapIds);
				}
				else
				{
					Localize();
				}
			}
		}

		private void SetDownsample()
		{
			if (downsample)
			{
				Immersal.Core.SetInteger("LocalizationMaxPixels", 1280*720);
			}
		}

		private void SetBurstMode(bool on)
		{
			m_BurstStartTime = Time.unscaledTime;
			m_BurstModeActive = on;
		}

		private void SetContinuousLocalization(bool on)
		{
			m_LocalizeContinuously = on;
		}

		public static void GetLocalizerPose(out LocalizerPose localizerPose, int mapHandle, Vector3 pos, Quaternion rot, Matrix4x4 m, double[] mapToEcef = null)
		{
			localizerPose = default;

			if (mapToEcef == null)
			{
				mapToEcef = new double[13];
				Immersal.Core.MapToEcefGet(mapToEcef, mapHandle);
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
 	}
}