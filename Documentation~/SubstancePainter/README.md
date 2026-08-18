> **AI-generated document.** This README was produced by an AI assistant.

# AntiqueRenderPipeline → Substance 3D Painter Shaders

Substance 3D Painter surface shaders that reproduce the three **AntiqueRenderPipeline** PBR shading models, so artists can see "how it will look in Unity" without leaving Painter.

| Shader file | Unity shader | Notable features |
| --- | --- | --- |
| `ARP_StandardPBR.glsl` | `Shading Models/Standard PBR` | GGX + height-correlated Smith + renormalised Burley |
| `ARP_AdvancedPBR.glsl` | `Shading Models/Advanced PBR` | + anisotropic GGX, **simplified coat** (Kelemen + lerp) |
| `ARP_ClearCoat.glsl` | `Shading Models/Clear Coat` | + **layered coat**, OpenPBR absorption, coat normal |

> The two coat models are **not** the same: Advanced uses `lerp(standard, Dc*V_Kelemen, F*coat)`, ClearCoat uses a separate layer with absorption. The same `coat` value will not look identical — that is by design.

> These shaders only affect the **viewport preview**. They do not take part in channel baking or texture export.

---

## 1. Install

Copy the three `.glsl` files into Painter's shelf directory (create it if needed), then restart Painter:

```
%USERPROFILE%\Documents\Adobe\Adobe Substance 3D Painter\shelf\shader\
```

Pick `ARP_StandardPBR` / `ARP_AdvancedPBR` / `ARP_ClearCoat` in the **Shader** dropdown (Painter lists shaders by file name).

Version baseline: **Substance 3D Painter 12.2.0** (checked against its bundled shader docs). The API used here is unchanged from 11.1.1, so 11.x builds still work; builds before 11 lack `channel_coat*` / `channel_anisotropy*`.

> **12.x legacy libraries.** Since 12.1.0 Adobe marks `lib-pbr.glsl`, `lib-pbr-aniso.glsl`, `lib-sampler.glsl` and `lib-emissive.glsl` as *legacy* (in favour of `lib-openpbr-*`, `lib-pbr-specular`, `lib-ao`, `lib-shadows`), but they are still shipped and Adobe's own `pbr-metal-rough.glsl` sample still imports `lib-pbr.glsl`. `getAO()` and `getShadowFactor()` now live in `lib-ao.glsl` / `lib-shadows.glsl` and are re-exported through `lib-sampler.glsl`, so the `import` lists above keep working unchanged.

---

## 2. Lighting modes

| Mode | What it does | When to use it |
| --- | --- | --- |
| `AntiqueRenderPipeline Replica` (default) | Computes sun + ambient itself with AntiqueRenderPipeline's BRDF and writes everything to the emissive output | A/B against Unity, tuning baseline |
| `Painter Native` | Only provides albedo / diffuse / specular; Painter lights the surface | Everyday painting |

In Replica mode the sun is controlled by the **Sun Light** group (azimuth / elevation / colour / intensity); ambient comes from Painter's current HDRI (`envSample` / `envIrradiance`).

### 2.1 IBL modes

Painter hands shaders a plain equirect environment with plain downsampled mips — not a prefiltered reflection probe. Replica mode therefore has to rebuild the reflection itself:

| `IBL Mode` | How | Notes |
| --- | --- | --- |
| `Phong Lobe (Unity probe)` (**default**) | Averages N samples from a `cos^n` lobe around the reflection vector, then multiplies by the GGX split-sum `envBRDF` | Mirrors Unity's Phong-prefiltered probes; no blockiness at mid roughness |
| `GGX Importance` | Samples half-vectors from the GGX distribution and integrates D·V·F directly | Physically cleanest; costs N environment taps |

`Replica IBL Samples` (8/16/32/64, default **16**) applies to both Replica modes. In **Painter Native** mode it is ignored: that path uses Painter's own **Specular quality** setting (group `Base Surface`). Painter's **TAA** (Display Settings) smooths the dithering.

- Advanced's `GGX Importance` is the **anisotropic** variant.
- The **coat layer** (Advanced, ClearCoat) always uses `Phong Lobe` — Unity's coat indirect also comes from probe prefiltering, and coat roughness is usually low, so it degenerates to a single sharp tap.
- `GGX Importance` falls back to `Phong Lobe` below `roughness 0.1`, where importance sampling gets noisy and the Phong lobe is already a mirror.

