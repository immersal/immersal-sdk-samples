# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1] - 2019-09-12
### Added
- A simple multiplayer scene using Unity Networking (Note: You need to enable Multiplayer in Unity Services).
- Variable lighting adaptation
- Android 64-bit support
- SampleScene and `MapListController.cs` are back by popular demand.
- On-server localization
- Gravity-based map alignment

### Fixed
- Mapping and localization now work on iPad Mini (probably fixes problems with various Android devices as well).
- Point cloud renderer and runtime map loading fixes.
- On-device localization was sometimes giving false results.

### Changed
- Supported Unity version is now 2019.2.3f1+, should work on 2018 still with correct AR Foundation packages.
- Android plugin is now an .aar file with both 32-bit and 64-bit binaries.

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
