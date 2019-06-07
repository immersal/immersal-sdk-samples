﻿/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Immersal.Samples.Mapping
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class GetCloudDebugStatistics : MonoBehaviour
    {
        private enum DebugMode {imageCount, inQueue, posesFound};
        private TextMeshProUGUI textMeshProUGUI;

        [SerializeField]
        private DebugMode debugMode = DebugMode.imageCount;

        [SerializeField]
        private string textAppend = "";
        private Mapper mapper;

        void Start ()
        {
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            this.mapper = UnityEngine.Object.FindObjectOfType<Mapper>();
        }
        
        void Update ()
        {
            if (mapper != null && textMeshProUGUI != null)
            {
                MapperStats s = mapper.Stats();

                switch (debugMode)
                {
                    case DebugMode.imageCount:
                        textMeshProUGUI.text = string.Format("{0} {1}", textAppend, s.imageCount);
                        break;
                    case DebugMode.inQueue:
                        textMeshProUGUI.text = string.Format("{0} {1}", textAppend, s.queueLen);
                        break;
                    case DebugMode.posesFound:
                        textMeshProUGUI.text = string.Format("{0} {1}/{2}", textAppend, s.locSucc, s.locFail);
                        break;
                }
            }
        }
    }
}
