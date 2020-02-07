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
	public class MappingUIManager : MonoBehaviour
    {
        [SerializeField]
        private WorkspaceManager m_WorkspaceManager;
        [SerializeField]
        private VisualizeManager m_VisualizeManager;

		private enum UIState {Workspace, Visualize};
		private UIState uiState = UIState.Workspace;

		public void SwitchMode() {
			if (uiState == UIState.Workspace) {
				uiState = UIState.Visualize;
			} else {
				uiState = UIState.Workspace;
			}		
			ChangeState(uiState);
		}

		private void ChangeState(UIState state) {
			switch (state) {
				case UIState.Workspace:
                    m_WorkspaceManager.gameObject.SetActive(true);
                    m_VisualizeManager.gameObject.SetActive(false);
                    break;
				case UIState.Visualize:
                    m_WorkspaceManager.gameObject.SetActive(false);
                    m_VisualizeManager.gameObject.SetActive(true);
                    break;
				default:
					break;
			}
		}

		private void Start() {

            if(m_WorkspaceManager == null)
            {
                m_WorkspaceManager = GetComponentInChildren<WorkspaceManager>();
            }
            if (m_VisualizeManager == null)
            {
                m_VisualizeManager = GetComponentInChildren<VisualizeManager>();
            }

            ChangeState(uiState);
        }
	}
}