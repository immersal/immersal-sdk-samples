/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Immersal.REST;

namespace Immersal.Samples.Mapping
{
    public class LoginManager : MonoBehaviour
    {
        public GameObject loginPanel;
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TextMeshProUGUI loginErrorText;
        public float fadeOutTime = 1f;
        private IEnumerator m_fadeAlpha;
        private CanvasGroup m_canvasGroup;
        private string m_server;
        private ImmersalARCloudSDK m_sdkSettings;
        private ToggleMappingMode m_toggleMappingMode;

        void Start()
        {
            m_sdkSettings = ImmersalARCloudSDK.Instance;
            m_server = m_sdkSettings.localizationServer;

            m_canvasGroup = loginPanel.GetComponent<CanvasGroup>();
            m_toggleMappingMode = loginPanel.GetComponent<ToggleMappingMode>();

            emailField.text = PlayerPrefs.GetString("login", "");
            passwordField.text = PlayerPrefs.GetString("password", "");
        }

        public void OnLoginClick()
        {
            if (emailField.text.Length > 0 && passwordField.text.Length > 0)
            {
                StartCoroutine(Login());
            }
        }

        private IEnumerator Login()
        {
            SDKLoginRequest loginRequest = new SDKLoginRequest();
            loginRequest.login = emailField.text;
            loginRequest.password = passwordField.text;

            string jsonString = JsonUtility.ToJson(loginRequest);
            //Debug.Log("jsonString: " + jsonString);
            byte[] myData = System.Text.Encoding.UTF8.GetBytes(jsonString);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format("{0}/{1}", m_server, "fcgi?6"), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                //Debug.Log("Response code: " + request.responseCode);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.Log(request.error);
                }
                else
                {
                    Debug.Log(request.downloadHandler.text);
                    PlayerPrefs.SetString("login", loginRequest.login);
                    PlayerPrefs.SetString("password", loginRequest.password);

                    SDKLoginResult loginResult = JsonUtility.FromJson<SDKLoginResult>(request.downloadHandler.text);
                    if (loginResult.error == "none")
                    {
                        PlayerPrefs.SetString("token", loginResult.token);
                        m_sdkSettings.developerToken = loginResult.token;

                        m_toggleMappingMode.EnableMappingMode();

                        if (m_toggleMappingMode.MappingUI != null)
                        {
                            m_toggleMappingMode.MappingUI.GetComponent<Mapper>().OnLogOut += OnLogOut;
                        }

                        loginErrorText.gameObject.SetActive(false);
                        
                        FadeOut();
                    }
                    else if (loginResult.error == "auth")
                    {
                        loginErrorText.text = "Login failed, please try again";
                        loginErrorText.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void OnLogOut()
        {
            m_toggleMappingMode.MappingUI.GetComponent<Mapper>().OnLogOut -= OnLogOut;
            m_toggleMappingMode.DisableMappingMode();
            m_canvasGroup.alpha = 1;
            loginPanel.SetActive(true);
        }

		private void FadeOut()
		{
			if (m_fadeAlpha != null)
			{
				StopCoroutine(m_fadeAlpha);
			}
			m_fadeAlpha = FadeAlpha();
			StartCoroutine(m_fadeAlpha);
		}

		IEnumerator FadeAlpha()
		{
			m_canvasGroup.alpha = 1f;
			yield return new WaitForSeconds(0.1f);
			while (m_canvasGroup.alpha > 0)
			{
				m_canvasGroup.alpha -= Time.deltaTime / fadeOutTime;
				yield return null;
			}
            loginPanel.SetActive(false);
			yield return null;
		}
    }
}