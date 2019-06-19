/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Immersal.Samples.Navigation
{
    public class BuildNavigationPath : MonoBehaviour
    {
        [HideInInspector]
        public Immersal.AR.ARSpace m_ARSpace;
        [SerializeField]
        private GameObject m_WaypointPrefab = null;
        [SerializeField]
        private GameObject m_ArrowPrefab = null;
        [SerializeField]
        private float m_ArrivedDistanceThreshold = 1.0f;
        [SerializeField]
        private float m_WaypointStepSize = 0.33f;
        [SerializeField]
        private float m_WaypointScale = 1f;

        private Transform m_CameraTransform;
        private Transform m_TargetTransform;
        private GameObject m_Arrow;
        private LookTowardsTarget m_ArrowDirection;

        private List<GameObject> m_Waypoints = new List<GameObject>();

        private bool m_NavigationArrived = false;
        private bool m_IsNavigating = false;
        private bool m_IsInitialized = false;
        private bool m_NavigationStarted = false;

        public void BuildPath(NavigationTargetListButton button)
        {
            m_CameraTransform = Camera.main.transform;
            m_TargetTransform = button.targetObject.transform;

            // check if main camera exists
            if (m_CameraTransform == null)
            {
                Debug.Log("Could not find the main camera. Do you have the MainCamera tag applied?");
                return;
            }

            // check if target from button was found
            if (m_TargetTransform == null)
            {
                Debug.Log("Target not found");
                return;
            }

            // check if AR Space exists
            if (m_ARSpace == null)
            {
                m_ARSpace = gameObject.GetComponentInParent<Immersal.AR.ARSpace>();
                if (m_ARSpace == null)
                {
                    Debug.Log("No AR Space found. Does one exist in scene?");
                    return;
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
        }

        public void ClearPath()
        {
            m_IsNavigating = false;
            foreach (GameObject go in m_Waypoints)
            {
                Destroy(go);
            }
            m_Waypoints.Clear();

            if (m_Arrow != null && m_Arrow.activeInHierarchy)
            {
                m_Arrow.SetActive(false);
            }
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

                Vector3 startPosition = ARSpaceToUnity(m_ARSpace.transform, m_CameraTransform.position);
                Vector3 targetPosition = ARSpaceToUnity(m_ARSpace.transform, m_TargetTransform.position);

                Vector3 currentPositionInUnitySpace = ARSpaceToUnity(m_ARSpace.transform, m_CameraTransform.position);
                Vector3 delta = targetPosition - currentPositionInUnitySpace;
                float distanceToTarget = new Vector3(delta.x, 0f, delta.z).magnitude;

                if (!m_NavigationArrived && distanceToTarget < m_ArrivedDistanceThreshold)
                {
                    m_NavigationArrived = true;
                    NavigationManager.Instance.DisplayArrivedNotification();
                    return;
                }

                if (NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path))
                {
                    if (!m_NavigationStarted)
                    {
                        NavigationManager.Instance.DisplayNavigation();
                        m_NavigationStarted = true;
                        m_NavigationArrived = false;
                    }

                    ClearPath();
                    m_IsNavigating = true;
                    CreateWaypoints(path.corners);
                }
                else
                {
                    if (!keepOldIfNotFound)
                        NavigationManager.Instance.DisplayPathNotFoundNotification();
                }
            }
        }

        private void CreateWaypoints(Vector3[] corners)
        {
            List<Vector3> points = new List<Vector3>();

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 start = corners[i];
                Vector3 end = corners[i + 1];
                float division = Mathf.Floor((end - start).magnitude / m_WaypointStepSize);

                for (int j = 1; j < division; j++)
                {
                    float blend = j / (division);
                    Vector3 p = Vector3.Lerp(start, end, blend);
                    points.Add(p);
                }
            }

            if (points.Count > 1)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 direction;
                    if (i == 0)
                    {
                        direction = UnityToARSpace(m_ARSpace.transform, points[1]) - UnityToARSpace(m_ARSpace.transform, points[0]);
                    }
                    else
                    {
                        direction = UnityToARSpace(m_ARSpace.transform, points[i]) - UnityToARSpace(m_ARSpace.transform, points[i - 1]);
                    }

                    Vector3 pos = UnityToARSpace(m_ARSpace.transform, points[i]);
                    Quaternion rot = Quaternion.LookRotation(direction, m_ARSpace.transform.up);
                    GameObject go = Instantiate(m_WaypointPrefab, pos, rot, transform);
                    go.transform.localScale = new Vector3(m_WaypointScale, m_WaypointScale, m_WaypointScale);
                    m_Waypoints.Add(go);
                }
            }

            // update compass m_Arrow direction
            if (m_Arrow != null && !m_Arrow.activeInHierarchy)
            {
                m_Arrow.SetActive(true);
            }

            if (corners.Length > 1 && m_ArrowDirection != null)
            {
                Vector3 cameraHeight = ARSpaceToUnity(m_ARSpace.transform, m_CameraTransform.position);
                Vector3 nextPoint = new Vector3(corners[1].x, cameraHeight.y, corners[1].z);
                nextPoint = UnityToARSpace(m_ARSpace.transform, nextPoint);
                m_ArrowDirection.LookAt(nextPoint, m_ARSpace.transform.up);
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

        private Vector3 ARSpaceToUnity(Transform space, Vector3 pos)
        {
            Matrix4x4 m = space.worldToLocalMatrix;
            pos = m.MultiplyPoint(pos);

            return pos;
        }

        private Vector3 UnityToARSpace(Transform space, Vector3 pos)
        {
            Matrix4x4 m = space.localToWorldMatrix;
            pos = m.MultiplyPoint(pos);

            return pos;
        }
    }
}