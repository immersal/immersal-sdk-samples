/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
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
        private enum CharacterSet {alphanumeric, number, integer};

        [SerializeField]
        private CharacterSet characterSet = CharacterSet.alphanumeric;

        void Start()
        {
            TMP_InputField inputField = GetComponent<TMP_InputField>();
            switch (characterSet)
            {
                case CharacterSet.alphanumeric:
                    inputField.contentType = TMP_InputField.ContentType.Alphanumeric;
                    break;
                case CharacterSet.number:
                    inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                    break;
                case CharacterSet.integer:
                    inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                    break;
                default:
                    inputField.contentType = TMP_InputField.ContentType.Alphanumeric;
                    break;
            }
        }
    }
}