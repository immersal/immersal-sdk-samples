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
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public SpaceContainer space;
    }

    public class ARSpace : MonoBehaviour
    {
        public static Dictionary<Transform, SpaceContainer> transformToSpace = new Dictionary<Transform, SpaceContainer>();
        public static Dictionary<SpaceContainer, Transform> spaceToTransform = new Dictionary<SpaceContainer, Transform>();
        public static Dictionary<int, MapOffset> mapHandleToOffset = new Dictionary<int, MapOffset>();
        public static Dictionary<int, ARMap> mapHandleToMap = new Dictionary<int, ARMap>();

		private static ARSpace instance = null;

        private Matrix4x4 m_InitialOffset = Matrix4x4.identity;

        public Matrix4x4 initialOffset
        {
            get { return m_InitialOffset; }
        }

    	public static ARSpace Instance
		{
			get
			{
#if UNITY_EDITOR
				if (instance == null && !Application.isPlaying)
				{
					instance = UnityEngine.Object.FindObjectOfType<ARSpace>();
				}
#endif
				if (instance == null)
				{
					Debug.LogError("No ARSpace instance found. Ensure one exists in the scene.");
				}
				return instance;
			}
		}

		void Awake()
		{
			if (instance == null)
			{
				instance = this;

                Vector3 pos = transform.position;
                Quaternion rot = transform.rotation;

                Matrix4x4 offset = Matrix4x4.TRS(pos, rot, Vector3.one);

                m_InitialOffset = offset;
			}
			if (instance != this)
			{
				Debug.LogError("There must be only one ARSpace object in a scene.");
				UnityEngine.Object.DestroyImmediate(this);
				return;
			}
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

        public static void RegisterSpace(Transform tr, int mapHandle, ARMap map, Vector3 offsetPosition, Quaternion offsetRotation, Vector3 offsetScale)
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

            spaceToTransform[sc] = tr;

            sc.mapCount++;

            MapOffset mo = new MapOffset();
            mo.position = offsetPosition;
            mo.rotation = offsetRotation;
            mo.scale = offsetScale;
            mo.space = sc;

            mapHandleToOffset[mapHandle] = mo;
            mapHandleToMap[mapHandle] = map;
		}

        public static void RegisterSpace(Transform tr, int mapHandle, ARMap map)
        {
            RegisterSpace(tr, mapHandle, map, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        public static void UnregisterSpace(Transform tr, int mapHandle)
		{
			if (transformToSpace.ContainsKey(tr))
			{
				SpaceContainer sc = transformToSpace[tr];
				if (--sc.mapCount == 0)
                {
					transformToSpace.Remove(tr);
                    spaceToTransform.Remove(sc);
                }
				if (mapHandleToOffset.ContainsKey(mapHandle))
					mapHandleToOffset.Remove(mapHandle);
                if (mapHandleToMap.ContainsKey(mapHandle))
                    mapHandleToMap.Remove(mapHandle);
			}
		}

		public static void UpdateSpace(SpaceContainer space, Vector3 pos, Quaternion rot)
        {
            if (spaceToTransform.ContainsKey(space))
            {
                Transform tr = spaceToTransform[space];
        		tr.SetPositionAndRotation(pos, rot);
            }
		}
    }
}