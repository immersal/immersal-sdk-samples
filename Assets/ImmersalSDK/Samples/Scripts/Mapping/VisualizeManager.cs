/*===============================================================================
Copyright (C) 2021 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

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
        private UIState uiState = UIState.Default;

        // for delete / restore
        public SDKJob activeFunctionJob
        {
            set { m_ActiveFunctionJob = value; }
        }

        public void SetMapListData(SDKJob[] data, List<int> activeMaps)
        {
            m_MapDownloadList.GetComponent<ScrollListControl>().SetData(data, activeMaps);
        }

        public void OnListItemSelect(SDKJob job)
        {
            OnItemSelected?.Invoke(job);
        }

        public void ToggleDeletePrompt(bool on)
        {
            m_PromptDeleteMap.SetActive(on);
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
            if (uiState == UIState.MapList)
            {
                uiState = UIState.Default;
                OnSelectorClosed?.Invoke();
            }
            else
            {
                uiState = UIState.MapList;
                OnSelectorOpened?.Invoke();
            }
            ChangeState(uiState);
        }

        public void AlignMapsPrompt()
        {
            if (uiState == UIState.AlignMaps)
            {
                uiState = UIState.Default;
            }
            else
            {
                uiState = UIState.AlignMaps;
            }
            ChangeState(uiState);
        }

        public void DefaultView()
        {
            uiState = UIState.Default;
            ChangeState(uiState);
        }

        public void Options()
        {
            if (uiState == UIState.Options)
            {
                uiState = UIState.Default;
            }
            else
            {
                uiState = UIState.Options;
            }
            ChangeState(uiState);
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
                    break;
                case UIState.MapList:
                    m_InfoPanel.Disable();
                    localizeButton.Disable();
                    m_OptionsButton.Disable();
                    m_MapDownloadButton.Activate();
                    m_MapDownloadList.SetActive(true);
                    m_OptionsScrollList.SetActive(false);
                    m_AlignMapsPrompt.SetActive(false);
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
        }

        private void Start()
        {
            ChangeState(uiState);
        }
    }
}