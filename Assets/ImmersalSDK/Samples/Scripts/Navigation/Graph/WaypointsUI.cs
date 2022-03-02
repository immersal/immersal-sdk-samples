/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Immersal.Samples.Navigation
{
    public class WaypointsUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_EditModePanel = null;
        [SerializeField]
        private GameObject m_EditModeButton = null;
        [SerializeField]
        private GameObject m_SettingsButton = null;
        [SerializeField]
        private GameObject m_SettingsPanel = null;

        private enum UIState { Navigation, EditMode, Settings };
        private UIState m_State = UIState.Navigation;

        void Start()
        {
            ChangeState(UIState.Navigation);
        }

        private void InitializeUI()
        {

        }

        public void ToggleEditMode()
        {
            if (m_State == UIState.EditMode)
            {
                m_State = UIState.Navigation;
            }
            else
            {
                m_State = UIState.EditMode;
            }
            ChangeState(m_State);
        }

        public void ToggleSettingsPanel()
        {
            if (m_State == UIState.Settings)
            {
                m_State = UIState.Navigation;
            }
            else
            {
                m_State = UIState.Settings;
            }
            ChangeState(m_State);
        }


        private void ChangeState(UIState state)
        {
            switch (state)
            {
                case UIState.Navigation:
                    Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode = false;
                    m_SettingsPanel.SetActive(false);
                    m_EditModePanel.SetActive(false);
                    m_SettingsButton.SetActive(true);
                    m_EditModeButton.SetActive(true);
                    break;
                case UIState.EditMode:
                    Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode = true;
                    m_SettingsPanel.SetActive(false);
                    m_EditModePanel.SetActive(true);
                    m_SettingsButton.SetActive(true);
                    m_EditModeButton.SetActive(true);
                    break;
                case UIState.Settings:
                    Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode = false;
                    m_SettingsPanel.SetActive(true);
                    m_EditModePanel.SetActive(false);
                    m_SettingsButton.SetActive(true);
                    m_EditModeButton.SetActive(true);
                    break;
                default:
                    break;
            }
        }
    }
}