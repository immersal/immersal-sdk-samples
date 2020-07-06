using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immersal.Samples.Util
{
    [RequireComponent(typeof(UnityEngine.CanvasGroup))]
    public class Fader : MonoBehaviour
    {
        public float fadeTime = 1f;
        private IEnumerator m_fade;
        private CanvasGroup m_canvasGroup;

        void Start()
        {
        }

        void OnEnable()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        public void ToggleFade(bool isOn)
        {
            if (isOn)
                FadeIn();
            else
                FadeOut();
        }

        public void FadeOut()
        {
            if (m_fade != null)
            {
                StopCoroutine(m_fade);
            }

            if (!gameObject.activeInHierarchy)
                return;
            else
                m_canvasGroup.alpha = 1f;
            
            m_fade = FadeOutCoroutine();
            StartCoroutine(m_fade);
        }

        public void FadeIn()
        {
            if (m_fade != null)
            {
                StopCoroutine(m_fade);
            }

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
                m_canvasGroup.alpha = 0f;
            }

            m_fade = FadeInCoroutine();
            StartCoroutine(m_fade);
        }

        IEnumerator FadeOutCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            while (m_canvasGroup.alpha > 0)
            {
                m_canvasGroup.alpha -= Time.deltaTime / fadeTime;
                yield return null;
            }
            gameObject.SetActive(false);
            yield return null;
        }

        IEnumerator FadeInCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            while (m_canvasGroup.alpha < 1f)
            {
                m_canvasGroup.alpha += Time.deltaTime / fadeTime;
                yield return null;
            }
            m_canvasGroup.alpha = 1f;
            yield return null;
        }
    }
}
