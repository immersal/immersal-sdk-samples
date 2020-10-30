/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Networking;
using Immersal.REST;

namespace Immersal
{
    public class ImmersalSDKEditor : EditorWindow
    {
        private string myEmail = "";
        private string myPassword = "";
        private string myToken = "";
        private ImmersalSDK sdk = null;

        private static UnityWebRequest request;

        [MenuItem("Immersal SDK/Open Settings")]
        static void Init()
        {
            ImmersalSDKEditor window = (ImmersalSDKEditor)EditorWindow.GetWindow(typeof(ImmersalSDKEditor));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Credentials", EditorStyles.boldLabel);
            myEmail = EditorGUILayout.TextField("Email", myEmail);
            myPassword = EditorGUILayout.PasswordField("Password", myPassword);

            if(GUILayout.Button("Login"))
            {
                SDKLoginRequest loginRequest = new SDKLoginRequest();
                loginRequest.login = myEmail;
                loginRequest.password = myPassword;

                Login(loginRequest);

                EditorApplication.update += EditorUpdate;
            }

            EditorGUILayout.Separator();

            myToken = EditorGUILayout.TextField("Token", myToken);

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("(C) 2020 Immersal Ltd. All Right Reserved.");
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void Login(SDKLoginRequest loginRequest)
        {
            string jsonString = JsonUtility.ToJson(loginRequest);
            sdk = ImmersalSDK.Instance;
            request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, sdk.localizationServer, Endpoint.LOGIN), jsonString);
            request.method = UnityWebRequest.kHttpVerbPOST;
            request.useHttpContinue = false;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SendWebRequest();
        }

        private void EditorUpdate()
        {
            while (!request.isDone)
                return;

            //Debug.Log("Response code: " + request.responseCode);

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log(request.error);
            }
            else
            {
                //Debug.Log(request.downloadHandler.text);
                SDKLoginResult loginResult = JsonUtility.FromJson<SDKLoginResult>(request.downloadHandler.text);
                if (loginResult.error == "none")
                {
                    myToken = loginResult.token;
                    sdk.developerToken = myToken;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }

            EditorApplication.update -= EditorUpdate;
        }
    }
}