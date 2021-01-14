/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.XR.MagicLeap;
using Immersal;

public class MLCameraDataProvider : MonoBehaviour, ICameraDataProvider
{
    [SerializeField] private MLPrivilegeRequesterBehavior privilegeRequester;
    [SerializeField] private GraphicsFormat pngFormat = GraphicsFormat.R8_SRGB;
    [SerializeField] private float captureInterval = 1.0f;
    [SerializeField] private bool verboseDebugLogging;
    
    private MLCamera.YUVBuffer latestYUVBuffer;
    private Camera cam;
    private Transform cameraTransformAtLatestCapture;
    
    private bool cameraIsConnected;
    private bool cameraHasStarted;
    private Thread cameraCaptureThread;
    private object cameraLockObject = new object();
    
    #pragma warning disable 414
    private bool appWasPaused;
    #pragma warning restore 414

    private void Awake()
    {
        if(privilegeRequester == null)
        {
            privilegeRequester = FindObjectOfType<MLPrivilegeRequesterBehavior>();

            if (privilegeRequester == null)
            {
                Abort("PrivilegeRequester is not set.");
                return;
            }
        }

        cam = Camera.main;
        
        #if PLATFORM_LUMIN
        privilegeRequester.OnPrivilegesDone += OnPrivilegesDone;
        #endif
    }

