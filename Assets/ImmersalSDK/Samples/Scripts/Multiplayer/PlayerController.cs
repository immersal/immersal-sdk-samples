/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.1.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SpatialTracking;
using Immersal.AR;

#pragma warning disable 0618
namespace Immersal.Samples.Multiplayer
{
	public class PlayerController : NetworkBehaviour {
		public Renderer playerBodyRenderer;

		private Camera m_MainCamera;
		private ARLocalizer m_Loc;
		private Vector3 m_Position;
		private Quaternion m_Rotation;
		private GameObject m_ArSpace;
		private ImmersalARCloudSDK m_Sdk;

		private void Start() {
			m_MainCamera = Camera.main;
			m_Sdk = ImmersalARCloudSDK.Instance;
			m_Loc = m_Sdk.gameObject.GetComponent<ARLocalizer>();
	#if UNITY_EDITOR || UNITY_STANDALONE
			TrackedPoseDriver tpd = m_MainCamera.GetComponent<TrackedPoseDriver>();
			if (tpd)
				tpd.enabled = false;
	#endif
			Vector3 pos = transform.position;
			transform.position = pos;
		}

		void OnEnable()
		{
			m_ArSpace = GameObject.Find("AR Space");
			var scale = transform.localScale;
			var pos = transform.localPosition;
			transform.SetParent(m_ArSpace.transform);
			transform.localScale = scale;
			transform.localPosition = pos;
		}

		void Update()
		{
			if (!isLocalPlayer)
			{
				return;
			}

	#if UNITY_EDITOR || UNITY_STANDALONE
			var x = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
			var z = Input.GetAxis("Vertical") * Time.deltaTime * 3.0f;
			Vector3 oldPos = transform.position;
			Quaternion oldRot = transform.rotation;

			transform.Rotate(0, x, 0);
			transform.Translate(0, 0, z);

			m_Position = transform.position;
			m_Rotation = transform.rotation;

			if (m_Position != oldPos || m_Rotation != oldRot)
			{
				m_MainCamera.transform.position = m_Position;
				m_MainCamera.transform.rotation = m_Rotation;

				if (isServer)
				{
					UpdateClientsPos();
				}
				else
				{
					CmdMove(m_Position, m_Rotation);
				}
			}
	#elif UNITY_IOS || UNITY_ANDROID
			Vector3 oldPos = transform.localPosition;
			Quaternion oldRot = transform.localRotation;
			Vector3 camPos = m_MainCamera.transform.position;
			Quaternion camRot = m_MainCamera.transform.rotation;
			camPos += m_MainCamera.transform.up * .2f;
			Matrix4x4 cloudSpace = ARHelper.ToCloudSpace(camPos, camRot, m_Loc.position, m_Loc.rotation);
			m_Rotation = cloudSpace.rotation;
			m_Position = cloudSpace.GetColumn(3);

			if (m_Position != oldPos || m_Rotation != oldRot)
			{
				transform.localPosition = m_Position;
				transform.localRotation = m_Rotation;

				if (isServer)
				{
					UpdateClientsPos();
				}
				else
				{
					CmdMove(m_Position, m_Rotation);
				}
			}
	#endif
		}

		[ClientRpc]
		public void RpcSyncedPos(Vector3 syncedPos, Quaternion syncedRotation)
		{
			transform.localPosition = syncedPos;
			transform.localRotation = syncedRotation;
		}

		[Server]
		private void UpdateClientsPos()
		{
			RpcSyncedPos(m_Position, m_Rotation);
		}

		[Command]
		public void CmdMove(Vector3 syncedPos, Quaternion syncedRotation)
		{
			transform.localPosition = syncedPos;
			transform.localRotation = syncedRotation;
		}

		public override void OnStartLocalPlayer()
		{
			playerBodyRenderer.material.color = Color.blue;
		}
	}
}