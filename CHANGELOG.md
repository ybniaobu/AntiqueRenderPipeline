# Changelog

## [0.2.0] - 2026-07-12

This version is compatible with Unity 6000.5

### Added
- Added Buddy atlas packer to support packing power-of-two textures into power-of-two atlas.
- Added support for cubemap rotation.
- Added support for screen space irradiance when using Unity APV.

### Fixed
- Fixed point light shadow map rendering error in DX12.
- Fixed black screen in builds due to VolumeComponent NullReferenceException by providing a default volume profile asset to VolumeManager.instance.Initialize().

### Changed
- Changed reflection probe implementation to use an atlas with configurable size.
- Changed punctual light shadow implementation from texture array to texture atlas.
- Applied normal weights in the upsampling stage to mitigate edge artifacts.

## [0.1.4] - 2026-04-28

This version is compatible with Unity 6000.4

### Added
- Added per-pixel reflection probe, including parallax correction, normalization & blending.
- Added render pipeline asset editor GUI.
- Added light component editor GUI.
- Added camera component editor GUI.
- Added reflection probe component editor GUI code.

### Fixed
- Fixed a reflection probe rendering error when bounding box center doesn’t match the cubemap position.
- Fixed a reflection probe blending error when object inside two probes.
- Fixed an issue where the material inspector rendering was incorrect.
- Fixed APV ConstantBuffer leak due to missing Release() call.
- Fixed an issue where UGUI mask component doesn’t work correctly.

### Changed
- Improved HBIL by adding radius & max screen percentage to control the influence range.
- Improved SSAO/GTAO/HBIL by adding the absolute depth threshold parameter, which could help to reduce the halo artifacts.