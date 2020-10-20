/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Immersal.Samples.Mapping
{
    public class MapperSettings : MonoBehaviour
    {
        public bool useGps { get; private set; } = true;
        public bool captureRgb { get; private set; } = false;
        public bool showPointClouds { get; private set; } = true;
        public bool useServerLocalizer { get; private set; } = false;
        public bool listOnlyNearbyMaps { get; private set; } = false;
        public int resolution { get; private set; } = 0;
        public int mapDetailLevel { get; private set; } = 600;
        public bool serverLocalizationWithIds { get; private set; } = true;
        public bool preservePoses { get; private set; } = false;
        public int windowSize { get; private set; } = 0;
        public int param1 { get; private set; } = 0;
        public int param2 { get; private set; } = 12;
        public float param3 { get; private set; } = 0f;
        public float param4 { get; private set; } = 2f;

        // workspace mode settings
        [SerializeField]
        private Toggle m_GpsCaptureToggle = null;
        [SerializeField]
        private Toggle m_RgbCaptureToggle = null;

        // visualize mode settings
        [SerializeField]
        private Toggle m_ShowPointCloudsToggle = null;
        [SerializeField]
        private Toggle m_OnServerLocalizationToggle = null;
        [SerializeField]
        private Toggle m_ListOnlyNearbyMapsToggle = null;

        // developer settings
        [SerializeField]
        private TMP_Dropdown m_ResolutionDropdown = null;
        [SerializeField]
        private TMP_InputField m_MapDetailLevelInput = null;
        [SerializeField]
        private Toggle m_ServerLocalizationWithIdsToggle = null;
        [SerializeField]
        private Toggle m_PreservePosesToggle = null;
        [SerializeField]
        private TMP_InputField m_WindowSizeInput = null;
        [SerializeField]
        private TMP_InputField m_Param1Input = null;
        [SerializeField]
        private TMP_InputField m_Param2Input = null;
        [SerializeField]
        private TMP_InputField m_Param3Input = null;
        [SerializeField]
        private TMP_InputField m_Param4Input = null;

        private string m_Filename = "settings.json";

        [System.Serializable]
        private struct MapperSettingsFile
        {
            public bool useGps;
            public bool captureRgb;

            public bool showPointClouds;
            public bool useServerLocalizer;
            public bool listOnlyNearbyMaps;

            public int resolution;
            public int mapDetailLevel;
            public bool serverLocalizationWithIds;
            public bool preservePoses;
            public int windowSize;
            public int param1;
            public int param2;
            public float param3;
            public float param4;
        }

        public void SetUseGPS(bool value)
        {
            useGps = value;
            SaveSettingsToPrefs();
        }

        public void SetCaptureRGB(bool value)
        {
            captureRgb = value;
            SaveSettingsToPrefs();
        }
        public void SetShowPointClouds(bool value)
        {
            showPointClouds = value;
            SaveSettingsToPrefs();
        }
        public void SetUseServerLocalizer(bool value)
        {
            useServerLocalizer = value;
            SaveSettingsToPrefs();
        }

        public void SetListOnlyNearbyMaps(bool value)
        {
            listOnlyNearbyMaps = value;
            SaveSettingsToPrefs();
        }

        public void SetResolution(int value)
        {
            resolution = value;
            SaveSettingsToPrefs();
        }

        public void SetMapDetailLevel(string value)
        {
            int a;
            int.TryParse(value, out a);

            mapDetailLevel = a;
            SaveSettingsToPrefs();
        }

        public void SetServerLocalizationWithIds(bool value)
        {
            serverLocalizationWithIds = value;
            SaveSettingsToPrefs();
        }

        public void SetPreservePoses(bool value)
        {
            preservePoses = value;
            SaveSettingsToPrefs();
        }

        public void SetWindowSize(string value)
        {
            int a;
            int.TryParse(value, out a);

            param1 = a;
            SaveSettingsToPrefs();
        }

        public void SetParam1(string value)
        {
            int a;
            int.TryParse(value, out a);

            param1 = a;
            SaveSettingsToPrefs();
        }

        public void SetParam2(string value)
        {
            int a;
            int.TryParse(value, out a);

            param2 = a;
            SaveSettingsToPrefs();
        }

        public void SetParam3(string value)
        {
            float a;
            float.TryParse(value, out a);

            param3 = a;
            SaveSettingsToPrefs();
        }

        public void SetParam4(string value)
        {
            float a;
            float.TryParse(value, out a);

            param4 = a;
            SaveSettingsToPrefs();
        }

        private void Start()
        {
            LoadSettingsFromPrefs();
        }

        private void LoadSettingsFromPrefs()
        {
            string dataPath = Path.Combine(Application.persistentDataPath, m_Filename);

            try
            {
                MapperSettingsFile loadFile = JsonUtility.FromJson<MapperSettingsFile>(File.ReadAllText(dataPath));

                m_GpsCaptureToggle.isOn = loadFile.useGps;
                useGps = loadFile.useGps;
                m_RgbCaptureToggle.SetIsOnWithoutNotify(loadFile.captureRgb);
                captureRgb = loadFile.captureRgb;

                m_ShowPointCloudsToggle.SetIsOnWithoutNotify(loadFile.showPointClouds);
                showPointClouds = loadFile.showPointClouds;
                m_OnServerLocalizationToggle.SetIsOnWithoutNotify(loadFile.useServerLocalizer);
                useServerLocalizer = loadFile.useServerLocalizer;
                m_ListOnlyNearbyMapsToggle.SetIsOnWithoutNotify(loadFile.listOnlyNearbyMaps);
                listOnlyNearbyMaps = loadFile.listOnlyNearbyMaps;

                //m_ResolutionDropdown.value = loadFile.resolution;
                //resolution = loadFile.resolution;

                m_MapDetailLevelInput.SetTextWithoutNotify(loadFile.mapDetailLevel.ToString());
                mapDetailLevel = loadFile.mapDetailLevel;
                m_ServerLocalizationWithIdsToggle.SetIsOnWithoutNotify(loadFile.serverLocalizationWithIds);
                serverLocalizationWithIds = loadFile.serverLocalizationWithIds;
                m_PreservePosesToggle.SetIsOnWithoutNotify(loadFile.preservePoses);
                preservePoses = loadFile.preservePoses;
                m_WindowSizeInput.SetTextWithoutNotify(loadFile.windowSize.ToString());
                windowSize = loadFile.windowSize;
                m_Param1Input.SetTextWithoutNotify(loadFile.param1.ToString());
                param1 = loadFile.param1;
                m_Param2Input.SetTextWithoutNotify(loadFile.param2.ToString());
                param2 = loadFile.param2;
                m_Param3Input.SetTextWithoutNotify(loadFile.param3.ToString());
                param3 = loadFile.param3;
                m_Param4Input.SetTextWithoutNotify(loadFile.param4.ToString());
                param4 = loadFile.param4;
            }
            catch (FileNotFoundException e)
            {
                Debug.Log(e.Message + "\nsettings.json file not found");
            }

            SaveSettingsToPrefs();
        }

        public void SaveSettingsToPrefs()
        {
            MapperSettingsFile saveFile = new MapperSettingsFile();

            saveFile.useGps = useGps;
            saveFile.captureRgb = captureRgb;

            saveFile.showPointClouds = showPointClouds;
            saveFile.useServerLocalizer = useServerLocalizer;
            saveFile.listOnlyNearbyMaps = listOnlyNearbyMaps;

            saveFile.resolution = resolution;
            saveFile.mapDetailLevel = mapDetailLevel;
            saveFile.serverLocalizationWithIds = serverLocalizationWithIds;
            saveFile.preservePoses = preservePoses;
            saveFile.windowSize = windowSize;
            saveFile.param1 = param1;
            saveFile.param2 = param2;
            saveFile.param3 = param3;
            saveFile.param4 = param4;

            string jsonstring = JsonUtility.ToJson(saveFile, true);
            string dataPath = Path.Combine(Application.persistentDataPath, m_Filename);
            //Debug.Log(dataPath);
            File.WriteAllText(dataPath, jsonstring);
        }

        public void ResetDeveloperSettings()
        {
            m_ResolutionDropdown.SetValueWithoutNotify(0);
            resolution = 0;
            m_MapDetailLevelInput.SetTextWithoutNotify(600.ToString());
            mapDetailLevel = 600;
            m_ServerLocalizationWithIdsToggle.SetIsOnWithoutNotify(true);
            serverLocalizationWithIds = true;
            m_Param1Input.SetTextWithoutNotify(0.ToString());
            param1 = 0;
            m_Param2Input.SetTextWithoutNotify(12.ToString());
            param2 = 12;
            m_Param3Input.SetTextWithoutNotify(0f.ToString());
            param3 = 0f;
            m_Param4Input.SetTextWithoutNotify(2f.ToString());
            param4 = 2f;

            SaveSettingsToPrefs();
        }
    }
}
