/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using TMPro;

namespace Immersal.Samples.Util
{
    public class DebugCanvas : MonoBehaviour
    {
        public TextMeshProUGUI field0 = null;
        public TextMeshProUGUI field1 = null;
        public TextMeshProUGUI field2 = null;

        public static DebugCanvas Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = UnityEngine.Object.FindObjectOfType<DebugCanvas>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No DebugCanvas instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }

        private static DebugCanvas instance = null;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("There must be only one DebugCanvas object in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }

            if (field0 != null && field1 != null && field2 != null)
            {
                field0.text = "";
                field1.text = "";
                field2.text = "";
            }
        }
    }
}