# Immersal AR Cloud SDK Samples
In this repository you will find sample projects that use our [Immersal AR Cloud SDK](https://immersal.com/developers/ "Register and download SDK"), demonstrating some of the functionalities of the SDK. The currently included examples are listed below:

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

# Compatibility

- Unity 2019.2+
- AR Foundation 2.1+

Note: Unity 2018 should still work with corresponding AR Foundation packages, but we are not officially supporting it.

# Installation steps

1. Clone this repository
```
git clone https://github.com/immersal/arcloud-sdk-samples.git
```
2. Download our Unity Plugin (`ImmersalARCloudSDKvX_X.unitypackage`) from [here](https://immersal.com/developers)
3. Launch Unity, click on **Open Project**, navigate to the `arcloud-sdk-samples` folder on your computer and press Apply/OK.
4. Click on **Assets -> Import Package -> Custom Package** and load the `ImmersalARCloudSDKvX_X.unitypackage`.

Optional step:

5. Click on **Window -> Package Manager** and install `AR Foundation`, `ARCore XR Plugin`, `ARKit XR Plugin` and `TextMesh Pro` if required.

Please visit our [Developer Documentation](https://immersal.com/developers/docs/ "SDK Documentation") for more detailed instructions as to how to use these examples.

