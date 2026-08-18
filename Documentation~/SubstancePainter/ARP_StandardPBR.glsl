//==============================================================================================
//  AntiqueRenderPipeline :: Standard PBR  ->  Substance 3D Painter 12.2.0  (surface shader)
//----------------------------------------------------------------------------------------------
//  Source (Unity):
//    MaterialModels/StandardPBR/StandardPBRInput.hlsl  / StandardPBR.shader
//    MaterialModels/StandardPBR/StandardPBRForwardPass.hlsl
//    ShaderLibrary/PBR/BRDFTermsLib.hlsl      (D / V / F / Burley ported line by line)
//    ShaderLibrary/PBR/BRDFModelLib.hlsl      (StandardBRDF)
//    ShaderLibrary/PBR/RenderingEquationLib.hlsl(direct + indirect assembly, energyCompensation)
//    ShaderLibrary/PBR/AmbientOcclusionLib.hlsl (ComputeSpecularAO)
//
//  Install: copy this file into Painter's shelf directory
//    <Documents>/Adobe/Adobe Substance 3D Painter/shelf/shader
//  Then pick "ARP_StandardPBR" in Painter's Shader list.
//
//  Design notes: see README.md
//    * BRDF terms are local (not lib-pbr's pbrComputeSpecular) so numbers match Unity;
//    * lighting_mode = 0 (Replica): own sun + ambient, everything routed through emissive;
//    * lighting_mode = 1 (Native): albedo / diffuse / specular handed to Painter.
//==============================================================================================

// import must NOT end with ';': Painter uses the raw token as the module name.
import lib-defines.glsl
import lib-sparse.glsl
import lib-sampler.glsl
import lib-normal.glsl
import lib-vectors.glsl
import lib-env.glsl
import lib-pbr.glsl
import lib-emissive.glsl
import lib-random.glsl
import lib-utils.glsl

//==============================================================================================
//  1. Channel bindings
//     Note: normal / emissive / ao / shadow come from the built-in libs (normal_texture, emissive_tex,
//           getAO(), getShadowFactor()); only project-specific channels are bound here.
//==============================================================================================

//: param auto channel_basecolor
uniform SamplerSparse arp_basecolor_tex;

//: param auto channel_roughness
uniform SamplerSparse arp_roughness_tex;

//: param auto channel_metallic
uniform SamplerSparse arp_metallic_tex;

//: param auto channel_opacity
uniform SamplerSparse arp_opacity_tex;

//==============================================================================================
//  2. Parameters (names and defaults follow Unity's StandardPBR.shader Properties)
//==============================================================================================

//: param custom {
//:   "group": "Shading", "label": "Lighting Mode", "widget": "combobox", "default": 0,
//:   "values": { "AntiqueRenderPipeline Replica": 0, "Painter Native": 1 },
//:   "description": "<html><head/><body><p><b>AntiqueRenderPipeline Replica</b>: own sun and ambient lighting, aiming to match Unity AntiqueRenderPipeline (use this for A/B).<br/><b>Painter Native</b>: only albedo/diffuse/specular are provided; Painter lights the material.</p></body></html>"
//: }
uniform int arp_lighting_mode;

//: param custom {
//:   "group": "Shading", "label": "Debug View", "widget": "combobox", "default": 0,
//:   "values": { "Off": 0, "D (GGX)": 1, "V (Smith)": 2, "F (Schlick)": 3, "Diffuse": 4, "Specular": 5, "World Normal": 6, "R / M / AO": 7 }
//: }
uniform int arp_debug_mode;

//----------------------------------------------------------------------------------------------
//  2.1 Base Surface
//----------------------------------------------------------------------------------------------

//: param custom { "group": "Base Surface", "label": "Base Color (tint)", "widget": "color", "default": [1.0, 1.0, 1.0, 1.0] }
uniform vec4 arp_base_color;                                  // Unity _BaseColor

