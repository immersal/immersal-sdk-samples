/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.2.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Immersal.Samples.Mapping;
using Immersal.AR;
using Immersal.REST;

namespace Immersal.Samples
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class MapListController : MonoBehaviour
    {
        [SerializeField]
        private ARMap m_ARMap = null;
        private List<SDKJob> m_Maps;
        private TMP_Dropdown m_Dropdown;
        private int m_Bank = 0;
        private string m_Token = "";
        private string m_Server;
        private List<CoroutineJob> m_Jobs = new List<CoroutineJob>();
        private int m_JobLock = 0;
        private ImmersalARCloudSDK m_Sdk;
        private TextAsset m_EmbeddedMap;

        void Awake()
        {
            m_Dropdown = GetComponent<TMP_Dropdown>();
            m_Dropdown.ClearOptions();
            if (m_ARMap.m_MapFile != null)
            {
                m_Dropdown.AddOptions( new List<string>() { string.Format("<{0}>", m_ARMap.m_MapFile.name) });
                m_EmbeddedMap = m_ARMap.m_MapFile;
            }
            else
            {
                m_Dropdown.AddOptions( new List<string>() { "Load map..." });
            }
            m_Maps = new List<SDKJob>();
        }

        void Start()
        {
            m_Sdk = ImmersalARCloudSDK.Instance;
            m_Token = m_Sdk.developerToken;
            m_Server = m_Sdk.localizationServer;

            GetMaps();
        }

        void Update()
        {
            if (m_JobLock == 1)
                return;
            if (m_Jobs.Count > 0)
            {
                m_JobLock = 1;
                StartCoroutine(RunJob(m_Jobs[0]));
            }
        }

        public void OnValueChanged(TMP_Dropdown dropdown)
        {
            m_ARMap.FreeMap();

            int value = dropdown.value - 1;

            // use embedded map
            if (m_EmbeddedMap != null && value == -1)
            {
                m_ARMap.m_MapFile = m_EmbeddedMap;
                m_ARMap.LoadMap();
            }
            else
            {
                if (value >= 0)
                {
                    SDKJob map = m_Maps[value];
                    LoadMap(map.id);
                }
            }
        }

        public void GetMaps()
        {
            CoroutineListMaps j = new CoroutineListMaps();
            j.server = m_Server;
            j.token = m_Token;
            j.bank = m_Bank;
            j.dropdown = m_Dropdown;
            j.maps = m_Maps;
            m_Jobs.Add(j);
        }

        public void LoadMap(int id)
        {
            CoroutineLoadMap j = new CoroutineLoadMap();
            j.server = m_Server;
            j.token = m_Token;
            j.id = id;
            j.bank = m_Bank;
            j.arMap = m_ARMap;
            m_Jobs.Add(j);
        }

        private IEnumerator RunJob(CoroutineJob j)
        {
            yield return StartCoroutine(j.RunJob());
            m_Jobs.RemoveAt(0);
            m_JobLock = 0;
        }
    }

    public class CoroutineListMaps : CoroutineJob
    {
        public int bank;
        public List<SDKJob> maps;
        public TMP_Dropdown dropdown;

        public override IEnumerator RunJob()
        {
            SDKJobsRequest r = new SDKJobsRequest();
            r.token = this.token;
            r.bank = this.bank;
            string jsonString = JsonUtility.ToJson(r);

            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.LIST_JOBS), jsonString))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                }
                else if (request.responseCode == (long)HttpStatusCode.OK)
                {
                    SDKJobsResult result = JsonUtility.FromJson<SDKJobsResult>(request.downloadHandler.text);

                    if (result.error == "none" && result.count > 0)
                    {
                        List<string> names = new List<string>();

                        foreach (SDKJob job in result.jobs)
                        {
                            if (job.status == "done")
                            {
                                maps.Add(job);
                                names.Add(job.name);
                            }
                        }

                        dropdown.AddOptions(names);
                    }
                }
            }
        }
    }

	public class CoroutineLoadMap : CoroutineJob
	{
		public int bank;
		public int id;
        public ARMap arMap;

		public override IEnumerator RunJob()
		{
			Debug.Log("*************************** CoroutineLoadMap ***************************");
			SDKMapRequest r = new SDKMapRequest();
			r.token = this.token;
			r.id = this.id;

			string jsonString2 = JsonUtility.ToJson(r);
			using (UnityWebRequest request = UnityWebRequest.Put(string.Format(Endpoint.URL_FORMAT, this.server, Endpoint.LOAD_MAP), jsonString2))
			{
				request.method = UnityWebRequest.kHttpVerbPOST;
				request.useHttpContinue = false;
				request.SetRequestHeader("Content-Type", "application/json");
				request.SetRequestHeader("Accept", "application/json");
				yield return request.SendWebRequest();

				if (request.isNetworkError || request.isHttpError)
				{
					Debug.Log(request.error);
				}
				else if (request.responseCode == (long)HttpStatusCode.OK)
				{
					SDKMapResult result = JsonUtility.FromJson<SDKMapResult>(request.downloadHandler.text);
					if (result.error == "none")
					{

						byte[] mapData = Convert.FromBase64String(result.b64);
						Debug.Log("Load map " + this.id + " (" + mapData.Length + " bytes)");

                        arMap.LoadMap(mapData);
					}
				}
			}
		}
	}
}