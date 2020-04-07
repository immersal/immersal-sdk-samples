/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.Util
{
    public class ActivateFromOrientation : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup m_portraitGroup = null;
        [SerializeField]
        private CanvasGroup m_landscapeLeftGroup = null;
        [SerializeField]
        private CanvasGroup m_landscapeRightGroup = null;

        private DeviceOrientation m_previousOrientation;

#if !UNITY_EDITOR
        private void Start()
        {
            UpdateOrientation();
        }

        private void Update()
        {
            UpdateOrientation();
        }
#endif
        private void UpdateOrientation()
        {
            if (m_portraitGroup == null || m_landscapeLeftGroup == null || m_landscapeRightGroup == null)
                return;

            DeviceOrientation orientation = Input.deviceOrientation;

            if (orientation == DeviceOrientation.Unknown)
                return;

            if (orientation != m_previousOrientation)
            {
                m_previousOrientation = orientation;

                switch (orientation)
                {
                    case DeviceOrientation.LandscapeLeft:
                        m_landscapeLeftGroup.gameObject.SetActive(true);
                        m_landscapeRightGroup.gameObject.SetActive(false);
                        m_portraitGroup.gameObject.SetActive(false);
                        break;
                    case DeviceOrientation.LandscapeRight:
                        m_landscapeLeftGroup.gameObject.SetActive(false);
                        m_landscapeRightGroup.gameObject.SetActive(true);
                        m_portraitGroup.gameObject.SetActive(false);
                        break;
                    default:
                        m_landscapeLeftGroup.gameObject.SetActive(false);
                        m_landscapeRightGroup.gameObject.SetActive(false);
                        m_portraitGroup.gameObject.SetActive(true);
                        break;
                }
            }
        }
    }
}