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
    [System.Serializable]
    public class NavigationEvent : UnityEvent<Transform>
    {
    }

    public class NavigationManager : MonoBehaviour
    {
        // Navigation Visualization references
        [Header("Visualization")]
        [SerializeField]
        private GameObject m_navigationPathPrefab = null;
        /*
        [SerializeField]
        private GameObject m_navigationArrowPrefab = null;
        */

        // UI Object references
        [Header("UI Objects")]
        [SerializeField]
        private GameObject m_TargetsList = null;
        [SerializeField]
        private Sprite m_ShowListIcon = null;
        [SerializeField]
        private Sprite m_SelectTargetIcon = null;
        [SerializeField]
        private Image m_TargetsListIcon = null;
        [SerializeField]
        private TextMeshProUGUI m_TargetsListText = null;
        [SerializeField]
        private GameObject m_StopNavigationButton = null;

        // Navigation Settings
        private enum NavigationMode { NavMesh, Graph};
        [Header("Settings")]
        [SerializeField]
        private NavigationMode m_navigationMode = NavigationMode.NavMesh;
        public bool inEditMode = false;
        /*
        [SerializeField]
        private bool m_showArrow = true;
        */
        [SerializeField]
        private float m_ArrivedDistanceThreshold = 1.0f;
        [SerializeField]
        private float m_pathWidth = 0.3f;
        [SerializeField]
        private float m_heightOffset = 0.5f;

        // Navigation State Events
        [Header("Events")]
        [SerializeField]
        private NavigationEvent onTargetFound = new NavigationEvent();
        [SerializeField]
        private NavigationEvent onTargetNotFound = new NavigationEvent();

        private ARSpace m_arSpace = null;
        private bool m_managerInitialized = false;
        private bool m_navigationActive = false;
        private Transform m_targetTransform = null;
        private IsNavigationTarget m_NavigationTarget = null;
        private Transform m_playerTransform = null;
        private GameObject m_navigationPathObject = null;
        private NavigationPath m_navigationPath = null;

        [SerializeField]
        private NavigationGraphManager m_NavigationGraphManager = null;

        private enum NavigationState { NotNavigating, Navigating};
        private NavigationState m_navigationState = NavigationState.NotNavigating;

        private static NavigationManager instance = null;
        public static NavigationManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = FindObjectOfType<NavigationManager>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No NavigationManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }

        public bool navigationActive
        {
            get { return m_navigationActive; }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("NavigationManager: There must be only one NavigationManager in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }

        private void Start()
        {
            InitializeNavigationManager();

            if (m_managerInitialized)
            {
                m_TargetsListIcon.sprite = m_ShowListIcon;
                m_TargetsListText.text = "Show Navigation Targets";
            }
        }

        private void Update()
        {
            if(m_managerInitialized && m_navigationState == NavigationState.Navigating)
            {
                TryToFindPath(m_NavigationTarget);
            }
        }

        public void InitializeNavigation(NavigationTargetListButton button)
        {
            if (!m_managerInitialized)
            {
                Debug.LogWarning("NavigationManager: Navigation Manager not properly initialized.");
                return;
            }

            m_targetTransform = button.targetObject.transform;
            m_NavigationTarget = button.targetObject.GetComponent<IsNavigationTarget>();
            TryToFindPath(m_NavigationTarget);
        }

        public void TryToFindPath(IsNavigationTarget navigationTarget)
        {
            List<Vector3> corners;

            // Convert to Unity's world space coordinates to use NavMesh
            Vector3 startPosition = m_playerTransform.position;
            Vector3 targetPosition = navigationTarget.position;

            Vector3 delta = targetPosition - startPosition;
            float distanceToTarget = new Vector3(delta.x, delta.y, delta.z).magnitude;

            if (distanceToTarget < m_ArrivedDistanceThreshold)
            {
                m_navigationActive = false;

                m_navigationState = NavigationState.NotNavigating;
                UpdateNavigationUI(m_navigationState);

                DisplayArrivedNotification();
                return;
            }

            switch (m_navigationMode)
            {
                case NavigationMode.NavMesh:

                    startPosition = ARSpaceToUnity(m_arSpace.transform, m_arSpace.initialOffset, startPosition);
                    targetPosition = ARSpaceToUnity(m_arSpace.transform, m_arSpace.initialOffset, targetPosition);

                    corners = FindPathNavMesh(startPosition, targetPosition);
                    if (corners.Count >= 2)
                    {
                        m_navigationActive = true;

                        m_navigationState = NavigationState.Navigating;
                        UpdateNavigationUI(m_navigationState);

                        m_navigationPath.GeneratePath(corners, m_arSpace.transform.up);
                        m_navigationPath.pathWidth = m_pathWidth;
                    }
                    else
                    {
                        Mapping.NotificationManager.Instance.GenerateNotification("Path to target not found.");
                        UpdateNavigationUI(m_navigationState);
                    }
                    break;

                case NavigationMode.Graph:

                    corners = m_NavigationGraphManager.FindPath(startPosition, targetPosition);

                    if (corners.Count >= 2)
                    {
                        m_navigationActive = true;

                        m_navigationState = NavigationState.Navigating;
                        UpdateNavigationUI(m_navigationState);

                        m_navigationPath.GeneratePath(corners, m_arSpace.transform.up);
                        m_navigationPath.pathWidth = m_pathWidth;
                    }
                    else
                    {
                        Mapping.NotificationManager.Instance.GenerateNotification("Path to target not found.");
                        UpdateNavigationUI(m_navigationState);
                    }
                    break;
            }
        }

        private List<Vector3> FindPathNavMesh(Vector3 startPosition, Vector3 targetPosition)
        {
            NavMeshPath path = new NavMeshPath();
            List<Vector3> collapsedCorners = new List<Vector3>();

            if (NavMesh.CalculatePath(startPosition, targetPosition, NavMesh.AllAreas, path))
            {
                List<Vector3> corners = new List<Vector3>(path.corners);

                for (int i = 0; i < corners.Count; i++)
                {
                    corners[i] = corners[i] + new Vector3(0f, m_heightOffset, 0f);
                    corners[i] = UnityToARSpace(m_arSpace.transform, m_arSpace.initialOffset, corners[i]);
                }

                for (int i = 0; i < corners.Count - 1; i++)
                {
                    Vector3 currentPoint = corners[i];
                    Vector3 nextPoint = corners[i + 1];
                    float threshold = 0.75f;

                    if (Vector3.Distance(currentPoint, nextPoint) > threshold)
                    {
                        collapsedCorners.Add(currentPoint);
                    }
                }

                collapsedCorners.Add(corners[corners.Count - 1]);
            }

            return collapsedCorners;
        }

        public void ToggleTargetsList()
        {
            if (!m_managerInitialized)
            {
                Debug.LogWarning("NavigationManager: Navigation Manager not properly initialized.");
                return;
            }

            if (m_TargetsList.activeInHierarchy)
            {
                m_TargetsList.SetActive(false);
                if (m_ShowListIcon != null && m_TargetsListIcon != null)
                {
                    m_TargetsListIcon.sprite = m_ShowListIcon;
                }
                if (m_TargetsListText != null)
                {
                    m_TargetsListText.text = "Show Navigation Targets";
                }
            }
            else
            {
                m_TargetsList.SetActive(true);
                m_TargetsList.GetComponent<NavigationTargetListControl>().GenerateButtons();
                if (m_SelectTargetIcon != null && m_TargetsListIcon != null)
                {
                    m_TargetsListIcon.sprite = m_SelectTargetIcon;
                }
                if (m_TargetsListText != null)
                {
                    m_TargetsListText.text = "Select Navigation Target";
                }
            }
        }

        public void ToggleEditMode()
        {
            inEditMode = !inEditMode;
        }

        public void DisplayPathNotFoundNotification()
        {
#if !(UNITY_STANDALONE || PLATFORM_LUMIN)
            Handheld.Vibrate();
#endif
            Mapping.NotificationManager.Instance.GenerateNotification("Path to target could not be found.");
            onTargetNotFound.Invoke(m_targetTransform);
        }

        public void DisplayArrivedNotification()
        {
#if !(UNITY_STANDALONE || PLATFORM_LUMIN)
            Handheld.Vibrate();
#endif
            Mapping.NotificationManager.Instance.GenerateNotification("You have arrived!");
            onTargetFound.Invoke(m_targetTransform);
        }

        public void StopNavigation()
        {
            m_navigationActive = false;

            m_navigationState = NavigationState.NotNavigating;
            UpdateNavigationUI(m_navigationState);

            Mapping.NotificationManager.Instance.GenerateNotification("Navigation stopped.");
        }

        private void UpdateNavigationUI(NavigationState navigationState)
        {
            switch(navigationState)
            {
                case NavigationState.NotNavigating:
                    m_StopNavigationButton.SetActive(false);
                    m_navigationPathObject.SetActive(false);
                    break;
                case NavigationState.Navigating:
                    m_StopNavigationButton.SetActive(true);
                    m_navigationPathObject.SetActive(true);
                    break;
            }
        }

        private void InitializeNavigationManager()
        {
            if (m_arSpace == null)
            {
                m_arSpace = FindObjectOfType<ARSpace>();

                if (m_arSpace == null)
                {
                    Debug.LogWarning("NavigationManager: No AR Space found in scene, ensure one exists.");
                    return;
                }
            }

            m_NavigationGraphManager = GetComponent<NavigationGraphManager>();
            if (m_NavigationGraphManager == null)
            {
                Debug.LogWarning("NavigationManager: Missing Navigation Graph Manager component.");
                return;
            }

            m_playerTransform = Camera.main.transform;
            if (m_playerTransform == null)
            {
                Debug.LogWarning("NavigationManager: Could not find the main camera. Do you have the MainCamera tag applied?");
                return;
            }

            if (m_navigationPathPrefab == null)
            {
                Debug.LogWarning("NavigationManager: Missing navigation path object reference.");
                return;
            }

            if(m_navigationPathPrefab != null)
            {
                if (m_navigationPathObject == null)
                {
                    m_navigationPathObject = Instantiate(m_navigationPathPrefab);
                    m_navigationPathObject.SetActive(false);
                    m_navigationPath = m_navigationPathObject.GetComponent<NavigationPath>();
                }

                if(m_navigationPath == null)
                {
                    Debug.LogWarning("NavigationManager: NavigationPath component in Navigation path is missing.");
                    return;
                }
            }

            if (m_TargetsList == null)
            {
                Debug.LogWarning("NavigationManager: Navigation Targets List reference is missing.");
                return;
            }

            if (m_ShowListIcon == null)
            {
                Debug.LogWarning("NavigationManager: \"Show List\" icon is missing.");
                return;
            }

            if (m_SelectTargetIcon == null)
            {
                Debug.LogWarning("NavigationManager: \"Select Target\" icon is missing.");
                return;
            }

            if (m_TargetsListIcon == null)
            {
                Debug.LogWarning("NavigationManager: \"Targets List\" icon reference is missing.");
                return;
            }

            if (m_TargetsListText == null)
            {
                Debug.LogWarning("NavigationManager: \"Targets List\" text reference is missing.");
                return;
            }

            if (m_StopNavigationButton == null)
            {
                Debug.LogWarning("NavigationManager: Stop Navigation Button reference is missing.");
                return;
            }

            m_managerInitialized = true;
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