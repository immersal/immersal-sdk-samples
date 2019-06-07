/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections;
using Immersal.Samples.Util;

namespace Immersal.AR
{
    public class ARSpace : MonoBehaviour
    {
        public TextAsset m_MapFile;
        public bool m_RenderPointCloud = true;
        private PointCloudRenderer m_PointRenderer;
        private int mapHandle;

        void Awake()
        {
            if (m_RenderPointCloud)
                m_PointRenderer = gameObject.AddComponent<PointCloudRenderer>();
        }

        void Start()
        {
            mapHandle = Immersal.Core.LoadMap(m_MapFile.bytes);

            if (m_RenderPointCloud)
            {
                Vector3[] points = new Vector3[65536];
                int num = Immersal.Core.GetPointCloud(mapHandle, points);
                m_PointRenderer.CreateCloud(points, num);
            }

            Immersal.AR.ARLocalizer.RegisterSpace(gameObject.transform, mapHandle);
        }
    }
}
