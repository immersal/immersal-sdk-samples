# Prerequisites

This is just a shallow copy of the master branch, none of the sample scenes are implemented for Nreal Light. There is a scene under **/Scenes/HelloImmersal.unity** that shows how to localize with the SDK on the Nreal Light headset.

In the Assets folder, you need to add Nreal SDK (NRSDK folder): We have used NRSDK 1.5.7. Make sure your device has an up-to-date firmware (407+).
You also need to import the Immersal SDK core .unitypackage from our [Developer Portal](https://developers.immersal.com/ "Register and download SDK"), as well as the *Samples* from the master branch of this repository.

This sample has been verified to work on Unity 2019.4.18f1 LTS.

Our demo video: https://youtu.be/qUU8OIPy9W4
uses our internal Massively Multi-User solution which is not publicly available at the moment. If you want to achieve something similar, use Unity Networking (UNET), Photon Unity Networking (PUN), Google Firebase Realtime Database, SpatialOS or any other 3rd party framework to transfer the "ARSpace" coordinates between localized users.

Known issues:
* Localization is done using the images from the RGB camera. With the left/right SLAM tracking cameras, better results could be achieved.
* Only the localizer script *NRLocalizer* has been ported for Nreal, so none of the sample scenes work. If you want to create a working app, you should first map a space with Immersal Mapper (App Store: https://apps.apple.com/us/app/immersal-mapper/id1466607906, Play Store: https://play.google.com/store/apps/details?id=com.immersal.sdk.mapper), embed or load the map into your Unity scene and then use the *NRLocalizer* script to localize against the map.
* Nreal video capture cannot be used while localizing, as the device reserves the camera for itself. As a workaround, create "Start capture" / "Stop capture" buttons somewhere in the scene, do a few initial successful relocalizations, stop *NRLocalizer* and start the capture while tracking with the device's SLAM tracker.
