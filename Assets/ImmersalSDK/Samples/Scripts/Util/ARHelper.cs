/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Immersal.Samples.Util
{
	public class ARHelper {
        public static Vector4 GetIntrinsics(ARCameraManager manager = null)
        {
            if (manager == null)
                manager = Camera.main.GetComponent<ARCameraManager>();
            
            Vector4 intrinsics = Vector4.zero;
			XRCameraIntrinsics intr;

			if (manager.TryGetIntrinsics(out intr))
			{
				intrinsics.x = intr.focalLength.x;
				intrinsics.y = intr.focalLength.y;
				intrinsics.z = intr.principalPoint.x;
				intrinsics.w = intr.principalPoint.y;
            }

            return intrinsics;
        }
	}
}
