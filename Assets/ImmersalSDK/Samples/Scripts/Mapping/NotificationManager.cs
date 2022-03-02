/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using TMPro;

namespace Immersal.Samples.Mapping
{
    public class NotificationManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_Notification = null;
        private CreateNotification m_CreateNotification = null;

        public static NotificationManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = UnityEngine.Object.FindObjectOfType<NotificationManager>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No NotificationManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }

        private static NotificationManager instance = null;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;

                m_Notification.SetActive(false);
                m_CreateNotification = m_Notification.GetComponent<CreateNotification>();
                }
            if (instance != this)
            {
                Debug.LogError("There must be only one NotificationManager object in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }

        private void InitNotification(CreateNotification.NotificationType type, string text)
        {
            m_Notification.SetActive(true);
            m_CreateNotification.SetIconAndText(type, text);
        }

        public void GenerateNotification(string text)
        {
            InitNotification(CreateNotification.NotificationType.Info, text);
        }

        public void GenerateWarning(string text)
        {
            InitNotification(CreateNotification.NotificationType.Warning, text);
        }

        public void GenerateError(string text)
        {
            InitNotification(CreateNotification.NotificationType.Error, text);
        }

        public void GenerateSuccess(string text)
        {
            InitNotification(CreateNotification.NotificationType.Success, text);
        }
    }
}