### 2.2 Why the Phong lobe is the default

Unity's reflection probes are convolved with a **Phong** lobe, while the EnvBRDF LUT is generated with **GGX** importance sampling (`BRDFIntegrationLib.hlsl:102`, `PreintegrateSpecular_SmithGGXCorrelated`). The lobe widths disagree, which is why `RoughnessToMipmapLevel` contains this compensation:

```hlsl
roughness = roughness * (1.7 - 0.7 * roughness);   // ImageBasedLightingLib.hlsl:107
```

AntiqueRenderPipeline applies this compensation, so the shader exposes it as a switch that is **on by default**:

- `Roughness Remap r*(1.7-0.7r)` = **on**: roughness is remapped before the Phong exponent is derived.
- off: roughness passes through unchanged (only for matching the raw, un-remapped value).

The lobe itself is rebuilt at runtime; its exponent comes from GGX's alpha:

```
alpha = roughness²        // AntiqueRenderPipeline's alphaRoughness
n     = 2/alpha² − 2      // inverse of alpha = sqrt(2/(n+2))
```

Effect of the remap on the exponent (smaller `n` = wider lobe):

| roughness | remapped | n (off) | n (on) |
| --- | --- | --- | --- |
| 0.1 | 0.163 | 19998 | 2831 |
| 0.3 | 0.447 | 245 | 48 |
| 0.5 | 0.675 | 30 | 7.6 |
| 0.8 | 0.912 | 2.9 | 0.9 |

As `roughness → 0`, `n → ∞` and the lobe degenerates to a mirror: all samples point the same way, which is one sharp mip-0 tap.

> The switch only affects `Phong Lobe` (including the coat layer). `GGX Importance` derives its mip from the sampling pdf, which already matches the GGX lobe — remapping again would double-compensate.

> **Mip range matches Painter**: the upper bound is Painter's `maxLod` (`environment_max_lod`). Unity's "6 mip levels" assumption is not applied — the two environment chains differ in resolution and mip count.

> Replica mode ignores Painter's *studio* lighting; it only reads the environment map.

---

## 3. Channels

Painter stores one texture per channel, so Unity's packed maps have to be split (Photoshop, Substance Designer, or any channel extractor):

| Unity map | Channel | → Painter channel | Fallback when missing |
| --- | --- | --- | --- |
| `_BaseTex` | RGB | **Base Color** | white |
| `_BaseTex` | A | **Opacity** (with Alpha Clip) | 1.0 |
| `_HybridTex` | R | **Roughness** | `Roughness (no map)`, 0.5 |
| `_HybridTex` | G | **Metallic** | `Metallic (no map)`, 0 |
| `_HybridTex` | A | **Ambient Occlusion** | 1.0 |
| `_NormalTex` | RGB | **Normal** | geometric normal |
| `_EmissionTex` | RGB | **Emissive** (built-in channel, `lib-emissive`) | black |
| `_Specular` | — | **no channel** (Unity uses a scalar) | `Specular (Unity _Specular)` param, 0.5 |
| `_AdvancedTex` | R | **Anisotropy Level** | `Level (no map)` |
| `_AdvancedTex` | G | **Anisotropy Angle** | `Rotation 0-1 (no map)` |
| `_AdvancedTex` | B | **Coat Opacity** | `Coat` |
| `_AdvancedTex` | A | **Coat Roughness** | `Coat Roughness` |
| `_CoatTex` | R / G | **Coat Opacity** / **Coat Roughness** | as above |
| `_CoatColorTex` | RGB | **Coat Color** | white |
| `_CoatNormalTex` | RGB | **Coat Normal** | main normal (matches Unity) |

Unpainted areas fall back to the panel parameter using **Unity's** defaults (not Painter's 0.3).

Add the channels you need in **Texture Set Settings → Channels**. If a channel does not exist in your build, the shader simply skips it and uses the parameter.

`normal` follows Painter's standard chain (baked normal + **height→normal** + Normal channel, blended by `normal_blending_mode`), so painted height does affect shading. Do not paint the same detail in both Height and Normal, or it will be applied twice.

---

## 4. Parameter mapping (Unity Inspector ↔ Painter)

