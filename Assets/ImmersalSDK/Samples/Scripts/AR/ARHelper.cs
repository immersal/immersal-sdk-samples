/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
#if HWAR
using HuaweiARUnitySDK;
#else
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif
using System.Runtime.InteropServices;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Immersal;

namespace Immersal.AR
{
	public class ARHelper {
		#if HWAR
        public static Vector4 GetIntrinsics(float width, float height)
        {
			ARCameraIntrinsics intr = ARFrame.ImageIntrinsics;
            Vector4 intrinsics = Vector4.zero;

			Camera cam = Camera.main;
			Matrix4x4 proj = ARSession.GetProjectionMatrix(cam.nearClipPlane, cam.farClipPlane);

			float fy = 0.5f * proj.m11 * width;
			float cx = 0.5f * (proj.m02 + 1.0f) * height;
			float cy = 0.5f * (proj.m12 + 1.0f) * width;

			intrinsics.x = intrinsics.y = fy;
			intrinsics.z = cy;
			intrinsics.w = cx;

            return intrinsics;
        }
		#endif
		public static Vector4 GetIntrinsics()
        {
            Vector4 intrinsics = Vector4.zero;
			#if HWAR
			ARCameraIntrinsics intr = ARFrame.ImageIntrinsics;
			
			intrinsics.x = intr.ARFocalLength.x;
			intrinsics.y = intr.ARFocalLength.y;
			intrinsics.z = intr.ARPrincipalPoint.y;
			intrinsics.w = intr.ARPrincipalPoint.x;
			#else
			XRCameraIntrinsics intr;
			ARCameraManager manager = ImmersalSDK.Instance?.cameraManager;

			if (manager != null && manager.TryGetIntrinsics(out intr))
			{
				intrinsics.x = intr.focalLength.x;
				intrinsics.y = intr.focalLength.y;
				intrinsics.z = intr.principalPoint.x;
				intrinsics.w = intr.principalPoint.y;
            }
			#endif

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

		#if HWAR
		public static void GetDimensions(out int width, out int height, ARCameraImageBytes image)
		{
			width = image.Width;
			height = image.Height;
		}
		#else
		public static void GetDimensions(out int width, out int height, XRCameraImage image)
		{
			width = image.width;
			height = image.height;
		}
		#endif

		#if HWAR
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
		#else
		public static void GetPlaneData(out byte[] pixels, XRCameraImage image)
		{
			XRCameraImagePlane plane = image.GetPlane(0); // use the Y plane
			int width = image.width, height = image.height;
			pixels = new byte[width * height];

			if (width == plane.rowStride)
			{
				plane.data.CopyTo(pixels);
			}
			else
			{
				unsafe
				{
					ulong handle;
					byte* srcPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(plane.data);
					byte* dstPtr = (byte*)UnsafeUtility.PinGCArrayAndGetDataAddress(pixels, out handle);
					if (width > 0 && height > 0) {
						UnsafeUtility.MemCpyStride(dstPtr, width, srcPtr, plane.rowStride, width, height);
					}
					UnsafeUtility.ReleaseGCObject(handle);
				}
			}
		}

		public static void GetPlaneDataRGB(out byte[] pixels, XRCameraImage image)
		{
			var conversionParams = new XRCameraImageConversionParams
			{
				inputRect = new RectInt(0, 0, image.width, image.height),
				outputDimensions = new Vector2Int(image.width, image.height),
				outputFormat = TextureFormat.RGB24,
				transformation = CameraImageTransformation.None
			};

			int size = image.GetConvertedDataSize(conversionParams);
			pixels = new byte[size];
			GCHandle bufferHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
			image.Convert(conversionParams, bufferHandle.AddrOfPinnedObject(), pixels.Length);
			bufferHandle.Free();
		}
		#endif

		public static bool TryGetTrackingQuality(out int quality)
		{
			quality = default;

			#if HWAR
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
			#else
			if (ImmersalSDK.Instance?.arSession == null)
				return false;
			
			var arSubsystem = ImmersalSDK.Instance?.arSession.subsystem;
			
			if (arSubsystem != null && arSubsystem.running)
			{
				switch (arSubsystem.trackingState)
				{
					case TrackingState.Tracking:
						quality = 4;
						break;
					case TrackingState.Limited:
						quality = 1;
						break;
					case TrackingState.None:
						quality = 0;
						break;
				}
			}
			#endif

			return true;
		}
	}
}
