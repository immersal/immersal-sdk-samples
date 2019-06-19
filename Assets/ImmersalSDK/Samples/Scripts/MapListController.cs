/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Immersal.Samples.Mapping;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Util;

namespace Immersal.Samples
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class MapListController : MonoBehaviour
    {
        /*
        [SerializeField]
        private GameObject m_Space = null;
        List<SDKJob> m_Maps;

        TMP_Dropdown m_Dropdown;
        private int m_Bank = 0;
        private string m_Token = "";
        private string m_Server;
        private List<CoroutineJob> m_Jobs = new List<CoroutineJob>();
        private int m_JobLock = 0;
        private ImmersalARCloudSDK m_Sdk;
        private ARSpace m_ARSpace;

        void Awake()
        {
            m_ARSpace = m_Space.GetComponent<ARSpace>();
            m_Dropdown = GetComponent<TMP_Dropdown>();
            m_Dropdown.ClearOptions();
            m_Dropdown.AddOptions( new List<string>() { string.Format("<{0}>", m_ARSpace.m_MapFile.name) });
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
            if (dropdown.value == 0)
            {
                if (m_ARSpace.m_RenderPointCloud)
                {
                    PointCloudRenderer renderer = m_Space.GetComponent<PointCloudRenderer>();
                    if (renderer != null)
                    {
                        renderer.ClearCloud();
                    }
                    
                    m_ARSpace.Awake();
                    m_ARSpace.Start();  // use embedded map
                }
            }
            else
            {
                SDKJob map = m_Maps[dropdown.value - 1];
                LoadMap(map.id);
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
            CoroutineJobLoadMap j = new CoroutineJobLoadMap();
            j.server = m_Server;
            j.token = m_Token;
            j.id = id;
            j.bank = m_Bank;
            j.stats = new MapperStats();
            j.pcr = new Dictionary<int, PointCloudRenderer>();

            if (m_ARSpace.m_RenderPointCloud)
            {
                PointCloudRenderer renderer = m_Space.GetComponent<PointCloudRenderer>();
                renderer.ClearCloud();
                j.go = m_Space;
            }

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
        public string server;
        public string token;
        public int bank;
        public List<SDKJob> maps;
        public TMP_Dropdown dropdown;

        public override IEnumerator RunJob()
        {
            SDKJobsRequest r = new SDKJobsRequest();
            r.token = this.token;
            r.bank = this.bank;
            string jsonString = JsonUtility.ToJson(r);
            using (UnityWebRequest request = UnityWebRequest.Put(string.Format(URL_FORMAT, this.server, "8"), jsonString))
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
        }*/
    }
}