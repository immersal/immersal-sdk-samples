/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.Mapping
{
    public class ToggleMappingMode : MonoBehaviour
    {
        public GameObject mappingUIPrefab;
        private GameObject m_MappingUI;

        public GameObject MappingUI
        {
            get { return m_MappingUI; }
        }

        public void EnableMappingMode()
        {
            if (m_MappingUI == null)
            {
                m_MappingUI = Instantiate(mappingUIPrefab);
            }
            else
            {
                m_MappingUI.SetActive(true);
            }
        }

        public void DisableMappingMode()
        {
            if (m_MappingUI != null)
            {
                m_MappingUI.SetActive(false);
            }
        }
    }
}
