/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.Util
{
	public class PointCloudRenderer : MonoBehaviour
	{
		private Material mat;
		private Mesh mesh;
		[HideInInspector]
		public GameObject go;
		[HideInInspector]
        public int mapHandle;
		public static bool visible = true;

		private Color[] m_PointcloudColors = new Color[]	{	new Color(0.22f, 1f, 0.46f), 
																new Color(0.96f, 0.14f, 0.14f),
																new Color(0.16f, 0.69f, 0.95f),
																new Color(0.93f, 0.84f, 0.12f),
																new Color(0.57f, 0.93f, 0.12f),
																new Color(1f, 0.38f, 0.78f),
																new Color(0.4f, 0f, 0.9f),
																new Color(0.89f, 0.4f, 0f)
															};

		public void Init()
		{
			if (go == null)
			{
				mat = new Material(Shader.Find("Immersal/pointcloud3d"));
				go = new GameObject("meshcontainer");
				mesh = new Mesh();
				go.hideFlags = HideFlags.HideAndDontSave;
				go.transform.SetParent(gameObject.transform, false);
				MeshRenderer mr = go.AddComponent<MeshRenderer>();
				go.AddComponent<MeshFilter>().mesh = mesh;
				mr.material = mat;
				if (Application.isEditor)
					mr.material.EnableKeyword("IN_EDITOR");
			}
		}

		// Use this for initialization
		void Awake()
		{
			Init();
			go.SetActive(false);
		}

		void Update()
		{
			go.SetActive(visible);
		}

		public void CreateCloud(Vector3[] points, int totalPoints, Matrix4x4 offset)
		{
			const int max_vertices = 65535;
			int numPoints = totalPoints >= max_vertices ? max_vertices : totalPoints;
			Color32 fix_col = m_PointcloudColors[Random.Range(0, m_PointcloudColors.Length)];
            int[] indices = new int[numPoints];
			Vector3[] pts = new Vector3[numPoints];
			Color32[] col = new Color32[numPoints];
            for (int i = 0; i < numPoints; i++)
			{
				indices[i] = i;
				pts[i] = offset.MultiplyPoint3x4(points[i]);
				col[i] = fix_col;
			}

			mesh.Clear();
			mesh.vertices = pts;
			mesh.colors32 = col;
			mesh.SetIndices(indices, MeshTopology.Points, 0);
			mesh.bounds = new Bounds(transform.position, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
		}

        public void CreateCloud(Vector3[] points, int totalPoints)
        {
            CreateCloud(points, totalPoints, Matrix4x4.identity);
        }

        public void ClearCloud()
		{
			mesh.Clear();

			if (!Application.isPlaying)
			{
				DestroyImmediate(go);
			}
			else
	            Destroy(this);
		}
	}
}