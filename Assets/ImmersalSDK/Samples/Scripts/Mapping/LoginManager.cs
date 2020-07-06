/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Immersal.REST;

namespace Immersal.Samples.Mapping
{
    public class LoginManager : MonoBehaviour, IJobHost
    {
        public GameObject loginPanel;
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TextMeshProUGUI loginErrorText;
        public float fadeOutTime = 1f;
        public event LoginEvent OnLogin = null;
        public delegate void LoginEvent();

        private IEnumerator m_FadeAlpha;
        private CanvasGroup m_CanvasGroup;
        private ImmersalSDK m_Sdk;
        private ToggleMappingMode m_ToggleMappingMode;
        private MapperBase m_Mapper = null;

        public string server
        {
            get { return m_Sdk.localizationServer; }
        }

        public string token
        {
            get { return m_Sdk.developerToken; }
        }

        void Start()
        {
            m_Sdk = ImmersalSDK.Instance;
            m_Mapper = UnityEngine.Object.FindObjectOfType<MapperBase>();

            m_CanvasGroup = loginPanel.GetComponent<CanvasGroup>();
            m_ToggleMappingMode = loginPanel.GetComponent<ToggleMappingMode>();

            Invoke("FillFields", 0.1f);
        }

        void FillFields()
        {
            emailField.text = PlayerPrefs.GetString("login", "");
            passwordField.text = PlayerPrefs.GetString("password", "");
        }

        public void OnLoginClick()
        {
            if (emailField.text.Length > 0 && passwordField.text.Length > 0)
            {
                Login();
            }
        }

        private void Login()
        {
            CoroutineJobLogin j = new CoroutineJobLogin();
            j.host = this;
            j.username = emailField.text;
            j.password = passwordField.text;
            j.OnStart += () =>
            {
                loginErrorText.gameObject.SetActive(false);
            };
            j.OnError += (UnityWebRequest request) =>
            {
                if (request.responseCode == (long)HttpStatusCode.BadRequest)
                {
                    loginErrorText.text = "Login failed, please try again";
                    loginErrorText.gameObject.SetActive(true);
                }
            };
            j.OnResult += (SDKLoginResult result) =>
            {
                if (result.error == "none")
                {
                    PlayerPrefs.SetString("login", j.username);
                    PlayerPrefs.SetString("password", j.password);
                    PlayerPrefs.SetString("token", result.token);
                    m_Sdk.developerToken = result.token;

                    m_ToggleMappingMode?.EnableMappingMode();

                    if (m_ToggleMappingMode?.MappingUI != null)
                    {
                        m_ToggleMappingMode.MappingUI.GetComponent<MapperBase>().OnLogOut += OnLogOut;
                    }

                    loginErrorText.gameObject.SetActive(false);
                    
                    FadeOut();

                    OnLogin?.Invoke();
                }
                else if (result.error == "auth")
                {
                    loginErrorText.text = "Login failed, please try again";
                    loginErrorText.gameObject.SetActive(true);
                }
            };

            StartCoroutine(j.RunJob());
        }

        private void OnLogOut()
        {
            m_Mapper.OnLogOut -= OnLogOut;
            m_ToggleMappingMode.DisableMappingMode();
            m_CanvasGroup.alpha = 1;
            loginPanel.SetActive(true);
        }

		private void FadeOut()
		{
			if (m_FadeAlpha != null)
			{
				StopCoroutine(m_FadeAlpha);
			}
			m_FadeAlpha = FadeAlpha();
			StartCoroutine(m_FadeAlpha);
		}

		IEnumerator FadeAlpha()
		{
			m_CanvasGroup.alpha = 1f;
			yield return new WaitForSeconds(0.1f);
			while (m_CanvasGroup.alpha > 0)
			{
				m_CanvasGroup.alpha -= Time.deltaTime / fadeOutTime;
				yield return null;
			}
            loginPanel.SetActive(false);
			yield return null;
		}
    }
}