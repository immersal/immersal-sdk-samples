/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.1.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
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

		public static Matrix4x4 ToCloudSpace(Vector3 camPos, Quaternion camRot, Vector3 cloudPos, Quaternion cloudRot)
		{
			Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
			Matrix4x4 trackerToCloudSpace = Matrix4x4.TRS(cloudPos, cloudRot, Vector3.one);
			Matrix4x4 cloudSpace = trackerToCloudSpace.inverse * trackerSpace;

			return cloudSpace;
		}

		public static Matrix4x4 FromCloudSpace(Vector3 cloudPos, Quaternion cloudRot, Vector3 camPos, Quaternion camRot)
		{
			Matrix4x4 cloudSpace = Matrix4x4.TRS(cloudPos, cloudRot, Vector3.one);
			Matrix4x4 trackerSpace = Matrix4x4.TRS(camPos, camRot, Vector3.one);
			Matrix4x4 m = trackerSpace * (cloudSpace.inverse);

			return m;
		}
	}
}
