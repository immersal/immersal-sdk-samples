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
        public delegate void CaptureUpdateEvent(byte[] data);
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
        private static bool isRGBCamStart = false;
        private static bool isInitiate = false;

        public static bool IsRGBCamPlaying
        {
            get
            {
                return isRGBCamStart;
            }
        }

        private static NRRgbCamera.FixedSizedQueue m_RGBFrames;

        public static ObjectPool FramePool;

        public static void Initialize()
        {
            if (isInitiate)
            {
                return;
            }
            NRDebugger.Log("[NRRgbCamera] Initialize");
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

            isInitiate = true;
            SetImageFormat(CameraImageFormat.YUV_420_888);
        }

        private static void SetImageFormat(CameraImageFormat format)
        {
#if !UNITY_EDITOR
            m_NativeCamera.SetImageFormat(format);
#endif
            NRDebugger.Log("[NRRgbCamera] SetImageFormat : " + format.ToString());
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

                NRDebugger.Log(string.Format("[NRRgbCamera] on first fram ready textureptr:{0} rawdatasize:{1} Resolution:{2}",
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
                byte[] data = new byte[RawDataSize];
                Marshal.Copy(TexturePtr, data, 0, RawDataSize);
                OnCaptureUpdate(data);
            }
            m_NativeCamera.DestroyImage(rgb_camera_image_handle);
        }

        public static void Play()
        {
            if (!isInitiate)
            {
                Initialize();
            }
            if (isRGBCamStart)
            {
                return;
            }
            NRDebugger.Log("[NRRgbCamera] Start to play");
#if !UNITY_EDITOR
            m_NativeCamera.StartCapture();
#endif
            isRGBCamStart = true;
        }

        public static bool HasFrame()
        {
            return isRGBCamStart && m_RGBFrames.Count > 0;
        }

        private static int _lastFrame = -1;
        private static RGBRawDataFrame _currentFrame;
        public static RGBRawDataFrame GetRGBFrame()
        {
            if (Time.frameCount != _lastFrame)
            {
                _currentFrame = m_RGBFrames.Dequeue();
                _lastFrame = Time.frameCount;
            }

            return _currentFrame;
        }

        private static void QueueFrame(IntPtr textureptr, int size, UInt64 timestamp)
        {
            if (!isRGBCamStart)
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
            if (!isRGBCamStart)
            {
                return;
            }
            NRDebugger.Log("[NRRgbCamera] Start to Stop");
#if !UNITY_EDITOR
            m_NativeCamera.StopCapture();
#endif
            isRGBCamStart = false;

            Release();
        }

        public static void Release()
        {
            if (m_NativeCamera == null)
            {
                return;
            }

            NRDebugger.Log("[NRRgbCamera] Start to Release");
#if !UNITY_EDITOR
                m_NativeCamera.Release();
                m_NativeCamera = null;
#endif
            OnError = null;
            OnImageUpdate = null;
            isInitiate = false;
            isRGBCamStart = false;
        }
    }
}
