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
    [ExecuteAlways]
    public class ARMap : MonoBehaviour
    {
        [HideInInspector]
        public Transform m_Root;
        [HideInInspector]
        public TextAsset m_MapFile;
        [HideInInspector]
        public Color m_Color = new Color(0.2f, 0.7f, 0.9f);
        [HideInInspector]
        public enum RenderMode { DoNotRender, EditorOnly, EditorAndRuntime }
        [HideInInspector]
        public RenderMode m_RenderMode = RenderMode.DoNotRender;

        private int m_MapId = -1;
        private Mesh m_Mesh = null;

        private MeshFilter m_MeshFilter = null;
        private MeshRenderer m_MeshRenderer = null;
        private ARSpace m_ARSpace = null;

        public void InitMesh()
        {
            m_Mesh = new Mesh();

            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            if (m_MeshFilter == null)
            {
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();
                m_MeshFilter.hideFlags = HideFlags.HideInInspector;
            }

            if (m_MeshRenderer == null)
            {
                m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();
                m_MeshRenderer.hideFlags = HideFlags.HideInInspector;
            }

            m_MeshFilter.mesh = m_Mesh;

            Material material = new Material(Shader.Find("Immersal/pointcloud3d"));
            m_MeshRenderer.material = material;

            if (Application.isEditor)
            {
                m_MeshRenderer.sharedMaterial.EnableKeyword("IN_EDITOR");
            }

            switch (m_RenderMode)
            {
                case RenderMode.DoNotRender:
                    FreeMap();
                    m_MeshRenderer.enabled = false;
                    break;
                case RenderMode.EditorOnly:
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        FreeMap();
                        LoadMap();
                        m_MeshRenderer.enabled = true;
                    }
                    else
                    {
                        m_MeshRenderer.enabled = false;
                    }
                    break;
                case RenderMode.EditorAndRuntime:
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        FreeMap();
                        LoadMap();
                        m_MeshRenderer.enabled = true;
                    }
                    else
                    {
                        m_MeshRenderer.enabled = true;
                    }
                    break;
                default:
                    break;
            }
        }

        public void FreeMap()
        {
            if (m_MapId != -1)
            {
                Immersal.Core.FreeMap(m_MapId);
                if (!Application.isEditor)
                {
                    ARLocalizer.UnregisterSpace(m_Root, m_MapId);
                }
            }
        }

        public int LoadMap(byte[] mapBytes = null)
        {
            if (mapBytes == null)
            {
                mapBytes = (m_MapFile != null) ? m_MapFile.bytes : null;
            }

            if (mapBytes == null)
                return -1;

            m_MapId = Immersal.Core.LoadMap(mapBytes);

            if (m_MapId != -1)
            {
                Vector3[] points = new Vector3[65536];
                int num = Immersal.Core.GetPointCloud(m_MapId, points);

                CreateCloud(points, num);

                if (!Application.isEditor)
                {
                    m_Root = m_ARSpace.transform;
                    Matrix4x4 offset = Matrix4x4.TRS(transform.localPosition, transform.localRotation, Vector3.one);
                    ARLocalizer.RegisterSpace(m_Root, m_MapId, offset);
                }
            }

            return m_MapId;
        }

        public void CreateCloud(Vector3[] points, int totalPoints, Matrix4x4 offset)
        {
            const int max_vertices = 65536;
            int numPoints = totalPoints >= max_vertices ? max_vertices : totalPoints;
            Color32 fix_col = m_Color;
            int[] indices = new int[numPoints];
            Vector3[] pts = new Vector3[numPoints];
            Color32[] col = new Color32[numPoints];
            for (int i = 0; i < numPoints; ++i)
            {
                indices[i] = i;
                pts[i] = offset.MultiplyPoint3x4(points[i]);
                col[i] = fix_col;
            }

            m_Mesh.Clear();
            m_Mesh.vertices = pts;
            m_Mesh.colors32 = col;
            m_Mesh.SetIndices(indices, MeshTopology.Points, 0);
            m_Mesh.bounds = new Bounds(transform.position, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
        }

        public void CreateCloud(Vector3[] points, int totalPoints)
        {
            CreateCloud(points, totalPoints, Matrix4x4.identity);
        }

        void Awake()
        {
            m_ARSpace = gameObject.GetComponentInParent<ARSpace>();
            if (!m_ARSpace)
            {
                GameObject go = new GameObject("AR Space");
                m_ARSpace = go.AddComponent<ARSpace>();
                transform.SetParent(go.transform);
            }
        }

        private void OnEnable()
        {
            InitMesh();

            if (!(Application.isEditor && !Application.isPlaying))
            {
                LoadMap();
            }
        }

        private void OnDisable()
        {
            FreeMap();
        }
    }
}