/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.2.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.Mapping
{
    public class ToggleMappingMode : MonoBehaviour
    {
        public GameObject mappingUIPrefab;
        private GameObject m_mappingUI;

        public GameObject MappingUI
        {
            get
            {
                return m_mappingUI;
            }
        }

        public void EnableMappingMode()
        {
            if (m_mappingUI == null)
            {
                m_mappingUI = Instantiate(mappingUIPrefab);
            }
            else
            {
                m_mappingUI.SetActive(true);
            }
        }

        public void DisableMappingMode()
        {
            if (m_mappingUI != null)
            {
                m_mappingUI.SetActive(false);
            }
        }
    }
}