//: param custom { "group": "Base Surface", "label": "Specular (Unity _Specular)", "min": 0.0, "max": 1.0, "default": 0.5 }
uniform float arp_specular_level;                             // Unity _Specular (scalar, no texture)

//: param custom { "group": "Base Surface", "label": "Roughness (no map)", "min": 0.0, "max": 1.0, "default": 0.5 }
uniform float arp_roughness_value;                            // Unity _Roughness

//: param custom { "group": "Base Surface", "label": "Metallic (no map)", "min": 0.0, "max": 1.0, "default": 0.0 }
uniform float arp_metallic_value;                             // Unity _Metallic

//----------------------------------------------------------------------------------------------
//  2.2 Sun Light (Replica mode only)
//----------------------------------------------------------------------------------------------

//: param custom { "group": "Sun Light", "label": "Azimuth (deg)", "min": 0.0, "max": 360.0, "default": 90.0 }
uniform float arp_sun_azimuth;

//: param custom { "group": "Sun Light", "label": "Elevation (deg)", "min": -90.0, "max": 90.0, "default": 45.0 }
uniform float arp_sun_elevation;

//: param custom { "group": "Sun Light", "label": "Color", "widget": "color", "default": [1.0, 1.0, 1.0, 1.0] }
uniform vec4 arp_sun_color;

//: param custom { "group": "Sun Light", "label": "Intensity", "min": 0.0, "max": 20.0, "default": 1.0 }
uniform float arp_sun_intensity;

//: param custom { "group": "Sun Light", "label": "Use Shadow Mask", "default": true }
uniform bool arp_use_shadow_mask;

//----------------------------------------------------------------------------------------------
//  2.3 Environment / Exposure
//----------------------------------------------------------------------------------------------

//: param custom { "group": "Environment", "label": "Diffuse Scale", "min": 0.0, "max": 4.0, "default": 1.0 }
uniform float arp_env_diffuse_scale;

//: param custom { "group": "Environment", "label": "Specular Scale", "min": 0.0, "max": 4.0, "default": 1.0 }
uniform float arp_env_specular_scale;

//: param custom {
//:   "group": "Environment", "label": "IBL Mode", "widget": "combobox", "default": 0,
//:   "values": { "Phong Lobe (Unity probe)": 0, "GGX Importance": 1 },
//:   "description": "<html><head/><body><p><b>Phong Lobe</b> (default): multi-sample a Phong lobe and multiply by the GGX split-sum, matching Unity's probe prefiltering.<br/><b>GGX Importance</b>: GGX importance sampling with D*V*F inside the integral.</p></body></html>"
//: }
uniform int arp_ibl_mode;

//: param custom {
//:   "group": "Environment", "label": "Replica IBL Samples", "widget": "combobox", "default": 16,
//:   "values": { "8": 8, "16": 16, "32": 32, "64": 64 },
//:   "description": "Samples per pixel used by Replica mode only. Painter Native mode ignores this and uses Painter's own Specular quality setting."
//: }
uniform int arp_ibl_samples;

//: param custom {
//:   "group": "Environment", "label": "Roughness Remap  r*(1.7-0.7r)", "default": true,
//:   "description": "Unity's PerceptualRoughnessToMipmapLevel compensation r*(1.7-0.7r). Phong Lobe mode only."
//: }
uniform bool arp_mip_remap;

//: param custom { "group": "Exposure", "label": "Exposure", "min": -5.0, "max": 5.0, "default": 0.0 }
uniform float arp_exposure;

//----------------------------------------------------------------------------------------------
//  2.4 Geometry
//----------------------------------------------------------------------------------------------

//: param custom { "group": "Geometry", "label": "Double Sided", "default": false }
uniform_specialization bool arp_double_sided;

//: param custom { "group": "Geometry", "label": "Alpha Clip", "default": false }
uniform bool arp_alpha_clip;

//: param custom {
//:   "group": "Geometry", "label": "Alpha Cutoff", "min": 0.0, "max": 1.0, "default": 0.5,
//:   "visible": "input.arp_alpha_clip"
//: }
uniform float arp_alpha_cutoff;

