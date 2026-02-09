#ifndef YPIPELINE_BRDF_TERMS_LIBRARY_INCLUDED
#define YPIPELINE_BRDF_TERMS_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ----------------------------------------------------------------------------------------------------
// BRDF input parameters struct
// ----------------------------------------------------------------------------------------------------

struct BRDFParams
{
    // NoV is in BRDFModelParams
    float NoL;
    float NoH;
    float LoH;
    float VoH;
};

struct AnisoBRDFParams
{
    float ToH;
    float ToL;
    float ToV;
    float BoH;
    float BoL;
    float BoV;
};

void InitializeBRDFParams(out BRDFParams BRDFParams, float3 N, float3 L, float3 V, float3 H)
{
    BRDFParams.NoL = saturate(dot(N, L));
    BRDFParams.NoH = saturate(dot(N, H));
    BRDFParams.LoH = saturate(dot(L, H));
    BRDFParams.VoH = saturate(dot(V, H));
}

void InitializeAnisoBRDFParams(out AnisoBRDFParams AnisoBRDFParams, float3 T, float3 B, float3 L, float3 V, float3 H)
{
    AnisoBRDFParams.ToH = dot(T, H);
    AnisoBRDFParams.ToL = dot(T, L);
    AnisoBRDFParams.ToV = dot(T, V);
    AnisoBRDFParams.BoH = dot(B, H);
    AnisoBRDFParams.BoL = dot(B, L);
    AnisoBRDFParams.BoV = dot(B, V);
}

// ----------------------------------------------------------------------------------------------------
// Fresnel Term
// ----------------------------------------------------------------------------------------------------

float F_Schlick(float f90, float f0, float VoH)
{
    return f0 + (f90 - f0) * pow(1.0 - VoH, 5.0);
}

float3 F_Schlick(float f90, float3 f0, float VoH)
{
    return f0 + (float3(f90, f90, f90) - f0) * pow(1.0 - VoH, 5.0);
}

float3 F_SchlickRoughness(float3 f0, float NoV, float roughness)
{
    float3 f90 = max(float3(1.0 - roughness, 1.0 - roughness, 1.0 - roughness), f0);
    return f0 + saturate(f90 - f0) * pow(1.0 - NoV, 5.0);
}

// ----------------------------------------------------------------------------------------------------
// Diffuse Term
// ----------------------------------------------------------------------------------------------------

float Fd_Lambert()
{
    return INV_PI;
}

float3 Fd_Lambert(float3 diffuseColor)
{
    return INV_PI * diffuseColor;
}

float Fd_Burley_Disney(float NoV, float NoL, float LoH, float roughness)
{
    float fd90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float FdV = F_Schlick(fd90, 1.0, NoV);
    float FdL = F_Schlick(fd90, 1.0, NoL);
    return FdV * FdL * INV_PI;
}

float3 Fd_Burley_Disney(float NoV, float NoL, float LoH, float roughness, float3 diffuseColor)
{
    float fd90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float FdV = F_Schlick(fd90, 1.0, NoV);
    float FdL = F_Schlick(fd90, 1.0, NoL);
    return FdV * FdL * INV_PI * diffuseColor;
}

float Fd_Burley_Disney_NoPI(float NoV, float NoL, float LoH, float roughness)
{
    float fd90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float FdV = F_Schlick(fd90, 1.0, NoV);
    float FdL = F_Schlick(fd90, 1.0, NoL);
    return FdV * FdL;
}

float Fd_RenormalizedBurley_Disney(float NoV, float NoL, float LoH, float roughness)
{
    float energyBias = lerp(0, 0.5, roughness);
    float energyFactor = lerp(1.0, 1.0 / 1.51, roughness);
    float fd90 = energyBias + 2.0 * LoH * LoH * roughness;
    float FdL = F_Schlick(fd90, 1.0, NoL);
    float FdV = F_Schlick(fd90, 1.0, NoV);
    return FdV * FdL * energyFactor * INV_PI;
}

float3 Fd_RenormalizedBurley_Disney(float NoV, float NoL, float LoH, float roughness, float3 diffuseColor)
{
    float energyBias = lerp(0, 0.5, roughness);
    float energyFactor = lerp(1.0, 1.0 / 1.51, roughness);
    float fd90 = energyBias + 2.0 * LoH * LoH * roughness;
    float FdL = F_Schlick(fd90, 1.0, NoL);
    float FdV = F_Schlick(fd90, 1.0, NoV);
    return FdV * FdL * energyFactor * INV_PI * diffuseColor;
}

float Fd_RenormalizedBurley_Disney_NoPI(float NoV, float NoL, float LoH, float roughness)
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

float D_GGX(float NoH, float roughness)
{
    float a2 = pow(roughness, 4.0);
    float d = (NoH * a2 - NoH) * NoH + 1.0;
    return a2 / (PI * d * d);
}

float D_GGX_Anisotropic(float at, float ab, float NoH, float ToH, float BoH)
{
    float d = ToH * ToH / (at * at) + BoH * BoH / (ab * ab) + NoH * NoH;
    return 1 / (PI * at * ab * d * d);
}

// ----------------------------------------------------------------------------------------------------
// Specular Geometry/Visibility Term
// ----------------------------------------------------------------------------------------------------

float V_SmithGGXCorrelated(float NoV, float NoL, float roughness)
{
    float a2 = pow(roughness, 4.0);
    float V_SmithL = NoV * sqrt((-NoL * a2 + NoL) * NoL + a2);
    float V_SmithV = NoL * sqrt((-NoV * a2 + NoV) * NoV + a2);
    return 0.5 / (V_SmithL + V_SmithV);
}

float V_SmithGGXCorrelatedApprox(float NoV, float NoL, float roughness)
{
    float a = roughness * roughness;
    float V_SmithL = NoV * (NoL * (1.0 - a) + a);
    float V_SmithV = NoL * (NoV * (1.0 - a) + a);
    return 0.5 / (V_SmithL + V_SmithV);
}

float V_SmithGGXCorrelated_Anisotropic(float at, float ab, float NoV, float NoL, float ToV, float BoV, float ToL, float BoL)
{
    float at2 = at * at;
    float ab2 = ab * ab;
    float V_SmithL = NoV * sqrt(NoL * NoL + at2 * ToL * ToL + ab2 * BoL * BoL);
    float V_SmithV = NoL * sqrt(NoV * NoV + at2 * ToV * ToV + ab2 * BoV * BoV);
    return 0.5 / (V_SmithL + V_SmithV);
}

// Clear coat
float V_Kelemen(float LoH)
{
    return 0.25 / (LoH * LoH);
}

#endif