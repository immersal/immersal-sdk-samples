/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using Immersal.AR;

namespace Immersal.Samples.Util
{
	[RequireComponent(typeof(Image))]
	public class PoseColorSolver : MonoBehaviour {

        public enum IndicatorMode { multiplyColor, changeSprite };
        public IndicatorMode indicatorMode = IndicatorMode.multiplyColor;

        public Color noPose;
		public Color weakPose;
		public Color goodPose;
		public Color excellentPose;

        public Sprite noPoseSprite;
        public Sprite weakPoseSprite;
        public Sprite goodPoseSprite;
        public Sprite excellentPoseSprite;

        public int secondsToDecayPose = 10;

		[SerializeField]
		private UnityEvent onPoseLost = null;
		[SerializeField]
		private UnityEvent onPoseFound = null;

		private Image m_Image;
		private int m_PreviousResults = 0;
		private int m_CurrentResults = 0;

		private float m_LatestPoseUpdated = 0f;
		private int m_TrackingQuality = 0;
		private bool m_HasPose = false;
		private ImmersalSDK m_Sdk;
		private ARLocalizer m_Localizer;

		void Start () {
			m_Sdk = ImmersalSDK.Instance;
			m_Localizer = m_Sdk.gameObject.GetComponent<ARLocalizer>();
			m_Image = GetComponent<Image> ();

            if (indicatorMode == IndicatorMode.multiplyColor)
            {
                m_Image.color = noPose;
            }

			onPoseLost?.Invoke();
        }

		void Update () {
			if (m_Sdk.arSession == null)
				return;
			
			var arSubsystem = m_Sdk.arSession.subsystem;
			
			if (arSubsystem != null && arSubsystem.running)
			{
				switch (arSubsystem.trackingState)
				{
					case TrackingState.Tracking:
						m_TrackingQuality = 4;
						break;
					case TrackingState.Limited:
						m_TrackingQuality = 1;
						break;
					case TrackingState.None:
						m_TrackingQuality = 0;
						break;
				}
			}

			LocalizerStats stats = m_Localizer.stats;
			if (stats.localizationAttemptCount > 0)
			{
				int q = CurrentResults(stats.localizationSuccessCount);
				if (q > m_TrackingQuality)
					q = m_TrackingQuality;
				
				if (!m_HasPose && q > 1)
				{
					m_HasPose = true;
					onPoseFound?.Invoke();
				}

				if (m_HasPose && q < 1 && m_TrackingQuality == 0)
				{
					m_HasPose = false;
					onPoseLost?.Invoke();
				}

                if (indicatorMode == IndicatorMode.multiplyColor)
                {
                    switch (q)
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
                    switch (q)
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