//: state cull_face off {"enable":"input.arp_double_sided"}

//==============================================================================================
//  3. Utilities
//==============================================================================================

#define ARP_SATURATE(x) clamp((x), 0.0, 1.0)

const float ARP_PI     = 3.14159265359;
const float ARP_INV_PI = 0.31830988618;

// Environment mip upper bound: Painter's maxLod (lib-pbr's environment_max_lod).
float arpMaxMip()
{
    return max(maxLod, 0.0);
}

// sparse channel read: same "value + coverage" semantics as lib-sampler, but Unity's defaults
// (SP's DEFAULT_ROUGHNESS is 0.3, Unity's is 0.5 - do not confuse them).
float arpGrey(SamplerSparse tex, SparseCoord coord, float valueWhenUnset)
{
    vec4 v = textureSparse(tex, coord);
    return v.r + valueWhenUnset * (1.0 - v.g);
}

vec3 arpColor(SamplerSparse tex, SparseCoord coord, vec3 valueWhenUnset)
{
    vec4 v = textureSparse(tex, coord);
    return v.rgb + valueWhenUnset * (1.0 - v.a);
}

//==============================================================================================
//  4. BRDF terms - ported line by line from ShaderLibrary/PBR/BRDFTermsLib.hlsl
//==============================================================================================

// F_Schlick(f90, f0, VoH)
float arpFSchlick(float f90, float f0, float voh)
{
    float m  = 1.0 - voh;
    float m2 = m * m;
    return f0 + (f90 - f0) * m2 * m2 * m;
}

// F_Schlick(float3 f90, float3 f0, float VoH)
vec3 arpFSchlick3(float f90, vec3 f0, float voh)
{
    float m  = 1.0 - voh;
    float m2 = m * m;
    return f0 + (vec3(f90) - f0) * m2 * m2 * m;
}

// D_GGX (argument is already alphaRoughness = roughness^2)
float arpDGGX(float ndh, float alphaRoughness)
{
    float a2 = alphaRoughness * alphaRoughness;
    float d  = 1.0 + ndh * ndh * (a2 - 1.0);
    return a2 / (ARP_PI * d * d);
}

// V_SmithGGXCorrelated
float arpVSmithGGXCorrelated(float ndv, float ndl, float alphaRoughness)
{
    float a2 = alphaRoughness * alphaRoughness;
    float vL = ndv * sqrt(ndl * ndl * (1.0 - a2) + a2);
    float vV = ndl * sqrt(ndv * ndv * (1.0 - a2) + a2);
    return 0.5 / max(vL + vV, 1e-8);
}

// Fd_RenormalizedBurley_Disney (returns a scalar; albedo is applied by the caller)
float arpFdRenormalizedBurley(float ndv, float ndl, float loh, float roughness)
{
    float energyBias   = mix(0.0, 0.5, roughness);
    float energyFactor = mix(1.0, 1.0 / 1.51, roughness);
    float fd90         = energyBias + 2.0 * loh * loh * roughness;
    float fdL          = arpFSchlick(fd90, 1.0, ndl);
    float fdV          = arpFSchlick(fd90, 1.0, ndv);
    return fdV * fdL * energyFactor * ARP_INV_PI;
}

//==============================================================================================
//  5. Environment approximation (replaces Unity's EnvBRDF LUT / reflection probe)
//==============================================================================================

// Unity uses a pre-integrated LUT (A, B); Painter has none, so use Karis' analytic fit (same meaning):
//   A = envBRDF.x, B = envBRDF.y
vec2 arpEnvBRDFApprox(float roughness, float ndv)
{
    const vec4 c0 = vec4(-1.0, -0.0275, -0.572,  0.022);
    const vec4 c1 = vec4( 1.0,  0.0425,  1.040, -0.040);
    vec4  r     = roughness * c0 + c1;
    float a004  = min(r.x * r.x, exp2(-9.28 * ndv)) * r.x + r.y;
    return vec2(-1.04, 1.04) * a004 + r.zw;
}

