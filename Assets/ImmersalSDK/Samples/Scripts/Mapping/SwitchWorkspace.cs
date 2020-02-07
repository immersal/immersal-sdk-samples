/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using TMPro;

namespace Immersal.Samples.Mapping
{
    public class SwitchWorkspace : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI m_TextMeshProUGUI = null;
        [SerializeField]
        private Mapper m_Mapper = null;

        private void Start()
        {
            if (m_TextMeshProUGUI && m_Mapper)
            {
                int currentWorkspaceId = m_Mapper.currentBank;
                m_TextMeshProUGUI.text = currentWorkspaceId.ToString();
            }
        }

        public void Switch()
        {
            if (m_TextMeshProUGUI && m_Mapper)
            {
                int currentWorkspaceId = m_Mapper.SwitchBank(5);
                m_TextMeshProUGUI.text = currentWorkspaceId.ToString();
            }
        }
    }
}