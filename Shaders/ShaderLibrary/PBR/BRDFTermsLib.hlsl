#ifndef YPIPELINE_BRDF_TERMS_LIBRARY_INCLUDED
#define YPIPELINE_BRDF_TERMS_LIBRARY_INCLUDED

// ----------------------------------------------------------------------------------------------------
// BRDF terms struct
// ----------------------------------------------------------------------------------------------------

struct BRDFTerms
{
    // NoV is in MaterialParams
    float NoL;
    float NoH;
    float LoH;
    float VoH;
};

struct AnisoBRDFTerms
{
    float ToH;
    float ToL;
    float ToV;
    float BoH;
    float BoL;
    float BoV;
};

void InitializeBRDFTerms(out BRDFTerms brdfTerms, float3 N, float3 L, float3 V, float3 H)
{
    brdfTerms.NoL = saturate(dot(N, L));
    brdfTerms.NoH = saturate(dot(N, H));
    brdfTerms.LoH = saturate(dot(L, H));
    brdfTerms.VoH = saturate(dot(V, H));
}

void InitializeAnisoBRDFTerms(out AnisoBRDFTerms anisoBRDFTerms, float3 T, float3 B, float3 L, float3 V, float3 H)
{
    anisoBRDFTerms.ToH = dot(T, H);
    anisoBRDFTerms.ToL = dot(T, L);
    anisoBRDFTerms.ToV = dot(T, V);
    anisoBRDFTerms.BoH = dot(B, H);
    anisoBRDFTerms.BoL = dot(B, L);
    anisoBRDFTerms.BoV = dot(B, V);
}

// ----------------------------------------------------------------------------------------------------
// Fresnel Term
// ----------------------------------------------------------------------------------------------------

inline float F_Schlick(float f0, float VoH)
{
    return f0 + (1.0 - f0) * pow(1.0 - VoH, 5.0);
}

inline float F_Schlick(float f90, float f0, float VoH)
{
    return f0 + (f90 - f0) * pow(1.0 - VoH, 5.0);
}

inline float3 F_Schlick(float f90, float3 f0, float VoH)
{
    return f0 + (float3(f90, f90, f90) - f0) * pow(1.0 - VoH, 5.0);
}

inline float3 F_SchlickRoughness(float3 f0, float NoV, float roughness)
{
    float3 f90 = max(float3(1.0 - roughness, 1.0 - roughness, 1.0 - roughness), f0);
    return f0 + saturate(f90 - f0) * pow(1.0 - NoV, 5.0);
}

// ----------------------------------------------------------------------------------------------------
// Diffuse Term
// ----------------------------------------------------------------------------------------------------

inline float Fd_Lambert()
{
    return INV_PI;
}

inline float3 Fd_Lambert(float3 diffuseColor)
{
    return INV_PI * diffuseColor;
}

inline float Fd_Burley_Disney(float NoV, float NoL, float LoH, float roughness)
{
    float fd90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float FdV = F_Schlick(fd90, 1.0, NoV);
    float FdL = F_Schlick(fd90, 1.0, NoL);
    return FdV * FdL * INV_PI;
}

inline float3 Fd_Burley_Disney(float NoV, float NoL, float LoH, float roughness, float3 diffuseColor)
{
    float fd90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float FdV = F_Schlick(fd90, 1.0, NoV);
    float FdL = F_Schlick(fd90, 1.0, NoL);
    return FdV * FdL * INV_PI * diffuseColor;
}

inline float Fd_Burley_Disney_NoPI(float NoV, float NoL, float LoH, float roughness)
{
    float fd90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float FdV = F_Schlick(fd90, 1.0, NoV);
    float FdL = F_Schlick(fd90, 1.0, NoL);
    return FdV * FdL;
}

inline float Fd_RenormalizedBurley_Disney(float NoV, float NoL, float LoH, float roughness)
{
    float energyBias = lerp(0, 0.5, roughness);
    float energyFactor = lerp(1.0, 1.0 / 1.51, roughness);
    float fd90 = energyBias + 2.0 * LoH * LoH * roughness;
    float FdL = F_Schlick(fd90, 1.0, NoL);
    float FdV = F_Schlick(fd90, 1.0, NoV);
    return FdV * FdL * energyFactor * INV_PI;
}

inline float3 Fd_RenormalizedBurley_Disney(float NoV, float NoL, float LoH, float roughness, float3 diffuseColor)
{
    float energyBias = lerp(0, 0.5, roughness);
    float energyFactor = lerp(1.0, 1.0 / 1.51, roughness);
    float fd90 = energyBias + 2.0 * LoH * LoH * roughness;
    float FdL = F_Schlick(fd90, 1.0, NoL);
    float FdV = F_Schlick(fd90, 1.0, NoV);
    return FdV * FdL * energyFactor * INV_PI * diffuseColor;
}