// Unity: energyCompensation = 1 + F0 * (1 / envBRDF.x - 1) * 0.5
vec3 arpEnergyCompensation(vec3 f0, float envA)
{
    // The Karis analytic fit degrades at grazing: its A term can reach ~0 (or go negative) where
    // the true split-sum integral stays finite, so 1/A explodes and the direct sun specular turns
    // into a white rim on smooth metals. Energy compensation is only a small correction, so clamp
    // the LUT term and the factor itself.
    float a = clamp(envA, 0.2, 1.0);
    return clamp(vec3(1.0) + f0 * (1.0 / a - 1.0) * 0.5, vec3(1.0), vec3(1.5));
}

//----------------------------------------------------------------------------------------------
//  Diffuse split-sum term (Unity: the b channel of _EnvBRDFLut)
//  That LUT is baked by BRDFIntegrationLib.hlsl:76 PreintegrateDiffuse_RenormalizedBurley_CosineVersion:
//      b = E_cos[ F_Schlick(fd90, 1, NoL) * F_Schlick(fd90, 1, NoV) * energyFactor ]
//      fd90 = 0.5 * r + 2 * LoH^2 * r,     energyFactor = lerp(1, 1 / 1.51, r)
//  Painter has no such LUT, so the integral is replaced here by a least-squares fit:
//      b = energyFactor(r) * [ A0*(1 - q^5) + r*(A1 + A2*q^5 + A3*q^4) + r^2*(A4*q^5 + A5*q^4) ]
//      q = 1 - NoV
//  Max abs error 0.0044 over roughness x NoV in [0,1]^2 (b itself spans 0.005 .. 1.03).
//  Set ARP_DIFFUSE_LUT_UNIFORM to 1 when the LUT was baked with the non-cosine
//  PreintegrateDiffuse_RenormalizedBurley (BRDFIntegrationLib.hlsl:50) instead: same shape,
//  other coefficients, max abs error 0.0045.
//----------------------------------------------------------------------------------------------

#define ARP_DIFFUSE_LUT_UNIFORM 0

float arpDiffuseLUTB(float roughness, float nov)
{
#if ARP_DIFFUSE_LUT_UNIFORM
    const vec3 arpLutA = vec3(0.83334924, 0.26695169, 0.74303403);
    const vec3 arpLutB = vec3(0.24570878, 0.41266354, 0.04414603);
#else
    const vec3 arpLutA = vec3(0.95239656, 0.07823402, 0.90376042);
    const vec3 arpLutB = vec3(0.45284062, 0.10431753, 0.02529342);
#endif

    float r  = clamp(roughness, 0.0, 1.0);
    float q  = 1.0 - clamp(nov, 0.0, 1.0);
    float q2 = q * q;
    float q4 = q2 * q2;
    float q5 = q4 * q;
    float energyFactor = 1.0 + (1.0 / 1.51 - 1.0) * r;

    return energyFactor * (arpLutA.x * (1.0 - q5)
                         + r * (arpLutA.y + arpLutA.z * q5 + arpLutB.x * q4)
                         + r * r * (arpLutB.y * q5 + arpLutB.z * q4));
}

// ComputeSpecularAO (AmbientOcclusionLib.hlsl)
float arpSpecularAO(float ndv, float ao, float alphaRoughness)
{
    return ARP_SATURATE(pow(abs(ndv + ao), exp2(-16.0 * alphaRoughness - 1.0)) - 1.0 + ao);
}

//----------------------------------------------------------------------------------------------
//  GGX importance sampling
//  Painter's environment mips are plain downsampled (no GGX prefilter), so a single tap can hit a
//  mip; instead N directions are drawn from the GGX distribution, each with its own pdf-derived mip.
//----------------------------------------------------------------------------------------------

