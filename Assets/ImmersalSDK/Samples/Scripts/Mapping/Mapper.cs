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
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Util;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Immersal.Samples.Mapping
{
    public class Mapper : MapperBase
    {
        private void SessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            ImageRunUpdate();

            m_IsTracking = args.state == ARSessionState.SessionTracking;

            var captureButton = workspaceManager.captureButton.GetComponent<Button>();
            var localizeButton = visualizeManager.localizeButton.GetComponent<Button>();
            captureButton.interactable = m_IsTracking;
            localizeButton.interactable = m_IsTracking;
        }

        protected override void OnEnable()
        {
#if !UNITY_EDITOR
            ARSession.stateChanged += SessionStateChanged;
#endif
            base.OnEnable();
        }

        protected override void OnDisable()
        {
#if !UNITY_EDITOR
            ARSession.stateChanged -= SessionStateChanged;
#endif
            base.OnDisable();
        }

        protected override IEnumerator Capture(bool anchor)
        {
            yield return new WaitForSeconds(0.25f);

            m_bCaptureRunning = true;
            float captureStartTime = Time.realtimeSinceStartup;
            float uploadStartTime = Time.realtimeSinceStartup;

            XRCameraImage image;
            ARCameraManager cameraManager = m_Sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image))
            {
                CoroutineJobCapture j = new CoroutineJobCapture();
                j.host = this;
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
                Quaternion _q = cam.transform.rotation;
                Matrix4x4 r = Matrix4x4.Rotate(new Quaternion(_q.x, _q.y, -_q.z, -_q.w));
                Vector3 _p = cam.transform.position;
                Vector3 p = new Vector3(_p.x, _p.y, -_p.z);
                j.rotation = r;
                j.position = p;
				j.intrinsics = ARHelper.GetIntrinsics();
                int width = image.width;
                int height = image.height;

                byte[] pixels;
                int channels = 0;

                if (m_RgbCapture)
                {
                    ARHelper.GetPlaneDataRGB(out pixels, image);
                    channels = 3;
                }
                else
                {
                    ARHelper.GetPlaneData(out pixels, image);
                    channels = 1;
                }

                byte[] capture = new byte[channels * width * height + 1024];

                Task<icvCaptureInfo> captureTask = Task.Run(() =>
                {
                    return Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                });

                while (!captureTask.IsCompleted)
                {
                    yield return null;
                }

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
                j.OnSuccess += (SDKImageResult result) =>
                {
                    if (result.error == "none")
                    {
                        float et = Time.realtimeSinceStartup - uploadStartTime;
                        Debug.Log(string.Format("Image uploaded successfully in {0} seconds", et));
                    }

                    mappingUIManager.HideProgressBar();
                };
                j.OnProgress += (float progress) =>
                {
                    int value = (int)(100f * progress);
                    //Debug.Log(string.Format("Upload progress: {0}%", value));
                    mappingUIManager.SetProgress(value);
                };
                j.OnError += (UnityWebRequest request) =>
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
            if (mapperSettings.useServerLocalizer)
            {
                LocalizeServer();
            }
            else
            {
                Localize();
            }
        }

        public override void Localize()
        {
            XRCameraImage image;
            ARCameraManager cameraManager = m_Sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image))
            {
                CoroutineJobLocalize j = new CoroutineJobLocalize();
                Camera cam = this.mainCamera;
                Vector3 camPos = cam.transform.position;
                Quaternion camRot = cam.transform.rotation;
                j.intrinsics = ARHelper.GetIntrinsics();
                j.width = image.width;
                j.height = image.height;
                j.rotation = camRot;
                j.position = camPos;
                j.param1 = mapperSettings.param1;
                j.param2 = mapperSettings.param2;
                j.param3 = mapperSettings.param3;
                j.param4 = mapperSettings.param4;
                j.OnSuccess += (int mapHandle, Vector3 position, Quaternion rotation) =>
                {
                    this.stats.locSucc++;

                    Debug.Log("*************************** Localization Succeeded ***************************");
                    Debug.Log(string.Format("params: {0}, {1}, {2}, {3}", j.param1, j.param2, j.param3, j.param4));
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
                };
                j.OnFail += () =>
                {
                    this.stats.locFail++;
                    Debug.Log("*************************** Localization Failed ***************************");
                };

                ARHelper.GetPlaneData(out j.pixels, image);
                m_Jobs.Add(j);
                image.Dispose();
            }
        }

        public override void LocalizeServer()
        {
            bool rgb = false;   

            ARCameraManager cameraManager = m_Sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            XRCameraImage image;
            if (cameraSubsystem.TryGetLatestImage(out image))
            {
                CoroutineJobLocalizeServer j = new CoroutineJobLocalizeServer();

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

                Camera cam = this.mainCamera;
                Vector3 camPos = cam.transform.position;
                Quaternion camRot = cam.transform.rotation;
                j.host = this;
                j.rotation = camRot;
                j.position = camPos;
                j.intrinsics = ARHelper.GetIntrinsics();
                j.width = image.width;
                j.height = image.height;
                j.param1 = mapperSettings.param1;
                j.param2 = mapperSettings.param2;
                j.param3 = mapperSettings.param3;
                j.param4 = mapperSettings.param4;

                if (rgb)
                {
                    ARHelper.GetPlaneDataRGB(out j.pixels, image);
                    j.channels = 3;
                }
                else
                {
                    ARHelper.GetPlaneData(out j.pixels, image);
                    j.channels = 1;
                }

                j.OnResult += (SDKLocalizeResult result) =>
                {
                    /*if (result.error == "none")
                    {*/
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

                            CoroutineJobEcef je = new CoroutineJobEcef();
                            je.host = this;
                            je.id = result.map;
                            je.OnSuccess += (SDKEcefResult result2) =>
                            {
                                if (result2.error == "none")
                                {
                                    Debug.Log(result2.ecef);
                                    LocalizerPose lastLocalizedPose;
                                    LocalizerBase.GetLocalizerPose(out lastLocalizedPose, result.map, cloudSpace.GetColumn(3), cloudSpace.rotation, m.inverse, result2.ecef);
                                    this.lastLocalizedPose = lastLocalizedPose;
                                }
                                else
                                {
                                    Debug.LogError(result2.error);
                                }
                            };

                            m_Jobs.Add(je);
                        }
                        else
                        {
                            this.stats.locFail++;
                            Debug.Log("*************************** On-Server Localization Failed ***************************");
                        }
                    /*}
                    else
                    {
                        Debug.LogError(result.error);
                    }*/
                };

                m_Jobs.Add(j);
                image.Dispose();
            }
        }
    }
}
