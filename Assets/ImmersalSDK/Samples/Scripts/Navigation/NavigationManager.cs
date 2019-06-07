/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Immersal.Samples.Navigation
{
    public class NavigationManager : MonoBehaviour
    {
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

        private GameObject m_Path = null;

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

        private static NavigationManager instance = null;

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
            {
                m_TargetsList.SetActive(false);
            }

            if (m_StopNavigationButton != null)
            {
                m_StopNavigationButton.SetActive(false);
            }

            if (m_TargetsListIcon != null && m_SelectTargetIcon != null)
            {
                m_TargetsListIcon.sprite = m_ShowListIcon;
            }
            if (m_TargetsListText != null)
            {
                m_TargetsListText.text = "Show Navigation Targets";
            }

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
            {
                m_StopNavigationButton.SetActive(false);
            }

            if (m_NavigationPath != null)
            {
                DeletePath();
                m_Path = Instantiate(m_NavigationPath, transform);
                m_Path.GetComponent<BuildNavigationPath>().BuildPath(button);
            }
        }

        public void DisplayPathNotFoundNotification()
        {
#if !UNITY_STANDALONE
            Handheld.Vibrate();
#endif
            Mapping.NotificationManager.Instance.GenerateNotification("Path to target could not be found.");
        }

        public void DisplayNavigation()
        {
            if (m_StopNavigationButton != null)
            {
                m_StopNavigationButton.SetActive(true);
            }

            ToggleTargetsList();
        }

        public void DisplayArrivedNotification()
        {
#if !UNITY_STANDALONE
            Handheld.Vibrate();
#endif
            Mapping.NotificationManager.Instance.GenerateNotification("You have arrived!");

            if (m_StopNavigationButton != null)
            {
                m_StopNavigationButton.SetActive(false);
            }

            DeletePath();
        }

        public void CloseNavigation()
        {
            if (m_StopNavigationButton != null)
            {
                m_StopNavigationButton.SetActive(false);
            }

            if (m_TargetsList != null)
            {
                m_TargetsList.SetActive(false);
            }

            DeletePath();
        }

        public void DeletePath()
        {
            if (m_Path != null)
            {
                Destroy(m_Path);
            }
        }
    }
}