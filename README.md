# Antique Render Pipeline
Antique Render Pipeline is a Custom Scriptable Render Pipeline (SRP) in Unity, primarily designed for PC (Windows). Current rendering features are sufficient for developing a small‑scale 3D indie game.

The following rendering features are not yet supported compared to URP: Super Resolution, GPU Pipeline (GPU Resident Drawer), Lens Flare, Depth of Field & Motion Blur.

# Supported Rendering Features
▲ = planned rendering feature

| Categories        | Features                           | Description                                                                    |
|:------------------|:-----------------------------------|:-------------------------------------------------------------------------------|
| Render Path       | Forward/Deferred                   |                                                                                |
| Material          | PBR                                |                                                                                |
|                   | ▲ Clear Coat                       |                                                                                |
|                   | ▲ Anisotropic                      |                                                                                |
|                   | ▲ SSSSS                            | screen space subsurface scattering                                             |
|                   | ▲ Cloth                            |                                                                                |
| Direct lighting   | Directional Light                  | only support one directional light, shadow atlas with 4 cascades               |
|                   | Punctual Light                     | up to 256 spot/point lights, with shadows packed into a single atlas           |
|                   | Shadow                             | PCSS/PCF                                                                       |
|                   | Light Culling                      | tiled based light culling, ▲ clustered based light culling                     |
| Indirect lighting | APV                                | integration with unity adaptive probe volume                                   |
|                   | Reflection Probe                   | cubemap blending, parallax correction, cubemap normalization, cubemap rotation |
|                   | Screen Space Ambient Occlusion     | Crytek SSAO, HBAO, GTAO                                                        |
|                   | Near Field Indirect Lighting       | HBIL                                                                           |
|                   | ▲ Screen Space Global Illumination | HZB accelerated SSGI                                                           |
|                   | ▲ Screen Space Reflection          |                                                                                |
| Anti-Aliasing     | FXAA                               |                                                                                |
|                   | TAA                                |                                                                                |
| Post Processing   | Bloom                              |                                                                                |
|                   | Tone Mapping                       | Reinhard, Khronos PBR Neutral, ACES                                            |
|                   | Color Grading                      |                                                                                |
|                   | ▲ Depth of field (DoF)             |                                                                                |