// build an orthonormal frame around N
void arpBuildFrame(vec3 N, out vec3 T, out vec3 B)
{
    vec3 helper = (abs(N.y) < 0.999) ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    T = normalize(cross(helper, N));
    B = cross(N, T);
}

// sample a half-vector from the GGX NDF (p_H = D * NoH)
vec3 arpImportanceSampleGGX(vec2 Xi, vec3 T, vec3 B, vec3 N, float roughness)
{
    float a    = roughness * roughness;
    float cosT = sqrt((1.0 - Xi.y) / max(1.0 + (a * a - 1.0) * Xi.y, 1e-6));
    float sinT = sqrt(max(0.0, 1.0 - cosT * cosT));
    float phi  = 2.0 * ARP_PI * Xi.x;
    return normalize(T * (sinT * cos(phi)) + B * (sinT * sin(phi)) + N * cosT);
}

// derive the environment mip from the L-space pdf (same shape as lib-pbr's computeLOD, our sample count)
float arpEnvLOD(vec3 dirEnv, float pdf, float samples)
{
    float distortion = max(sqrt(max(0.0, 1.0 - dirEnv.y * dirEnv.y)), 1e-2);
    float lod = (arpMaxMip() - 1.5) - 0.5 * log2(max(samples * pdf * distortion, 1e-6));
    return clamp(lod, 0.0, arpMaxMip());
}

//----------------------------------------------------------------------------------------------
//  Phong lobe sampling (default mode)
//  Unity's reflection probe cubemaps are convolved with a Phong lobe; rebuild that filter here:
//  average N directions from a cos^n lobe, then multiply by the GGX split-sum envBRDF.
//  GGX alpha to Phong exponent: alpha = sqrt(2/(n+2))  =>  n = 2/alpha^2 - 2
//----------------------------------------------------------------------------------------------

float arpPhongExponent(float roughness)
{
    float a = max(roughness * roughness, 1e-4);
    return max(2.0 / (a * a) - 2.0, 1e-4);
}

// Unity's perceptualRoughness -> mip compensation: r * (1.7 - 0.7 * r) (RoughnessToMipmapLevel)
float arpRoughnessRemap(float roughness)
{
    float r = clamp(roughness, 0.0, 1.0);
    if (arp_mip_remap) {
        r = r * (1.7 - 0.7 * r);
    }
    return r;
}

// sample around R from a cos^n distribution
vec3 arpImportanceSamplePhong(vec2 Xi, vec3 T, vec3 B, vec3 R, float n)
{
    float cosT = pow(max(Xi.y, 1e-6), 1.0 / (n + 1.0));
    float sinT = sqrt(max(0.0, 1.0 - cosT * cosT));
    float phi  = 2.0 * ARP_PI * Xi.x;
    return normalize(T * (sinT * cos(phi)) + B * (sinT * sin(phi)) + R * cosT);
}

vec3 arpEnvSpecularPhong(vec3 R, float roughness, int samples)
{
    float n = arpPhongExponent(arpRoughnessRemap(roughness));

    vec3 T;
    vec3 B;
    arpBuildFrame(R, T, B);

    float count = float(max(samples, 1));
    vec3  sum   = vec3(0.0);

    // uniform weights (the lobe is the filter); constant mip from the lobe-centre density, to avoid noise
    float pdfCenter = (n + 1.0) / (2.0 * ARP_PI);

    for (int i = 0; i < samples; ++i) {
        vec2 Xi = fibonacci2DDitheredTemporal(i, samples);
        vec3 L  = arpImportanceSamplePhong(Xi, T, B, R, n);
        vec3  dirEnv = worldToEnvSpace(L);
        sum += envSample(dirEnv, arpEnvLOD(dirEnv, pdfCenter, count));
    }

    return sum / count * arp_env_specular_scale;
}

//==============================================================================================
//  6. Normal / light direction
//==============================================================================================

vec3 arpSunDirection()
{
    float az = radians(arp_sun_azimuth);
    float el = radians(arp_sun_elevation);
    float ce = cos(el);
    return normalize(vec3(ce * sin(az), sin(el), ce * cos(az)));
}

