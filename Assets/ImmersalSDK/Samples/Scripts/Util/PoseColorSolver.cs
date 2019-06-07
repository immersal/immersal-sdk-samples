using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;
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
		private bool m_firstTime = true;
		private bool m_hasPose = false;
		private ImmersalARCloudSDK m_sdk;
		private ARLocalizer m_localizer;

		void Start () {
			m_image = GetComponent<Image> ();
			m_localizer = UnityEngine.Object.FindObjectOfType<ARLocalizer>();

            if (indicatorMode == IndicatorMode.multiplyColor)
            {
                m_image.color = noPose;
            }

			m_sdk = ImmersalARCloudSDK.Instance;

			onPoseLost.Invoke();
        }

		public void ARFrameUpdated(FrameReceivedEventArgs args) {
			switch (ARSubsystemManager.sessionSubsystem.TrackingState)
			{
				case TrackingState.Tracking:
					m_trackingQuality = 4;
					break;
				case TrackingState.Unknown:
					m_trackingQuality = 1;
					break;
				case TrackingState.Unavailable:
					m_trackingQuality = 0;
					break;
			}
		}

		void OnDestroy() {
			if (ARSubsystemManager.cameraSubsystem != null)
				ARSubsystemManager.cameraSubsystem.FrameReceived -= ARFrameUpdated;
		}

		void Update () {

			if (m_firstTime && ARSubsystemManager.cameraSubsystem != null) {
				ARSubsystemManager.cameraSubsystem.FrameReceived += ARFrameUpdated;
				m_firstTime = false;
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
