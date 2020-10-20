/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.AR
{
    [ExecuteAlways]
    public class MLMap : ARMap
    {
        [SerializeField] private int m_ServerMapId = -1;
        public int serverMapId
        {
            get => m_ServerMapId;
            private set => m_ServerMapId = value;
        }
        
        public enum MLRenderMode { DoNotRender, EditorOnly }
        [HideInInspector]
        public MLRenderMode MLrenderMode = MLRenderMode.DoNotRender;
        
        public override void FreeMap()
        {
            if (mapHandle >= 0)
            {
                if (Application.isEditor)
                {
                    Immersal.Core.FreeMap(mapHandle);
                    ARSpace.UnregisterSpace(root, mapHandle);
                    mapHandle = -1;
                }
            }

            if (serverMapId > 0 && root != null && !Application.isEditor)
            {
                ARSpace.UnregisterSpace(root, serverMapId);
            }
        }

        public override int LoadMap(byte[] mapBytes = null)
        {
            if (mapBytes == null)
            {
                mapBytes = (mapFile != null) ? mapFile.bytes : null;
            }

            if (mapBytes != null && mapHandle < 0 && Application.isEditor)
            {
                mapHandle = Immersal.Core.LoadMap(mapBytes);
            }

            if (mapHandle >= 0)
            {
                if (Application.isEditor)
                {
                    Vector3[] points = new Vector3[MAX_VERTICES];
                    int num = Immersal.Core.GetPointCloud(mapHandle, points);

                    CreateCloud(points, num);

                    root = m_ARSpace.transform;
                    ARSpace.RegisterSpace(root, mapHandle, this, transform.localPosition, transform.localRotation, transform.localScale);
                }
            }

            if (serverMapId > 0 && !Application.isEditor)
            {
                root = m_ARSpace.transform;
                ARSpace.RegisterSpace(root, serverMapId, this, transform.localPosition, transform.localRotation, transform.localScale);
            }

            return mapHandle;
        }
    }
}
