/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using NRKernal;
using Immersal.AR;

namespace Immersal.Samples.Util
{
	[RequireComponent(typeof(Image))]
	public class NRPoseIndicator : MonoBehaviour {

        public enum IndicatorMode { multiplyColor, changeSprite };
        public IndicatorMode indicatorMode = IndicatorMode.multiplyColor;
        public int secondsToDecayPose = 10;

        public Color noPose;
		public Color weakPose;
		public Color goodPose;
		public Color excellentPose;

        public Sprite noPoseSprite;
        public Sprite weakPoseSprite;
        public Sprite goodPoseSprite;
        public Sprite excellentPoseSprite;

		public UnityEvent onPoseLost = null;
		public UnityEvent onPoseFound = null;

        public int TrackingQuality { get; private set; }

		private Image m_Image;
		private ImmersalSDK m_Sdk;
		private bool m_HasPose = false;
		private int m_PreviousResults = 0;
		private int m_CurrentResults = 0;
		private int q = 0;
		private float m_LatestPoseUpdated = 0f;

		void Start () {
			m_Image = GetComponent<Image>();
			m_Sdk = ImmersalSDK.Instance;

            if (indicatorMode == IndicatorMode.multiplyColor)
            {
                m_Image.color = noPose;
            }
        }

		void Update ()
		{
            if (m_Sdk.Localizer != null && NRFrame.SessionStatus == SessionState.Running)
            {
				LocalizerStats stats = m_Sdk.Localizer.stats;
				if (stats.localizationAttemptCount > 0)
				{
					q = CurrentResults(stats.localizationSuccessCount);
					
					if (!m_HasPose && q > 1)
					{
						m_HasPose = true;
						onPoseFound?.Invoke();
					}

					if (m_HasPose && q < 1 && !m_Sdk.Localizer.isTracking)
					{
						m_HasPose = false;
						m_Sdk.Localizer.Reset();
						m_PreviousResults = 0;
						m_CurrentResults = 0;
						onPoseLost?.Invoke();
					}

					TrackingQuality = q;
				}
            }

			if (indicatorMode == IndicatorMode.multiplyColor)
			{
				switch (TrackingQuality)
				{
					case 0:
						m_Image.color = noPose;
						break;
					case 1:
						m_Image.color = weakPose;
						break;
					case 2:
						m_Image.color = goodPose;
						break;
					default:
						m_Image.color = excellentPose;
						break;
				}
			}
			else if (indicatorMode == IndicatorMode.changeSprite)
			{
				switch (TrackingQuality)
				{
					case 0:
						m_Image.sprite = noPoseSprite;
						break;
					case 1:
						m_Image.sprite = weakPoseSprite;
						break;
					case 2:
						m_Image.sprite = goodPoseSprite;
						break;
					default:
						m_Image.sprite = excellentPoseSprite;
						break;
				}
			}
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
