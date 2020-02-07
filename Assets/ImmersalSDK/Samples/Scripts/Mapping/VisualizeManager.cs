/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

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
        [SerializeField]
        private MappingUIComponent topMenu = null;
        [SerializeField]
        public MappingUIComponent localizeButton = null;
        [SerializeField]
        private GameObject mapSelect = null;
        [SerializeField]
        private GameObject deletePrompt = null;
        [SerializeField]
        private GameObject restoreMapImagesPrompt = null;

        public event ItemSelected OnItemSelected = null;
        public event ItemDeleted OnItemDeleted = null;
        public event ItemRestored OnItemRestored = null;
        public event SelectorOpened OnSelectorOpened = null;
        public event SelectorClosed OnSelectorClosed = null;
        public delegate void ItemSelected(SDKJob job);
        public delegate void ItemDeleted(SDKJob job);
        public delegate void ItemRestored(SDKJob job);
        public delegate void SelectorOpened();
        public delegate void SelectorClosed();

        private SDKJob m_ActiveFunctionJob = null;

//        private IEnumerator m_ClosePanel = null;

        private enum UIState { Default, SlotSelect};
        private UIState uiState = UIState.Default;

        // for delete / restore
        public SDKJob activeFunctionJob
        {
            set { m_ActiveFunctionJob = value; }
        }

        public void SetSelectSlotData(SDKJob[] data, List<int> activeMaps)
        {
            mapSelect.GetComponent<ScrollListControl>().SetData(data, activeMaps);
        }

        public void OnListItemSelect(SDKJob job)
        {
            OnItemSelected?.Invoke(job);
        }

        public void ToggleDeletePrompt(bool on)
        {
            deletePrompt.SetActive(on);
        }

        public void ToggleRestoreMapImagesPrompt(bool on)
        {
            restoreMapImagesPrompt.SetActive(on);
        }

        public void OnListItemDelete()
        {
            if (m_ActiveFunctionJob != null)
            {
                OnItemDeleted?.Invoke(m_ActiveFunctionJob);
            }
        }

        public void OnListItemRestore()
        {
            if (m_ActiveFunctionJob != null)
            {
                OnItemRestored?.Invoke(m_ActiveFunctionJob);
            }
        }

        public void SlotSelect()
        {
            if (uiState == UIState.SlotSelect)
            {
                uiState = UIState.Default;
                OnSelectorClosed?.Invoke();
            }
            else
            {
                uiState = UIState.SlotSelect;
                OnSelectorOpened?.Invoke();
            }
            ChangeState(uiState);
        }

        public void HandleToggle()
        {
            /*
            if (m_ClosePanel != null)
            {
                StopCoroutine(m_ClosePanel);
            }
            m_ClosePanel = ClosePanel();
            StartCoroutine(m_ClosePanel);
            */
        }

        /*
        IEnumerator ClosePanel()
        {
            yield return new WaitForSeconds(1.5f);
            DefaultView();
            yield return null;
        }
        */

        public void DefaultView()
        {
            uiState = UIState.Default;
            ChangeState(uiState);
        }

        private void ChangeState(UIState state)
        {
            switch (state)
            {
                case UIState.Default:
                    topMenu.Activate();
                    localizeButton.Activate();
                    mapSelect.SetActive(false);
                    break;
                case UIState.SlotSelect:
                    topMenu.Activate();
                    localizeButton.Disable();
                    mapSelect.SetActive(true);
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