/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.Navigation
{
    public class LookTowardsTarget : MonoBehaviour
    {
        [SerializeField]
        private Transform m_TransformToRotate = null;

        public void LookAt(Vector3 target, Vector3 up)
        {
            if (m_TransformToRotate != null)
            {
                Vector3 pos = transform.position;
                Vector3 direction = (target - pos).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction, up);

                m_TransformToRotate.rotation = rotation;
            }
        }
    }
}