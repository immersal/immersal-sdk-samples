/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Util;
using HuaweiARUnitySDK;

namespace Immersal.Samples.Mapping
{
    public class HWARMapper : MapperBase
    {
        private void SetSessionState()
        {
            bool isTracking = ARFrame.GetTrackingState() == ARTrackable.TrackingState.TRACKING;

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

        public override void Update()
        {
            SetSessionState();

            base.Update();
        }

        protected override async void Capture(bool anchor)
        {
            await Task.Delay(250);

            m_bCaptureRunning = true;
            float captureStartTime = Time.realtimeSinceStartup;
            float uploadStartTime = Time.realtimeSinceStartup;

			ARCameraImageBytes image = null;
			bool isHD = HWARHelper.TryGetCameraImageBytes(out image);

			if (image != null && image.IsAvailable)
            {
                JobCaptureAsync j = new JobCaptureAsync();
                j.run = (int)(m_ImageRun & 0xEFFFFFFF);
                j.index = m_ImageIndex++;
                j.anchor = anchor;

                if (gpsOn)
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
                Quaternion _q = cam.transform.rotation;
                Matrix4x4 rot = Matrix4x4.Rotate(new Quaternion(_q.x, _q.y, -_q.z, -_q.w));
                Vector3 _p = cam.transform.position;
                Vector3 pos = new Vector3(_p.x, _p.y, -_p.z);
                j.rotation = rot;
                j.position = pos;

                HWARHelper.GetIntrinsics(out j.intrinsics, isHD, image.Width, image.Height);
                
                int width = image.Width;
                int height = image.Height;

                byte[] pixels;
                int channels = 0;

                HWARHelper.GetPlaneData(out pixels, image);
                channels = 1;

                byte[] capture = new byte[channels * width * height + 1024];

                Task<icvCaptureInfo> captureTask = Task.Run(() =>
                {
                    return Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                });

                await captureTask;

                string path = string.Format("{0}/{1}", this.tempImagePath, System.Guid.NewGuid());
                using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
                {
                    writer.Write(capture, 0, captureTask.Result.captureSize);
                }

                j.imagePath = path;
                j.encodedImage = "";

                NotifyIfConnected(captureTask.Result);

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

                m_Jobs.Add(j.RunJobAsync());
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
            if (mapperSettings.useServerLocalizer)
            {
                LocalizeServer();
            }
            else
            {
                Localize();
            }
        }

        public override async void Localize()
        {
			ARCameraImageBytes image = null;
            bool isHD = HWARHelper.TryGetCameraImageBytes(out image);

			if (image != null && image.IsAvailable)
            {
                Vector4 intrinsics;
                Camera cam = this.mainCamera;
                Vector3 camPos = cam.transform.position;
                Quaternion camRot = cam.transform.rotation;
                int param1 = mapperSettings.param1;
                int param2 = mapperSettings.param2;
                float param3 = mapperSettings.param3;
                float param4 = mapperSettings.param4;
                int method = mapperSettings.localizer;

                HWARHelper.GetIntrinsics(out intrinsics, isHD, image.Width, image.Height);
                HWARHelper.GetPlaneDataFast(ref m_PixelBuffer, image);

                if (m_PixelBuffer != IntPtr.Zero)
                {
                    Vector3 position = Vector3.zero;
                    Quaternion rotation = Quaternion.identity;

                    Task<int> t = Task.Run(() =>
                    {
                        return Immersal.Core.LocalizeImage(out position, out rotation, image.Width, image.Height, ref intrinsics, m_PixelBuffer, param1, param2, param3, param4, method);
                    });

                    await t;

                    int mapHandle = t.Result;

                    if (mapHandle >= 0)
                    {
                        this.stats.locSucc++;

                        Debug.Log("*************************** Localization Succeeded ***************************");
                        Debug.Log(string.Format("params: {0}, {1}, {2}, {3}", param1, param2, param3, param4));
                        Matrix4x4 cloudSpace = Matrix4x4.TRS(position, rotation, Vector3.one);
                        Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                        Debug.Log("handle " + mapHandle + "\n" +
                                "fc 4x4\n" + cloudSpace + "\n" +
                                "ft 4x4\n" + trackerSpace);

                        Matrix4x4 m = trackerSpace*(cloudSpace.inverse);

                        LocalizerPose lastLocalizedPose;
                        LocalizerBase.GetLocalizerPose(out lastLocalizedPose, mapHandle, position, rotation, m.inverse);
                        this.lastLocalizedPose = lastLocalizedPose;

                        foreach (PointCloudRenderer p in this.pcr.Values)
                        {
                            if (p.mapHandle == mapHandle)
                            {
                                p.go.transform.position = m.GetColumn(3);
                                p.go.transform.rotation = m.rotation;
                                break;
                            }
                        }
                    }
                    else
                    {
                        this.stats.locFail++;
                        Debug.Log("*************************** Localization Failed ***************************");
                    }
                    
                }

                image.Dispose();
            }
        }

        public override async void LocalizeServer()
        {
			ARCameraImageBytes image = null;
            bool isHD = HWARHelper.TryGetCameraImageBytes(out image);

			if (image != null && image.IsAvailable)
            {
                JobLocalizeServerAsync j = new JobLocalizeServerAsync();

                if (mapperSettings.serverLocalizationWithIds)
                {
                    int n = pcr.Count;

                    j.mapIds = new SDKMapId[n];

                    int count = 0;
                    foreach (int id in pcr.Keys)
                    {
                        j.mapIds[count] = new SDKMapId();
                        j.mapIds[count++].id = id;
                    }
                }
                else
                {
                    j.useGPS = true;
                    j.latitude = m_Latitude;
                    j.longitude = m_Longitude;
                    j.radius = DefaultRadius;
                }

                byte[] pixels;
                Camera cam = this.mainCamera;
                Vector3 camPos = cam.transform.position;
                Quaternion camRot = cam.transform.rotation;
                int channels = 1;
                int width = image.Width, height = image.Height;
                
                j.param1 = mapperSettings.param1;
                j.param2 = mapperSettings.param2;
                j.param3 = mapperSettings.param3;
                j.param4 = mapperSettings.param4;

                HWARHelper.GetIntrinsics(out j.intrinsics, isHD, image.Width, image.Height);
                HWARHelper.GetPlaneData(out pixels, image);

                Task<(byte[], icvCaptureInfo)> t = Task.Run(() =>
                {
                    byte[] capture = new byte[channels * width * height + 1024];
                    icvCaptureInfo info = Immersal.Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                    Array.Resize(ref capture, info.captureSize);
                    return (capture, info);
                });

                await t;

                j.image = t.Result.Item1;

                j.OnResult += (SDKLocalizeResult result) =>
                {
                    if (result.success)
                    {
                        Matrix4x4 m = Matrix4x4.identity;
                        Matrix4x4 cloudSpace = Matrix4x4.identity;
                        cloudSpace.m00 = result.r00; cloudSpace.m01 = result.r01; cloudSpace.m02 = result.r02; cloudSpace.m03 = result.px;
                        cloudSpace.m10 = result.r10; cloudSpace.m11 = result.r11; cloudSpace.m12 = result.r12; cloudSpace.m13 = result.py;
                        cloudSpace.m20 = result.r20; cloudSpace.m21 = result.r21; cloudSpace.m22 = result.r22; cloudSpace.m23 = result.pz;
                        Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                        this.stats.locSucc++;

                        Debug.Log("*************************** On-Server Localization Succeeded ***************************");
                        Debug.Log(string.Format("params: {0}, {1}, {2}, {3}", j.param1, j.param2, j.param3, j.param4));
                        Debug.Log("fc 4x4\n" + cloudSpace + "\n" +
                                "ft 4x4\n" + trackerSpace);

                        m = trackerSpace * (cloudSpace.inverse);

                        foreach (KeyValuePair<int, PointCloudRenderer> p in this.pcr)
                        {
                            if (p.Key == result.map)
                            {
                                p.Value.go.transform.position = m.GetColumn(3);
                                p.Value.go.transform.rotation = m.rotation;
                                break;
                            }
                        }

                        JobEcefAsync je = new JobEcefAsync();
                        je.id = result.map;
                        je.OnResult += (SDKEcefResult result2) =>
                        {
                            LocalizerPose lastLocalizedPose;
                            LocalizerBase.GetLocalizerPose(out lastLocalizedPose, result.map, cloudSpace.GetColumn(3), cloudSpace.rotation, m.inverse, result2.ecef);
                            this.lastLocalizedPose = lastLocalizedPose;
                        };

                        m_Jobs.Add(je.RunJobAsync());
                    }
                    else
                    {
                        this.stats.locFail++;
                        Debug.Log("*************************** On-Server Localization Failed ***************************");
                    }
                };

                m_Jobs.Add(j.RunJobAsync());
                image.Dispose();
            }
        }
    }
}
#endif