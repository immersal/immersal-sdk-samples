/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AI;
using Immersal.AR;
using TMPro;

namespace Immersal.Samples.Navigation
{
    public class NavigationPath : MonoBehaviour
    {
        [HideInInspector]
        public float pathWidth = 0.3f;

        private float m_StepSize = 0.2f;
        private float m_ResampledStepSize = 0.2f;
        private bool m_Resample = true;
        private bool m_GenerateMesh = true;
        private float m_tension = 0.5f;

        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;
        private float m_PathLength;
        private float m_minStepSize = 0.1f;

        private void Awake()
        {
            if (m_Mesh == null)
                m_Mesh = new Mesh();

            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            if (m_MeshFilter == null)
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();

            if (m_MeshRenderer == null)
                m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();

            m_MeshFilter.mesh = m_Mesh;
        }

        public void GeneratePath(List<Vector3> points, Vector3 up)
        {
            if (points.Count < 2)
                return;

            if (points.Count == 2)
            {
                points.Insert(1, Vector3.Lerp(points[0], points[1], 0.333f));
                points.Insert(1, Vector3.Lerp(points[0], points[1], 0.666f));
            }

            List<Vector3> curvePoints = CatmullRomCurvePoints(points);

            if (m_Resample)
                curvePoints = ResampleCurve(curvePoints);

            if (curvePoints.Count < 2)
                return;

            if (m_GenerateMesh)
                GenerateMesh(curvePoints, up);
        }

        public void GeneratePath(List<Vector3> points)
        {
            GeneratePath(points, Vector3.up);
        }

        public void ClearMesh()
        {
            if (m_Mesh)
                m_Mesh.Clear();
        }

        private float GetT(float t, Vector3 p0, Vector3 p1)
        {
            float a = Mathf.Pow((p1.x - p0.x), 2.0f) + Mathf.Pow((p1.y - p0.y), 2.0f) + Mathf.Pow((p1.z - p0.z), 2.0f);
            float b = Mathf.Pow(a, 0.5f);
            float c = Mathf.Pow(b, m_tension);
            return (c + t);
        }

