/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Immersal.Samples.Mapping
{
    public class MapperSettings : MonoBehaviour
    {
        public const int VERSION = 9;

        public bool useGps { get; private set; } = true;
        public bool captureRgb { get; private set; } = false;
        public bool checkConnectivity { get; private set; } = true;
        public bool showPointClouds { get; private set; } = true;
        public bool renderPointsAs3D { get; private set; } = true;
        public float pointSize { get; private set; } = 0.33f;
        public bool useServerLocalizer { get; private set; } = false;
        public bool listOnlyNearbyMaps { get; private set; } = false;
        public bool transformRootToOrigin { get; private set; } = true;
        public bool downsampleWhenLocalizing { get; private set; } = false;
        public int resolution { get; private set; } = 0;
        public int localizer { get; private set; } = 1;
        public int mapDetailLevel { get; private set; } = 1024;
        public bool serverLocalizationWithIds { get; private set; } = true;
        public bool useDifferentARSpaces { get; private set; } = true;
        public bool preservePoses { get; private set; } = false;
        public bool automaticCapture { get; private set; } = false;
        public int automaticCaptureMaxImages { get; private set; } = 40;
        public float automaticCaptureInterval { get; private set; } = 0.6f;
        public int windowSize { get; private set; } = 0;

        // workspace mode settings
        [SerializeField]
        private Toggle m_GpsCaptureToggle = null;
        [SerializeField]
        private Toggle m_RgbCaptureToggle = null;
        [SerializeField]
        private Toggle m_CheckConnectivityToggle = null;

        // visualize mode settings
        [SerializeField]
        private Toggle m_ShowPointCloudsToggle = null;
        [SerializeField]
        private Toggle m_OnServerLocalizationToggle = null;
        [SerializeField]
        private Toggle m_RenderPointsAs3DToggle = null;
        [SerializeField]
        private Slider m_PointSizeSlider = null;
        [SerializeField]
        private Toggle m_ListOnlyNearbyMapsToggle = null;
        [SerializeField]
        private Toggle m_DownsampleWhenLocalizingToggle = null;
        [SerializeField]
        private Toggle m_TransformRootToOrigin = null;

        // developer settings
        [SerializeField]
        private TMP_Dropdown m_ResolutionDropdown = null;
        /*[SerializeField]
        private TMP_Dropdown m_LocalizerDropdown = null;*/
        [SerializeField]
        private TMP_InputField m_MapDetailLevelInput = null;
        [SerializeField]
        private Toggle m_ServerLocalizationWithIdsToggle = null;
        [SerializeField]
        private Toggle m_UseDifferentARSpacesToggle = null;
        [SerializeField]
        private Toggle m_PreservePosesToggle = null;
        [SerializeField]
        private Toggle m_AutomaticCaptureToggle = null;
        [SerializeField]
        private GameObject m_AutomaticCaptureMaxImages = null;
        [SerializeField]
        private Slider m_AutomaticCaptureMaxImagesSlider = null;
        [SerializeField]
        private TMP_Text m_AutomaticCaptureMaxImagesText = null;
        [SerializeField] 
        private GameObject m_AutomaticCaptureInterval = null;
        [SerializeField] 
        private Slider m_AutomaticCaptureIntervalSlider = null;
        [SerializeField]
        private TMP_Text m_AutomaticCaptureIntervalText = null;
        [SerializeField]
        private GameObject m_ManualCaptureButton = null;
        [SerializeField]
        private GameObject m_AutomaticCaptureButton = null;
        [SerializeField]
        private TMP_InputField m_WindowSizeInput = null;
        [SerializeField]
        private string m_Filename = "settings.json";

        [System.Serializable]
        public struct MapperSettingsFile
        {
            public int version;
            public bool useGps;
            public bool captureRgb;
            public bool checkConnectivity;

            public bool showPointClouds;
            public bool useServerLocalizer;
            public bool listOnlyNearbyMaps;
            public bool transformRootToOrigin;
            public bool downsampleWhenLocalizing;
            public bool renderPointsAs3D;
            public float pointSize;

            public int resolution;
            public int localizer;
            public int mapDetailLevel;
            public bool serverLocalizationWithIds;
            public bool useDifferentARSpaces;
            public bool preservePoses;
            public bool automaticCapture;
            public int automaticCaptureMaxImages;
            public float automaticCaptureInterval;
            public int windowSize;
        }
        
        void Awake()
        {
            /*
            m_LocalizerDropdown.ClearOptions();
            m_LocalizerDropdown.AddOptions( new List<string>() { "v1.10", "v1.11" });
            m_LocalizerDropdown.SetValueWithoutNotify(localizer);
            */
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

        public void SetConnectivityCheck(bool value)
        {
            checkConnectivity = value;
            SaveSettingsToPrefs();
        }

        public void SetShowPointClouds(bool value)
        {
            showPointClouds = value;
            SaveSettingsToPrefs();
        }

        public void SetRenderPointsAs3D(bool value)
        {
            renderPointsAs3D = value;
            SaveSettingsToPrefs();
        }

        public void SetPointSize(float value)
        {
            pointSize = value;
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
        public void SetTransformRootToOrigin(bool value)
        {
            transformRootToOrigin = value;
            SaveSettingsToPrefs();
        }

        public void SetDownsampleWhenLocalizing(bool value)
        {
            downsampleWhenLocalizing = value;
            if (value)
            {
                Immersal.Core.SetInteger("LocalizationMaxPixels", 1280*720);
            }
            else
            {
                Immersal.Core.SetInteger("LocalizationMaxPixels", 0);
            }

            SaveSettingsToPrefs();
        }

        public void SetResolution(int value)
        {
            resolution = value;
            SaveSettingsToPrefs();
        }

        public void SetLocalizer(int value)
        {
            localizer = value;
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

        public void SetUseDifferentARSpaces(bool value)
        {
            useDifferentARSpaces = value;
            SaveSettingsToPrefs();
        }

        public void SetPreservePoses(bool value)
        {
            preservePoses = value;
            SaveSettingsToPrefs();
        }

        public void SetAutomaticCapture(bool value)
        {
            automaticCapture = value;
            EnableAutomaticCaptureUI(automaticCapture);
            SaveSettingsToPrefs();
        }
		
		public void SetAutomaticCaptureMaxImageValue(float maxImages)
        {
            automaticCaptureMaxImages = Mathf.RoundToInt(maxImages);
            m_AutomaticCaptureMaxImagesText.text = automaticCaptureMaxImages.ToString();
            SaveSettingsToPrefs();
        }
        
        public void SetAutomaticCaptureIntervalValue(float interval)
        {
            automaticCaptureInterval = interval;
            m_AutomaticCaptureIntervalText.text = automaticCaptureInterval.ToString("F2");
            SaveSettingsToPrefs();
        }

        public void SetWindowSize(string value)
        {
            int a;
            int.TryParse(value, out a);

            windowSize = a;
            SaveSettingsToPrefs();
        }

        private void Start()
        {
            LoadSettingsFromPrefs();
        }

        private void EnableAutomaticCaptureUI(bool value)
        {
            //enable sub-setting items for automatic capture
            m_AutomaticCaptureMaxImages.SetActive(value);
            m_AutomaticCaptureInterval.SetActive(value);

            //enable manual capture button
            m_ManualCaptureButton.SetActive(!value);
            m_AutomaticCaptureButton.SetActive(value);
        }

        private void LoadSettingsFromPrefs()
        {
            string dataPath = Path.Combine(Application.persistentDataPath, m_Filename);

            try
            {
                MapperSettingsFile loadFile = JsonUtility.FromJson<MapperSettingsFile>(File.ReadAllText(dataPath));

                // set defaults for old file versions
                if (loadFile.version < VERSION)
                {
                    ResetDeveloperSettings();
                    return;
                }

                m_GpsCaptureToggle.isOn = loadFile.useGps;
                useGps = loadFile.useGps;
                m_RgbCaptureToggle.SetIsOnWithoutNotify(loadFile.captureRgb);
                captureRgb = loadFile.captureRgb;
                m_CheckConnectivityToggle.SetIsOnWithoutNotify(loadFile.checkConnectivity);
                checkConnectivity = loadFile.checkConnectivity;

                m_ShowPointCloudsToggle.SetIsOnWithoutNotify(loadFile.showPointClouds);
                showPointClouds = loadFile.showPointClouds;
                m_RenderPointsAs3DToggle.SetIsOnWithoutNotify(loadFile.renderPointsAs3D);
                renderPointsAs3D = loadFile.renderPointsAs3D;
                Immersal.AR.ARMap.renderAs3dPoints = renderPointsAs3D;
                m_PointSizeSlider.SetValueWithoutNotify(loadFile.pointSize);
                pointSize = loadFile.pointSize;
                Immersal.AR.ARMap.pointSize = pointSize;
                m_OnServerLocalizationToggle.SetIsOnWithoutNotify(loadFile.useServerLocalizer);
                useServerLocalizer = loadFile.useServerLocalizer;
                m_ListOnlyNearbyMapsToggle.SetIsOnWithoutNotify(loadFile.listOnlyNearbyMaps);
                listOnlyNearbyMaps = loadFile.listOnlyNearbyMaps;
                m_TransformRootToOrigin.SetIsOnWithoutNotify(loadFile.transformRootToOrigin);
                transformRootToOrigin = loadFile.transformRootToOrigin;

                m_DownsampleWhenLocalizingToggle.SetIsOnWithoutNotify(loadFile.downsampleWhenLocalizing);
                downsampleWhenLocalizing = loadFile.downsampleWhenLocalizing;

                m_MapDetailLevelInput.SetTextWithoutNotify(loadFile.mapDetailLevel.ToString());
                mapDetailLevel = loadFile.mapDetailLevel;
                m_ServerLocalizationWithIdsToggle.SetIsOnWithoutNotify(loadFile.serverLocalizationWithIds);
                serverLocalizationWithIds = loadFile.serverLocalizationWithIds;
                m_UseDifferentARSpacesToggle.SetIsOnWithoutNotify(loadFile.useDifferentARSpaces);
                useDifferentARSpaces = loadFile.useDifferentARSpaces;
                m_PreservePosesToggle.SetIsOnWithoutNotify(loadFile.preservePoses);
                preservePoses = loadFile.preservePoses;
                m_AutomaticCaptureToggle.SetIsOnWithoutNotify(loadFile.automaticCapture);
                automaticCapture = loadFile.automaticCapture;
                EnableAutomaticCaptureUI(automaticCapture);
                
                automaticCaptureMaxImages = Mathf.Max(loadFile.automaticCaptureMaxImages, Mathf.RoundToInt(m_AutomaticCaptureMaxImagesSlider.minValue));
                m_AutomaticCaptureMaxImagesSlider.value = loadFile.automaticCaptureMaxImages;
                m_AutomaticCaptureMaxImagesText.text = automaticCaptureMaxImages.ToString();

                automaticCaptureInterval = Mathf.Max(loadFile.automaticCaptureInterval, m_AutomaticCaptureIntervalSlider.minValue);
                m_AutomaticCaptureIntervalSlider.value = loadFile.automaticCaptureInterval;
                m_AutomaticCaptureIntervalText.text = automaticCaptureInterval.ToString("F2");

                m_WindowSizeInput.SetTextWithoutNotify(loadFile.windowSize.ToString());
                windowSize = loadFile.windowSize;
            }
            catch (FileNotFoundException e)
            {
                Debug.Log(e.Message + "\nsettings.json file not found");
                SaveSettingsToPrefs();
                LoadSettingsFromPrefs();
                return;
            }

            SaveSettingsToPrefs();
        }

        public void SaveSettingsToPrefs()
        {
            MapperSettingsFile saveFile = new MapperSettingsFile();

            saveFile.version = VERSION;
            saveFile.useGps = useGps;
            saveFile.captureRgb = captureRgb;
            saveFile.checkConnectivity = checkConnectivity;

            saveFile.showPointClouds = showPointClouds;
            saveFile.useServerLocalizer = useServerLocalizer;
            saveFile.listOnlyNearbyMaps = listOnlyNearbyMaps;
            saveFile.transformRootToOrigin = transformRootToOrigin;
            saveFile.downsampleWhenLocalizing = downsampleWhenLocalizing;
            saveFile.renderPointsAs3D = renderPointsAs3D;
            saveFile.pointSize = pointSize;

            saveFile.resolution = resolution;
            saveFile.localizer = localizer;
            saveFile.mapDetailLevel = mapDetailLevel;
            saveFile.serverLocalizationWithIds = serverLocalizationWithIds;
            saveFile.useDifferentARSpaces = useDifferentARSpaces;
            saveFile.preservePoses = preservePoses;
            saveFile.automaticCapture = automaticCapture;
            saveFile.automaticCaptureMaxImages = automaticCaptureMaxImages;
            saveFile.automaticCaptureInterval = automaticCaptureInterval;
            saveFile.windowSize = windowSize;

            string jsonstring = JsonUtility.ToJson(saveFile, true);
            string dataPath = Path.Combine(Application.persistentDataPath, m_Filename);
            //Debug.Log(dataPath);
            File.WriteAllText(dataPath, jsonstring);
        }

        public void ResetDeveloperSettings()
        {
            m_ResolutionDropdown.SetValueWithoutNotify(0);
            resolution = 0;
            //m_LocalizerDropdown.SetValueWithoutNotify(1);
            //localizer = 1;
            m_MapDetailLevelInput.SetTextWithoutNotify(1024.ToString());
            mapDetailLevel = 1024;
            m_ServerLocalizationWithIdsToggle.SetIsOnWithoutNotify(true);
            serverLocalizationWithIds = true;
            m_ListOnlyNearbyMapsToggle.SetIsOnWithoutNotify(false);
            listOnlyNearbyMaps = false;
            m_DownsampleWhenLocalizingToggle.SetIsOnWithoutNotify(false);
            downsampleWhenLocalizing = false;
            m_UseDifferentARSpacesToggle.SetIsOnWithoutNotify(true);
            useDifferentARSpaces = true;
            m_PreservePosesToggle.SetIsOnWithoutNotify(false);
            preservePoses = false;
            m_AutomaticCaptureToggle.SetIsOnWithoutNotify(false);
            automaticCapture = false;
            EnableAutomaticCaptureUI(automaticCapture);
            
            automaticCaptureMaxImages = Mathf.RoundToInt(40);
            m_AutomaticCaptureMaxImagesSlider.value = automaticCaptureMaxImages;

            automaticCaptureInterval = 0.6f;
            m_AutomaticCaptureIntervalSlider.value = automaticCaptureInterval;

            SaveSettingsToPrefs();
        }
    }
}
