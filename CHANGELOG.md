# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.8.0] - 2020-09-11
### Added
- `NRYuvCamera.cs` to obtain the pixel buffer from Nreal Light's RGB camera
- `NRLocalizer.cs` to localize the headset against a map
- `NRAppController.cs`, a simple class for map loading and startup
- `HelloImmersal.unity` sample scene showing how to set up everything
- Known issues: Camera intrinsics might be a bit off, needs more research. Nreal Light's video capture cannot be used at the same time when running `NRLocalizer`.
