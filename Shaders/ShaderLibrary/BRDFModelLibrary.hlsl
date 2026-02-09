#ifndef YPIPELINE_BRDF_MODEL_LIBRARY_INCLUDED
#define YPIPELINE_BRDF_MODEL_LIBRARY_INCLUDED

#include "../ShaderLibrary/BRDFTermsLibrary.hlsl"
#include "../ShaderLibrary/AmbientOcclusionLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Standard PBR Model
// ----------------------------------------------------------------------------------------------------

struct StandardPBRParams
{
    float3 albedo;
    float alpha;
    float3 emission;
    float roughness;
    float metallic;
    float ao;
    float3 F0;
    float F90;
    float3 N; // L、H is related to the light，see XXXLightsLibrary.
    float3 V;
    float3 R;
    float NoV;
};

float3 StandardPBR(in BRDFParams BRDFParams, in StandardPBRParams standardPBRParams)
{
    float roughness = clamp(standardPBRParams.roughness, 0.05, 1.0); //make sure there is a tiny specular lobe when roughness is zero
    float3 diffuse = Fd_RenormalizedBurley_Disney(standardPBRParams.NoV, BRDFParams.NoL, BRDFParams.LoH, roughness, standardPBRParams.albedo);
    
    float D = D_GGX(BRDFParams.NoH, roughness);
    float V = V_SmithGGXCorrelated(standardPBRParams.NoV, BRDFParams.NoL, roughness);
    float3 F = F_Schlick(standardPBRParams.F90, standardPBRParams.F0, BRDFParams.VoH);
    float3 specular = D * V * F;
    
    specular *= ComputeHorizonSpecularOcclusion(standardPBRParams.R, standardPBRParams.N);
    
    return (diffuse * (1 - standardPBRParams.metallic) + specular) * BRDFParams.NoL;
}

float3 StandardPBR_EnergyCompensation(in BRDFParams BRDFParams, in StandardPBRParams standardPBRParams, float3 energyCompensation)
{
    float roughness = clamp(standardPBRParams.roughness, 0.05, 1.0); //make sure there is a tiny specular lobe when roughness is zero
    float3 diffuse = Fd_RenormalizedBurley_Disney(standardPBRParams.NoV, BRDFParams.NoL, BRDFParams.LoH, roughness, standardPBRParams.albedo);

    float D = D_GGX(BRDFParams.NoH, roughness);
    float V = V_SmithGGXCorrelated(standardPBRParams.NoV, BRDFParams.NoL, roughness);
    float3 F = F_Schlick(standardPBRParams.F90, standardPBRParams.F0, BRDFParams.VoH);
    float3 specular = D * V * F;
    
    specular *= ComputeHorizonSpecularOcclusion(standardPBRParams.R, standardPBRParams.N);
    
    return (diffuse * (1 - standardPBRParams.metallic) + specular * energyCompensation) * BRDFParams.NoL;
}

// ----------------------------------------------------------------------------------------------------
// Anisotropic Model
// ----------------------------------------------------------------------------------------------------

struct AnisotropicModelParams
{
    float anisotropy;
    float3 albedo;
    float roughness;
    float metallic;
    float ao;
    float3 F0;
    float3 N; //L、H is related to the light，see XXXLightsLibrary.
    float3 V;
    float3 R;
    float NoV;
};

#endif