| Unity property | Painter parameter | Notes |
| --- | --- | --- |
| `_BaseColor` | Base Color (tint) | multiplied with the Base Color channel |
| `_Specular` | **Specular (Unity _Specular)** | scalar only; the parameterisation follows `Lighting Mode` (see below) |
| `_Roughness` / `_Metallic` | Roughness / Metallic (no map) | used only where the channel is unpainted |
| `_NormalIntensity` | **not exposed** | normal strength is expressed by the Height / Normal channels and the blend mode |
| `_EmissionColor` (HDR) | **Emissive intensity** (Painter built-in) | no tint parameter: the **Emissive** channel carries the colour, Emissive intensity (0–100) drives the HDR strength |
| `_Cutoff` | Alpha Clip + Alpha Cutoff | reads the **Opacity** channel |
| `_Cull` | Double Sided | `state cull_face off` |
| `_Anisotropy` / `_AnisotropyRotation` | Anisotropy group | Unity: `0-1 → 0-π`; Painter's channel is `0-1 → 0-2π`, the shader converts internally — copy the map as-is |
| `_Coat` / `_CoatRoughness` | Coat group | Advanced = simplified model, ClearCoat = layered model |
| `_CoatIOR` | Coat IOR (1–4, default 1.5) | drives `coatF0` and the absorption |
| `_CoatColor` | Coat Color (tint) | multiplied with the Coat Color channel |
| `_CoatNormalIntensity` | **not exposed** | ClearCoat only; the Coat Normal channel is unpacked without scaling |
| — | **IBL Mode / Replica IBL Samples / Roughness Remap** | see §2.1 and §2.2 |
| — | **AO Intensity** (Painter built-in) | the only AO slider; 0.75 by default, 1.0 = Unity passthrough |

### `_Specular` is not Painter's specular level

| Lighting Mode | Parameterisation | s=0.5 | s=1.0 |
| --- | --- | --- | --- |
| `AntiqueRenderPipeline Replica` | `0.16 · s²` (Unity) | 0.04 | 0.16 |
| `Painter Native` | `0.08 · s` (Painter) | 0.04 | 0.08 |

The shader picks the parameterisation automatically from `Lighting Mode` — there is no separate "Specular Model" switch any more. Both agree at the default `s = 0.5` (F0 = 0.04) and diverge elsewhere, so feed the value that belongs to the mode you are validating in: `s = 1` means F0 = 0.16 in Replica but 0.08 in Native.

---

## 5. Debug view

`Debug View` outputs intermediate terms as grayscale (independent of the lighting mode, handy for screenshots against Unity):

`D (GGX)` / `V (Smith)` / `F (Schlick)` / `Diffuse` / `Specular` / `World Normal` / `R / M / AO`, plus `Coat Absorption` in ClearCoat.

`Specular` is the microfacet term **including the N·L cosine** (`D·V·F·N·L`): without it the Smith visibility `G/(4·N·L·N·V)` diverges at the mesh silhouette and shows a white rim that gets worse with roughness. `V (Smith)` is still the raw visibility term, so it does blow up at grazing by definition.

Pin roughness/metallic for a pixel, then compare the same angle in Unity (frame debugger or a numeric table) and Painter. That separates "wrong formula" from "different lighting".

---

## 6. Known differences (not bugs)

