/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.1.

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
    public class SwitchWorkspace : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI m_textMeshProUGUI = null;
        [SerializeField]
        private Mapper m_mapper = null;

        private void Start()
        {
            if (m_textMeshProUGUI && m_mapper)
            {
                int current_workspace_id = m_mapper.GetCurrentBank();
                m_textMeshProUGUI.text = current_workspace_id.ToString();
            }
        }

        public void Switch()
        {
            if (m_textMeshProUGUI && m_mapper)
            {
                int current_workspace_id = m_mapper.SwitchBank(5);
                m_textMeshProUGUI.text = current_workspace_id.ToString();
            }
        }
    }
}