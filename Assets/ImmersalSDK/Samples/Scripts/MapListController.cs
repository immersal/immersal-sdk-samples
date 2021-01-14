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
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
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
        private List<Task> m_Jobs = new List<Task>();
        private int m_JobLock = 0;
        private TextAsset m_EmbeddedMap;

        void Awake()
        {
            m_Dropdown = GetComponent<TMP_Dropdown>();
            m_Dropdown.ClearOptions();
            if (m_ARMap.mapFile != null)
            {
                m_Dropdown.AddOptions( new List<string>() { string.Format("<{0}>", m_ARMap.mapFile.name) });
                m_EmbeddedMap = m_ARMap.mapFile;
            }
            else
            {
                m_Dropdown.AddOptions( new List<string>() { "Load map..." });
            }
            m_Maps = new List<SDKJob>();
        }

        void Start()
        {
            Invoke("GetMaps", 0.5f);
        }

        void Update()
        {
            if (m_JobLock == 1)
                return;
            
            if (m_Jobs.Count > 0)
            {
                m_JobLock = 1;
                RunJob(m_Jobs[0]);
            }
        }

        public void OnValueChanged(TMP_Dropdown dropdown)
        {
            m_ARMap.FreeMap();

            int value = dropdown.value - 1;

            // use embedded map
            if (m_EmbeddedMap != null && value == -1)
            {
                m_ARMap.mapFile = m_EmbeddedMap;
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
            JobListJobsAsync j = new JobListJobsAsync();
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKJobsResult result && result.error == "none" && result.count > 0)
                {
                    List<string> names = new List<string>();

                    foreach (SDKJob job in result.jobs)
                    {
                        if (job.status == "sparse" || job.status == "done")
                        {
                            this.m_Maps.Add(job);
                            names.Add(job.name);
                        }
                    }

                    this.m_Dropdown.AddOptions(names);
                }
            };

            m_Jobs.Add(j.RunJobAsync());
        }

        public void LoadMap(int jobId)
        {
            JobLoadMapAsync j = new JobLoadMapAsync();
            j.id = jobId;
            j.OnResult += (SDKResultBase r) =>
            {
                if (r is SDKMapResult result && result.error == "none")
                {
                    byte[] mapData = Convert.FromBase64String(result.b64);
                    Debug.Log(string.Format("Load map {0} ({1} bytes)", jobId, mapData.Length));

                    this.m_ARMap.LoadMap(mapData);
                }
            };

            m_Jobs.Add(j.RunJobAsync());
        }

        private async void RunJob(Task t)
        {
            await t;
            if (m_Jobs.Count > 0)
                m_Jobs.RemoveAt(0);
            m_JobLock = 0;
        }
    }
}
