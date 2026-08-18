#ifndef YPIPELINE_BRDF_MODEL_LIBRARY_INCLUDED
#define YPIPELINE_BRDF_MODEL_LIBRARY_INCLUDED

#include "BRDFTermsLib.hlsl"
#include "AmbientOcclusionLib.hlsl"

// ----------------------------------------------------------------------------------------------------
// Standard PBR Model
// ----------------------------------------------------------------------------------------------------

struct StandardMaterialParams
{
    float3 albedo;
    float alpha;
    float3 emission;
    float roughness; // perceptually linear roughness
    float alphaRoughness;
    float metallic;
    float ao;
    float3 F0;
    float F90;
    float3 N; // L、H is related to the light，see XXXLightsLibrary.
    float3 V;
    float NoV;
};

float3 StandardBRDF(in StandardMaterialParams stdMatParams, float3 L, float3 H, float3 energyCompensation = float3(1.0, 1.0, 1.0))
{
    BRDFTerms terms = (BRDFTerms) 0;
    InitializeBRDFTerms(terms, stdMatParams.N, L, stdMatParams.V, H);
    
    float roughness = clamp(stdMatParams.roughness, 0.05, 1.0); //make sure there is a tiny specular lobe when roughness is zero
    float alphaRoughness = roughness * roughness;
    float3 diffuseBRDF = Fd_RenormalizedBurley_Disney(stdMatParams.NoV, terms.NoL, terms.LoH, roughness, stdMatParams.albedo);
    
    float D = D_GGX(terms.NoH, alphaRoughness);
    float V = V_SmithGGXCorrelated(stdMatParams.NoV, terms.NoL, alphaRoughness);
    float3 F = F_Schlick(stdMatParams.F90, stdMatParams.F0, terms.VoH);
    float3 specularBRDF = D * V * F;
    
    return (diffuseBRDF * (1 - stdMatParams.metallic) + specularBRDF * energyCompensation) * terms.NoL;
}

// ----------------------------------------------------------------------------------------------------
// Advanced PBR Model (Anisotropic & Clear Coat)
// ----------------------------------------------------------------------------------------------------

struct AdvancedMaterialParams
{
    float3 albedo;
    float alpha;
    float3 emission;
    float roughness; // perceptually linear roughness
    float alphaRoughness;
    float metallic;
    float ao;
    float3 F0;
    float F90;
    float3 N; //L、H is related to the light，see XXXLightsLibrary.
    float3 V;
    float NoV;
    float anisotropy;
    float3 anisotropicT;
    float3 anisotropicB;
    float clearCoat;
    float clearCoatRoughness;
    float3 clearCoatN;
    float clearCoatNoV;
};

float3 AdvancedBRDF(in AdvancedMaterialParams advMatParams, float3 L, float3 H, float3 energyCompensation = float3(1.0, 1.0, 1.0))
{
    BRDFTerms terms = (BRDFTerms) 0;
    InitializeBRDFTerms(terms, advMatParams.N, L, advMatParams.V, H);
    
    AnisoBRDFTerms anisoTerms = (AnisoBRDFTerms) 0;
    InitializeAnisoBRDFTerms(anisoTerms, advMatParams.anisotropicT, advMatParams.anisotropicB, L, advMatParams.V, H);
    
    float roughness = clamp(advMatParams.roughness, 0.05, 1.0); //make sure there is a tiny specular lobe when roughness is zero
    float alphaRoughness = roughness * roughness;
    float3 diffuseBRDF = Fd_RenormalizedBurley_Disney(advMatParams.NoV, terms.NoL, terms.LoH, roughness, advMatParams.albedo);
    
    float2 atb = AnisotropicRoughness_Burley(alphaRoughness, advMatParams.anisotropy);
    float D = D_GGX_Anisotropic(atb.x, atb.y, terms.NoH, anisoTerms.ToH, anisoTerms.BoH);
    float V = V_SmithGGXCorrelated_Anisotropic(atb.x, atb.y, advMatParams.NoV, terms.NoL, anisoTerms.ToV, anisoTerms.BoV, anisoTerms.ToL, anisoTerms.BoL);
    float3 F = F_Schlick(advMatParams.F90, advMatParams.F0, terms.VoH);
    float3 specularBRDF = D * V * F;
    
    float3 standardBRDF = diffuseBRDF * (1 - advMatParams.metallic) + specularBRDF * energyCompensation;
    
    float clearCoatRoughness = clamp(advMatParams.clearCoatRoughness, 0.05, 1.0);
    float clearCoatAlphaRoughness = clearCoatRoughness * clearCoatRoughness;
    float clearCoatNoH = saturate(dot(advMatParams.clearCoatN, H));
    float Dc = D_GGX(clearCoatNoH, clearCoatAlphaRoughness);
    float Vc = V_Kelemen(terms.LoH);
    float Fc = F_Schlick(0.04, terms.VoH) * advMatParams.clearCoat;
    float clearCoatBRDF = Dc * Vc;
    
    return lerp(standardBRDF, clearCoatBRDF, Fc) * terms.NoL;
}

#endif