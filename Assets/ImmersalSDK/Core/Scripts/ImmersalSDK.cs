/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using System;
using Immersal.AR;
using UnityEngine.XR.ARSubsystems;

namespace Immersal
{
	public class ImmersalSDK : MonoBehaviour
	{
		public static string sdkVersion = "1.10.0";
		public static bool isHWAR = false;

        public enum CameraResolution { Default, HD, FullHD, Max };	// With Huawei AR Engine SDK, only Default (640x480) and Max (1440x1080) are supported.
		private static ImmersalSDK instance = null;

		[Tooltip("SDK developer token")]
		public string developerToken;
		[SerializeField]
		[Tooltip("Application target frame rate")]
		private int m_TargetFrameRate = 60;
		[SerializeField]
		[Tooltip("Android resolution")]
		private CameraResolution m_AndroidResolution = CameraResolution.FullHD;
		[SerializeField]
		[Tooltip("iOS resolution")]
		private CameraResolution m_iOSResolution = CameraResolution.Default;
		[SerializeField]
		private UnityEvent onPoseLost = null;
		[SerializeField]
		private UnityEvent onPoseFound = null;

        public int secondsToDecayPose = 10;

		public LocalizerBase Localizer { get; set; }
		public int TrackingQuality { get; private set; }

		private ARCameraManager m_CameraManager;
		private ARSession m_ARSession;
        private bool m_bCamConfigDone = false;
		private string m_LocalizationServer = "https://api.immersal.com";
		private int m_PreviousResults = 0;
		private int m_CurrentResults = 0;
		private float m_LatestPoseUpdated = 0f;
		private int m_SLAMTrackingQuality = 0;
		private bool m_HasPose = false;
		private XRCameraConfiguration? m_InitialConfig;

		public int targetFrameRate
		{
			get { return m_TargetFrameRate; }
			set
			{
				m_TargetFrameRate = value;
				SetFrameRate();
			}
		}

		public CameraResolution androidResolution
		{
			get { return m_AndroidResolution; }
			set
			{
				m_AndroidResolution = value;
				ConfigureCamera();
			}
		}

		public CameraResolution iOSResolution
		{
			get { return m_iOSResolution; }
			set
			{
				m_iOSResolution = value;
				ConfigureCamera();
			}
		}

		public string localizationServer
		{
			get { return m_LocalizationServer; }
			set
			{
				m_LocalizationServer = value;
			}
		}

		public ARCameraManager cameraManager
		{
			get
			{
				if (m_CameraManager == null)
				{
					m_CameraManager = UnityEngine.Object.FindObjectOfType<ARCameraManager>();
				}
				return m_CameraManager;
			}
		}

		public ARSession arSession
		{
			get
			{
				if (m_ARSession == null)
				{
					m_ARSession = UnityEngine.Object.FindObjectOfType<ARSession>();
				}
				return m_ARSession;
			}
		}

		public static ImmersalSDK Instance
		{
			get
			{
#if UNITY_EDITOR
				if (instance == null && !Application.isPlaying)
				{
					instance = UnityEngine.Object.FindObjectOfType<ImmersalSDK>();
				}
#endif
				if (instance == null)
				{
					Debug.LogError("No ImmersalSDK instance found. Ensure one exists in the scene.");
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
				Debug.LogError("There must be only one ImmersalSDK object in a scene.");
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}

			if (developerToken != null && developerToken.Length > 0)
			{
				PlayerPrefs.SetString("token", developerToken);
			}
		}

		void Start()
		{
			SetFrameRate();
			onPoseLost?.Invoke();
		}

		private void SetFrameRate()
		{
			Application.targetFrameRate = targetFrameRate;
		}

		private void Update()
		{
			bool trackingQualityAvailable = false;

			if (ImmersalSDK.isHWAR)
			{
				#if HWAR
				trackingQualityAvailable = HWARHelper.TryGetTrackingQuality(out m_SLAMTrackingQuality);
				#endif
			}
			else
			{
				trackingQualityAvailable = ARHelper.TryGetTrackingQuality(out m_SLAMTrackingQuality);
			}

			if (trackingQualityAvailable && this.Localizer != null)
			{
				LocalizerStats stats = this.Localizer.stats;
				if (stats.localizationAttemptCount > 0)
				{
					int q = CurrentResults(stats.localizationSuccessCount);
					if (q > m_SLAMTrackingQuality)
						q = m_SLAMTrackingQuality;
					
					if (!m_HasPose && q > 1)
					{
						m_HasPose = true;
						onPoseFound?.Invoke();
					}

					if (m_HasPose && q < 1 && m_SLAMTrackingQuality == 0)
					{
						m_HasPose = false;
						onPoseLost?.Invoke();
					}

					this.TrackingQuality = q;
				}
			}
			
			if (!isHWAR)
			{
				if (!m_bCamConfigDone && cameraManager != null)
					ConfigureCamera();
			}
		}

		private void ConfigureCamera()
		{
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
			var cameraSubsystem = cameraManager.subsystem;
			if (cameraSubsystem == null || !cameraSubsystem.running)
				return;
			var configurations = cameraSubsystem.GetConfigurations(Allocator.Temp);
			if (!configurations.IsCreated || (configurations.Length <= 0))
				return;
			int bestError = int.MaxValue;
			var currentConfig = cameraSubsystem.currentConfiguration;
			int dw = (int)currentConfig?.width;
			int dh = (int)currentConfig?.height;
			if (dw == 0 && dh == 0)
				return;
#if UNITY_ANDROID
			CameraResolution reso = androidResolution;
#else
			CameraResolution reso = iOSResolution;
#endif

			if (!m_bCamConfigDone)
			{
				m_InitialConfig = currentConfig;
			}

			switch (reso)
			{
				case CameraResolution.Default:
					dw = (int)currentConfig?.width;
					dh = (int)currentConfig?.height;
					break;
				case CameraResolution.HD:
					dw = 1280;
					dh = 720;
					break;
				case CameraResolution.FullHD:
					dw = 1920;
					dh = 1080;
					break;
				case CameraResolution.Max:
					dw = 80000;
					dh = 80000;
					break;
			}

			foreach (var config in configurations)
			{
				int perror = config.width * config.height - dw * dh;
				if (Math.Abs(perror) < bestError)
				{
					bestError = Math.Abs(perror);
					currentConfig = config;
				}
			}

			if (reso != CameraResolution.Default) {
				Debug.Log("resolution = " + (int)currentConfig?.width + "x" + (int)currentConfig?.height);
				cameraSubsystem.currentConfiguration = currentConfig;
			}
			else
			{
				cameraSubsystem.currentConfiguration = m_InitialConfig;
			}
#endif
			m_bCamConfigDone = true;
		}

		int CurrentResults(int localizationResults) {
			int diffResults = localizationResults - m_PreviousResults;
			m_PreviousResults = localizationResults;
			if (diffResults > 0)
			{
				m_LatestPoseUpdated = Time.time;
				m_CurrentResults += diffResults;
				if (m_CurrentResults > 3)
				{
					m_CurrentResults = 3;
				}
			}
			else if (Time.time - m_LatestPoseUpdated > secondsToDecayPose)
			{
				m_LatestPoseUpdated = Time.time;
				if (m_CurrentResults > 0)
				{
					m_CurrentResults--;
				}
			}
				
			return m_CurrentResults;
		}
	}
}