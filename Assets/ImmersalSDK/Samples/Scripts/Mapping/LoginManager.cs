/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

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
    public class LoginManager : MonoBehaviour
    {
        public GameObject loginPanel;
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TextMeshProUGUI loginErrorText;
        public float fadeOutTime = 1f;

        private IEnumerator m_FadeAlpha;
        private CanvasGroup m_CanvasGroup;
        private ImmersalSDK m_Sdk;
        private ToggleMappingMode m_ToggleMappingMode;

        void Start()
        {
            m_Sdk = ImmersalSDK.Instance;

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
                StartCoroutine(Login());
            }
        }

        private IEnumerator Login()
        {
            SDKLoginRequest loginRequest = new SDKLoginRequest();
            loginRequest.login = emailField.text;
            loginRequest.password = passwordField.text;

            loginErrorText.gameObject.SetActive(false);

            string jsonString = JsonUtility.ToJson(loginRequest);
            byte[] myData = System.Text.Encoding.UTF8.GetBytes(jsonString);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, m_Sdk.localizationServer, Endpoint.LOGIN), jsonString))
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
                    if (request.responseCode == (long)HttpStatusCode.BadRequest)
                    {
                        loginErrorText.text = "Login failed, please try again";
                        loginErrorText.gameObject.SetActive(true);
                    }
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
                        m_Sdk.developerToken = loginResult.token;

                        m_ToggleMappingMode.EnableMappingMode();

                        if (m_ToggleMappingMode.MappingUI != null)
                        {
                            m_ToggleMappingMode.MappingUI.GetComponent<Mapper>().OnLogOut += OnLogOut;
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
            m_ToggleMappingMode.MappingUI.GetComponent<Mapper>().OnLogOut -= OnLogOut;
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