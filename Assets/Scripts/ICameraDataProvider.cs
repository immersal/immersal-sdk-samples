/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.XR.MagicLeap;

public interface ICameraDataProvider
{
    bool TryAcquireIntrinsics(out Vector4 intrinsics);
    bool TryAcquireLatestData(out byte[] processedImageData, out MLCamera.YUVBuffer yBuffer, out Transform cameraTransform);
    bool TryAcquirePngBytes(out byte[] pngBytes, out Transform cameraTransform);
}