float arpShadowFactor()
{
    return arp_use_shadow_mask ? getShadowFactor() : 1.0;
}

//==============================================================================================
//  7. Material parameters
//==============================================================================================

struct ARPMaterial
{
    vec3  albedo;
    vec3  emission;
    vec3  F0;
    vec3  N;
    vec3  V;
    float F90;
    float roughness;
    float metallic;
    float ao;
    float NoV;
    float opacity;
};

ARPMaterial arpBuildMaterial(SparseCoord coord, LocalVectors vectors)
{
    ARPMaterial m;

    vec3 baseColor = arpColor(arp_basecolor_tex, coord, vec3(1.0)) * arp_base_color.rgb;

    m.albedo   = baseColor;
    m.opacity  = arpGrey(arp_opacity_tex, coord, 1.0) * arp_base_color.a;

    // Emissive uses Painter's built-in Emissive channel: lib-emissive already declares
    // the SamplerSparse (bound to channel_emissive) and emissive_intensity,
    // so it is not re-bound here (one auto channel must not be bound twice).
    m.emission = pbrComputeEmissive(emissive_tex, coord);

    m.roughness = clamp(arpGrey(arp_roughness_tex, coord, arp_roughness_value), 0.0, 1.0);
    m.metallic  = clamp(arpGrey(arp_metallic_tex,  coord, arp_metallic_value),  0.0, 1.0);

    // ---- F0 / F90 ----
    // The specular parameterisation follows the lighting mode:
    //   AntiqueRenderPipeline Replica -> Unity's 0.16 * s^2 (see the setup in *ForwardPass.hlsl)
    //   Painter Native                -> Painter's 0.08 * s (lib-sampler's generateSpecularColor)
    float specLevel = clamp(arp_specular_level, 0.0, 1.0);
    float dielectricF0 = (arp_lighting_mode == 1) ? (0.08 * specLevel)
                                                  : (0.16 * specLevel * specLevel);

    m.F0  = mix(vec3(dielectricF0), m.albedo, m.metallic);
    m.F90 = ARP_SATURATE(dot(m.F0, vec3(50.0 * 0.3333)));

    m.N = vectors.normal;
    m.V = vectors.eye;

    // avoid dark speckles (same as Unity)
    m.NoV = ARP_SATURATE(dot(m.N, m.V)) + 1e-3;

    // lib-sampler's getAO() already applies Painter's own "AO Intensity" (is_full_strength = false);
    // it stays the only AO slider, so do not modulate it a second time here.
    m.ao = getAO(coord, true, false);

    return m;
}

//==============================================================================================
//  8. Shading (Replica mode)
//==============================================================================================

// StandardBRDF (BRDFModelLib.hlsl)
vec3 arpStandardBRDF(ARPMaterial m, vec3 L, vec3 energyComp)
{
    vec3  H   = normalize(L + m.V);
    float NoL = ARP_SATURATE(dot(m.N, L));
    float NoH = ARP_SATURATE(dot(m.N, H));
    float LoH = ARP_SATURATE(dot(L, H));
    float VoH = ARP_SATURATE(dot(m.V, H));

    float roughness = clamp(m.roughness, 0.05, 1.0);     // keep a tiny specular lobe
    float alphaR    = roughness * roughness;

    vec3  diffuse = arpFdRenormalizedBurley(m.NoV, NoL, LoH, roughness) * m.albedo;
    float D       = arpDGGX(NoH, alphaR);
    float V       = arpVSmithGGXCorrelated(m.NoV, NoL, alphaR);
    vec3  F       = arpFSchlick3(m.F90, m.F0, VoH);

    return (diffuse * (1.0 - m.metallic) + (D * V) * F * energyComp) * NoL;
}

