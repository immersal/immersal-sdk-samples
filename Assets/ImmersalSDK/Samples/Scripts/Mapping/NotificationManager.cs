/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immersal.Samples.Mapping
{
    public class NotificationManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject notification = null;

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
            }
            if (instance != this)
            {
                Debug.LogError("There must be only one NotificationManager object in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }

        public void GenerateNotification(string text)
        {
            if (notification != null)
            {
                GameObject go = Instantiate(notification, transform);
                CreateNotification createNotification = go.GetComponent<CreateNotification>();
                createNotification.SetText(text);
            }
        }
    }
}
