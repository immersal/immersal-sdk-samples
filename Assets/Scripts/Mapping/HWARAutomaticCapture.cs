/*===============================================================================
Copyright (C) 2023 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Immersal.AR;
using Immersal.REST;
using HuaweiARUnitySDK;

namespace Immersal.Samples.Mapping
{
    public class HWARAutomaticCapture : MonoBehaviour
    {
        public Action OnMapSubmitted;
        public Action OnImageUploaded;

        protected bool m_bCaptureRunning = false;
        protected uint m_ImageRun = 0;
        protected int m_ImageIndex = 0;
        protected bool m_RgbCapture = false;

        protected ImmersalSDK m_Sdk = null;
        protected List<JobAsync> m_Jobs = new List<JobAsync>();
        protected int m_JobLock = 0;
        protected Camera m_MainCamera = null;

        void Start()
        {
            m_Sdk = ImmersalSDK.Instance;
            m_MainCamera = Camera.main;

            DirectoryInfo dataDir = new DirectoryInfo(tempImagePath);
            if (dataDir.Exists)
            {
                dataDir.Delete(true);
            }

            Directory.CreateDirectory(tempImagePath);

#if UNITY_IOS
            UnityEngine.iOS.Device.SetNoBackupFlag(tempImagePath);
#endif
        }

        private void Update()
        {
            if (m_JobLock == 1)
                return;
            if (m_Jobs.Count > 0)
            {
                m_JobLock = 1;
                RunJob(m_Jobs[0]);
            }
        }

        public string tempImagePath
        {
            get
            {
                return string.Format("{0}/Images", Application.persistentDataPath);
            }
        }

        virtual public async void Capture()
        {
            await Task.Delay(250);

            m_bCaptureRunning = true;
            float captureStartTime = Time.realtimeSinceStartup;
            float uploadStartTime = Time.realtimeSinceStartup;

			if (HWARHelper.TryGetCameraImageBytes(out ARCameraImageBytes image, out bool isHD))
            {
                JobCaptureAsync j = new JobCaptureAsync();
                j.run = (int)(m_ImageRun & 0xEFFFFFFF);
                j.index = m_ImageIndex++;
                j.anchor = false;

                if (AutomaticCaptureLocationProvider.Instance.gpsOn)
                {
                    j.latitude = AutomaticCaptureLocationProvider.Instance.latitude;
                    j.longitude = AutomaticCaptureLocationProvider.Instance.longitude;
                    j.altitude = AutomaticCaptureLocationProvider.Instance.altitude;
                }
                else
                {
                    j.latitude = j.longitude = j.altitude = 0.0;
                }

                Camera cam = m_MainCamera;
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

                Task<CaptureInfo> captureTask = Task.Run(() =>
                {
                    return Core.CaptureImage(capture, capture.Length, pixels, width, height, channels, 0);
                });

                await captureTask;

                string path = string.Format("{0}/{1}", tempImagePath, System.Guid.NewGuid());
                using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
                {
                    writer.Write(capture, 0, captureTask.Result.captureSize);
                }

                j.imagePath = path;
                j.encodedImage = "";

                j.OnStart += () =>
                {
                    uploadStartTime = Time.realtimeSinceStartup;
                };
                j.OnResult += (SDKImageResult result) =>
                {
                    float et = Time.realtimeSinceStartup - uploadStartTime;
                    Debug.Log(string.Format("Image uploaded successfully in {0} seconds", et));
                    OnImageUploaded?.Invoke();
                };
                j.Progress.ProgressChanged += (s, progress) =>
                {
                    int value = (int)(100f * progress);
                    Debug.Log(string.Format("Upload progress: {0}%", value));
                };
                j.OnError += (e) =>
                {
                    Debug.Log(string.Format("Capture error: " + e));
                };

                m_Jobs.Add(j);
                image.Dispose();

                float elapsedTime = Time.realtimeSinceStartup - captureStartTime;
                Debug.Log(string.Format("Capture in {0} seconds", elapsedTime));
            }

            m_bCaptureRunning = false;
        }

        public void Construct(string mapName, bool preservePoses, bool isPublic)
        {
            JobConstructAsync j = new JobConstructAsync();
            j.name = mapName;
            j.featureCount = 600;
            j.preservePoses = preservePoses;
            j.windowSize = 0;
            j.OnResult += (SDKConstructResult result) =>
            {
                Debug.Log(string.Format("Started constructing a map width ID {0}, containing {1} images and detail level of {2}", result.id, result.size, j.featureCount));

                OnMapSubmitted?.Invoke();

                if (isPublic)
                {
                    SetSharingMode(result.id, true);
                }
            };

            m_Jobs.Add(j);
        }

        public void SetSharingMode(int mapId, bool isPublic)
        {
            JobSetPrivacyAsync j = new JobSetPrivacyAsync();
            j.id = mapId;
            j.privacy = isPublic ? 1 : 0;
            j.OnResult += (SDKMapPrivacyResult result) =>
            {
                Debug.Log(string.Format("Sharing mode set successfully, set to: {0}", j.privacy));
            };

            m_Jobs.Add(j);
        }

        private async void RunJob(JobAsync j)
        {
            await j.RunJobAsync();

            if (m_Jobs.Count > 0)
            {
                m_Jobs.RemoveAt(0);
            }
            m_JobLock = 0;
        }

        internal void ImageRunUpdate()
        {
            long bin = System.DateTime.Now.ToBinary();
            uint data = (uint)bin ^ (uint)(bin >> 32);
            m_ImageRun = (m_ImageRun ^ data) * 16777619;
        }
    }
}
#endif