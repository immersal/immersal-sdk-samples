# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.10.0] - 2020-11-11
### Added
- REST API: proper URI identifiers
- REST API: new endpoints for image capture and on-server localization, where the image payload can be sent as binary instead of a Base64-encoded string.
- Immersal Mapper / REST API: Use tracker poses (beta)

### Changed
- Immersal Mapper: Smaller filesize in image uploads
- Immersal Mapper: Localization and capturing is now faster especially on older devices, such as iPhone 7 and beyond

## [1.9.0] - 2020-10-20
### Added
- Samples: Magic Leap localization support and a reference Unity project, [see here](https://github.com/immersal/arcloud-sdk-samples/tree/magicleap)
- Core: OnPoseFound and OnPoseLost events in `ImmersalSDK`, OnFirstLocalization event in `ARMap`
- Core: `ARLocalizer` now has start/stop/pause/resume methods

### Changed
- Core: Localizer does not return the map id anymore. Instead, it returns a map handle like an earlier SDK version did.
- Immersal Mapper: Updated UI
- Samples: Restructured Unity core package and the Unity sample project. `ARMap`, `ARLocalizer`, and other essential scripts are now part of the core package. This should make future SDK updates to new versions easier.
- Samples: Code cleanup
- Samples: Reworked the `pointcloud3d` shader to support point size attribute on OpenGL, Vulkan and Metal platforms
- Samples: Mapper app sample updated to the latest version

## [1.8.0] - 2020-09-11
### Added
- Samples: Nreal Light localization support, [see here](https://github.com/immersal/arcloud-sdk-samples/tree/nreal)
- Samples: Progress bar for map loading

### Changed
- Samples: Huawei AR Engine support moved to its [own branch](https://github.com/immersal/arcloud-sdk-samples/tree/hwar)
- Samples: Api Compatibility Level was errorneously set to .NET 4.x, switched back to .NET Standard 2.0
- Samples: Mapper now stores queued images into the persistent data directory to avoid out-of-memory crashes
- Samples: Updated to Unity 2019.4.9f1 LTS and AR Foundation 4.0.8

### Fixed
- Samples: `NativeBindings` errors in Unity Editor

## [1.7.0] - 2020-07-02
### Added
- Core: Improved localization robustness
- Core: REST API clean-up; error, start, progress and complete callbacks
- Core: REST API; Map construct detail level
- Samples: Native double precision GPS coordinates for iOS and Android
- Samples: Image upload progress bar to Mapper UI
- Samples: Map construct detail level in Mapper
- Samples: Visual compass bearing in Mapper

### Changed
- Core: Smaller plug-in size
- Core: Changed map hashes from MD5 to SHA256
- Samples: Updated to Unity 2019.4 LTS and AR Foundation 4

### Fixed
- Samples: `ARMap.cs` crash problems in Unity Editor
- Samples: Bug fixes to Huawei AR sample scripts

## [1.6.0] - 2020-06-05
### Added
- Server / Developer Portal: Support for textured meshes (both grayscale and RGB).
- Samples: `ARLocalizer.cs`, `ARSpace.cs` & `ARMap.cs`: support for uniform scaling of maps.
- Samples: `ARLocalizer.cs` now has `MapChanged` and `PoseFound` events.

### Fixed
- Plugin: image capturing is now a lot faster.

## [1.5.0] - 2020-04-30
### Added
- Server / Developer Portal: Support for private/public maps, copying maps to other accounts, searching for public maps by user and/or GPS location, and resetting the private user token.

### Changed
- Map file format is not compatible with v1.3 anymore. v1.5.0 plugin can still load maps done with v1.3.
- MD5 hashing of map files changed to SHA256

## [1.4.1] - 2020-04-17 (INTERNAL)
### Added
- MD5 hashing of map files

### Changed
- Huawei AR Engine selection can be done at runtime.

## [1.4.0] - 2020-04-07 (INTERNAL)
### Added
- SDK and samples now support Huawei AR Engine SDK as an alternative to AR Foundation (not included, can be downloaded [here](https://developer.huawei.com/consumer/en/ar).
- `ARLocalizer.cs`: Toggle to reset localizer filtering when the last localized map ID changes -- might be useful in a multimap scenario, where maps are not aligned to each other on purpose.
- `ARLocalizer.cs`: Made `lastLocalizedMapId` public.
- Graph-based navigation example, initial version.

### Fixed
- Android now properly requests for location (GPS) permissions.

### Changed
- Generated maps are now 80% smaller.
- The native plugins are now 75% smaller.
- Both the localization accuracy and speed have been improved.

## [1.3] - 2020-02-07
### Added
- Plugin, Server, Mapper sample: GPS support. When the GPS toggle is on, the list of maps will be populated only by maps generated within a 200 meter radius. When mapping (taking pictures), the latitude/longitude/altitude is saved with the image and used to create a geopose for the constructed map. Also, the on-server localizer accepts lat/lon as parameters for faster retrieval of the map to relocalize against to.
- Plugin, Server, Mapper sample: Visual GPS (VGPS) / geopose support. If geopose data is saved with the map, the localize functions will return the device's geolocation (GPS latitude/longitude/altitude coordinates). This is approximately 10.000 times faster than satellite-based GPS, more accurate, and works indoors! As an added benefit, there is no need to have Location Services enabled. Geopose (geolocation with orientation) can also be calculated.
- On-server localizer now accepts RGB24 images, too. Might be useful with Web AR / headsets, if an 8-bit grayscale PNG is not available or is too costly to generate.
- Developer Portal and Mapper sample: Added support for a 'sparse' processing state. Generated maps are downloadable as `.bytes` and `sparse.ply` files at this point and relocalization is possible. The `dense.ply` will be generated in the background and will be available on the Developer Portal when the processing state is "done". This will speed up the map generation a lot!
- Increased the accuracy of dense map point cloud files.

### Fixed
- `ARLocalizer.cs`: "Burst mode" is now also run when the app enters foreground.
- `ARLocalizer.cs` and `ARHelper.cs`: Fixed rotation for different screen orientations, so you can publish in portrait/landscape/auto.
- Plugin: Localizer returns -1 handle if no maps are loaded.

### Changed
- Renamed Immersal AR Cloud SDK to just Immersal SDK.
- Samples: Updated project to Unity 2019.2.20f1.
- Samples: Added dependency to AR Foundation 3.0.1.
- Changed `mapHandle` in the core and samples scripts to `mapId`, as the localizer now returns the real database ID for the map (if available).
- Map format: Maps generated with the SDK v1.3 are not compatible with earlier versions of the SDK. However, maps generated with older versions continue to work with v1.3.

## [1.2] - 2019-10-16
### Added
- Developer Portal: Map stitching; you can select multiple maps and combine them into one new map (assuming the maps have overlapping features).
- Samples: Mapping App; continue updating older maps by restoring the map source data.
- Samples: Mapping App; you can now delete maps in the app.
- SDK: Mapping performance improvements.
- SDK: REST API updated and cleaned up.

### Fixed
- Samples: The "AR Cloud space" rotation and position can now be get from `ARSpace.cs` instead of `ARLocalizer.cs`, which was buggy anyway and returned only the first map's pose, thus giving incorrect results when using multimaps.
- Samples: Navigation and multiplayer samples fixed to work with multimaps.
- Samples: UI fixes

### Changed
- Samples: Updated project to Unity 2019.2.8f1 and AR Foundation 3.0.0 preview.3, should continue to work just fine with older versions.
- Developer Portal: Updated EULA.

## [1.1] - 2019-09-12
### Added
- A simple Multiplayer Sample using Unity Networking (Note: You need to enable Multiplayer in Unity Services).
- Variable lighting adaptation
- Android 64-bit support
- Map Download Sample (previously SampleScene) and `MapListController.cs` are back by popular demand.
- On-server localization
- Gravity-based map alignment when constructing a new map.

### Fixed
- Mapping and localization now work on iPad Mini (probably fixes problems with various Android devices as well).
- Point cloud renderer and runtime map loading fixes.
- On-device localization was sometimes giving false results.

### Changed
- Supported Unity version is now 2019.2.3f1+, should work on 2018 with corresponding AR Foundation packages.
- Android plugin is now an `.aar` file with both 32-bit and 64-bit binaries.

## [1.01] - 2019-06-24
### Added
- Samples: Mapping App now notifies the user if sequential captured images can be connected by their matching feature points.

### Changed
- Samples: C# API updated for AR Foundation 1.5 / 2.2.
- Samples: Point cloud preview bugs fixed.
- Samples: Now distributed in a separate [GitHub repository](https://github.com/immersal/arcloud-sdk-samples).
- SDK: Requires Unity 2018.4 LTS.
- SDK: Now distributed as a .unitypackage (available on the Developer Portal).

## [0.19] - 2019-06-12
### Added
- SDK: Improved multimap support in Unity Editor.
- SDK: `ARLocalizer.cs` finds the first pose much faster.

### Changed
- Samples: Improved Mapping App with separate Workspace and Visualize modes.
- Samples: Changes to sample scenes to support multimap feature.
- SDK: `ARSpace.cs` functionality moved to `ARMap.cs` to clarify multimap workflow.

## [0.18] - 2019-05-23
### Added
- Samples: Downsample option in `ARLocalizer.cs`. Uses less memory and is faster.
- Samples: Multimap loading support in Mapping App.
- Samples: RGB Image Capture Toggle in Mapping App.
- Developer Portal: Delete map function.
- SDK: Initial multimap support.

### Changed
- SDK: Fixed camera intrinsics calculation (fixes screen space Y offset bug on iOS devices).
- SDK: `ARCloud.cs` script removed, `ARSpace.cs` has the same functionality.
- Known issue in Samples: Removed the drop-down menu for dynamic map loading. It needs to be completely reworked to support multimaps.

## [0.17] - 2019-04-08
### Added
- Samples: Persistent Content Placement Sample Scene.
- Samples: Pose Filtering in SampleScene.
- Developer Portal: Dense point cloud download.
- SDK: RGB Camera Capture option.

### Changed
- Samples: Mapping App UI and UX improvements.
- SDK: Updated to OpenCV 4.
- SDK: Improved network bandwidth usage during mapping.

## [0.16] - 2019-03-07
### Added
- Samples: Indoor Navigation Sample Scene.
- Samples: Tracking Quality Indicator PoseIndicator in SampleScene.
- Developer Portal: Sparse point cloud download.
- SDK: Feature Anchor sets map orientation.

### Changed
- Samples: Option to switch between different maps in addition to the embedded one.
- Samples: Mapping App UI and UX improvements.
- Samples: Mapping App Capture Delay decreased from 0.5 seconds to 0.25 seconds.
- Unity Package: Project cleanup.

### Fixed
- Samples: Fixed crash when no debug text was assigned to ARLocalizer.
- SDK: Fixed a bug with setting the camera resolution. Now defaults to best possible.

## [0.15] - 2019-02-17

### This is the first release of the Immersal AR Cloud SDK for Unity.
