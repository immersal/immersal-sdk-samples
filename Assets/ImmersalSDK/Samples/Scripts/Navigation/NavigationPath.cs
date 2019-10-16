/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.2.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Immersal.AR;

namespace Immersal.Samples.Navigation
{
    [RequireComponent(typeof(PathMeshGenerator))]
    public class NavigationPath : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_ArrowPrefab = null;
        [SerializeField]
        private float m_yOffset = 0.5f;

        private Transform m_CameraTransform;
        private Transform m_TargetTransform;
        private GameObject m_Arrow;
        private LookTowardsTarget m_ArrowDirection;
        private PathMeshGenerator m_PathMeshGenerator;
        private NavigationManager m_NavigationManager;

        private bool m_NavigationArrived = false;
        private bool m_IsNavigating = false;
        private bool m_IsInitialized = false;
        private bool m_NavigationStarted = false;
        private ARSpace m_ARSpace;

        public ARSpace arSpace
        {
            get { return m_ARSpace; }
            set { m_ARSpace = value; }
        }

        public NavigationManager navigationManager
        {
            get
            {
                if (m_NavigationManager == null)
                    m_NavigationManager = NavigationManager.Instance;
                
                if (m_NavigationManager == null)
                {
                    Debug.Log("No NavigationManager found. Ensure one exists in the scene.");
                }
                
                return m_NavigationManager;
            }
        }

        public Transform BuildPath(NavigationTargetListButton button)
        {
            m_CameraTransform = Camera.main.transform;
            m_TargetTransform = button.targetObject.transform;

            // check if main camera exists
            if (m_CameraTransform == null)
            {
                Debug.Log("Could not find the main camera. Do you have the MainCamera tag applied?");
                return null;
            }

            // check if target from button was found
            if (m_TargetTransform == null)
            {
                Debug.Log("Target not found");
                return null;
            }

            // check if AR Space exists
            if (m_ARSpace == null)
            {
                arSpace = gameObject.GetComponentInParent<Immersal.AR.ARSpace>();
                if (arSpace == null)
                {
                    Debug.Log("No AR Space found. Does one exist in scene?");
                    return null;
                }
            }

            m_IsInitialized = true;
            m_NavigationArrived = false;
            UpdatePath(false);

            // create the compass m_Arrow
            if (m_ArrowPrefab != null)
            {
                m_Arrow = Instantiate(m_ArrowPrefab, m_CameraTransform.transform);
                m_ArrowDirection = m_Arrow.GetComponent<LookTowardsTarget>();
                m_Arrow.SetActive(false);
            }

            return m_TargetTransform;
        }

        public void ClearPath()
        {
            m_IsNavigating = false;

            if (m_Arrow != null && m_Arrow.activeInHierarchy)
                m_Arrow.SetActive(false);

            if (m_PathMeshGenerator)
                m_PathMeshGenerator.ClearMesh();
        }

        private void OnEnable()
        {
            if (!m_PathMeshGenerator)
                m_PathMeshGenerator = GetComponent<PathMeshGenerator>();
        }

        private void Update()
        {
            if (m_IsNavigating)
            {
                if (m_TargetTransform == null)
                {
                    m_IsNavigating = false;
                    return;
                }
                UpdatePath(true);
            }
        }

        public void UpdatePath(bool keepOldIfNotFound)
        {
            if (m_IsInitialized)
            {
                NavMeshPath path = new NavMeshPath();

                Vector3 startPosition = ARSpaceToUnity(m_ARSpace.transform, m_ARSpace.initialOffset, m_CameraTransform.position);
                Vector3 targetPosition = ARSpaceToUnity(m_ARSpace.transform, m_ARSpace.initialOffset, m_TargetTransform.position);

                Vector3 currentPositionInUnitySpace = ARSpaceToUnity(m_ARSpace.transform, m_ARSpace.initialOffset, m_CameraTransform.position);
                Vector3 delta = targetPosition - currentPositionInUnitySpace;
                float distanceToTarget = new Vector3(delta.x, 0f, delta.z).magnitude;

                if (!m_NavigationArrived && distanceToTarget < navigationManager.arrivedDistanceThreshold)
                {
                    m_NavigationArrived = true;
                    navigationManager.DisplayArrivedNotification();
                    return;
                }

                if (NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path))
                {
                    if (!m_NavigationStarted)
                    {
                        navigationManager.DisplayNavigation();
                        m_NavigationStarted = true;
                        m_NavigationArrived = false;
                    }

                    m_IsNavigating = true;

                    List<Vector3> corners = new List<Vector3>(path.corners);
                    List<Vector3> collapsedCorners = new List<Vector3>();

                    for (int i = 0; i < corners.Count; i++)
                    {
                        corners[i] = corners[i] + new Vector3(0f, m_yOffset, 0f);
                        corners[i] = UnityToARSpace(m_ARSpace.transform, m_ARSpace.initialOffset, corners[i]);
                    }

                    for(int i = 0; i < corners.Count-1; i++)
                    {
                        Vector3 cp = corners[i];
                        Vector3 np = corners[i+1];
                        float threshold = 0.75f;

                        if (Vector3.Distance(cp, np) > threshold)
                        {
                            collapsedCorners.Add(cp);
                            //Debug.DrawLine(cp, cp + Vector3.up, Color.red);
                        }
                    }
                    collapsedCorners.Add(corners[corners.Count - 1]);

                    m_PathMeshGenerator.GeneratePath(collapsedCorners, m_ARSpace.transform.up);

                    // update compass m_Arrow direction
                    if (m_Arrow != null && !m_Arrow.activeInHierarchy)
                    {
                        m_Arrow.SetActive(true);
                    }

                    if (corners.Count > 1 && m_ArrowDirection != null)
                    {
                        Vector3 nextPoint = new Vector3(corners[1].x, Camera.main.transform.position.y, corners[1].z);
                        m_ArrowDirection.LookAt(nextPoint, Vector3.up);
                    }
                }
                else
                {
                    if (!keepOldIfNotFound)
                        navigationManager.DisplayPathNotFoundNotification();
                }
            }
        }

        private void OnDisable()
        {
            ClearPath();
            if (m_Arrow != null)
            {
                m_Arrow.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            ClearPath();
            if (m_Arrow != null)
            {
                Destroy(m_Arrow);
            }
        }

        private Vector3 ARSpaceToUnity(Transform arspace, Matrix4x4 arspaceoffset, Vector3 pos)
        {
            Matrix4x4 m = arspace.worldToLocalMatrix;
            pos = m.MultiplyPoint(pos);
            pos = arspaceoffset.MultiplyPoint(pos);
            return pos;
        }

        private Vector3 ARSpaceToUnity(Transform arspace, Vector3 pos)
        {
            pos = ARSpaceToUnity(arspace, Matrix4x4.identity, pos);
            return pos;
        }

        private Vector3 UnityToARSpace(Transform arspace, Matrix4x4 arspaceOffset, Vector3 pos)
        {
            pos = arspaceOffset.inverse.MultiplyPoint(pos);
            Matrix4x4 m = arspace.localToWorldMatrix;
            pos = m.MultiplyPoint(pos);
            return pos;
        }

        private Vector3 UnityToARSpace(Transform arspace, Vector3 pos)
        {
            pos = UnityToARSpace(arspace, Matrix4x4.identity, pos);
            return pos;
        }
    }
}