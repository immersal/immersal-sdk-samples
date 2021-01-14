# Prerequisites

This is just a shallow copy of the master branch, none of the sample scenes are implemented for Magic Leap. There is a scene under **/Scenes/BasicLocalization.unity** that shows how to localize with the SDK on the Magic Leap 1 headset.

In the Assets folder, you need to add Magic Leap Unity SDK (MagicLeap folder): We have used version 0.24.2.

Recommended / tested configuration:

* Unity 2020.1.17f1
* AR Foundation 3.1.6
* Magic Leap XR Plugin 5.1.2
* XR Plugin Management 3.2.17
* Lumin SDK 0.24.1
* Magic Leap Unity Package 0.24.2

Known issues:
* Unity's Magic Leap XR Plugin 6.0 reserves the device's camera during the whole session, so Unity 2020.2 / AR Foundation 4 cannot be used until Unity fixes the plugin.
* None of the sample scenes from the master branch work. If you want to create a working app, you should first map a space with Immersal Mapper (App Store: https://apps.apple.com/us/app/immersal-mapper/id1466607906, Play Store: https://play.google.com/store/apps/details?id=com.immersal.sdk.mapper), and load the map into your Unity scene and then use the *MLLocalizer* script to localize against the map.


# Immersal SDK Samples
In this repository you will find sample projects that use our [Immersal SDK](https://developers.immersal.com/ "Register and download SDK"), demonstrating some of the functionalities of the Augmented Reality SDK. The currently included examples are listed below:

## MultimapSampleScene
A simple scene that localizes the device using previously generated (embedded) maps and displays 3D objects relative to the maps. You need to capture and download your own maps to demonstrate this functionality, see **MappingApp** below.

## ContentPlacementSample
Allows for dropping objects in the AR space. The locations are saved locally, but not persisted across devices.

### Content.json

Q: Oh noes! I ran ContentPlacementSample locally on the Unity Editor, and got a `content.json not found` error.

A: Relax, the `content.json` is generated when the app is run on an Android or iOS device.

## NavigationSample
An AR wayfinding example.

## MappingApp
A full-featured app for mapping spaces using an iOS or Android device.

Also available pre-built on [App Store](https://apps.apple.com/app/immersal-mapper/id1466607906) and [Play Store](https://play.google.com/store/apps/details?id=com.immersal.sdk.mapper).

# Compatibility

- Unity 2019.4 LTS
- AR Foundation 4

Note: Earlier versions of Unity and AR Foundation will still work with minimal script changes.

# Installation steps

1. Clone this repository
```
git clone https://github.com/immersal/arcloud-sdk-samples.git
```
2. Download our Unity Plugin (`ImmersalSDKvX_X_X.unitypackage`) from [here](https://developers.immersal.com/)
3. Launch Unity, click on **Open Project**, navigate to the `arcloud-sdk-samples` folder on your computer and press Apply/OK.
4. Click on **Assets -> Import Package -> Custom Package** and load the `ImmersalSDKvX_X_X.unitypackage`.

Optional step:

5. Click on **Window -> Package Manager** and install `AR Foundation`, `ARCore XR Plugin`, `ARKit XR Plugin` and `TextMesh Pro` if required.

Please visit our [Developer Documentation](https://developers.immersal.com/docs/ "SDK Documentation") for more detailed instructions as to how to use these examples.

