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
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using Immersal.Samples.Util;

namespace Immersal.Samples.Mapping
{
    public struct ARPoint
    {
        public float x;
        public float y;
        public float z;
        public ARPoint(Vector3 pos)
        {
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }
    }

    public class AutomaticCaptureManager : MonoBehaviour
    {
        public const int MAX_VERTICES = 65535;

        [SerializeField] private int m_MaxImages = 40;
        [SerializeField] private float m_ImageInterval = 0.6f;
        [SerializeField] private Image m_CaptureButtonIcon = null;
        [SerializeField] private Sprite m_StartCaptureSprite = null;
        [SerializeField] private Sprite m_StopCaptureSprite = null;
        [SerializeField] private CanvasGroup m_MoveDevicePromptCanvasGroup = null;

        protected bool m_IsMapping = false;
        private int m_ImagesSubmitted = 0;
        private int m_ImagesUploaded = 0;
        private Camera m_MainCamera = null;
        private bool m_CameraHasMoved = false;
        private Vector3 camPrevPos = Vector3.zero;
        private Quaternion camPrevRot = Quaternion.identity;

        private GameObject m_PointCloud = null;
        private Mesh m_Mesh = null;
        private MeshFilter m_MeshFilter = null;
        private MeshRenderer m_MeshRenderer = null;
        private List<ARPoint> m_Points = new List<ARPoint>();
        protected List<ARPoint> m_CurrentPoints = new List<ARPoint>();

        protected AutomaticCapture m_AutomaticCapture = null;
        private ARPointCloudManager m_ArPointCloudManager = null;
        private Fader m_MoveDevicePromptFader = null;

        virtual protected void Start()
        {
            InitMesh();
            m_MainCamera = Camera.main;
            camPrevPos = m_MainCamera.transform.position;
            camPrevRot = m_MainCamera.transform.rotation;
            m_AutomaticCapture = GetComponent<AutomaticCapture>();
            m_ArPointCloudManager = FindObjectOfType<ARPointCloudManager>();
            m_CaptureButtonIcon.sprite = m_StartCaptureSprite;
            m_MoveDevicePromptFader = m_MoveDevicePromptCanvasGroup.GetComponent<Fader>();

            if (m_AutomaticCapture != null)
                m_AutomaticCapture.OnImageUploaded += OnImageUploaded;
            
            if (m_ArPointCloudManager != null)
                m_ArPointCloudManager.pointCloudsChanged += PointCloudManager_pointCloudsChanged;
        }

        virtual protected void OnDisable()
        {
            if (m_AutomaticCapture != null)
                m_AutomaticCapture.OnImageUploaded -= OnImageUploaded;
            
            if (m_ArPointCloudManager != null)
                m_ArPointCloudManager.pointCloudsChanged -= PointCloudManager_pointCloudsChanged;
        }

        virtual protected void Update()
        {
            CheckCameraMovement();
        }

        protected void OnImageUploaded()
        {
            m_ImagesUploaded++;
        }

        private void CheckCameraMovement()
        {
            Vector3 camPos = m_MainCamera.transform.position;
            Quaternion camRot = m_MainCamera.transform.rotation;
            m_CameraHasMoved = (camPos - camPrevPos).magnitude > 0.02f;
        }

        private void PointCloudManager_pointCloudsChanged(ARPointCloudChangedEventArgs obj)
        {
            m_CurrentPoints.Clear();

            List<ARPoint> addedPoints = new List<ARPoint>();
            foreach (var pointCloud in obj.added)
            {
                foreach (var pos in pointCloud.positions)
                {
                    ARPoint newPoint = new ARPoint(pos);
                    addedPoints.Add(newPoint);
                }
            }
            List<ARPoint> updatedPoints = new List<ARPoint>();
            foreach (var pointCloud in obj.updated)
            {
                foreach (var pos in pointCloud.positions)
                {
                    ARPoint newPoint = new ARPoint(pos);
                    updatedPoints.Add(newPoint);
                    m_CurrentPoints.Add(newPoint);
                }
            }
        }

