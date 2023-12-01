/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immersal.AR;
using Immersal.REST;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Immersal.Samples.Mapping
{
    public class Mapper : MapperBase
    {
        private Button m_CaptureButton;
        private Button m_LocalizeButton;

        private void SessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            ImageRunUpdate();

            m_IsTracking = args.state == ARSessionState.SessionTracking;
            m_CaptureButton.interactable = m_IsTracking;
            m_LocalizeButton.interactable = m_IsTracking;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CaptureButton = workspaceManager.captureButton.GetComponent<Button>();
            m_LocalizeButton = visualizeManager.localizeButton.GetComponent<Button>();
#if !UNITY_EDITOR
            ARSession.stateChanged += SessionStateChanged;
#endif
        }

        protected override void OnDisable()
        {
#if !UNITY_EDITOR
            ARSession.stateChanged -= SessionStateChanged;
#endif
            m_CaptureButton = null;
            m_LocalizeButton = null;

            base.OnDisable();
        }

        protected override async void Capture(bool anchor)
        {
            await Task.Delay(250);

            if (this.stats.imageCount + this.stats.queueLen >= this.stats.imageMax)
            {
                ImageLimitExceeded();
            }

            m_bCaptureRunning = true;
            float captureStartTime = Time.realtimeSinceStartup;
            float uploadStartTime = Time.realtimeSinceStartup;

            if (m_Sdk.cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                using (image)
                {
                    JobCaptureAsync j = new JobCaptureAsync();
                    j.run = (int)(m_ImageRun & 0x7FFFFFFF);
                    j.index = m_ImageIndex++;
                    j.anchor = anchor;

                    if (mapperSettings.useGps)
                    {
                        j.latitude = m_Latitude;
                        j.longitude = m_Longitude;
                        j.altitude = m_Altitude;
                    }
                    else
                    {
                        j.latitude = j.longitude = j.altitude = 0.0;
                    }

                    Camera cam = this.mainCamera;
                    ARHelper.GetIntrinsics(out j.intrinsics);
                    Quaternion rot = cam.transform.rotation;
                    Vector3 pos = cam.transform.position;
                    ARHelper.GetRotation(ref rot);
                    j.rotation = ARHelper.SwitchHandedness(Matrix4x4.Rotate(rot));
                    j.position = ARHelper.SwitchHandedness(pos);

                    int width = image.width;    
                    int height = image.height;

                    byte[] pixels;
                    int channels = 1;

                    if (mapperSettings.captureRgb)
                    {
                        ARHelper.GetPlaneDataRGB(out pixels, image);
                        channels = 3;
                    }
                    else
                    {
                        ARHelper.GetPlaneData(out pixels, image);
                    }

                    byte[] capture = new byte[channels * width * height + 8192];
                    int useMatching = mapperSettings.checkConnectivity ? 1 : 0;

                    Task<CaptureInfo> captureTask = Task.Run(() =>
                    {
                        return Core.CaptureImage(capture, capture.Length, pixels, width, height, channels, useMatching);
                    });

                    await captureTask;

                    string path = string.Format("{0}/{1}", this.tempImagePath, System.Guid.NewGuid());
                    using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
                    {
                        writer.Write(capture, 0, captureTask.Result.captureSize);
                    }

                    j.imagePath = path;
                    j.encodedImage = "";

                    if (mapperSettings.checkConnectivity)
                    {
                        NotifyIfConnected(captureTask.Result);
                    }

                    if (m_SessionFirstImage)
                        m_SessionFirstImage = false;
                    
                    j.OnStart += () =>
                    {
                        uploadStartTime = Time.realtimeSinceStartup;
                        mappingUIManager.SetProgress(0);
                        mappingUIManager.ShowProgressBar();
                    };
                    j.OnResult += (SDKImageResult result) =>
                    {
                        float et = Time.realtimeSinceStartup - uploadStartTime;
                        Debug.LogFormat("Image uploaded successfully in {0} seconds", et);

                        mappingUIManager.HideProgressBar();
                    };
                    j.Progress.ProgressChanged += (s, progress) =>
                    {
                        int value = (int)(100f * progress);
                        //Debug.LogFormat("Upload progress: {0}%", value);
                        mappingUIManager.SetProgress(value);
                    };
                    j.OnError += (e) =>
                    {
                        mappingUIManager.HideProgressBar();
                    };

                    m_Jobs.Add(j);

                    float elapsedTime = Time.realtimeSinceStartup - captureStartTime;
                    Debug.LogFormat("Capture in {0} seconds", elapsedTime);
                }
            }

            m_bCaptureRunning = false;
            m_CaptureButton.interactable = true;
        }

        public void TryLocalize()
        {
            m_Sdk.Localizer.SolverType = mapperSettings.solverType;
            
            if (mapperSettings.useGeoPoseLocalizer)
            {
                SDKMapId[] mapIds = GetActiveMapIds();
                m_Sdk.Localizer.LocalizeGeoPose(mapIds);
            }
            else if (mapperSettings.useServerLocalizer)
            {
                SDKMapId[] mapIds = GetActiveMapIds();
                m_Sdk.Localizer.LocalizeServer(mapIds);
            }
            else
            {
                m_Sdk.Localizer.Localize();
            }
        }
    }
}
