/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using System;
using TMPro;
using UnityEngine;

public class SetVersionInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_VersionText;
    void Start()
    {
        string versionText = string.Format("{0} v{1}", Application.productName, Application.version);
        string copyrightText = string.Format("© {0} {1} All Rights Reserved.", DateTime.Now.Year, Application.companyName);

        m_VersionText.text = versionText + Environment.NewLine + copyrightText;
    }
}
