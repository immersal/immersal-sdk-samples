/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
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

        public event ItemSelected OnItemSelected = null;
        public event SelectorOpened OnSelectorOpened = null;
        public event SelectorClosed OnSelectorClosed = null;
        public delegate void ItemSelected(SDKJob job);
        public delegate void SelectorOpened();
        public delegate void SelectorClosed();

        private IEnumerator m_ClosePanel = null;

        private enum UIState { Default, SlotSelect};
        private UIState uiState = UIState.Default;

        public void SetSelectSlotData(SDKJob[] data, List<int> activeMaps)
        {
            mapSelect.GetComponent<ScrollListControl>().SetData(data, activeMaps);
        }

        public void OnListItemSelect(SDKJob job)
        {
            if (OnItemSelected != null)
            {
                OnItemSelected(job);
            }
        }

        public void SlotSelect()
        {
            if (uiState == UIState.SlotSelect)
            {
                uiState = UIState.Default;
                if (OnSelectorClosed != null)
                {
                    OnSelectorClosed();
                }
            }
            else
            {
                uiState = UIState.SlotSelect;
                if (OnSelectorOpened != null)
                {
                    OnSelectorOpened();
                }
            }
            ChangeState(uiState);
        }

        public void HandleToggle()
        {
            if (m_ClosePanel != null)
            {
                StopCoroutine(m_ClosePanel);
            }
            m_ClosePanel = ClosePanel();
            StartCoroutine(m_ClosePanel);
        }

        IEnumerator ClosePanel()
        {
            yield return new WaitForSeconds(1.5f);
            DefaultView();
            yield return null;
        }

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