vec3 arpIndirectDiffuse(ARPMaterial m, float diffuseLUT)
{
    // Unity: irradiance * albedo * envBRDF.b * (1 - metallic) * ao
    return envIrradiance(m.N) * m.albedo * (1.0 - m.metallic) * m.ao * diffuseLUT * arp_env_diffuse_scale;
}

// Environment specular: GGX importance sampling (BRDF terms match AntiqueRenderPipeline)
vec3 arpEnvSpecularGGX(ARPMaterial m, int samples)
{
    float roughness = clamp(m.roughness, 0.05, 1.0);
    float alphaR    = roughness * roughness;

    vec3 T;
    vec3 B;
    arpBuildFrame(m.N, T, B);

    float n        = float(max(samples, 1));
    vec3  radiance = vec3(0.0);

    for (int i = 0; i < samples; ++i) {
        vec2 Xi = fibonacci2DDitheredTemporal(i, samples);
        vec3 H  = arpImportanceSampleGGX(Xi, T, B, m.N, roughness);
        vec3 L  = reflect(-m.V, H);

        float NoL = dot(m.N, L);
        if (NoL <= 0.0) {
            continue;
        }
        NoL = max(NoL, 1e-4);

        float NoH = max(dot(m.N, H), 1e-4);
        float VoH = max(dot(m.V, H), 1e-4);

        float D = arpDGGX(NoH, alphaR);
        float V = arpVSmithGGXCorrelated(m.NoV, NoL, alphaR);
        vec3  F = arpFSchlick3(m.F90, m.F0, VoH);

        vec3  dirEnv = worldToEnvSpace(L);
        float pdfL   = max(D * NoH / (4.0 * VoH), 1e-5);
        vec3  Li     = envSample(dirEnv, arpEnvLOD(dirEnv, pdfL, n));

        // weight = f*NoL/(4*VoH*p_H) with p_H = D*NoH, so D cancels
        radiance += Li * F * (4.0 * V * NoL * VoH / NoH);
    }

    return radiance / n * arp_env_specular_scale;
}

vec3 arpIndirectSpecular(ARPMaterial m, vec2 envBRDF)
{
    float rough  = clamp(m.roughness, 0.0, 1.0);
    float specAO = arpSpecularAO(m.NoV, m.ao, rough * rough);

    // GGX importance sampling: the BRDF is already in the integral, so no envBRDF A/B multiply
    // (importance sampling is noisy at low roughness; fall back to the Phong lobe, which degenerates to a mirror tap)
    if (arp_ibl_mode == 1 && rough >= 0.1) {
        return arpEnvSpecularGGX(m, arp_ibl_samples) * specAO;
    }

    // Phong lobe: rebuild Unity's Phong prefilter, then apply the GGX split-sum
    vec3 pre     = arpEnvSpecularPhong(reflect(-m.V, m.N), rough, arp_ibl_samples);
    vec3 envSpec = pre * (vec3(envBRDF.x) * m.F0 + (vec3(m.F90) - m.F0) * envBRDF.y);
    return envSpec * specAO;
}

vec3 arpShadeReplica(ARPMaterial m)
{
    vec2 envBRDF     = arpEnvBRDFApprox(clamp(m.roughness, 0.0, 1.0), m.NoV);
    vec3 energyComp  = arpEnergyCompensation(m.F0, envBRDF.x);

    vec3 color = m.emission;
    float lutB = arpDiffuseLUTB(clamp(m.roughness, 0.0, 1.0), m.NoV);   // Unity's _EnvBRDFLut.b
    color += arpIndirectDiffuse(m, lutB) + arpIndirectSpecular(m, envBRDF);

    vec3 L             = arpSunDirection();
    vec3 sunIrradiance = arp_sun_color.rgb * arp_sun_intensity * arpShadowFactor();
    color += sunIrradiance * arpStandardBRDF(m, L, energyComp);

    return color;
}

//==============================================================================================
//  9. Output (Native mode / debug)
//==============================================================================================