        private List<Vector3> CatmullRomCurveSegmentPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float segmentDivisions)
        {
            List<Vector3> points = new List<Vector3>();

            float t0 = 1.0f;
            float t1 = GetT(t0, p0, p1);
            float t2 = GetT(t1, p1, p2);
            float t3 = GetT(t2, p2, p3);

            for (float t = t1; t < t2; t += (t2 - t1) / segmentDivisions)
            {
                Vector3 a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
                Vector3 a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
                Vector3 a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

                Vector3 b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2;
                Vector3 b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3;

                Vector3 c = (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;

                points.Add(c);
            }

            return points;
        }

        private List<Vector3> CatmullRomCurvePoints(List<Vector3> in_points)
        {
            List<Vector3> out_points = new List<Vector3>();

            for (int i = 0; i < in_points.Count - 1; i++)
            {
                Vector3 p0;
                Vector3 p1;
                Vector3 p2;
                Vector3 p3;

                if (i == 0)
                {
                    p0 = in_points[i] + (in_points[i] - in_points[i + 1]);
                }
                else
                {
                    p0 = in_points[i - 1];
                }

                p1 = in_points[i];
                p2 = in_points[i + 1];

                if (i > in_points.Count - 3)
                {
                    p3 = in_points[i] + (in_points[i] - in_points[i - 1]);
                }
                else
                {
                    p3 = in_points[i + 2];
                }

                float segmentDivisions = Mathf.Ceil((p2 - p1).magnitude / Mathf.Max(m_minStepSize, m_StepSize));

                List<Vector3> segmentPoints = CatmullRomCurveSegmentPoints(p0, p1, p2, p3, segmentDivisions);
                out_points.AddRange(segmentPoints);
            }

            out_points.Add(in_points[in_points.Count - 1]);

            return out_points;
        }

        private List<Vector3> ResampleCurve(List<Vector3> oldPointPositions)
        {
            List<Vector3> points = new List<Vector3>();

            List<float> distanceAlongOldCurve = new List<float>();
            List<float> relativePositionOnOldCurve = new List<float>();
            List<float> relativePositionOnNewCurve = new List<float>();
            List<int> indexOfFirstPoint = new List<int>();
            List<float> blendBetweenPoints = new List<float>();
            float totalCurveLength = 0.0f;
            int oldPointCount = oldPointPositions.Count;

            //calculate distance along curve and total distance
            for (int i = 0; i < oldPointCount; i++)
            {
                float d;

                if (i == 0)
                {
                    d = 0f;
                }
                else
                {
                    Vector3 a = oldPointPositions[i - 1];
                    Vector3 b = oldPointPositions[i];
                    d = (b - a).magnitude;
                }

                totalCurveLength += d;
                distanceAlongOldCurve.Add(totalCurveLength);
            }

            m_PathLength = totalCurveLength;

            //calculate relative position on curve based on distance
            for (int i = 0; i < oldPointCount; i++)
            {
                float rp;
                if (i == 0)
                {
                    rp = 0f;
                }
                else
                {
                    rp = distanceAlongOldCurve[i] / totalCurveLength;
                }

                relativePositionOnOldCurve.Add(rp);
            }

            //calculate how many new points are needed
            int newPointCount = (int)Mathf.Ceil(totalCurveLength / Mathf.Max(m_minStepSize, m_ResampledStepSize));

            //find first old point further than the new one
            for (int i = 0; i < newPointCount; i++)
            {
                //new point relative position on new curve
                float t = (float)i / (float)(newPointCount - 1.0f);
                relativePositionOnNewCurve.Add(t);

                int k = 0;
                float j = relativePositionOnOldCurve[k];

                while (j < t)
                {
                    j = relativePositionOnOldCurve[k];
                    if (j <= t)
                    {
                        k++;
                    }
                }

                indexOfFirstPoint.Add(Mathf.Min(oldPointCount - 1, k));
            }

            for (int i = 0; i < newPointCount; i++)
            {
                int lower = Mathf.Max(indexOfFirstPoint[i] - 1, 0);
                int upper = indexOfFirstPoint[i];
                Vector3 a = oldPointPositions[lower];
                Vector3 b = oldPointPositions[upper];

                float d0 = relativePositionOnOldCurve[lower];
                float d1 = relativePositionOnOldCurve[upper];
                float blend;

                if (d1 - d0 > 0f)
                {
                    blend = (relativePositionOnNewCurve[i] - d0) / (d1 - d0);
                }
                else
                {
                    blend = 0f;
                }

                Vector3 p = Vector3.Lerp(a, b, blend);

                points.Add(p);
            }

            return points;
        }

        private void GenerateMesh(List<Vector3> points, Vector3 y)
        {
            List<Matrix4x4> matrices = new List<Matrix4x4>();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 z, x = new Vector3();

                // last point
                if (i == points.Count - 1)
                {
                    z = (points[i] - points[i - 1]).normalized;
                }
                else
                {
                    z = (points[i + 1] - points[i]).normalized;
                }

                x = Vector3.Cross(y, z);
                //y = Vector3.Cross(z, x);

                Quaternion q = Quaternion.LookRotation(z, -y);
                Matrix4x4 m = Matrix4x4.TRS(points[i], q, new Vector3(1f, 1f, 1f));

                matrices.Add(m);

                //Debug.DrawRay(points[i], x * 0.4f, Color.red);
                //Debug.DrawRay(points[i], y * 0.4f, Color.green);
                //Debug.DrawRay(points[i], z * 0.4f, Color.blue);
            }

            Vector3[] shape = new Vector3[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f) };
            float[] shapeU = new float[] { 0f, 1f };

            int vertsInShape = shape.Length;
            int segments = points.Count - 1;
            int edgeLoops = points.Count;
            int vertCount = vertsInShape * edgeLoops;
            int triCount = shape.Length * segments;
            int triIndexCount = triCount * 3;

            int[] triIndices = new int[triIndexCount];
            int[] lines = new int[] { 0, 1 };

            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            for (int i = 0; i < points.Count; i++)
            {
                int offset = i * vertsInShape;

                for (int j = 0; j < vertsInShape; j++)
                {
                    int id = offset + j;
                    vertices[id] = matrices[i].MultiplyPoint(shape[j] * pathWidth);
                    uvs[id] = new Vector2(i / (float)edgeLoops * m_PathLength, shapeU[j]);
                }
            }

            int ti = 0;
            for (int i = 0; i < segments; i++)
            {
                int offset = i * vertsInShape;

                for (int l = 0; l < lines.Length; l += 2)
                {
                    int a = offset + lines[l] + vertsInShape;
                    int b = offset + lines[l];
                    int c = offset + lines[l + 1];
                    int d = offset + lines[l + 1] + vertsInShape;

                    triIndices[ti] = a; ti++;
                    triIndices[ti] = b; ti++;
                    triIndices[ti] = c; ti++;
                    triIndices[ti] = c; ti++;
                    triIndices[ti] = d; ti++;
                    triIndices[ti] = a; ti++;
                }
            }

            m_Mesh.Clear();
            m_Mesh.vertices = vertices;
            m_Mesh.triangles = triIndices;
            m_Mesh.uv = uvs;
        }
    }
}
