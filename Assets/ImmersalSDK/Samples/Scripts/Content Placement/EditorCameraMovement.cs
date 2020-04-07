/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.ContentPlacement
{
    public class EditorCameraMovement : MonoBehaviour
    {
        [SerializeField]
        private float m_CameraMoveSpeed = 3f;
        [SerializeField]
        private float m_MoveSpeedMultiplier = 4f;
        [SerializeField]
        private Vector2 m_CameraSensitivity = new Vector2(15f, 15f);
        [SerializeField]
        private bool m_FlyMode = true;

        private Vector3 m_PointerDownScreenPos;
        private Vector3 m_PointerScreenPos;
        private float m_RotationY = 0f;

        private void Update()
        {

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                m_CameraMoveSpeed *= m_MoveSpeedMultiplier;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                m_CameraMoveSpeed /= m_MoveSpeedMultiplier;
            }

            if (Input.GetKey(KeyCode.W))
            {
                MoveCamera(Vector3.forward);
            }

            if (Input.GetKey(KeyCode.A))
            {
                MoveCamera(Vector3.left);
            }

            if (Input.GetKey(KeyCode.S))
            {
                MoveCamera(Vector3.back);
            }

            if (Input.GetKey(KeyCode.D))
            {
                MoveCamera(Vector3.right);
            }

            if (Input.GetMouseButton(1))
            {
                float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * m_CameraSensitivity.x;

                m_RotationY += Input.GetAxis("Mouse Y") * m_CameraSensitivity.y;
                m_RotationY = Mathf.Clamp(m_RotationY, -60f, 60f);

                Vector3 leuler = new Vector3(-m_RotationY, rotationX, 0f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(leuler), Time.deltaTime * 32f);
            }
#endif
        }

        private void MoveCamera(Vector3 direction)
        {
            Vector3 movement = transform.TransformDirection(direction) * m_CameraMoveSpeed * Time.deltaTime;
            if (!m_FlyMode)
            {
                movement.y = 0f;
            }
            transform.Translate(movement, Space.World);
        }
    }
}