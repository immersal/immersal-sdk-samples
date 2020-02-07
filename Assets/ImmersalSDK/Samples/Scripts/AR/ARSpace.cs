/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.AR
{
    public class ARSpace : MonoBehaviour
    {
        private Matrix4x4 m_InitialOffset = Matrix4x4.identity;

        public Matrix4x4 initialOffset
        {
            get { return m_InitialOffset; }
        }

		public Pose ToCloudSpace(Vector3 camPos, Quaternion camRot)
		{
			Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
			Matrix4x4 trackerToCloudSpace = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
			Matrix4x4 cloudSpace = trackerToCloudSpace.inverse * trackerSpace;

			return new Pose(cloudSpace.GetColumn(3), cloudSpace.rotation);
		}

		public Pose FromCloudSpace(Vector3 camPos, Quaternion camRot)
		{
			Matrix4x4 cloudSpace = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
			Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
			Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

			return new Pose(m.GetColumn(3), m.rotation);
		}

        private void Awake()
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            Matrix4x4 offset = Matrix4x4.TRS(pos, rot, Vector3.one);

            m_InitialOffset = offset;
        }
    }
}