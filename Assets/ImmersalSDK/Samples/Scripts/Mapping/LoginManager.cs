/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Immersal.REST;
using UnityEngine.UI;

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

        private bool m_autoLoginEnabled;
        [SerializeField] private Toggle m_rememberMeToggle;
        
        [SerializeField] private GameObject m_loginControlContainer;
        [SerializeField] private TMP_Text m_autoLoggingIndicatorText;

        private const string IpApiUrl = "https://ipinfo.io?token=437904fba78a27";

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

        private void OnEnable()
        {
            if (PlayerPrefs.HasKey("rememberMe"))
            {
                m_autoLoginEnabled = bool.Parse(PlayerPrefs.GetString("rememberMe"));
            }
            
            m_loginControlContainer.SetActive(!m_autoLoginEnabled);
            m_autoLoggingIndicatorText.gameObject.SetActive(m_autoLoginEnabled);

            if (m_autoLoginEnabled)
            {
                m_autoLoggingIndicatorText.text = string.Format("Logging in automatically as {0}", PlayerPrefs.GetString("login"));
            }
        }

        
        void FillFields()
        {
            StartCoroutine(CheckIfUserIsInChina(isInChina => 
            {
                //Debug.Log("Is user in China? " + isInChina);
                if (isInChina)
                {
                    m_Sdk.defaultServer = ImmersalSDK.APIServer.ChinaServer;
                }
                
                emailField.text = PlayerPrefs.GetString("login", "");
                passwordField.text = PlayerPrefs.GetString("password", "");
                serverField.text = PlayerPrefs.GetString("server", m_Sdk.localizationServer);

                if (serverField.text != m_Sdk.defaultServerURL)
                {
                    m_Sdk.localizationServer = serverField.text;
                }

                AttemptAutoLogin();
            }));
        }

        private IEnumerator CheckIfUserIsInChina(Action<bool> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(IpApiUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogErrorFormat("Error fetching IP geolocation data: {0}", request.error);
                }
                else
                {
                    string jsonResult = request.downloadHandler.text;
                    GeolocationData data = JsonUtility.FromJson<GeolocationData>(jsonResult);

                    bool isInChina = false;
                    if (data.country.Equals("CN"))
                    {
                        //Debug.Log("User is in China.");
                        isInChina = true;
                    }
                    else
                    {
                        //Debug.LogFormat("User is not in China. Country: {0}", data.country);
                    }
                    
                    callback(isInChina);
                }
            }
        }

        [System.Serializable]
        private class GeolocationData
        {
            public string country;
        }
        
        void AttemptAutoLogin()
        {
            if (m_autoLoginEnabled)
            {
                m_Sdk.developerToken = PlayerPrefs.GetString("token");    
                CompleteLogin();    
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
            if (s.Length == 0)
            {
                s = m_Sdk.defaultServerURL;
                serverField.text = s;
            }
            else if (s[s.Length - 1] == '/')
            {
                s = s.Substring(0, s.Length - 1);
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
                Debug.LogFormat("Logged in to {0}", m_Sdk.localizationServer);
                m_Sdk.developerToken = result.token;

                CompleteLogin();
            };

            await j.RunJobAsync();
        }

        private void CompleteLogin()
        {
            loginErrorText.gameObject.SetActive(false);
            m_Sdk.ValidateUser();
                
            FadeOut();

            OnLogin?.Invoke();
        }

        public void Logout()
        {
            SetRememberMe(false);
            
            m_CanvasGroup.alpha = 1;
            loginPanel.SetActive(true);
            
            m_rememberMeToggle.isOn = false;

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

        public void SetRememberMe(bool value)
        {
            PlayerPrefs.SetString("rememberMe", value.ToString());
        }
    }
}