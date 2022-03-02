/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Collections.Generic;
using UnityEngine.EventSystems;

#pragma warning disable 0618
namespace Immersal.Samples.Multiplayer
{
	[RequireComponent(typeof(NetworkManager))]
	public class HostGame : MonoBehaviour {

		enum ConnectionState { Disconnected, Listing, Listed, Connecting, ConnectedHost, ConnectedClient };

		NetworkManager manager;
		bool shouldConnect = false;

		ConnectionState connectionState = ConnectionState.Disconnected;

		UIController uiController;

		void Awake() {
			uiController = FindObjectOfType<UIController>();

			manager = GetComponent<NetworkManager>();
			manager.StartMatchMaker();
		}

		public void Update()
		{
			if (connectionState == ConnectionState.ConnectedHost || 
				connectionState == ConnectionState.ConnectedClient)
				return;
			
			bool noConnection = (manager.client == null || manager.client.connection == null ||
						manager.client.connection.connectionId == -1);

			if (!NetworkServer.active && !manager.IsClientConnected() && noConnection && shouldConnect)
			{
				if (manager.matchInfo == null) {
					if (manager.matches == null || manager.matches.Count < 1)
					{
						if (connectionState == ConnectionState.Disconnected)
						{
							connectionState = ConnectionState.Listing;
							manager.matchMaker.ListMatches(0, 20, "", false, 0, 0, OnMatchList);
						}
						else if (connectionState == ConnectionState.Listed)
						{
							manager.matchMaker.CreateMatch(manager.matchName, manager.matchSize, true, "", "", "", 0, 0, OnMatchCreate);
							connectionState = ConnectionState.Connecting;
						}
					}
					else if (manager.matches.Count > 0 && connectionState == ConnectionState.Listed)
					{
						var match = manager.matches[0];
						manager.matchMaker.JoinMatch(match.networkId, "", "", "", 0, 0, OnMatchJoined);
						connectionState = ConnectionState.Connecting;
					}
				}
			}
		}

		public void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
		{
			manager.matches = matchList;
			connectionState = ConnectionState.Listed;
		}

		public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			if (success)
			{
				manager.StartHost(matchInfo);
				connectionState = ConnectionState.ConnectedHost;
			}
		}

		public void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			if (success)
			{
				manager.StartClient(matchInfo);
				connectionState = ConnectionState.ConnectedClient;
			}
		}

		public void Connect()
		{
			if (NetworkServer.active || manager.IsClientConnected())
			{
				Disconnect();
			}
			else
			{
                uiController.ChangeState(UIController.ConnectionsState.Connected);
				EventSystem.current.SetSelectedGameObject(null);
				shouldConnect = true;
			}
		}

		public void Disconnect()
		{
			shouldConnect = false;

			if (connectionState == ConnectionState.ConnectedClient)
			{ 
				manager.StopClient();
			}
			else if (connectionState == ConnectionState.ConnectedHost)
			{
				manager.StopHost();
			}
			manager.StartMatchMaker();

			connectionState = ConnectionState.Disconnected;
            uiController.ChangeState(UIController.ConnectionsState.NotConnected);
            EventSystem.current.SetSelectedGameObject(null);
		}
	}
}