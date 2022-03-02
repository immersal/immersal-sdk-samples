/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Immersal.Samples.Navigation
{
    public class Waypoint : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public List<Waypoint> neighbours = new List<Waypoint>();

        public Vector3 position
        {
            get
            {
                return m_collider.bounds.center;
            }

            set
            {

            }
        }

        [SerializeField]
        private float m_ClickHoldTime = 1f;
        private float m_timeHeld = 0f;

        private Collider m_collider = null;
        private bool isPressed = false;
        private bool isEditing = false;

        private Camera m_mainCamera = null;
        private float m_DragPlaneDistance = 0f;

        private MeshFilter m_MeshFilter = null;
        private MeshRenderer m_MeshRenderer = null;
        private Mesh m_Mesh = null;

        void Start()
        {
            m_mainCamera = Camera.main;

            InitializeNode();
            NavigationGraphManager.Instance.AddWaypoint(this);
        }

        private void InitializeNode()
        {
            m_collider = GetComponent<Collider>();
            if (m_collider == null)
                m_collider = gameObject.AddComponent<SphereCollider>();

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

        void Update()
        {
            if (isPressed)
            {
                Vector3 projection = m_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, m_DragPlaneDistance));
                DrawPreviewConnection(position, projection, 0.1f, m_Mesh);

                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider != null)
                    {
                        Waypoint wp = hit.collider.GetComponent<Waypoint>();
                        if (wp == this)
                        {
                            m_timeHeld += Time.deltaTime;
                        }
                        else
                        {
                            m_timeHeld = 0f;
                        }
                    }
                }

                if (m_timeHeld >= m_ClickHoldTime && !isEditing)
                {
                    isEditing = true;
                }

                if (isEditing)
                {
                    transform.position = projection;
                }
            }
        }

        private void OnDestroy()
        {
            NavigationGraphManager.Instance.RemoveWaypoint(this);
        }

        private void DrawPreviewConnection(Vector3 startPosition, Vector3 endPosition, float lineWidth, Mesh mesh)
        {
            Vector3 camPos = m_mainCamera.transform.position;
            float length = (endPosition - startPosition).magnitude;

            List<Vector3> points = new List<Vector3>();
            points.Add(startPosition);
            points.Add(endPosition);

            Vector3[] x = new Vector3[2];
            Vector3[] z = new Vector3[2];
            Vector3[] y = new Vector3[2];
            Quaternion[] q = new Quaternion[2];
            Matrix4x4[] m = new Matrix4x4[2];

            z[0] = (endPosition - startPosition).normalized;
            z[1] = (startPosition - endPosition).normalized;

            y[0] = (camPos - startPosition).normalized;
            y[1] = (camPos - endPosition).normalized;

            x[0] = Vector3.Cross(y[0], z[0]);
            x[1] = Vector3.Cross(y[1], -z[1]);

            //Debug.DrawRay(startPosition, x[0] * 0.4f, Color.red);
            //Debug.DrawRay(endPosition, x[0] * 0.4f, Color.red);
            //Debug.DrawRay(startPosition, y[0] * 0.4f, Color.green);
            //Debug.DrawRay(endPosition, y[0] * 0.4f, Color.green);
            //Debug.DrawRay(startPosition, z[0] * 0.4f, Color.blue);
            //Debug.DrawRay(endPosition, z[0] * 0.4f, Color.blue);

            Vector3[] vtx = new Vector3[4];
            int[] idx = new int[6];
            Vector2[] uv = new Vector2[4];

            vtx[0] = transform.worldToLocalMatrix.MultiplyPoint(new Vector3(startPosition.x, startPosition.y, startPosition.z) - (x[0] * lineWidth * 0.5f));
            vtx[1] = transform.worldToLocalMatrix.MultiplyPoint(new Vector3(endPosition.x, endPosition.y, endPosition.z) - (x[1] * lineWidth * 0.5f));
            vtx[2] = transform.worldToLocalMatrix.MultiplyPoint(new Vector3(endPosition.x, endPosition.y, endPosition.z) + (x[1] * lineWidth * 0.5f));
            vtx[3] = transform.worldToLocalMatrix.MultiplyPoint(new Vector3(startPosition.x, startPosition.y, startPosition.z) + (x[0] * lineWidth * 0.5f));

            idx[0] = 0;
            idx[1] = 1;
            idx[2] = 3;
            idx[3] = 1;
            idx[4] = 2;
            idx[5] = 3;

            uv[0] = new Vector2(0f, 0f);
            uv[1] = new Vector2(1f, 0f);
            uv[2] = new Vector2(1f, 1f);
            uv[3] = new Vector2(0f, 1f);

            mesh.Clear();
            mesh.vertices = vtx;
            mesh.triangles = idx;
            mesh.uv = uv;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode)
            {
                Debug.Log("pointer down");
                Debug.Log(m_timeHeld);

                isPressed = true;
                m_DragPlaneDistance = Vector3.Dot(transform.position - m_mainCamera.transform.position, m_mainCamera.transform.forward) / m_mainCamera.transform.forward.sqrMagnitude;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            isEditing = false;
            m_timeHeld = 0f;

            m_Mesh.Clear();

            if (Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider != null)
                    {
                        Waypoint wp = hit.collider.GetComponent<Waypoint>();
                        if (wp != null && wp != this)
                        {
                            if (!neighbours.Contains(wp))
                            {
                                neighbours.Add(wp);
                            }
                            if (!wp.neighbours.Contains(this))
                            {
                                wp.neighbours.Add(this);
                            }
                        }
                    }
                }
            }
        }
    }
}