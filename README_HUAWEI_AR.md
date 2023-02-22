# Huawei AR Engine Support

The last official version of the Huawei AR Engine Unity SDK was 2.11.0.2. At the time of writing (Feb 22nd, 2023) it is still available on the [Chinese version of this page](https://developer.huawei.com/consumer/cn/doc/development/graphics-Library/sdk-download-0000001050150851). It works up to Unity 2019.x and doesn't support Universal Render Pipeline (URP).

Some community enthusiasts have been updating the Unity SDK to later versions of the AR Engine. Immersal support is based on [this GitHub project](https://github.com/chick-soups/HuaWeiAREngineUnitySDK), which uses AR Engine 3.7.0.3. To build this Immersal sample project, download the aforementioned project and copy the `HuaweiARUnitySDK` folder under `Assets/`. After that you can use Huawei AR Engine in your Unity 2020+ projects with URP. To build the project, make sure you have the string `HWAR` in `Player Settings / Other Settings / Scripting Define Symbols`.

To access the FullHD camera image from the SDK, some patching is required:

In `HuaweiARUnitySDK/Scripts/ARFrame.cs`, add:

```
public static ARCameraImageBytes AcquirePreviewImageBytes()
{
    if (ARSessionManager.Instance.SessionStatus != ARSessionStatus.RUNNING &&
        ARSessionManager.Instance.SessionStatus != ARSessionStatus.PAUSED)
    {
        throw new ARNotYetAvailableException();
    }
    IntPtr imagePtr = ARSessionManager.Instance.m_ndkSession.FrameAdapter.AcquirePreviewImage();
    return new ARCameraImageBytes(imagePtr, ARSessionManager.Instance.m_ndkSession);
}
```

In `HuaweiARUnitySDK/Scripts/Adapter/ARFrameAdapter.cs`, add:

```
public IntPtr AcquirePreviewImage()
{
    IntPtr imageHandle = IntPtr.Zero;
    NDKARStatus status = NDKAPI.HwArFrame_acquirePreviewImage(m_ndkSession.SessionHandle, m_ndkSession.FrameHandle, ref imageHandle);
    ARExceptionAdapter.ExtractException(status);
    return imageHandle;
}
```

and

```
[DllImport(AdapterConstants.HuaweiARNativeApi)]
public static extern NDKARStatus HwArFrame_acquirePreviewImage(IntPtr sessionHandle, IntPtr frameHandle,
    ref IntPtr ImageHandle);
```