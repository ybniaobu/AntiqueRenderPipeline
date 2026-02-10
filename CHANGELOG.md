# Changelog

## [0.1.4] - 2026-04-28

This version is compatible with Unity 6000.4

### Changed
- Improved HBIL by adding new parameters to control the influence range.
- Improved SSAO/GTAO/HBIL by adding the absolute depth threshold parameter, which could help to reduce the halo artifacts.

## [0.1.3] - 2026-03-28

This version is compatible with Unity 6000.4

### Changed
- Optimized render pipeline asset editor GUI code.
- Optimized camera component editor GUI code.
- Optimized light component editor GUI code.
- Optimized reflection probe component editor GUI code.

### Fixed
- Fixed a reflection probe rendering error when bounding box center doesn’t match the cubemap position.
- Fixed a reflection probe blending error when object inside two probes.
- Fixed an issue where the material inspector rendering was incorrect.
- Fixed APV ConstantBuffer leak due to missing Release() call.
- Fixed an issue where UGUI mask component doesn’t work correctly.

## [0.1.2] - 2026-02-12

This version is compatible with Unity 6000.3

### Changed
- Improved camera component editor GUI.

## [0.1.1] - 2026-02-11

This version is compatible with Unity 6000.3

### Changed
- Improved light component editor GUI.

## [0.1.0] - 2026-02-10

This version is compatible with Unity 6000.3

### Added
- Added per-pixel reflection probe, including parallax correction, normalization & blending.
- Added render pipeline asset editor GUI.