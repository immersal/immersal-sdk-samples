/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using TMPro;
using UnityEngine;

public class SetVersionInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_VersionText;
    void Start()
    {
        string versionText = Application.productName+" v" + Application.version;
        string copyrightText = "© " + DateTime.Now.Year + " "+Application.companyName+" All Rights Reserved.";

        m_VersionText.text = versionText + Environment.NewLine + copyrightText;
    }
}
