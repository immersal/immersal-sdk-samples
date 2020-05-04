/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections.Generic;

namespace Immersal.AR
{
	public class SpaceContainer
	{
        public int mapCount = 0;
		public Vector3 targetPosition = Vector3.zero;
		public Quaternion targetRotation = Quaternion.identity;
		public PoseFilter filter = new PoseFilter();
	}

    public class MapOffset
    {
        public Matrix4x4 offset;
        public SpaceContainer space;
    }

    public class ARSpace : MonoBehaviour
    {
        public static Dictionary<Transform, SpaceContainer> transformToSpace = new Dictionary<Transform, SpaceContainer>();
        public static Dictionary<int, MapOffset> mapIdToOffset= new Dictionary<int, MapOffset>();

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

        public static void RegisterSpace(Transform tr, int mapId, Matrix4x4 offset)
		{
            SpaceContainer sc;

            if (!transformToSpace.ContainsKey(tr))
            {
                sc = new SpaceContainer();
                transformToSpace[tr] = sc;
            }
            else
            {
                sc = transformToSpace[tr];
            }

            sc.mapCount++;

            MapOffset mo = new MapOffset();
            mo.offset = offset;
            mo.space = sc;

            mapIdToOffset[mapId] = mo;
		}

        public static void RegisterSpace(Transform tr, int mapId)
        {
            RegisterSpace(tr, mapId, Matrix4x4.identity);
        }

        public static void UnregisterSpace(Transform tr, int spaceId)
		{
			if (transformToSpace.ContainsKey(tr))
			{
				SpaceContainer sc = transformToSpace[tr];
				if (--sc.mapCount == 0)
					transformToSpace.Remove(tr);
				if (mapIdToOffset.ContainsKey(spaceId))
					mapIdToOffset.Remove(spaceId);
			}
		}
    }
}