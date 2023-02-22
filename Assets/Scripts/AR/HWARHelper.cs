/*===============================================================================
Copyright (C) 2023 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

#if HWAR
using UnityEngine;
using HuaweiARUnitySDK;
using System.Runtime.InteropServices;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Immersal.AR
{
	public class HWARHelper {

        public static void GetIntrinsics(out Vector4 intrinsics, bool isHD, float width, float height)
        {
            intrinsics = Vector4.zero;

			if (!ARFrame.TextureIsAvailable())
				return;

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
        }
        
		public static void GetPlaneDataFast(ref IntPtr pixels, ARCameraImageBytes image)
		{
			int width = image.Width, height = image.Height;

			if (width == image.YRowStride)
			{
				pixels = image.Y;
			}
			else
			{
				unsafe
				{
					ulong handle;
					byte[] data = new byte[width * height];
					byte* srcPtr = (byte*)image.Y;
					byte* dstPtr = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(data, out handle);
					if (width > 0 && height > 0) {
						UnsafeUtility.MemCpyStride(dstPtr, width, srcPtr, image.YRowStride, width, height);
					}
					pixels = (IntPtr)dstPtr;
					UnsafeUtility.ReleaseGCObject(handle);
				}
			}
		}

		public static void GetPlaneData(out byte[] pixels, ARCameraImageBytes image)
		{
			IntPtr plane = image.Y;
			int width = image.Width, height = image.Height;
			int size = width * height;
			pixels = new byte[size];

			if (width == image.YRowStride)
			{
				Marshal.Copy(plane, pixels, 0, size);
			}
			else
			{
				unsafe
				{
					ulong handle;
					byte* srcPtr = (byte*)plane.ToPointer();
					byte* dstPtr = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(pixels, out handle);
					if (width > 0 && height > 0) {
						UnsafeUtility.MemCpyStride(dstPtr, width, srcPtr, image.YRowStride, width, height);
					}
					UnsafeUtility.ReleaseGCObject(handle);
				}
			}
		}

		public static bool TryGetCameraImageBytes(out ARCameraImageBytes image, out bool isHD)
		{
			isHD = false;

			if (ImmersalSDK.Instance.androidResolution == ImmersalSDK.CameraResolution.Max)
			{
				try
				{
					image = ARFrame.AcquirePreviewImageBytes();
					isHD = true;
				}
				catch (SystemException e)
				{
					Debug.LogError("Cannot acquire FullHD image: " + e.Message);
					image = ARFrame.AcquireCameraImageBytes();
				}
			}
			else
			{
				image = ARFrame.AcquireCameraImageBytes();
			}

			if (image != null && image.IsAvailable)
			{
				Debug.LogFormat("Got image with dimensions {0}x{1}", image.Width, image.Height);
				return true;
			}
			else
				return false;
		}

		public static bool TryGetTrackingQuality(out int quality)
		{
			quality = default;

			if (!ARFrame.TextureIsAvailable())
				return false;
			
			ARTrackable.TrackingState trackingState = default;

			using (ARCamera camera = ARFrame.GetCamera())
			{
				trackingState = camera.GetTrackingState();
			}
			
			switch (trackingState)
			{
				case ARTrackable.TrackingState.TRACKING:
					quality = 4;
					break;
				case ARTrackable.TrackingState.PAUSED:
					quality = 1;
					break;
				default:
					quality = 0;
					break;
			}

			return true;
		}
	}
}
#endif