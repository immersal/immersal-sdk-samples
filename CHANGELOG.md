# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.16.1] - 2022-03-03
### Changed
- Changed `NRLocalizer` registration to work with Immersal SDK 1.16
- Updated Unity packages

## [1.12.1] - 2021-05-04
### Changed
- `NRLocalizer.cs`: Better way for acquiring RGB camera position and intrinsics. Should result in more accurate localization.
- Project settings: Disabled URP for now, as the Nreal logo asset used in the project doesn't support it (URP can still be used of course).

### Added
- Support for RGB video capturing while localizing. This has the following prerequisites:
  * Disable Multithreaded Rendering in Android build settings
  * In the `ImmersalSDK.prefab`, disable 'Use YUV' in `NRLocalizer`
  * Enable the 'Canvas' and 'VideoCapture' GameObjects in the sample scene

## [1.12.0] - 2021-04-06
### Changed
- Updated project to work with Immersal SDK 1.12.0

### Added
- GeoPose localize function

## [1.11.3] - 2021-02-09
### Changed
- Updated to NRSDK 1.5.7 (REQUIRED, also make sure your Nreal firmware is 407 or newer)
- Removed `NRYuvCamera.cs` as obsolete
- Removed `NRAppController.cs` as obsolete

## [1.11.2] - 2021-01-12
### Changed
- `NRLocalizer`: Added support for the on-server localizer
- `NRLocalizer`: Minor changes to reflect the Immersal SDK 1.11 API changes
- Updated project files

## [1.10.0] - 2020-11-11
### Changed
- `NRLocalizer.cs`: Minor changes to reflect the Immersal SDK 1.10.0 API changes

## [1.9.0] - 2020-10-20
### Changed
- Updated for NRSDK 1.4.8 and ImmersalSDK 1.9.0
- Faster way of acquiring the camera pixel buffer

## [1.8.0] - 2020-09-11
### Added
- `NRYuvCamera.cs` to obtain the pixel buffer from Nreal Light's RGB camera
- `NRLocalizer.cs` to localize the headset against a map
- `NRAppController.cs`, a simple class for map loading and startup
- `HelloImmersal.unity` sample scene showing how to set up everything
- Known issues: Camera intrinsics might be a bit off, needs more research. Nreal Light's video capture cannot be used at the same time when running `NRLocalizer`.