void arpOutputNative(ARPMaterial m, LocalVectors vectors, float exposureScale)
{
    float shadow = arpShadowFactor();

    albedoOutput(m.albedo * (1.0 - m.metallic));
    diffuseShadingOutput(vec3(m.ao * shadow) * envIrradiance(m.N) * arp_env_diffuse_scale * exposureScale);
    specularShadingOutput(pbrComputeSpecular(vectors,
                                             m.F0,
                                             vec3(m.F90),
                                             clamp(m.roughness, 0.05, 1.0),
                                             m.ao,
                                             0.0) * arp_env_specular_scale * exposureScale);
    emissiveColorOutput(m.emission * exposureScale);
}

void arpOutputDebug(ARPMaterial m, int mode)
{
    // direct-light terms (default sun direction, for numeric comparison against Unity)
    vec3  L   = arpSunDirection();
    vec3  H   = normalize(L + m.V);
    float NoL = ARP_SATURATE(dot(m.N, L));
    float NoH = ARP_SATURATE(dot(m.N, H));
    float LoH = ARP_SATURATE(dot(L, H));
    float VoH = ARP_SATURATE(dot(m.V, H));
    float roughness = clamp(m.roughness, 0.05, 1.0);
    float alphaR    = roughness * roughness;

    vec3 result;
    if (mode == 1) {
        result = vec3(arpDGGX(NoH, alphaR));
    } else if (mode == 2) {
        result = vec3(arpVSmithGGXCorrelated(m.NoV, NoL, alphaR));
    } else if (mode == 3) {
        result = arpFSchlick3(m.F90, m.F0, VoH);
    } else if (mode == 4) {
        result = vec3(arpFdRenormalizedBurley(m.NoV, NoL, LoH, roughness));
    } else if (mode == 5) {
        // x NoL: the Smith visibility is G / (4 NoL NoV), so without the cosine it diverges where
        // NoV -> 0 (mesh silhouette) and the rim saturates white. Same convention as the shaded result.
        result = vec3(arpDGGX(NoH, alphaR) * arpVSmithGGXCorrelated(m.NoV, NoL, alphaR)) *
                 arpFSchlick3(m.F90, m.F0, VoH) * NoL;
    } else if (mode == 6) {
        result = m.N * 0.5 + vec3(0.5);
    } else {                                             // mode 7 (and anything unhandled)
        result = vec3(clamp(m.roughness, 0.0, 1.0), clamp(m.metallic, 0.0, 1.0), m.ao);
    }

    albedoOutput(vec3(0.0));
    diffuseShadingOutput(vec3(0.0));
    specularShadingOutput(vec3(0.0));
    emissiveColorOutput(result);
}

//==============================================================================================
//  10. Entry point
//==============================================================================================

void shade(V2F inputs)
{
    SparseCoord coord = inputs.sparse_coord;

    // ---- Alpha clip (Unity: _CLIPPING + _Cutoff; Painter uses the opacity channel) ----
    if (arp_alpha_clip && arpGrey(arp_opacity_tex, coord, 1.0) * arp_base_color.a < arp_alpha_cutoff) {
        discard;
    }

    // Normal: Painter's standard chain (baked normal + height->normal + Normal channel + blend mode)
    LocalVectors vectors = computeLocalFrame(inputs);
    ARPMaterial   m       = arpBuildMaterial(coord, vectors);

    if (arp_debug_mode != 0) {
        arpOutputDebug(m, arp_debug_mode);
        return;
    }

    float exposureScale = exp2(arp_exposure);

    if (arp_lighting_mode == 1) {
        // ---- Painter Native: lit by Painter's environment ----
        arpOutputNative(m, vectors, exposureScale);
    } else {
        // ---- AntiqueRenderPipeline Replica: own lighting, everything routed through emissive ----
        vec3 color = arpShadeReplica(m) * exposureScale;
        albedoOutput(vec3(0.0));
        diffuseShadingOutput(vec3(0.0));
        specularShadingOutput(vec3(0.0));
        emissiveColorOutput(color);
    }
}
