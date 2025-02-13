/*===============================================================================
Copyright (C) 2025 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using HuaweiARUnitySDK;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Immersal.XR.Huawei
{
    public class HuaweiAREngineSupport : MonoBehaviour, IPlatformSupport
    {
        private Transform m_CameraTransform;
        private IPlatformConfiguration m_Configuration;
        private bool m_ConfigDone = false;

        private bool m_OverrideScreenOrientation = false;
        private ScreenOrientation m_ScreenOrientationOverride = ScreenOrientation.Portrait;


        public enum CameraResolution { Default, Max };	// With Huawei AR Engine SDK, only Default (640x480) and Max (1440x1080) are supported.
        
        [SerializeField]
        [Tooltip("Android resolution")]
        private CameraResolution m_AndroidResolution = CameraResolution.Max;
                
        [SerializeField]
        private CameraDataFormat m_CameraDataFormat = CameraDataFormat.SingleChannel;

        public CameraResolution androidResolution
        {
            get { return m_AndroidResolution; }
            set
            {
                m_AndroidResolution = value;
            }
        }

        private Task<(bool, CameraData)> m_CurrentCameraDataTask;
        private bool m_IsTracking = false;

        public async Task<IPlatformConfigureResult> ConfigurePlatform()
        {
            PlatformConfiguration config = new PlatformConfiguration
            {
                CameraDataFormat = m_CameraDataFormat
            };
            return await ConfigurePlatform(config);
        }

        public async Task<IPlatformConfigureResult> ConfigurePlatform(IPlatformConfiguration configuration)
        {
            ImmersalLogger.Log("Configuring Huawei Platform");
            
#if UNITY_EDITOR
            ImmersalLogger.LogWarning("Running Huawei AREngine Platform in Unity Editor will result in failed updates.");
#endif
            m_ConfigDone = await ConfigureHuawei();
            m_Configuration = configuration;
            
            if (Camera.main != null) m_CameraTransform = Camera.main.transform;

            IPlatformConfigureResult r = new SimplePlatformConfigureResult
            { 
                Success = m_ConfigDone
            };
            
            return r;
        }
        
        public async Task<IPlatformUpdateResult> UpdatePlatform()
        {
            return await UpdateWithConfiguration(m_Configuration);
        }
        
        public async Task<IPlatformUpdateResult> UpdatePlatform(IPlatformConfiguration oneShotConfiguration)
        {
            return await UpdateWithConfiguration(oneShotConfiguration);
        }
        
        private async Task<IPlatformUpdateResult> UpdateWithConfiguration(IPlatformConfiguration configuration)
        {
            ImmersalLogger.Log("Updating Huawei Platform");
            
            if (!m_ConfigDone)
                throw new ComponentTaskCriticalException("Trying to update platform before configuration.");
            
			using (ARCamera camera = ARFrame.GetCamera())
			{
	            m_IsTracking = camera.GetTrackingState() == ARTrackable.TrackingState.TRACKING;
			}
            
            // Status
            SimplePlatformStatus platformStatus = new SimplePlatformStatus
            {
                TrackingQuality = m_IsTracking ? 1 : 0
            };

            m_CurrentCameraDataTask = GetCameraData(configuration.CameraDataFormat);
            (bool success, CameraData data) = await m_CurrentCameraDataTask;

            // UpdateResult
            SimplePlatformUpdateResult r = new SimplePlatformUpdateResult
            {
                Success = success,
                Status = platformStatus,
                CameraData = data
            };
       
            return r;
        }

		private bool TryAcquireLatestCpuImage(out ARCameraImageBytes image, out bool isHD)
		{
			isHD = false;

            if (androidResolution == CameraResolution.Max)
			{
				try
				{
					image = ARFrame.AcquirePreviewImageBytes();
					isHD = true;
				}
				catch (SystemException e)
				{
                    ImmersalLogger.LogError("Cannot acquire FullHD image: " + e.Message);
					image = ARFrame.AcquireCameraImageBytes();
				}
			}
			else
			{
				image = ARFrame.AcquireCameraImageBytes();
			}

			if (image != null && image.IsAvailable && image.Width > 0 && image.Height > 0)
			{
                ImmersalLogger.Log($"Got image with dimensions {image.Width}x{image.Height}");
				return true;
			}
			else
            {
				return false;
            }
		}

        private async Task<(bool, CameraData)> GetCameraData(CameraDataFormat cameraDataFormat)
        {
            if (m_CameraTransform == null)
            {
                ImmersalLogger.LogError("Could not acquire camera pose.");
                return (false, null);
            }

            bool imageAcquired = TryAcquireLatestCpuImage(out ARCameraImageBytes image, out bool isHD);
            
            if (!imageAcquired)
            {
                ImmersalLogger.LogError("Could not acquire camera image.");
                return (false, null);
            }

            if (!GetIntrinsics(out Vector4 intrinsics, isHD, image.Width, image.Height))
            {
                ImmersalLogger.LogError("Could not acquire camera intrinsics.");
                return (false, null);
            }
            
            AREImageData imageData = new AREImageData(image, cameraDataFormat);
            CameraData data = new CameraData(imageData)
            {
                Width = image.Width,
                Height = image.Height,
                Intrinsics = intrinsics,
                Format = cameraDataFormat,
                Channels = cameraDataFormat == CameraDataFormat.SingleChannel ? 1 : 3,
                CameraPositionOnCapture = m_CameraTransform.position,
                CameraRotationOnCapture = m_CameraTransform.rotation,
                Orientation = GetOrientation()
            };

            return (true, data);
        }

        public bool GetIntrinsics(out Vector4 intrinsics, bool isHD, float width, float height)
        {
            intrinsics = Vector4.zero;

			if (!ARFrame.TextureIsAvailable())
            {
				return false;
            }

			if (isHD)
			{
				using (ARCamera camera = ARFrame.GetCamera())
				{
					ARCameraIntrinsics intr = camera.GetImageIntrinsics();
					Vector2 principalPoint = intr.GetPrincipalPoint();
					Vector2 focalLength = intr.GetFocalLength();

					intrinsics.x = focalLength.x;
					intrinsics.y = focalLength.y;
					intrinsics.z = principalPoint.y;	// wrong order in HWAR SDK?
					intrinsics.w = principalPoint.x;
				}
			}
			else
			{
				Camera cam = Camera.main;
				Matrix4x4 proj = HuaweiARUnitySDK.ARSession.GetProjectionMatrix(cam.nearClipPlane, cam.farClipPlane);

				float fy = 0.5f * proj.m11 * width;
				float cx = 0.5f * (proj.m02 + 1.0f) * height;
				float cy = 0.5f * (proj.m12 + 1.0f) * width;

				intrinsics.x = intrinsics.y = fy;
				intrinsics.z = cy;
				intrinsics.w = cx;
			}

            return true;
        }

        public void SetOrientationOverride(ScreenOrientation newOrientation)
        {
            m_OverrideScreenOrientation = true;
            m_ScreenOrientationOverride = newOrientation;
        }

        public void DisableOrientationOverride()
        {
            m_OverrideScreenOrientation = false;
        }
        
        public Quaternion GetOrientation()
        {
            ScreenOrientation orientation =
                m_OverrideScreenOrientation ? m_ScreenOrientationOverride : Screen.orientation;
            float angle = orientation switch
            {
                ScreenOrientation.Portrait => 90f,
                ScreenOrientation.LandscapeLeft => 180f,
                ScreenOrientation.LandscapeRight => 0f,
                ScreenOrientation.PortraitUpsideDown => -90f,
                _ => 0f
            };
            return Quaternion.Euler(0f, 0f, angle);
        }
        
        public async Task StopAndCleanUp()
        {
	         // there is no cancellation token for the update procedure here, just wait
             await m_CurrentCameraDataTask;
             m_ConfigDone = false;
             m_IsTracking = false;
        }

        private async Task<bool> ConfigureHuawei()
        {
			while (!ARFrame.TextureIsAvailable())
            {
                await Task.Yield();
            }

            return true;
        }
    }
    
    public class AREImageData : ImageData
    {
        public ARCameraImageBytes Image;
        private IntPtr m_unmanagedDataPointer;
        private byte[] m_managedBytes;

        public override IntPtr UnmanagedDataPointer => m_unmanagedDataPointer;

        public override byte[] ManagedBytes
        {
            get
            {
                if (m_managedBytes == null || m_managedBytes.Length == 0)
                {
                    m_managedBytes = CopyBytes();
                }

                return m_managedBytes;
            }
        }

        private CameraDataFormat m_Format;

        public AREImageData(ARCameraImageBytes image, CameraDataFormat format)
        {
            Image = image;
            m_Format = format;
            switch (format)
            {
                case CameraDataFormat.RGB:
                    GetPointerToRGB(ref m_unmanagedDataPointer, Image);
                    break;
                default:
                case CameraDataFormat.SingleChannel:
                    GetPointerFast(ref m_unmanagedDataPointer, Image);
                    break;
            }
        }

        public override void DisposeData()
        {
            Image.Dispose();
            m_unmanagedDataPointer = IntPtr.Zero;
        }

        private void GetPointerFast(ref IntPtr unmanagedPointer, ARCameraImageBytes image)
        {
			int width = image.Width, height = image.Height;

			if (width == image.YRowStride)
			{
                unmanagedPointer = image.Y;
			}
			else
			{
                byte[] data = new byte[width * height];

                unsafe
                {
                    fixed (byte* dstPtr = data)
                    {
                        byte* srcPtr = (byte*)image.Y;
                        if (width > 0 && height > 0)
                        {
                            UnsafeUtility.MemCpyStride(dstPtr, width, srcPtr, image.YRowStride, width, height);
                        }

                        unmanagedPointer = (IntPtr)dstPtr;
                    }
                }
			}
        }

        private void GetPointerToRGB(ref IntPtr unmanagedPointer, ARCameraImageBytes image)
        {
            int width = image.Width, height = image.Height;
            byte[] data = new byte[width * height * 3];

            unsafe
            {
                byte* yPlane = (byte*)image.Y;
                byte* uPlane = (byte*)image.U;
                byte* vPlane = (byte*)image.V;

                fixed (byte* rgbPtr = data)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int yIndex = y * image.YRowStride + x;
                            int uvIndex = (y / 2) * image.UVRowStride + (x / 2) * image.UVPixelStride;

                            byte Y = yPlane[yIndex];
                            byte U = uPlane[uvIndex];
                            byte V = vPlane[uvIndex];

                            int C = Y - 16;
                            int D = U - 128;
                            int E = V - 128;

                            int R = (int)(1.164f * C + 1.596f * E);
                            int G = (int)(1.164f * C - 0.392f * D - 0.813f * E);
                            int B = (int)(1.164f * C + 2.017f * D);

                            R = Mathf.Clamp(R, 0, 255);
                            G = Mathf.Clamp(G, 0, 255);
                            B = Mathf.Clamp(B, 0, 255);

                            int rgbIndex = (y * width + x) * 3;
                            rgbPtr[rgbIndex] = (byte)R;
                            rgbPtr[rgbIndex + 1] = (byte)G;
                            rgbPtr[rgbIndex + 2] = (byte)B;
                        }
                    }

                    unmanagedPointer = (IntPtr)rgbPtr;
                 }
            }
        }

        private byte[] CopyBytes()
        {
            int pixelSize = m_Format == CameraDataFormat.SingleChannel ? 1 : 3;
            int size = Image.Width * Image.Height * pixelSize;
            byte[] bytes = new byte[size];
            Marshal.Copy(m_unmanagedDataPointer, bytes, 0, size);
            return bytes;
        }
    }
}
#endif