/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using TMPro;

namespace Immersal.Samples.Mapping
{
    [RequireComponent(typeof(TMP_InputField))]
    public class RestrictInputText : MonoBehaviour
    {
        void Start()
        {
            GetComponent<TMP_InputField>().contentType = TMP_InputField.ContentType.Alphanumeric;
        }
    }
}