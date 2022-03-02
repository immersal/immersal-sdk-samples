/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using Immersal.AR;
using TMPro;

namespace Immersal.Samples.Mapping
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class GetCloudDebugStatistics : MonoBehaviour
    {
        private enum DebugMode {imageCount, inQueue, posesFound};
        private TextMeshProUGUI textMeshProUGUI;
        private ImmersalSDK m_Sdk;

        [SerializeField]
        private DebugMode debugMode = DebugMode.imageCount;

        [SerializeField]
        private string textAppend = "";
        private MapperBase mapper;

        void Start ()
        {
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            m_Sdk = ImmersalSDK.Instance;
            mapper = UnityEngine.Object.FindObjectOfType<MapperBase>();
        }
        
        void Update ()
        {
            if (mapper != null && textMeshProUGUI != null && m_Sdk != null)
            {
                MapperStats ms = mapper.stats;
                LocalizerStats ls = m_Sdk.Localizer.stats;

                switch (debugMode)
                {
                    case DebugMode.imageCount:
                        textMeshProUGUI.text = string.Format("{0} {1}", textAppend, ms.imageCount);
                        break;
                    case DebugMode.inQueue:
                        textMeshProUGUI.text = string.Format("{0} {1}", textAppend, ms.queueLen);
                        break;
                    case DebugMode.posesFound:
                        textMeshProUGUI.text = string.Format("{0} {1}/{2}", textAppend, ls.localizationSuccessCount, ls.localizationAttemptCount);
                        break;
                }
            }
        }
    }
}
