/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

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

namespace Immersal.AR.HWAR
{
	public class HWARHelper {

        public static Vector4 GetIntrinsics(float width, float height)
        {
			ARCameraIntrinsics intr = ARFrame.ImageIntrinsics;
            Vector4 intrinsics = Vector4.zero;

			Camera cam = Camera.main;
			Matrix4x4 proj = HuaweiARUnitySDK.ARSession.GetProjectionMatrix(cam.nearClipPlane, cam.farClipPlane);

			float fy = 0.5f * proj.m11 * width;
			float cx = 0.5f * (proj.m02 + 1.0f) * height;
			float cy = 0.5f * (proj.m12 + 1.0f) * width;

			intrinsics.x = intrinsics.y = fy;
			intrinsics.z = cy;
			intrinsics.w = cx;

            return intrinsics;
        }

		public static Vector4 GetIntrinsics()
        {
            Vector4 intrinsics = Vector4.zero;
			ARCameraIntrinsics intr = ARFrame.ImageIntrinsics;
			
			intrinsics.x = intr.ARFocalLength.x;
			intrinsics.y = intr.ARFocalLength.y;
			intrinsics.z = intr.ARPrincipalPoint.y;
			intrinsics.w = intr.ARPrincipalPoint.x;

            return intrinsics;
        }

		public static void GetRotation(ref Quaternion rot)
		{
			float angle = 0f;
			switch (Screen.orientation)
			{
				case ScreenOrientation.Portrait:
					angle = 0f;
					break;
				case ScreenOrientation.LandscapeLeft:
					angle = -90f;
					break;
				case ScreenOrientation.LandscapeRight:
					angle = 90f;
					break;
				case ScreenOrientation.PortraitUpsideDown:
					angle = 180f;
					break;
				default:
					angle = 0f;
					break;
			}

			rot *= Quaternion.Euler(0f, 0f, angle);
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

		public static bool TryGetTrackingQuality(out int quality)
		{
			quality = default;

			if (!ARFrame.TextureIsAvailable())
				return false;
			
			ARTrackable.TrackingState trackingState = ARFrame.GetTrackingState();
			
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