1. Unity's pre-integrated LUT is replaced by an analytic fit (Karis), typically within ~2% at grazing angles.
2. **`Painter Native` is a different renderer**: it uses Painter's own importance-sampled IBL and Smith-Schlick geometry term, so it must not be used for validation.
3. **AO is modulated by Painter's built-in `AO Intensity`** (group `Base Surface`, default 0.75). It is the *only* AO slider — the shaders no longer add one of their own. Unity passes AO through, so set it to 1.0 for a like-for-like A/B.
4. **No tone mapping**: the shader outputs linear HDR and leaves display mapping to Painter. If Replica highlights look blown out, lower `Exposure` or `Sun Light → Intensity`.
5. **Diffuse IBL uses an analytic fit** of Unity's `_EnvBRDFLut.b` instead of the LUT: `arpDiffuseLUTB()` reproduces `PreintegrateDiffuse_RenormalizedBurley_CosineVersion` within ±0.005 (≈0.5 % of the diffuse term).
6. **Energy compensation is clamped.** Unity scales the direct specular by `1 + F0·(1/A − 1)·0.5` with `A` from its baked LUT. This shader uses the Karis analytic fit instead, which returns `A ≈ 0` (or negative) at grazing — `1/A` then explodes. On a smooth **metal** (`F0 = albedo`, so 12–25× larger than a dielectric's 0.04) that showed up as a **white rim on the sun-facing silhouette**, up to ~5000× at `roughness 0`. `arpEnergyCompensation()` therefore clamps `A` to ≥ 0.2 and the factor to `[1, 1.5]`. It only changes grazing angles at low roughness; mid roughness and normal incidence are untouched. Do not remove the clamp.

---

## 7. Troubleshooting

| Symptom | Fix |
| --- | --- |
| `No matching uniform was found` + shader creation fails | a `//:` metadata token leaked into prose. Painter parses `//:` anywhere in a line, so never write it mid-sentence |
| `module lib-xxx.glsl; not found`, followed by `undeclared identifier` errors | the `import` line ends with a semicolon. Painter uses the raw token as the module name — write `import lib-x.glsl` |
| `line continuation used at end of comment` | a `//` comment ends with a backslash (e.g. a Windows path); remove it or use `/` |
| Imports stop working after saving | the file was saved as UTF-8 **with BOM**; save without BOM |
| Blotchy / blocky environment reflections | make sure `IBL Mode` is `Phong Lobe` (or raise `Replica IBL Samples` for `GGX Importance`); enable Painter's TAA; use a higher-resolution HDRI |
| Noisy / flickering reflections | raise `Replica IBL Samples` to 32/64 and enable TAA |
| `Replica IBL Samples` seems to do nothing | you are in `Painter Native` mode — that path uses Painter's own **Specular quality** instead |
| Too dark / too bright overall | `Environment → Diffuse Scale / Specular Scale`; in Replica mode also `Sun Light → Intensity` |
| Reflections look too soft after enabling the remap | expected: `r*(1.7-0.7r)` widens the lobe; turn `Roughness Remap` off if you want to match the un-remapped value |
| Normals look flipped | check the normal Y setting on both sides (Painter texture-set settings, Unity import settings) |
| Coat has no effect | the **Coat Opacity** channel is empty; raise the `Coat` parameter |
| Anisotropy direction is wrong | the `Anisotropy Angle` channel is stored in Painter's `0-1 → 0-2π` range; the shader converts to Unity's `0-π`, so copy the map unchanged |
| Channel is black | the channel exists but was never painted; the shader falls back to the parameter, which is expected |
| `Double Sided` does nothing, or a bright rim appears at the silhouette | the uniform driving `//: state cull_face off {"enable": …}` must be declared **`uniform_specialization bool`**, not `uniform bool`. With a plain uniform the state is applied unconditionally, so the mesh is always rendered double sided and the back faces leak at the silhouette |
| Bright rim only on the sun-facing silhouette, mostly with `Metallic = 1` | energy-compensation blow-up at grazing — see §6 item 6; the clamp is the fix, keep it |

---

## 8. Files and maintenance

```
SubstancePainter/
  ARP_StandardPBR.glsl
  ARP_AdvancedPBR.glsl
  ARP_ClearCoat.glsl
  README.md
```

- The BRDF block is **duplicated** in all three files on purpose: Painter's support for user `import`s of custom files is not documented, so each shader is self-contained. When changing a shared helper, change all three.
- Every file's header lists the Unity sources it was ported from (with `.hlsl` file names) so later Unity changes can be traced.
- The files are **UTF-8 without BOM**, CRLF line endings (README is LF). Do not rewrite them with tooling that alters the encoding: a BOM breaks the first line for Painter's import parser.
- Internal identifiers use the `arp_` / `ARP_` prefix (`ARPMaterial`, `arpDiffuseLUTB`, `arp_ibl_samples`, ...). The same prefix appears in `input.arp_*` expressions inside `visible` / `enable` metadata — rename both together.
- `arpMaxMip()` reads `maxLod`, which 12.x keeps as a deprecated alias of `environment_max_lod` (both bound to the same engine parameter). If a future version drops it, switch that one call to `environment_max_lod`.
- `arpDiffuseLUTB()` is **fitted, not baked** (see the comment block above it). If your `_EnvBRDFLut.b` was baked with the non-cosine `PreintegrateDiffuse_RenormalizedBurley`, set `ARP_DIFFUSE_LUT_UNIFORM` to `1` in all three shaders.
