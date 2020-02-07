/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections;
using TMPro;

namespace Immersal.Samples.Mapping
{
	public class WorkspaceManager : MonoBehaviour
    {
        [SerializeField]
        private MappingUIComponent toolsButton = null;
		[SerializeField]
		public MappingUIComponent captureButton = null;
		[SerializeField]
		public MappingUIComponent statsBox = null;
		[SerializeField]
		private GameObject toolsScrollList = null;
		[SerializeField]
		private GameObject promptDeleteData = null;
		[SerializeField]
		private GameObject promptSubmitMap = null;
		[SerializeField]
		public TMP_InputField newMapName = null;

//		private IEnumerator m_ClosePanel = null;

		private enum UIState { Default, Tools, DeleteData, SubmitNewMap };
		private UIState uiState = UIState.Default;

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

		public void DefaultView() {
			uiState = UIState.Default;
			ChangeState(uiState);
		}

		private void ChangeState(UIState state) {
			switch (state) {
				case UIState.Default:
					toolsButton.Activate();
					captureButton.Activate();
					statsBox.Activate();
					toolsScrollList.SetActive(false);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(false);
					statsBox.gameObject.SetActive(true);
					break;
				case UIState.Tools:
					toolsButton.Activate();
					captureButton.Disable();
					statsBox.Disable();
					toolsScrollList.SetActive(true);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(false);
					statsBox.gameObject.SetActive(true);
					break;
				case UIState.DeleteData:
					toolsButton.Disable();
					captureButton.Disable();
					statsBox.Disable();
					toolsScrollList.SetActive(false);
					promptDeleteData.SetActive(true);
					promptSubmitMap.SetActive(false);
					statsBox.gameObject.SetActive(true);
					break;
				case UIState.SubmitNewMap:
					toolsButton.Disable();
					captureButton.Disable();
					statsBox.Disable();
					toolsScrollList.SetActive(false);
					promptDeleteData.SetActive(false);
					promptSubmitMap.SetActive(true);
					statsBox.gameObject.SetActive(true);
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