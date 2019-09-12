/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.1.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
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

		private Image m_image;
		private int m_previousResults = 0;
		private int m_currentResults = 0;

		private float m_latestPoseUpdated = 0f;
		private int m_trackingQuality = 0;
		private bool m_hasPose = false;
		private ImmersalARCloudSDK m_sdk;
		private ARLocalizer m_localizer;

		void Start () {
			m_sdk = ImmersalARCloudSDK.Instance;
			m_localizer = m_sdk.gameObject.GetComponent<ARLocalizer>();
			m_image = GetComponent<Image> ();

            if (indicatorMode == IndicatorMode.multiplyColor)
            {
                m_image.color = noPose;
            }

			onPoseLost.Invoke();
        }

		void Update () {
			if (m_sdk.arSession == null)
				return;
			
			var arSubsystem = m_sdk.arSession.subsystem;
			
			if (arSubsystem != null && arSubsystem.running)
			{
				switch (arSubsystem.trackingState)
				{
					case TrackingState.Tracking:
						m_trackingQuality = 4;
						break;
					case TrackingState.Limited:
						m_trackingQuality = 1;
						break;
					case TrackingState.None:
						m_trackingQuality = 0;
						break;
				}
			}

			LocalizerStats stats = m_localizer.stats;
			if (stats.localizationAttemptCount > 0)
			{
				int q = CurrentResults(stats.localizationSuccessCount);
				if (q > m_trackingQuality)
					q = m_trackingQuality;
				
				if (!m_hasPose && q > 1)
				{
					m_hasPose = true;
					if (onPoseFound != null)
						onPoseFound.Invoke();
				}

				if (m_hasPose && q < 1 && m_trackingQuality == 0)
				{
					m_hasPose = false;
					if (onPoseLost != null)
						onPoseLost.Invoke();
				}

                if (indicatorMode == IndicatorMode.multiplyColor)
                {
                    switch (q)
                    {
                        case 0:
                            m_image.color = noPose;
                            break;
                        case 1:
                            m_image.color = weakPose;
                            break;
                        case 2:
                            m_image.color = goodPose;
                            break;
                        default:
                            m_image.color = excellentPose;
                            break;
                    }
                }
                else if (indicatorMode == IndicatorMode.changeSprite)
                {
                    switch (q)
                    {
                        case 0:
                            m_image.sprite = noPoseSprite;
                            break;
                        case 1:
                            m_image.sprite = weakPoseSprite;
                            break;
                        case 2:
                            m_image.sprite = goodPoseSprite;
                            break;
                        default:
                            m_image.sprite = excellentPoseSprite;
                            break;
                    }
                }
            }
		}

		int CurrentResults(int localizationResults) {
			int diffResults = localizationResults - m_previousResults;
			m_previousResults = localizationResults;
			if (diffResults > 0) {
				m_latestPoseUpdated = Time.time;
				m_currentResults += diffResults;
				if (m_currentResults > 3)
					m_currentResults = 3;
			} else if (Time.time - m_latestPoseUpdated > secondsToDecayPose) {
				m_latestPoseUpdated = Time.time;
				if (m_currentResults > 0) {
					m_currentResults--;
				}
			}
				
			return m_currentResults;
		}
	}
}
