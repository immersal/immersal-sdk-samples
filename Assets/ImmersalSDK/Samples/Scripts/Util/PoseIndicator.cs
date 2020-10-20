/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.UI;

namespace Immersal.Samples.Util
{
	[RequireComponent(typeof(Image))]
	public class PoseIndicator : MonoBehaviour {

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

		private Image m_Image;
		private ImmersalSDK m_Sdk;

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
			int q = m_Sdk.TrackingQuality;

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
}
