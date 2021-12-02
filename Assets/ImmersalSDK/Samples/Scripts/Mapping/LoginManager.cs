/*===============================================================================
Copyright (C) 2021 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Immersal.REST;
using Immersal.Samples.Mapping;

namespace Immersal.Samples.Mapping
{
    public class LoginManager : MonoBehaviour
    {
        public GameObject loginPanel;
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TMP_InputField serverField;
        public TextMeshProUGUI loginErrorText;
        public float fadeOutTime = 1f;
        public event LoginEvent OnLogin = null;
        public event LoginEvent OnLogout = null;
        public delegate void LoginEvent();

        private IEnumerator m_FadeAlpha;
        private CanvasGroup m_CanvasGroup;
        private ImmersalSDK m_Sdk;

        private static LoginManager instance = null;

        public static LoginManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = UnityEngine.Object.FindObjectOfType<LoginManager>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No LoginManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }

        void Awake()
        {
            if(!PlayerPrefs.HasKey("sdkversion"))
            {
                PlayerPrefs.DeleteKey("server");
            }
            else
            {
                if (!PlayerPrefs.GetString("sdkversion").Equals(ImmersalSDK.sdkVersion))
                {
                    PlayerPrefs.DeleteKey("server");
                }
            }
            
            PlayerPrefs.SetString("sdkversion", ImmersalSDK.sdkVersion);
            
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("There must be only one LoginManager object in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }

        void Start()
        {
            m_Sdk = ImmersalSDK.Instance;
            m_CanvasGroup = loginPanel.GetComponent<CanvasGroup>();

            Invoke("FillFields", 0.1f);
        }

        void FillFields()
        {
            emailField.text = PlayerPrefs.GetString("login", "");
            passwordField.text = PlayerPrefs.GetString("password", "");
            serverField.text = PlayerPrefs.GetString("server", ImmersalSDK.DefaultServer);

            if (serverField.text != ImmersalSDK.DefaultServer)
            {
                m_Sdk.localizationServer = serverField.text;
            }
        }

        public void OnLoginClick()
        {
            if (emailField.text.Length > 0 && passwordField.text.Length > 0)
            {
                Login();
            }
        }

        public void OnServerEndEdit(string s)
        {
            if(s.Length == 0)
            {
                s = ImmersalSDK.DefaultServer;
                serverField.text = s;
            }

            m_Sdk.localizationServer = s;
        }

        public async void Login()
        {
            JobLoginAsync j = new JobLoginAsync();
            j.username = emailField.text;
            j.password = passwordField.text;
            j.OnStart += () =>
            {
                loginErrorText.gameObject.SetActive(false);
            };
            j.OnError += (e) =>
            {
                if (e == "auth")
                {
                    loginErrorText.text = "Login failed, please try again";
                    loginErrorText.gameObject.SetActive(true);
                }
                else if (e == "conn")
                {
                    loginErrorText.text = "Unable to connect - please check server url, network availability and app permissions.";
                    loginErrorText.gameObject.SetActive(true);
                }
            };
            j.OnResult += (SDKLoginResult result) =>
            {
                PlayerPrefs.SetString("login", j.username);
                PlayerPrefs.SetString("password", j.password);
                PlayerPrefs.SetString("token", result.token);
                PlayerPrefs.SetString("server", serverField.text);
                m_Sdk.developerToken = result.token;

                loginErrorText.gameObject.SetActive(false);
                
                FadeOut();

                OnLogin?.Invoke();
            };

            await j.RunJobAsync();
        }

        public void Logout()
        {
            m_CanvasGroup.alpha = 1;
            loginPanel.SetActive(true);

            OnLogout?.Invoke();
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