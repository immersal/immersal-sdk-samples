/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.Events;

namespace Immersal.AR
{
    [System.Serializable]
    public class MapLocalizedEvent : UnityEvent<int>
    {
    }
    
    [ExecuteAlways]
    public class ARMap : MonoBehaviour
    {
        public const int MAX_VERTICES = 65535;

        public enum RenderMode { DoNotRender, EditorOnly, EditorAndRuntime }

        [HideInInspector]
        public RenderMode renderMode = RenderMode.EditorOnly;
        [HideInInspector]
        public TextAsset mapFile;
        [HideInInspector]
        public Color color = new Color(0.57f, 0.93f, 0.12f);
		public MapLocalizedEvent OnFirstLocalization = null;
        private Mesh m_Mesh = null;
        private MeshFilter m_MeshFilter = null;
        private MeshRenderer m_MeshRenderer = null;
        protected ARSpace m_ARSpace = null;
        private bool m_LocalizedOnce = false;
        private int m_MapHandle = -1;

        public Transform root { get; protected set; }

        public int mapHandle
        {
            get { return m_MapHandle; }
            set { m_MapHandle = value; }
        }

        public void InitMesh()
        {
            m_Mesh = new Mesh();

            m_MeshFilter = gameObject.GetComponent<MeshFilter>();
            m_MeshRenderer = gameObject.GetComponent<MeshRenderer>();

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

            m_MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_MeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            m_MeshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            switch (renderMode)
            {
                case RenderMode.DoNotRender:
                    m_MeshRenderer.enabled = false;
                    break;
                case RenderMode.EditorOnly:
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        m_MeshRenderer.enabled = true;
                    }
                    else
                    {
                        m_MeshRenderer.enabled = false;
                    }
                    break;
                case RenderMode.EditorAndRuntime:
                    m_MeshRenderer.enabled = true;
                    break;
                default:
                    break;
            }
        }

        public virtual void FreeMap()
        {
            if (mapHandle >= 0)
            {
                Immersal.Core.FreeMap(mapHandle);
                ARSpace.UnregisterSpace(root, mapHandle);
                mapHandle = -1;
                m_Mesh.Clear();
                Reset();
            }
        }

        public virtual void Reset()
        {
            m_LocalizedOnce = false;
        }

        public virtual int LoadMap(byte[] mapBytes = null)
        {
            if (mapBytes == null)
            {
                mapBytes = (mapFile != null) ? mapFile.bytes : null;
            }

            if (mapBytes != null && mapHandle < 0)
            {
                mapHandle = Immersal.Core.LoadMap(mapBytes);
            }

            if (mapHandle >= 0)
            {
                Vector3[] points = new Vector3[MAX_VERTICES];
                int num = Immersal.Core.GetPointCloud(mapHandle, points);

                CreateCloud(points, num);

                root = m_ARSpace.transform;
                ARSpace.RegisterSpace(root, mapHandle, this, transform.localPosition, transform.localRotation, transform.localScale);
            }

            return mapHandle;
        }

        public void CreateCloud(Vector3[] points, int totalPoints, Matrix4x4 offset)
        {
            int numPoints = totalPoints >= MAX_VERTICES ? MAX_VERTICES : totalPoints;
            Color32 fix_col = color;
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

        public void NotifySuccessfulLocalization(int mapHandle)
        {
            if (m_LocalizedOnce)
                return;
            
            OnFirstLocalization?.Invoke(mapHandle);
            m_LocalizedOnce = true;
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
            LoadMap();
        }

        private void OnDisable()
        {
            FreeMap();
        }
    }
}