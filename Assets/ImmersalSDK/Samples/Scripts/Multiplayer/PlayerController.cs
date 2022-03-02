/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
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
		private Vector3 m_Position;
		private Quaternion m_Rotation;
		private ARSpace m_ArSpace;

		private void Start() {
			m_MainCamera = Camera.main;

			if (Application.isEditor)
			{
				TrackedPoseDriver tpd = m_MainCamera.GetComponent<TrackedPoseDriver>();
				if (tpd)
					tpd.enabled = false;
			}
		}

		void OnEnable()
		{
			m_ArSpace = UnityEngine.Object.FindObjectOfType<ARSpace>();
			if (m_ArSpace == null)
				Debug.LogError("No ARSpace found");
			
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

			if (Application.isEditor)
			{
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
			}
			else
			{
				Vector3 oldPos = transform.localPosition;
				Quaternion oldRot = transform.localRotation;
				Vector3 camPos = m_MainCamera.transform.position;
				Quaternion camRot = m_MainCamera.transform.rotation;
				camPos += m_MainCamera.transform.up * .2f;
				Pose p = m_ArSpace.ToCloudSpace(camPos, camRot);
				m_Rotation = p.rotation;
				m_Position = p.position;

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
			}
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