/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Immersal.Samples.Multiplayer
{
	public class UIController : MonoBehaviour
    {
        [SerializeField]
		private Button m_ConnectButton = null;
        [SerializeField]
		private Button m_DisconnectButton = null;

        public enum ConnectionsState { Connected, NotConnected};

        private void Start()
        {
            ChangeState(ConnectionsState.NotConnected);
        }

        public void ChangeState(ConnectionsState state)
        {
            if (m_DisconnectButton != null && m_ConnectButton != null)
            {

                switch (state)
                {
                    case ConnectionsState.Connected:
                        m_DisconnectButton.gameObject.SetActive(true);
                        m_ConnectButton.gameObject.SetActive(false);
                        break;
                    case ConnectionsState.NotConnected:
                        m_DisconnectButton.gameObject.SetActive(false);
                        m_ConnectButton.gameObject.SetActive(true);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}