/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using UnityEngine;
using TMPro;
using Immersal;

namespace Immersal.Samples.Util
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizationStatus : MonoBehaviour
    {
        private const string StringFormat = "Successful localizations: {0}/{1}";

        private TextMeshProUGUI m_LabelText;
        private ImmersalSDK m_Sdk;

        void Start()
        {
            m_LabelText = GetComponent<TextMeshProUGUI>();
            m_Sdk = ImmersalSDK.Instance;
        }

        void Update()
        {
            if (m_Sdk.Localizer == null)
                return;
            
            m_LabelText.text = string.Format(StringFormat, m_Sdk.Localizer.stats.localizationSuccessCount, m_Sdk.Localizer.stats.localizationAttemptCount);
        }
    }
}
