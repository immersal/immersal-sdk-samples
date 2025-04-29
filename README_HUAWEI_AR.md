# Huawei Device Support

Immersal supports any device that is compatible with Huawei AR Engine. Since Huawei devices differ from typical Android devices, they do not support Google ARCore. Some Huawei devices support Huawei’s own AR Engine. Similar to ARCore, AR Engine provides SLAM tracking capabilities. Immersal’s integration works by obtaining the camera image, pose, and related parameters from AR Engine. Therefore, any Huawei device that supports Huawei AR Engine is supported by Immersal.

Please refer to the following official Huawei documentation for the list of supported models:
- [List of devices supported by AR Engine](https://developer.huawei.com/consumer/cn/doc/graphics-Guides/introduction-0000001050130900) 
- [Hardware and software dependency table for AR Engine features](https://developer.huawei.com/consumer/cn/doc/graphics-Guides/features-0000001060501339)

However, the last official version of the Huawei AR Engine Unity SDK is 2.11.0.2. While it is still available for download [here](https://developer.huawei.com/consumer/cn/doc/graphics-Library/sdk-download-0000001050150851), this version supports up to Unity 2019.x and does not support Universal Render Pipeline (URP).

Although Huawei no longer provides an official Unity SDK for newer versions, some community enthusiasts have created third-party Unity SDKs. Immersal’s integration is based on [this](https://github.com/chick-soups/HuaWeiAREngineUnitySDK) GitHub project, which uses AR Engine 3.7.0.3.

To build this Immersal sample project, follow the steps below to set up your environment.


## Step 1
Clone Immersal SDK Sample for HWAR. Note that you need to switch to the hwar branch.

## Step 2
Download the third-party Unity SDK mentioned above and copy the Assets/HuaweiARUnitySDK folder into the Assets/ directory of your current Unity project.

## Step 3
To access FullHD camera image resolution, apply the following patches.

Add the following code to `HuaweiARUnitySDK/Scripts/ARFrame.cs`:

```cs
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

Add the following code to `HuaweiARUnitySDK/Scripts/Adapter/ARFrameAdapter.cs`:
```cs
public IntPtr AcquirePreviewImage()
{
    IntPtr imageHandle = IntPtr.Zero;
    NDKARStatus status = NDKAPI.HwArFrame_acquirePreviewImage(m_ndkSession.SessionHandle, m_ndkSession.FrameHandle, ref imageHandle);
    ARExceptionAdapter.ExtractException(status);
    return imageHandle;
}
```

And also add:
```cs
[DllImport(AdapterConstants.HuaweiARNativeApi)]
public static extern NDKARStatus HwArFrame_acquirePreviewImage(IntPtr sessionHandle, IntPtr frameHandle,
    ref IntPtr ImageHandle);
```

## Step 4
Open the `Scene/SimpleSample` scene. In the Hierarchy, select the `ImmersalSDK` object, then choose Immersal Server. In the Unity interface, log in to your Immersal account via `Immersal SDK/Login`.
You can now import your map and begin development.

## Step 5
Before building the project, make sure to add the string `HWAR` to `Player Settings / Other Settings / Scripting Define Symbols`.

## Step 6
Build the project and install it on your device. Make sure to enable Developer Options and disable the `App Guard` feature on Huawei devices (otherwise, apps can only be installed via Huawei AppGallery).



# 华为设备的支持

Immersal支持任何适配了华为AR Engine的设备. 由于华为设备不同于通常的安卓设备, 华为设备不支持谷歌的ARCore, 部分设备支持华为的AR Engine. AR Engine类似于ARCore, 为设备提供了SLAM跟踪的能力. Immersal的适配是通过从AR Engine获得相机图片, 位姿和相关参数来实现的. 所以任何支持华为AR Engine的华为设备都可以支持. 请参考以下华为官方文档获知支持的型号名单:

- [AR Engine支持设备列表](https://developer.huawei.com/consumer/cn/doc/graphics-Guides/introduction-0000001050130900) 
- [AR Engine特性软硬件依赖表](https://developer.huawei.com/consumer/cn/doc/graphics-Guides/features-0000001060501339)

然而, 华为AR Engine Unity SDK的最后一个官方版本是 2.11.0.2。虽然开发者仍然可以从[这里](https://developer.huawei.com/consumer/cn/doc/graphics-Library/sdk-download-0000001050150851)下载, 但该版本最多支持 Unity 2019.x，并且不支持 Universal Render Pipeline（URP）. 

虽然华为官方不再为最新版本提供官方的Unity SDK. 但一些社区爱好者制作了第三方的Unity SDK。Immersal 的适配基于[这个GitHub项目](https://github.com/chick-soups/HuaWeiAREngineUnitySDK)，它使用的是 AR Engine 3.7.0.3。

要构建这个Immersal示例项目，按照下列步骤配置环境.


## Step 1.
克隆[Immersal SDK Sample for HWAR](https://github.com/immersal/immersal-sdk-samples/tree/hwar), 注意你需要切换到`hwar`分支.

## Step 2.
下载上述第三方Unity SDK，并将其中的`Assets/HuaweiARUnitySDK`文件夹复制到当前Unity项目的`Assets/`目录下。


## Step 3
为了能获取到FullHD分辨率的相机图片, 需要添加下列补丁.

在`HuaweiARUnitySDK/Scripts/ARFrame.cs`中添加下列代码:
```cs
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

在`HuaweiARUnitySDK/Scripts/Adapter/ARFrameAdapter.cs`中添加下列代码:
```cs
public IntPtr AcquirePreviewImage()
{
    IntPtr imageHandle = IntPtr.Zero;
    NDKARStatus status = NDKAPI.HwArFrame_acquirePreviewImage(m_ndkSession.SessionHandle, m_ndkSession.FrameHandle, ref imageHandle);
    ARExceptionAdapter.ExtractException(status);
    return imageHandle;
}
```

以及:
```cs
[DllImport(AdapterConstants.HuaweiARNativeApi)]
public static extern NDKARStatus HwArFrame_acquirePreviewImage(IntPtr sessionHandle, IntPtr frameHandle,
    ref IntPtr ImageHandle);
```

## Step 4. 
打开`Scene/SimpleSample`场景, 在Hierarchy中选中`ImmersalSDK`对象, 选择`Immersal Server`, 在Unity界面中通过`Immersal SDK/Login`登陆你的Immersal账号. 然后你可以导入你的地图, 开始开发.

## Step 5. 
在构建项目之前，请确保在 `Player Settings / Other Settings / Scripting Define Symbols` 中添加了 `HWAR` 字符串。

## Step 6. 
构建项目, 并安装至设备, 注意开启开发者选项, 并且禁用华为设备的'App Guard'功能(否则只能从华为官方商店安装应用).
