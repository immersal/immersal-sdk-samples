/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Immersal.AR;
using Immersal.REST;

namespace Immersal.Samples
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class ResolutionListController : MonoBehaviour
    {
        private TMP_Dropdown m_Dropdown;
        private ImmersalSDK m_Sdk;

        void Awake()
        {
            m_Dropdown = GetComponent<TMP_Dropdown>();
            m_Dropdown.ClearOptions();
        }

        void Start()
        {
            m_Sdk = ImmersalSDK.Instance;

            List<string> modes = new List<string>();

            foreach (ImmersalSDK.CameraResolution reso in Enum.GetValues(typeof(ImmersalSDK.CameraResolution)))
            {
                modes.Add(reso.ToString());
            }

            m_Dropdown.AddOptions(modes);
        }

        public void OnValueChanged(TMP_Dropdown dropdown)
        {
            var values = Enum.GetValues(typeof(ImmersalSDK.CameraResolution));
            ImmersalSDK.CameraResolution camReso = (ImmersalSDK.CameraResolution)values.GetValue((long)dropdown.value);
#if UNITY_ANDROID
            m_Sdk.androidResolution = camReso;
#elif UNITY_IOS
            m_Sdk.iOSResolution = camReso;
#endif
        }
    }
}
