/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using UnityEngine;
using TMPro;

namespace Immersal.Samples.Mapping
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CreateNotification : MonoBehaviour {
        [SerializeField]
        private TextMeshProUGUI textMeshProUGUI = null;
        [SerializeField]
        private float durationBeforeFadeOut = 0.5f;
        [SerializeField]
        private float fadeOutDuration = 1f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        private int minWidth = 192;
        private int maxWidth = 800;
        private int minHeight = 128;
        private int maxHeight = 512;
        private int padding = 32;

        public void SetText(string text)
        {
            if(textMeshProUGUI == null)
            {
                Destroy(gameObject, 1f);
                return;
            }

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            textMeshProUGUI.text = text;

            Vector2 size = textMeshProUGUI.GetPreferredValues(textMeshProUGUI.text);

            float lines = Mathf.Ceil(size.x / maxWidth);
            int width = Mathf.Max(Mathf.Min((int)size.x, maxWidth), minWidth);
            int height = Mathf.Max(Mathf.Min((int)(size.y * lines), maxHeight), minHeight) + padding;

            rectTransform.sizeDelta = new Vector2(width, height);
            textMeshProUGUI.ForceMeshUpdate();

            StartCoroutine(FadeOut(fadeOutDuration, durationBeforeFadeOut));
        }

        IEnumerator FadeOut(float duration, float startAfter)
        {
            for (float t = -startAfter; t < 1f; t += (Time.deltaTime / duration))
            {
                float alpha = Mathf.Lerp(1f, 0f, Mathf.Pow(Mathf.Max(t, 0f), 1.6f));
                canvasGroup.alpha = alpha;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}