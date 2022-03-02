/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Immersal.Samples.Mapping
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CreateNotification : MonoBehaviour
    {
        [HideInInspector]
        public enum NotificationType { Success, Info, Warning, Error}
        [SerializeField]
        private RectTransform m_RectTransform = null;
        [SerializeField]
        private TextMeshProUGUI m_TextMeshProUGUI = null;

        [SerializeField]
        private Image m_Image = null;
        [SerializeField]
        private float m_DurationBeforeFadeOut = 0.5f;
        [SerializeField]
        private float m_FadeOutDuration = 1f;
        [SerializeField]
        private Sprite m_SuccessIcon = null;
        [SerializeField]
        private Sprite m_InfoIcon = null;
        [SerializeField]
        private Sprite m_WarningIcon = null;
        [SerializeField]
        private Sprite m_ErrorIcon = null;

        private int m_MinWidth = 192;
        private int m_MaxWidth = 800;
        private int m_MinHeight = 128;
        private int m_MaxHeight = 512;
        private int m_Padding = 32;

        public void SetIconAndText(NotificationType notificationType, string text)
        {
            m_TextMeshProUGUI.text = text;

            Sprite icon = m_InfoIcon;
            int iconSize = 64;

            switch (notificationType)
            {
                case NotificationType.Success:
                    icon = m_SuccessIcon;
                    break;
                case NotificationType.Info:
                    icon = m_InfoIcon;
                    break;
                case NotificationType.Warning:
                    icon = m_WarningIcon;
                    break;
                case NotificationType.Error:
                    icon = m_ErrorIcon;
                    break;
                default:
                    icon = m_InfoIcon;
                    break;
            }

            m_Image.sprite = icon;

            Vector2 size = m_TextMeshProUGUI.GetPreferredValues(m_TextMeshProUGUI.text);

            float lines = Mathf.Ceil(size.x / m_MaxWidth);
            int width = Mathf.Max(Mathf.Min((int)size.x, m_MaxWidth), m_MinWidth) + 3 * iconSize;
            int height = Mathf.Max(Mathf.Min((int)(size.y * lines), m_MaxHeight), m_MinHeight) + m_Padding;

            m_RectTransform.sizeDelta = new Vector2(width, height);
            m_TextMeshProUGUI.ForceMeshUpdate();

            StartCoroutine(FadeOut(m_FadeOutDuration, m_DurationBeforeFadeOut));
        }

        private void Start()
        {
            StartCoroutine(FadeOut(m_FadeOutDuration, m_DurationBeforeFadeOut));
        }

        IEnumerator FadeOut(float duration, float startAfter)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            for (float t = -startAfter; t < 1f; t += (Time.deltaTime / duration))
            {
                float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(Mathf.Max(t, 0f), 1.6f));
                canvasGroup.alpha = alpha;
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}