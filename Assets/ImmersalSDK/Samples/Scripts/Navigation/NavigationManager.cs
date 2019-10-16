/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.2.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
        [SerializeField]
        private NavigationEvent onTargetFound = new NavigationEvent();
        [SerializeField]
        private NavigationEvent onTargetNotFound = new NavigationEvent();
        [SerializeField]
        private ARSpace m_ARSpace;
        [SerializeField]
        private GameObject m_NavigationPath = null;
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
        [SerializeField]
        private float m_ArrivedDistanceThreshold = 1.0f;

        private GameObject m_Path = null;
        private Transform m_TargetTransform = null;

        private static NavigationManager instance = null;

        public static NavigationManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = UnityEngine.Object.FindObjectOfType<NavigationManager>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No NavigationManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }

        public float arrivedDistanceThreshold
        {
            get { return m_ArrivedDistanceThreshold; }
        }

        private ARSpace arSpace
        {
            get
            {
                if (m_ARSpace == null)
                    m_ARSpace = GameObject.FindObjectOfType<ARSpace>();
                
                if (m_ARSpace == null)
                {
                    Debug.Log("No AR Space found");
                }
                return m_ARSpace;
            }
            set { m_ARSpace = value; }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("There must be only one NavigationManager object in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }

        private void Start()
        {
            if (m_TargetsList != null)
                m_TargetsList.SetActive(false);

            if (m_StopNavigationButton != null)
                m_StopNavigationButton.SetActive(false);

            if (m_TargetsListIcon != null && m_SelectTargetIcon != null)
                m_TargetsListIcon.sprite = m_ShowListIcon;

            if (m_TargetsListText != null)
                m_TargetsListText.text = "Show Navigation Targets";

            DeletePath();
        }

        public void ToggleTargetsList()
        {
            if (m_TargetsList != null)
            {
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
        }

        public void TryToFindPath(NavigationTargetListButton button)
        {
            if (m_StopNavigationButton != null)
                m_StopNavigationButton.SetActive(false);

            if (m_NavigationPath != null)
            {
                DeletePath();
                m_Path = Instantiate(m_NavigationPath);
                NavigationPath navpath = m_Path.GetComponent<NavigationPath>();

                navpath.arSpace = arSpace;
                m_TargetTransform = navpath.BuildPath(button);
            }
        }

        public void DisplayPathNotFoundNotification()
        {
#if !UNITY_STANDALONE
            Handheld.Vibrate();
#endif
            Mapping.NotificationManager.Instance.GenerateNotification("Path to target could not be found.");

            onTargetNotFound.Invoke(m_TargetTransform);
        }

        public void DisplayNavigation()
        {
            if (m_StopNavigationButton != null)
                m_StopNavigationButton.SetActive(true);
            
            ToggleTargetsList();
        }

        public void DisplayArrivedNotification()
        {
#if !UNITY_STANDALONE
            Handheld.Vibrate();
#endif
            Mapping.NotificationManager.Instance.GenerateNotification("You have arrived!");

            if (m_StopNavigationButton != null)
                m_StopNavigationButton.SetActive(false);

            onTargetFound.Invoke(m_TargetTransform);

            DeletePath();
        }

        public void CloseNavigation()
        {
            if (m_StopNavigationButton != null)
                m_StopNavigationButton.SetActive(false);

            if (m_TargetsList != null)
                m_TargetsList.SetActive(false);

            DeletePath();
        }

        public void DeletePath()
        {
            if (m_Path != null)
                Destroy(m_Path);
        }
    }
}