    private void OnDisable()
    {
        lock (cameraLockObject)
        {
            if (cameraIsConnected)
            {
                #if PLATFORM_LUMIN
                MLCamera.OnRawImageAvailableYUV -= OnCaptureRawImageYUVComplete;
                #endif
                DisableMLCamera();
            }
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;
        appWasPaused = true;
        
        lock (cameraLockObject)
        {
            if (cameraIsConnected)
            {
                // Stop repeating capture invokes
                CancelInvoke();
                    
                #if PLATFORM_LUMIN
                MLCamera.OnRawImageAvailableYUV -= OnCaptureRawImageYUVComplete;
                #endif

                DisableMLCamera();
            }
        }

        cameraHasStarted = false;
    }
    private void EnableMLCamera()
    {
#if PLATFORM_LUMIN
        lock (cameraLockObject)
        {
            if (MLCamera.Start().IsOk)
            {
                if (MLCamera.Connect().IsOk)
                {
                    cameraIsConnected = true;
                }
                else
                {
                    Abort("Failed to connect to camera resource.");
                }
            }
            else
            {
                Abort("Failed to start camera API.");
            }
        }
#endif
    }

    private void OnDestroy()
    {
        if (privilegeRequester != null)
        {
            #if PLATFORM_LUMIN
            privilegeRequester.OnPrivilegesDone -= OnPrivilegesDone;
            #endif
        }
    }

    private void DisableMLCamera()
    {
        #if PLATFORM_LUMIN
        lock (cameraLockObject)
        {
            if (MLCamera.IsStarted)
            {
                MLCamera.Disconnect();
                cameraIsConnected = false;
                MLCamera.Stop();
            }
        }
        #endif
    }
    
    private void EnableCapture()
    {
        if (!cameraHasStarted)
        {
            lock (cameraLockObject)
            {
                EnableMLCamera();

                #if PLATFORM_LUMIN
                MLCamera.OnRawImageAvailableYUV += OnCaptureRawImageYUVComplete;
                #endif
            }

            cameraHasStarted = true;
            
            InvokeRepeating("TriggerAsyncCapture", captureInterval, captureInterval);
        }
    }
    
    public void TriggerAsyncCapture()
    {
        Log("Triggering asynchronous capture.");
        if (cameraCaptureThread == null || (!cameraCaptureThread.IsAlive))
        {
            Log("Starting camera capture thread.");
            cameraCaptureThread = new Thread(CaptureThreadWorker);
            cameraCaptureThread.Start();
        }
        else
        {
            Log("Canceled new capture due to existing capture thread running.");
        }
    }
    
    private void CaptureThreadWorker()
    {
    #if PLATFORM_LUMIN
        lock (cameraLockObject)
        {
            if (!MLCamera.IsStarted || !cameraIsConnected) return;
            
            if (!MLCamera.CaptureRawImageAsync(MLCamera.OutputFormat.YUV_420_888).IsOk)
            {
                Log("Failed to initialize camera capture request.");
            }
        }
    #endif
    }
    
    #if PLATFORM_LUMIN
    private void OnPrivilegesDone(MLResult result)
    {
        if (!result.IsOk)
        {
            Abort($"Failed to get all requested privileges: {result}");
            return;
        }
        Log("Succeeded in requesting all privileges.");

        // Check if we are resuming from application pause
        if (appWasPaused)
        {
            appWasPaused = false;
            
            if (!MLCamera.ApplicationPause(appWasPaused).IsOk)
            {
                Abort($"Failed to resume MLCamera: {result}");
                return;
            }

            cameraIsConnected = true;
        }
        else
        {
            EnableCapture();
        }
    }
    #endif
    
    private void OnCaptureRawImageYUVComplete(MLCamera.YUVFrameInfo frameInfo)
    {
        Log("Capture process completed.");
        latestYUVBuffer = frameInfo.Y;
        cameraTransformAtLatestCapture = cam.transform;
    }

    public bool TryAcquireIntrinsics(out Vector4 intrinsics)
    {
        intrinsics = Vector4.zero;

        if (!MLCamera.IsStarted) return false;
        
        MLCamera.IntrinsicCalibrationParameters intr;

        if (MLCamera.GetIntrinsicCalibrationParameters(out intr).IsOk)
        {
            intrinsics.x = intr.FocalLength.x;
            intrinsics.y = intr.FocalLength.y;
            intrinsics.z = intr.PrincipalPoint.x;
            intrinsics.w = intr.PrincipalPoint.y;
            return true;
        }
        return false;
    }

    public bool TryAcquireLatestData(out byte[] processedImageData, out MLCamera.YUVBuffer yBuffer, out Transform cameraTransform)
    {
        processedImageData = null;
        yBuffer = latestYUVBuffer;
        cameraTransform = cameraTransformAtLatestCapture;

        if (yBuffer.Data is null || yBuffer.Data.Length <= 0) return false;
        GetUnpaddedBytes(latestYUVBuffer, false, out processedImageData);

        return !(processedImageData is null || processedImageData.Length <= 0);
    }

    public bool TryAcquirePngBytes(out byte[] pngBytes, out Transform cameraTransform)
    {
        pngBytes = null;
        cameraTransform = cameraTransformAtLatestCapture;
        if (latestYUVBuffer.Data is null || latestYUVBuffer.Data.Length <= 0) return false;

        byte[] pixels;
        int channels = 1;
        int width = (int)latestYUVBuffer.Width;
        int height = (int)latestYUVBuffer.Height;

        GetUnpaddedBytes(latestYUVBuffer, false, out pixels);

        pngBytes = new byte[channels * width * height + 1024];
        icvCaptureInfo info = Immersal.Core.CaptureImage(pngBytes, pngBytes.Length, pixels, width, height, channels);
        Array.Resize(ref pngBytes, info.captureSize);
        return !(pngBytes is null || pngBytes.Length <= 0);
    }

/*    public bool TryAcquirePngBytes(out byte[] pngBytes, out Transform cameraTransform)
    {
        pngBytes = null;
        cameraTransform = cameraTransformAtLatestCapture;
        if (latestYUVBuffer.Data is null || latestYUVBuffer.Data.Length <= 0) return false;

        GetUnpaddedBytes(latestYUVBuffer, true, out pngBytes);
        const uint channel = 4;
        byte[] newPngBytes = null;
        GetChannelTimesBytes(latestYUVBuffer, channel, pngBytes, out newPngBytes);
        pngBytes = newPngBytes;
        Log("Encoding YUV data into PNG format.");
        pngBytes = ImageConversion.EncodeArrayToPNG(pngBytes, pngFormat, latestYUVBuffer.Width, latestYUVBuffer.Height, latestYUVBuffer.Width * channel);
//        pngBytes = ImageConversion.EncodeArrayToPNG(pngBytes, pngFormat, latestYUVBuffer.Width, latestYUVBuffer.Height);
        return !(pngBytes is null || pngBytes.Length <= 0);
    }*/

    private void GetUnpaddedBytes(MLCamera.YUVBuffer yBuffer, bool invertVertically, out byte[] pixelBuffer)
    {
        Log("Removing padding from image bytes.");
        
        byte[] data = yBuffer.Data;
        int width = (int)yBuffer.Width, height = (int)yBuffer.Height;
        int stride = invertVertically ? -(int)yBuffer.Stride : (int)yBuffer.Stride;
        int invertStartOffset = ((int)yBuffer.Stride * height) - (int)yBuffer.Stride;
        pixelBuffer = new byte[width * height];

        unsafe
        {
            fixed (byte* pinnedData = data)
            {
                ulong handle;
                byte* srcPtr = invertVertically ? pinnedData + invertStartOffset : pinnedData;
                byte* dstPtr = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(pixelBuffer, out handle);
                if (width > 0 && height > 0) {
                    UnsafeUtility.MemCpyStride(dstPtr, width, srcPtr, stride, width, height);
                }
                UnsafeUtility.ReleaseGCObject(handle);                
            }
        }
    }

    private void GetChannelTimesBytes(MLCamera.YUVBuffer yBuffer, uint channel, byte[] pixelBuffer, out byte[] newPixelBuffer)
    {
        int width = (int)yBuffer.Width, height = (int)yBuffer.Height;
        newPixelBuffer = new byte[width * channel * height];
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                for (int h = 0; h < channel; h++)
                {
                    newPixelBuffer[width * channel * j + i * channel + h] = h != channel - 1 ? pixelBuffer[width * j + i] : System.Convert.ToByte(255);
                }
            }
        }
    }

    private void Log(string message)
    {
        if (verboseDebugLogging) Debug.Log($"MLCDP: {message}");
    }

    private void Abort(string errorMessage, params object[] args)
    {
        Debug.LogErrorFormat($"MLCDP aborting: {errorMessage}", args);
        enabled = false;
    }
}
