/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections;
using TMPro;

namespace Immersal.Samples.Mapping
{
	public class WorkspaceManager : MonoBehaviour
    {
		public MappingUIComponent captureButton = null;
		public TMP_InputField newMapName = null;
		public TMP_Dropdown detailLevelDropdown = null;

		[SerializeField]
        private MappingUIComponent m_OptionsButton = null;
		[SerializeField]
		public MappingUIComponent m_InfoPanel = null;
		[SerializeField]
		private GameObject m_OptionsScrollList = null;
		[SerializeField]
		private GameObject m_PromptDeleteData = null;
		[SerializeField]
		private GameObject m_PromptConstructMap = null;

		private enum UIState { Default, Options, DeleteData, SubmitNewMap };
		private UIState uiState = UIState.Default;

		public void Options() {
			if (uiState == UIState.Options) {
				uiState = UIState.Default;
			} else {
				uiState = UIState.Options;
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

		public void DefaultView() {
			uiState = UIState.Default;
			ChangeState(uiState);
		}

		private void ChangeState(UIState state) {
			switch (state) {
				case UIState.Default:
					m_OptionsButton.Activate();
					captureButton.Activate();
					m_InfoPanel.Activate();
					m_OptionsScrollList.SetActive(false);
					m_PromptDeleteData.SetActive(false);
					m_PromptConstructMap.SetActive(false);
					m_InfoPanel.gameObject.SetActive(true);
					break;
				case UIState.Options:
					m_OptionsButton.Activate();
					captureButton.Disable();
					m_InfoPanel.Disable();
					m_OptionsScrollList.SetActive(true);
					m_PromptDeleteData.SetActive(false);
					m_PromptConstructMap.SetActive(false);
					m_InfoPanel.gameObject.SetActive(true);
					break;
				case UIState.DeleteData:
					m_OptionsButton.Disable();
					captureButton.Disable();
					m_InfoPanel.Disable();
					m_OptionsScrollList.SetActive(false);
					m_PromptDeleteData.SetActive(true);
					m_PromptConstructMap.SetActive(false);
					m_InfoPanel.gameObject.SetActive(true);
					break;
				case UIState.SubmitNewMap:
					m_OptionsButton.Disable();
					captureButton.Disable();
					m_InfoPanel.Disable();
					m_OptionsScrollList.SetActive(false);
					m_PromptDeleteData.SetActive(false);
					m_PromptConstructMap.SetActive(true);
					m_InfoPanel.gameObject.SetActive(true);
					break;
				default:
					break;
			}
		}

		private void Start() {
			ChangeState(uiState);
		}
	}
}