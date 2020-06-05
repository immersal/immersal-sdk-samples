/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immersal.AR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Immersal.Samples.Mapping
{
    public class Mapper : BaseMapper
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
            float startTime = Time.realtimeSinceStartup;

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

                if (useGPS)
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

                Task<string> convertTask = captureTask.ContinueWith<string>((antecedent) =>
                {
                    return Convert.ToBase64String(capture, 0, antecedent.Result.captureSize);
                });

                while (!convertTask.IsCompleted)
                {
                    yield return null;
                }

                /*
                Task<(string, icvCaptureInfo)> t = Task.Run(() =>
                {
                    icvCaptureInfo info = Core.CaptureImage(capture, capture.Length, pixels, width, height, channels);
                    return (Convert.ToBase64String(capture, 0, info.captureSize), info);
                });

                while (!t.IsCompleted)
                {
                    yield return null;
                }

                j.encodedImage = t.Result.Item1;
                NotifyIfConnected(t.Result.Item2);
                */

                j.encodedImage = convertTask.Result;
                NotifyIfConnected(captureTask.Result);

                if (m_SessionFirstImage)
                    m_SessionFirstImage = false;

                m_Jobs.Add(j);
                image.Dispose();

                float elapsedTime = Time.realtimeSinceStartup - startTime;
                Debug.Log(string.Format("Capture in {0} seconds", elapsedTime));
            }

            m_bCaptureRunning = false;
            var captureButton = workspaceManager.captureButton.GetComponent<Button>();
            captureButton.interactable = true;
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
                j.intrinsics = ARHelper.GetIntrinsics();
                j.width = image.width;
                j.height = image.height;
                j.rotation = cam.transform.rotation;
                j.position = cam.transform.position;
                j.host = this;

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
                j.host = this;

                if (this.useGPS)
                {
                    j.useGPS = true;
                    j.latitude = m_Latitude;
                    j.longitude = m_Longitude;
                    j.radius = DefaultRadius;
                }

                Camera cam = this.mainCamera;
                j.rotation = cam.transform.rotation;
                j.position = cam.transform.position;
                j.intrinsics = ARHelper.GetIntrinsics();
                j.width = image.width;
                j.height = image.height;

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

                m_Jobs.Add(j);
                image.Dispose();
            }
        }
    }
}
