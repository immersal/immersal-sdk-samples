# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