        private void InitMesh()
        {
            m_PointCloud = new GameObject("Automatic Capture Preview Point Cloud", typeof(MeshFilter), typeof(MeshRenderer));
            
            m_MeshFilter = m_PointCloud.GetComponent<MeshFilter>();
            m_MeshRenderer = m_PointCloud.GetComponent<MeshRenderer>();
            m_Mesh = new Mesh();
            m_MeshFilter.mesh = m_Mesh;

            Material material = new Material(Shader.Find("Immersal/Point Cloud"));
            m_MeshRenderer.material = material;
            m_MeshRenderer.material.SetFloat("_PointSize", 20f);
            m_MeshRenderer.material.SetFloat("_PerspectiveEnabled", 0f);
            m_MeshRenderer.material.SetColor("_PointColor", new Color(0.57f, 0.93f, 0.12f));
        }

        public void CreateCloud(Vector3[] points, int totalPoints, Matrix4x4 offset)
        {
            int numPoints = totalPoints >= MAX_VERTICES ? MAX_VERTICES : totalPoints;
            int[] indices = new int[numPoints];
            Vector3[] pts = new Vector3[numPoints];
            Color32[] col = new Color32[numPoints];
            for (int i = 0; i < numPoints; ++i)
            {
                indices[i] = i;
                pts[i] = offset.MultiplyPoint3x4(points[i]);
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

        private void UpdatePoints()
        {
            foreach (ARPoint p in m_CurrentPoints)
            {
                m_Points.Add(p);
            }

            int maxPoints = m_Points.Count >= MAX_VERTICES ? MAX_VERTICES : m_Points.Count;
            Vector3[] points = new Vector3[maxPoints];

            for (int i = 0; i < maxPoints; i++)
            {
                float x = m_Points[i].x;
                float y = m_Points[i].y;
                float z = m_Points[i].z;

                points[i] = new Vector3(x, y, z);
            }

            CreateCloud(points, points.Length);
        }

        private void ResetPoints()
        {
            m_Points.Clear();
            Vector3[] points = new Vector3[0];

            CreateCloud(points, points.Length);
        }

        public void ToggleMapping()
        {
            if (!m_IsMapping)
            {
                StartMapping();
            }
            else
            {
                StopMapping();
            }
        }

        public void CancelMapping()
        {
            StopMapping();
        }

        private void StartMapping()
        {
            ResetPoints();

            m_IsMapping = true;
            m_CaptureButtonIcon.sprite = m_StopCaptureSprite;
            StartCoroutine("CaptureImages");
            Debug.Log("Auto mapping started");
        }

        private void StopMapping()
        {
            ResetPoints();

            m_IsMapping = false;
            m_CaptureButtonIcon.sprite = m_StartCaptureSprite;
            StopCoroutine("CaptureImages");
            Debug.Log("Auto mapping stopped");
        }

        private IEnumerator CaptureImages()
        {
            m_ImagesSubmitted = 0;
            m_ImagesUploaded = 0;
            float t = 0f;
            bool promptActive = false;
            
            while (m_ImagesUploaded < m_MaxImages)
            {
                t += Time.deltaTime;
        
                if (t > m_ImageInterval)
                {
                    if (m_CameraHasMoved)
                    {
                        m_MoveDevicePromptFader.fadeTime = 0.15f;
                        m_MoveDevicePromptFader.FadeOut();
                        promptActive = false;
        
                        float progressStart = (float)m_ImagesSubmitted / (float)m_MaxImages;
                        float progressEnd = progressStart + 1f / (float)m_MaxImages;
        
                        // Capture image
                        m_AutomaticCapture.Capture();

                        t = 0f;
                        m_ImagesSubmitted++;
                        camPrevPos = m_MainCamera.transform.position;
                        camPrevRot = m_MainCamera.transform.rotation;
        
                        UpdatePoints();
                    }
                    else if(!promptActive)
                    {
                        m_MoveDevicePromptFader.fadeTime = 1f;
                        m_MoveDevicePromptFader.FadeIn();
                        promptActive = true;
                    }
                }
        
                yield return null;
            }
        
            StopMapping();
        }

        public void SetMaxImages(float maxImages)
        {
            m_MaxImages = Mathf.RoundToInt(maxImages);
        }

        public void SetInterval(float interval)
        {
            m_ImageInterval = interval;
        }
    }
}