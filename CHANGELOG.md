# Changelog

## [0.2.1] - 2026-10-19
This version is compatible with Unity 6000.6

### Added
- Supported simplified clear coat and anisotropy material models in the Advanced PBR shader.
- Supported layered clear coat material model in the Clear Coat shader.
- Implemented ShaderGUI for all material shaders.
- Added Substance Painter integration (see [Documentation~/SubstancePainter/README.md](Documentation~/SubstancePainter/README.md)).

### Changed
- Refactored shader code to improve structure and maintainability.

## [0.2.0] - 2026-07-12

This version is compatible with Unity 6000.5

### Added
- Added Buddy atlas packer to support packing power-of-two textures into power-of-two atlas.
- Added support for cubemap rotation.
- Added support for screen space irradiance when using Unity APV.

### Fixed
- Fixed point light shadow map rendering error in DX12.
- Fixed a black screen error in builds caused by VolumeComponent NullReferenceException, by providing a default volume profile asset to VolumeManager.instance.Initialize().

### Changed
- Changed reflection probe implementation to use an atlas with configurable size.
- Changed punctual light shadow implementation from texture arrays to a texture atlas.
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
- Fixed a reflection probe blending error when an object inside two probes.
- Fixed an issue where the material inspector rendering was incorrect.
- Fixed APV ConstantBuffer leak due to missing Release() call.
- Fixed an issue where UGUI mask component doesn’t work correctly.

### Changed
- Improved HBIL by adding radius & max screen percentage to control the influence range.
- Improved SSAO/GTAO/HBIL by adding the absolute depth threshold parameter, which helps to reduce the halo artifacts.