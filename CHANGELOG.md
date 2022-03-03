# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.16.0] - 2022-03-03
### Changed
- Updated sample to work with Immersal SDK v1.16

## [1.12.0] - 2021-04-06
### Changed
- Updated project to work with Immersal SDK 1.12.0
- Should now also work with Unity 2020.2 and Magic Leap XR Plugin 6+

### Added
- GeoPose localize function

## [1.11.2] - 2021-01-15
### Added
- Native plugin for Lumin
- `MLLocalizer` now has both on-device and on-server localizers

### Changed
- Now works on verified versions of Unity 2020.1.17f1, AR Foundation 3.1.6 and Magic Leap XR Plugin 5.1.2
- Now work with the 'master' branch of Immersal SDK 1.11.2

## [1.11.0] - 2021-01-08
### Changed
- `MLLocalizer` now overrides the `LocalizeServer()` function from `LocalizerBase`. The server map IDs should be set in the Unity Editor.
- Localizing now uses the new async/await system.

## [1.10.0] - 2020-11-11
### Changed
- Minor changes to take advantage of the Immersal SDK 1.10.0

## [1.9.0] - 2020-10-20
### Added
- Samples: Magic Leap localization support, currently using the on-server localizer only
