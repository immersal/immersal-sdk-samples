/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immersal.AR.HWAR;
using HuaweiARUnitySDK;

namespace Immersal.Samples.Mapping.HWAR
{
    public class HWARMapper : BaseMapper
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

        protected override IEnumerator Capture(bool anchor)
        {
            yield return new WaitForSeconds(0.25f);

            m_bCaptureRunning = true;

			ARCameraImageBytes image = null;
			bool isHD = HWARHelper.TryGetCameraImageBytes(out image);

			if (image != null && image.IsAvailable)
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
				j.intrinsics = isHD ? HWARHelper.GetIntrinsics() : HWARHelper.GetIntrinsics(image.Width, image.Height);
                int width = image.Width;
                int height = image.Height;

                byte[] pixels;
                int channels = 0;

                HWARHelper.GetPlaneData(out pixels, image);
                channels = 1;

                byte[] capture = new byte[channels * width * height + 1024];

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

                if (m_SessionFirstImage)
                    m_SessionFirstImage = false;

                m_Jobs.Add(j);
                image.Dispose();
            }

            m_bCaptureRunning = false;
            var captureButton = workspaceManager.captureButton.GetComponent<Button>();
            captureButton.interactable = true;
        }

        public override void Localize()
        {
			ARCameraImageBytes image = null;
            bool isHD = HWARHelper.TryGetCameraImageBytes(out image);

			if (image != null && image.IsAvailable)
            {
                CoroutineJobLocalize j = new CoroutineJobLocalize();
                Camera cam = this.mainCamera;
                j.intrinsics = isHD ? HWARHelper.GetIntrinsics() : HWARHelper.GetIntrinsics(image.Width, image.Height);
                j.width = image.Width;
                j.height = image.Height;
                j.rotation = cam.transform.rotation;
                j.position = cam.transform.position;
                j.host = this;

                HWARHelper.GetPlaneData(out j.pixels, image);
                m_Jobs.Add(j);
                image.Dispose();
            }
        }

        public override void LocalizeServer()
        {
			ARCameraImageBytes image = null;
            bool isHD = HWARHelper.TryGetCameraImageBytes(out image);

			if (image != null && image.IsAvailable)
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
                j.intrinsics = isHD ? HWARHelper.GetIntrinsics() : HWARHelper.GetIntrinsics(image.Width, image.Height);
                j.width = image.Width;
                j.height = image.Height;

                HWARHelper.GetPlaneData(out j.pixels, image);
                j.channels = 1;

                m_Jobs.Add(j);
                image.Dispose();
            }
        }
    }
}
#endif