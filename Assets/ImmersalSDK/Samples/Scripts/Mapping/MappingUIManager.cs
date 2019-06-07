/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections;
using TMPro;
using Immersal.REST;
using Immersal.Samples.Mapping.ScrollList;

namespace Immersal.Samples.Mapping
{
	public class MappingUIManager : MonoBehaviour
    {
        [SerializeField]
		private MappingUIComponent slotSelectButton = null;
		[SerializeField]
		private MappingUIComponent slotEditButton = null;
		[SerializeField]
		private MappingUIComponent toolsButton = null;
		[SerializeField]
		public MappingUIComponent captureButton = null;
		[SerializeField]
		public MappingUIComponent localizeButton = null;
		[SerializeField]
		private GameObject scrollListSelectSlot = null;
		[SerializeField]
		public MappingUIComponent statsBox = null;
		[SerializeField]
		private GameObject scrollListEditSlot = null;
		[SerializeField]
		private GameObject scrollListTools = null;
		[SerializeField]
		private GameObject promptDeleteData = null;
		[SerializeField]
		private GameObject promptSubmitMap = null;
		[SerializeField]
		private GameObject cancelClickCatcher = null;
		[SerializeField]
		private TextMeshProUGUI activeSlotName = null;
		public TMP_InputField newMapName = null;
        public event ItemSelected OnItemSelected = null;
		public event SelectorOpened OnSelectorOpened = null;
		public event SelectorClosed OnSelectorClosed = null;
        public delegate void ItemSelected(SDKJob job);
		public delegate void SelectorOpened();
		public delegate void SelectorClosed();

		private IEnumerator m_ClosePanel = null;

		private enum UIState { Default, SlotSelect, SlotEdit, Tools, DeleteData, SubmitNewMap };
		private UIState uiState = UIState.Default;

		public void SetSelectSlotData(SDKJob[] data)
		{
			scrollListSelectSlot.GetComponent<ScrollListControl>().data = data;
		}

		public void OnListItemSelect(SDKJob job)
		{
			if (OnItemSelected != null)
			{
				OnItemSelected(job);
			}
		}

		public void SlotSelect() {
			if (uiState == UIState.SlotSelect) {
				uiState = UIState.Default;
				if (OnSelectorClosed != null)
				{
					OnSelectorClosed();
				}
			} else {
				uiState = UIState.SlotSelect;
				if (OnSelectorOpened != null)
				{
					OnSelectorOpened();
				}
			}
			ChangeState(uiState);
		}

		public void SlotEdit() {
			if (uiState == UIState.SlotEdit) {
				uiState = UIState.Default;
			} else {
				uiState = UIState.SlotEdit;
			}		
			ChangeState(uiState);
		}

		public void Tools() {
			if (uiState == UIState.Tools) {
				uiState = UIState.Default;
			} else {
				uiState = UIState.Tools;
			}
			ChangeState(uiState);
		}

		public void DeleteData() {
			if (uiState == UIState.DeleteData) {
				uiState = UIState.Default;
			} else {
				uiState = UIState.DeleteData;
			}
			ChangeState(uiState);
		}

		public void ConstructMap() {
			if (uiState == UIState.SubmitNewMap) {
				uiState = UIState.Default;
			} else {
				uiState = UIState.SubmitNewMap;
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

		public void DefaultView() {
			uiState = UIState.Default;
			ChangeState(uiState);
		}

		public void UpdateActiveMapName(string text, bool closeSelector = false) {
			if(activeSlotName != null) {
				activeSlotName.text = text;
			}
			if (closeSelector)
			{
				DefaultView();
			}
		}

		private void ChangeState(UIState state) {
			switch (state) {
				case UIState.Default:
					slotSelectButton.Activate();
					slotEditButton.Activate();
					toolsButton.Activate();
					captureButton.Activate();
					localizeButton.Activate();
					statsBox.Activate();
					scrollListSelectSlot.SetActive(false);
					scrollListEditSlot.SetActive(false);
					scrollListTools.SetActive(false);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(false);
					cancelClickCatcher.SetActive(false);
					statsBox.gameObject.SetActive(true);
					break;
				case UIState.SlotSelect:
					slotSelectButton.Activate();
					slotEditButton.Disable();
					toolsButton.Disable();
					captureButton.Disable();
					localizeButton.Disable();
					statsBox.Disable();
					scrollListSelectSlot.SetActive(true);
					scrollListEditSlot.SetActive(false);
					scrollListTools.SetActive(false);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(false);
					cancelClickCatcher.SetActive(true);
					statsBox.gameObject.SetActive(false);
					break;
				case UIState.SlotEdit:
					slotSelectButton.Disable();
					slotEditButton.Activate();
					toolsButton.Disable();
					captureButton.Disable();
					localizeButton.Disable();
					statsBox.Disable();
					scrollListSelectSlot.SetActive(false);
					scrollListEditSlot.SetActive(true);
					scrollListTools.SetActive(false);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(false);
					cancelClickCatcher.SetActive(true);
					statsBox.gameObject.SetActive(false);
					break;
				case UIState.Tools:
					slotSelectButton.Disable();
					slotEditButton.Disable();
					toolsButton.Activate();
					captureButton.Disable();
					localizeButton.Disable();
					statsBox.Disable();
					scrollListSelectSlot.SetActive(false);
					scrollListEditSlot.SetActive(false);
					scrollListTools.SetActive(true);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(false);
					cancelClickCatcher.SetActive(true);
					statsBox.gameObject.SetActive(true);
					break;
				case UIState.DeleteData:
					slotSelectButton.Disable();
					slotEditButton.Disable();
					toolsButton.Disable();
					captureButton.Disable();
					localizeButton.Disable();
					statsBox.Disable();
					scrollListSelectSlot.SetActive(false);
					scrollListEditSlot.SetActive(false);
					scrollListTools.SetActive(false);
					promptDeleteData.SetActive(true);
					promptSubmitMap.SetActive(false);
					cancelClickCatcher.SetActive(true);
					statsBox.gameObject.SetActive(true);
					break;
				case UIState.SubmitNewMap:
					slotSelectButton.Disable();
					slotEditButton.Disable();
					toolsButton.Disable();
					captureButton.Disable();
					localizeButton.Disable();
					statsBox.Disable();
					scrollListSelectSlot.SetActive(false);
					scrollListEditSlot.SetActive(false);
					scrollListTools.SetActive(false);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(true);
					cancelClickCatcher.SetActive(true);
					statsBox.gameObject.SetActive(true);
					break;
				default:
					break;
			}
		}

        public void GenerateNotification(string text)
        {
            NotificationManager.Instance.GenerateNotification(text);
        }

		private void Start() {
			ChangeState(uiState);
		}
	}
}