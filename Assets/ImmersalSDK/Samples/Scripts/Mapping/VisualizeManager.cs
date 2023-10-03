/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Immersal.REST;
using Immersal.Samples.Mapping.ScrollList;

namespace Immersal.Samples.Mapping
{
    public class VisualizeManager : MonoBehaviour
    {
        public MappingUIComponent localizeButton = null;
        public static List<int> loadJobs = new List<int>();

        public event ItemSelected OnItemSelected = null;
        public event ItemDeleted OnItemDeleted = null;
        public event ItemRestored OnItemRestored = null;
        public event SelectorOpened OnSelectorOpened = null;
        public event SelectorClosed OnSelectorClosed = null;
        public delegate void ItemSelected(SDKJob job);
        public delegate void ItemDeleted(SDKJob job);
        public delegate void ItemRestored(SDKJob job, bool clear);
        public delegate void SelectorOpened();
        public delegate void SelectorClosed();

        [SerializeField]
        private MappingUIComponent m_OptionsButton = null;
        [SerializeField]
        private MappingUIComponent m_MapDownloadButton = null;
        [SerializeField]
        private MappingUIComponent m_InfoPanel = null;
        [SerializeField]
        private GameObject m_MapDownloadList = null;
        [SerializeField]
        private GameObject m_PromptDeleteMap = null;
        [SerializeField]
        private GameObject m_PromptRestoreMap = null;
        [SerializeField]
        private GameObject m_OptionsScrollList = null;
        [SerializeField]
        private GameObject m_AlignMapsPrompt = null;

        private SDKJob m_ActiveFunctionJob = default;

        private enum UIState { Default, MapList, Options, AlignMaps};
        private UIState currentUIState = UIState.Default;

        // for delete / restore
        public SDKJob activeFunctionJob
        {
            set { m_ActiveFunctionJob = value; }
        }

        public void SetMapListData(SDKJob[] data, List<int> activeMaps)
        {
            m_MapDownloadList.GetComponent<ScrollListControl>().SetData(data, activeMaps);
        }

        public void ResetMaps()
        {
            m_MapDownloadList.GetComponent<ScrollListControl>().ResetMaps();
        }

        public void OnListItemSelect(SDKJob job)
        {
            OnItemSelected?.Invoke(job);
        }

        public void ToggleDeletePrompt(bool on)
        {
            m_PromptDeleteMap.SetActive(on);
        }

        private void OnEnable()
        {
            ChangeState(UIState.Default);
        }

        public void ToggleRestoreMapImagesPrompt(bool on)
        {
            m_PromptRestoreMap.SetActive(on);
        }

        public void OnListItemDelete()
        {
            if (m_ActiveFunctionJob.id > 0)
            {
                OnItemDeleted?.Invoke(m_ActiveFunctionJob);
            }
        }

        public void OnListItemRestore(bool clear)
        {
            if (m_ActiveFunctionJob.id > 0)
            {
                OnItemRestored?.Invoke(m_ActiveFunctionJob, clear);
            }
        }

        public void MapList()
        {
            if (currentUIState == UIState.MapList)
            {
                ChangeState(UIState.Default);
            }
            else
            {
                ChangeState(UIState.MapList);
            }
        }

        public void AlignMapsPrompt()
        {
            if (currentUIState == UIState.AlignMaps)
            {
                ChangeState(UIState.Default);
            }
            else
            {
                ChangeState(UIState.AlignMaps);
            }
        }

        public void DefaultView()
        {
            ChangeState(UIState.Default);
        }

        public void Options()
        {
            if (currentUIState == UIState.Options)
            {
                ChangeState(UIState.Default);
            }
            else
            {
                ChangeState(UIState.Options);
            }
        }

        private void ChangeState(UIState state)
        {
            switch (state)
            {
                case UIState.Default:
                    m_InfoPanel.Activate();
                    localizeButton.Activate();
                    m_OptionsButton.Activate();
                    m_MapDownloadButton.Activate();
                    m_MapDownloadList.SetActive(false);
                    m_OptionsScrollList.SetActive(false);
                    m_AlignMapsPrompt.SetActive(false);
                    OnSelectorClosed?.Invoke();
                    break;
                case UIState.MapList:
                    m_InfoPanel.Disable();
                    localizeButton.Disable();
                    m_OptionsButton.Disable();
                    m_MapDownloadButton.Activate();
                    m_MapDownloadList.SetActive(true);
                    m_OptionsScrollList.SetActive(false);
                    m_AlignMapsPrompt.SetActive(false);
                    OnSelectorOpened?.Invoke();
                    break;
                case UIState.Options:
                    m_InfoPanel.Disable();
                    localizeButton.Disable();
                    m_OptionsButton.Activate();
                    m_MapDownloadButton.Disable();
                    m_MapDownloadList.SetActive(false);
                    m_OptionsScrollList.SetActive(true);
                    m_AlignMapsPrompt.SetActive(false);
                    break;
                case UIState.AlignMaps:
                    m_InfoPanel.Disable();
                    localizeButton.Disable();
                    m_OptionsButton.Activate();
                    m_MapDownloadButton.Disable();
                    m_MapDownloadList.SetActive(false);
                    m_OptionsScrollList.SetActive(false);
                    m_AlignMapsPrompt.SetActive(true);
                    break;
                default:
                    break;
            }

            currentUIState = state;
        }
    }
}