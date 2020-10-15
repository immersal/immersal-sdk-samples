/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

namespace NRKernal
{
    using AOT;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using UnityEngine;

    public class NRYuvCamera
    {
        public delegate void CaptureEvent();
        public delegate void CaptureErrorEvent(string msg);
        public delegate void CaptureUpdateEvent(IntPtr texturePtr);
        public static CaptureEvent OnImageUpdate;
        public static CaptureUpdateEvent OnCaptureUpdate;
        public static CaptureErrorEvent OnError;

        private static NativeCamera m_NativeCamera { get; set; }

        private static IntPtr TexturePtr = IntPtr.Zero;

        public static NativeResolution Resolution
        {
            get
            {
#if !UNITY_EDITOR
                NativeResolution resolution = NRDevice.Instance.NativeHMD.GetEyeResolution(NativeEye.RGB);
#else   
                NativeResolution resolution = new NativeResolution(1280, 720);
#endif
                return resolution;
            }
        }

        public static int FrameCount = 0;
        private static bool m_IsPlaying = false;
        private static bool m_IsInitialized = false;

        public static bool IsRGBCamPlaying
        {
            get
            {
                return m_IsPlaying;
            }
        }

        private static NRRgbCamera.FixedSizedQueue m_RGBFrames;

        public static ObjectPool FramePool;

        private static List<RGBCameraTextureBase> m_ActiveTextures;

        public static void Initialize()
        {
            if (m_IsInitialized)
            {
                return;
            }
            NRDebugger.Log("[NRYuvCamera] Initialize");
            m_NativeCamera = new NativeCamera();
#if !UNITY_EDITOR
            m_NativeCamera.Create();
            m_NativeCamera.SetCaptureCallback(Capture);
#endif
            if (FramePool == null)
            {
                FramePool = new ObjectPool();
                FramePool.InitCount = 10;
            }
            if (m_RGBFrames == null)
            {
                m_RGBFrames = new NRRgbCamera.FixedSizedQueue(FramePool);
                m_RGBFrames.Limit = 5;
            }

            m_ActiveTextures = new List<RGBCameraTextureBase>();

            m_IsInitialized = true;
            SetImageFormat(CameraImageFormat.YUV_420_888);
        }

        public static void Regist(RGBCameraTextureBase tex)
        {
            Initialize();
            m_ActiveTextures.Add(tex);
        }

        public static void UnRegist(RGBCameraTextureBase tex)
        {
            int index = -1;
            for (int i = 0; i < m_ActiveTextures.Count; i++)
            {
                if (tex == m_ActiveTextures[i])
                {
                    index = i;
                }
            }
            if (index != -1)
            {
                m_ActiveTextures.RemoveAt(index);
            }
        }

        private static void SetImageFormat(CameraImageFormat format)
        {
#if !UNITY_EDITOR
            m_NativeCamera.SetImageFormat(format);
#endif
            NRDebugger.Log("[NRYuvCamera] SetImageFormat : " + format.ToString());
        }

        [MonoPInvokeCallback(typeof(NativeCamera.NRRGBCameraImageCallback))]
        public static void Capture(UInt64 rgb_camera_handle, UInt64 rgb_camera_image_handle, UInt64 userdata)
        {
            FrameCount++;
            int RawDataSize = 0;
            if (TexturePtr == IntPtr.Zero)
            {
                m_NativeCamera.GetRawData(rgb_camera_image_handle, ref TexturePtr, ref RawDataSize);
                m_NativeCamera.DestroyImage(rgb_camera_image_handle);

                NRDebugger.Log(string.Format("[NRYuvCamera] on first fram ready textureptr:{0} rawdatasize:{1} Resolution:{2}",
                   TexturePtr, RawDataSize, Resolution.ToString()));
                return;
            }

            m_NativeCamera.GetRawData(rgb_camera_image_handle, ref TexturePtr, ref RawDataSize);
            var timestamp = m_NativeCamera.GetHMDTimeNanos(rgb_camera_image_handle);
            QueueFrame(TexturePtr, RawDataSize, timestamp);

            if (OnImageUpdate != null)
            {
                OnImageUpdate();
            }

            if (OnCaptureUpdate != null)
            {
                OnCaptureUpdate(TexturePtr);
            }
            m_NativeCamera.DestroyImage(rgb_camera_image_handle);
        }

        public static void Play()
        {
            if (!m_IsInitialized)
            {
                Initialize();
            }
            if (m_IsPlaying)
            {
                return;
            }
            NRDebugger.Log("[NRYuvCamera] Start to play");
#if !UNITY_EDITOR
            m_NativeCamera.StartCapture();
#endif
            m_IsPlaying = true;
        }

        public static bool HasFrame()
        {
            return m_IsPlaying && (m_RGBFrames.Count > 0 || m_CurrentFrame.data != null);
        }

        private static int m_LastFrame = -1;
        private static RGBRawDataFrame m_CurrentFrame;
        public static RGBRawDataFrame GetRGBFrame()
        {
            if (Time.frameCount != m_LastFrame && m_RGBFrames.Count > 0)
            {
                m_CurrentFrame = m_RGBFrames.Dequeue();
                m_LastFrame = Time.frameCount;
            }

            return m_CurrentFrame;
        }

        private static void QueueFrame(IntPtr textureptr, int size, UInt64 timestamp)
        {
            if (!m_IsPlaying)
            {
                NRDebugger.LogError("rgb camera not stopped properly, it still sending data.");
                return;
            }
            RGBRawDataFrame frame = FramePool.Get<RGBRawDataFrame>();
            bool result = RGBRawDataFrame.MakeSafe(TexturePtr, size, timestamp, ref frame);
            if (result)
            {
                m_RGBFrames.Enqueue(frame);
            }
            else
            {
                FramePool.Put<RGBRawDataFrame>(frame);
            }
        }

        public static void Stop()
        {
            if (!m_IsPlaying)
            {
                return;
            }
            NRDebugger.Log("[NRYuvCamera] Start to Stop");

            // If there is no a active texture, pause and release camera resource.
            if (m_ActiveTextures.Count == 0)
            {
                m_IsPlaying = false;
#if !UNITY_EDITOR
                m_NativeCamera.StopCapture();
#endif
                Release();
            }
        }

        public static void Release()
        {
            if (m_NativeCamera == null)
            {
                return;
            }

            NRDebugger.Log("[NRYuvCamera] Start to Release");
#if !UNITY_EDITOR
            m_NativeCamera.Release();
            m_NativeCamera = null;
#endif
            m_CurrentFrame.data = null;
            OnError = null;
            OnImageUpdate = null;
            m_IsInitialized = false;
            m_IsPlaying = false;
        }
    }
}
