/*===============================================================================
Copyright (C) 2023 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immersal.AR;
using Immersal.REST;
using HuaweiARUnitySDK;

namespace Immersal.Samples.Mapping
{
    public class HWARMapper : MapperBase
    {
        private void SetSessionState()
        {
			if (!ARFrame.TextureIsAvailable())
				return;

            using (ARCamera camera = ARFrame.GetCamera())
            {
                bool isTracking = camera.GetTrackingState() == ARTrackable.TrackingState.TRACKING;

                if (isTracking != m_IsTracking)
                {
                    ImageRunUpdate();
                }

                var captureButton = workspaceManager.captureButton.GetComponent<Button>();
                var localizeButton = visualizeManager.localizeButton.GetComponent<Button>();
                captureButton.interactable = isTracking;
                localizeButton.interactable = isTracking;
                m_IsTracking = isTracking;
            }
        }

        public override void Update()
        {
            SetSessionState();

            base.Update();
        }

        protected override async void Capture(bool anchor)
        {
            await Task.Delay(250);

            if(this.stats.imageCount + this.stats.queueLen >= this.stats.imageMax)
            {
                ImageLimitExceeded();
            }

            m_bCaptureRunning = true;
            float captureStartTime = Time.realtimeSinceStartup;
            float uploadStartTime = Time.realtimeSinceStartup;

			if (HWARHelper.TryGetCameraImageBytes(out ARCameraImageBytes image, out bool isHD))
            {
                JobCaptureAsync j = new JobCaptureAsync();
                j.run = (int)(m_ImageRun & 0xEFFFFFFF);
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
                HWARHelper.GetIntrinsics(out j.intrinsics, isHD, image.Width, image.Height);
                Quaternion rot = cam.transform.rotation;
                Vector3 pos = cam.transform.position;
                ARHelper.GetRotation(ref rot);
                j.rotation = ARHelper.SwitchHandedness(Matrix4x4.Rotate(rot));
                j.position = ARHelper.SwitchHandedness(pos);

                int width = image.Width;    
                int height = image.Height;

                byte[] pixels;
                int channels = 1;

                HWARHelper.GetPlaneData(out pixels, image);

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
                    Debug.Log(string.Format("Image uploaded successfully in {0} seconds", et));

                    mappingUIManager.HideProgressBar();
                };
                j.Progress.ProgressChanged += (s, progress) =>
                {
                    int value = (int)(100f * progress);
                    //Debug.Log(string.Format("Upload progress: {0}%", value));
                    mappingUIManager.SetProgress(value);
                };
                j.OnError += (e) =>
                {
                    mappingUIManager.HideProgressBar();
                };

                m_Jobs.Add(j);
                image.Dispose();

                float elapsedTime = Time.realtimeSinceStartup - captureStartTime;
                Debug.Log(string.Format("Capture in {0} seconds", elapsedTime));
            }

            m_bCaptureRunning = false;
            var captureButton = workspaceManager.captureButton.GetComponent<Button>();
            captureButton.interactable = true;
        }

        public void TryLocalize()
        {
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
#endif