inline float Fd_RenormalizedBurley_Disney_NoPI(float NoV, float NoL, float LoH, float roughness)
{
    float energyBias = lerp(0, 0.5, roughness);
    float energyFactor = lerp(1.0, 1.0 / 1.51, roughness);
    float fd90 = energyBias + 2.0 * LoH * LoH * roughness;
    float FdL = F_Schlick(fd90, 1.0, NoL);
    float FdV = F_Schlick(fd90, 1.0, NoV);
    return FdV * FdL * energyFactor;
}

// ----------------------------------------------------------------------------------------------------
// Specular NDF Term
// ----------------------------------------------------------------------------------------------------

inline float D_GGX_LinearRoughness(float NoH, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float d = 1.0 + NoH * NoH * (a2 - 1.0);
    return a2 / (PI * d * d);
}

inline float D_GGX(float NoH, float alphaRoughness)
{
    float a2 = alphaRoughness * alphaRoughness;
    float d = 1.0 + NoH * NoH * (a2 - 1.0);
    return a2 / (PI * d * d);
}

// inline float D_GGX_Anisotropic(float at, float ab, float NoH, float ToH, float BoH)
// {
//     float d = ToH * ToH / (at * at) + BoH * BoH / (ab * ab) + NoH * NoH;
//     return 1 / (PI * at * ab * d * d);
// }

inline float D_GGX_Anisotropic(float at, float ab, float NoH, float ToH, float BoH)
{
    float a2 = at * ab;
    float3 f = float3(ab * ToH, at * BoH, a2 * NoH);
    float w2 = a2 / dot(f, f);
    return a2 * w2 * w2 * INV_PI;
}

// ----------------------------------------------------------------------------------------------------
// Specular Geometry/Visibility Term
// ----------------------------------------------------------------------------------------------------

inline float V_SmithGGXCorrelated_LinearRoughness(float NoV, float NoL, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float V_SmithL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    float V_SmithV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    return 0.5 / (V_SmithL + V_SmithV);
}

inline float V_SmithGGXCorrelated(float NoV, float NoL, float alphaRoughness)
{
    float a2 = alphaRoughness * alphaRoughness;
    float V_SmithL = NoV * sqrt(NoL * NoL * (1.0 - a2) + a2);
    float V_SmithV = NoL * sqrt(NoV * NoV * (1.0 - a2) + a2);
    return 0.5 / (V_SmithL + V_SmithV);
}

inline float V_SmithGGXCorrelatedApprox_LinearRoughness(float NoV, float NoL, float roughness)
{
    float a = roughness * roughness;
    float V_SmithL = NoV * (NoL * (1.0 - a) + a);
    float V_SmithV = NoL * (NoV * (1.0 - a) + a);
    return 0.5 / (V_SmithL + V_SmithV);
}

inline float V_SmithGGXCorrelatedApprox(float NoV, float NoL, float alphaRoughness)
{
    float a = alphaRoughness;
    float V_SmithL = NoV * (NoL * (1.0 - a) + a);
    float V_SmithV = NoL * (NoV * (1.0 - a) + a);
    return 0.5 / (V_SmithL + V_SmithV);
}

// inline float V_SmithGGXCorrelated_Anisotropic(float at, float ab, float NoV, float NoL, float ToV, float BoV, float ToL, float BoL)
// {
//     float at2 = at * at;
//     float ab2 = ab * ab;
//     float V_SmithL = NoV * sqrt(NoL * NoL + at2 * ToL * ToL + ab2 * BoL * BoL);
//     float V_SmithV = NoL * sqrt(NoV * NoV + at2 * ToV * ToV + ab2 * BoV * BoV);
//     return 0.5 / (V_SmithL + V_SmithV);
// }

inline float V_SmithGGXCorrelated_Anisotropic(float at, float ab, float NoV, float NoL, float ToV, float BoV, float ToL, float BoL)
{
    float V_SmithV = NoL * length(float3(at * ToV, ab * BoV, NoV));
    float V_SmithL = NoV * length(float3(at * ToL, ab * BoL, NoL));
    return 0.5 / (V_SmithL + V_SmithV);
}

// Clear coat
inline float V_Kelemen(float LoH)
{
    return 0.25 / (LoH * LoH);
}

// ----------------------------------------------------------------------------------------------------
// Anisotropic Roughness
// ----------------------------------------------------------------------------------------------------

inline float2 AnisotropicRoughness_Neubelt(float alphaRoughness, float anisotropy)
{
    float a = alphaRoughness;
    float at = max(0.001, a);
    float ab = lerp(0.001, a, 1.0 - anisotropy);
    return float2(at, ab);
}

// 这个效果最好
inline float2 AnisotropicRoughness_Burley(float alphaRoughness, float anisotropy)
{
    float a = alphaRoughness;
    float aspect = sqrt(1.0 - 0.9 * anisotropy);
    float at = max(0.001, a / aspect);
    float ab = max(0.001, a * aspect);
    return float2(at, ab);
}

inline float2 AnisotropicRoughness_Kulla(float alphaRoughness, float anisotropy)
{
    float a = alphaRoughness;
    float at = max(0.001, a * (1 + anisotropy));
    float ab = max(0.001, a * (1 - anisotropy));
    return float2(at, ab);
}

#endif