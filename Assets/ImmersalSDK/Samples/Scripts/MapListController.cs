/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

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
        private List<JobAsync> m_Jobs = new List<JobAsync>();
        private int m_JobLock = 0;
        private TextAsset m_EmbeddedMap;

        void Awake()
        {
            m_Dropdown = GetComponent<TMP_Dropdown>();
            m_Dropdown.ClearOptions();
            if (m_ARMap?.mapFile != null)
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

        public async void OnValueChanged(TMP_Dropdown dropdown)
        {
            int value = dropdown.value - 1;

            // use embedded map
            if (m_EmbeddedMap != null && value == -1)
            {
                m_ARMap.mapFile = m_EmbeddedMap;
                await m_ARMap.LoadMap();
            }
            else
            {
                if (value >= 0)
                {
                    SDKJob map = m_Maps[value];
                    LoadMap(map);
                }
            }
        }

        public void GetMaps()
        {
            JobListJobsAsync j = new JobListJobsAsync();
            j.token = ImmersalSDK.Instance.developerToken;
            j.OnResult += (SDKJobsResult result) =>
            {
                if (result.count > 0)
                {
                    List<string> names = new List<string>();

                    foreach (SDKJob job in result.jobs)
                    {
                        if (job.type != (int)SDKJobType.Alignment && (job.status == SDKJobState.Sparse || job.status == SDKJobState.Done))
                        {
                            this.m_Maps.Add(job);
                            names.Add(job.name);
                        }
                    }

                    this.m_Dropdown.AddOptions(names);
                }
            };

            m_Jobs.Add(j);
        }

        public void ClearMaps()
        {
            ARMap[] arMaps = GameObject.FindObjectsOfType<ARMap>();
            foreach (ARMap arMap in arMaps)
            {
                arMap.FreeMap(true);
            }

            m_Dropdown.SetValueWithoutNotify(0);
        }

        public void LoadMap(SDKJob job)
        {
            JobLoadMapBinaryAsync j = new JobLoadMapBinaryAsync();
            j.id = job.id;
            j.OnResult += async (SDKMapResult result) =>
            {
                Debug.Log(string.Format("Load map {0} ({1} bytes)", job.id, result.mapData.Length));

                Color pointCloudColor = ARMap.pointCloudColors[UnityEngine.Random.Range(0, ARMap.pointCloudColors.Length)];
                ARMap.RenderMode renderMode = m_ARMap?.renderMode ?? ARMap.RenderMode.EditorAndRuntime;

                await ARSpace.LoadAndInstantiateARMap(null, result, renderMode, pointCloudColor);
            };

            m_Jobs.Add(j);
        }

        private async void RunJob(JobAsync j)
        {
            await j.RunJobAsync();
            if (m_Jobs.Count > 0)
                m_Jobs.RemoveAt(0);
            m_JobLock = 0;
        }
    }
}
