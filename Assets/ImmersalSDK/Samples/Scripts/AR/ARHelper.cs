/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Runtime.InteropServices;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Immersal.AR
{
	public class ARHelper {
        public static Vector4 GetIntrinsics(ARCameraManager manager)
        {
            Vector4 intrinsics = Vector4.zero;
			XRCameraIntrinsics intr;

			if (manager != null && manager.TryGetIntrinsics(out intr))
			{
				intrinsics.x = intr.focalLength.x;
				intrinsics.y = intr.focalLength.y;
				intrinsics.z = intr.principalPoint.x;
				intrinsics.w = intr.principalPoint.y;
            }

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
	}
}
