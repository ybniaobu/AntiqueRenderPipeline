#ifndef YPIPELINE_ADVANCED_PBR_FORWARD_PASS_INCLUDED
#define YPIPELINE_ADVANCED_PBR_FORWARD_PASS_INCLUDED

#include "../../ShaderLibrary/PBR/RenderingEquationLib.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float4 tangentWS    : TEXCOORD3;
};

void InitializeGeometryParams(Varyings IN, out GeometryParams geoParams)
{
    geoParams.positionWS = IN.positionWS;
    geoParams.normalWS = normalize(IN.normalWS);
    geoParams.tangentWS = float4(normalize(IN.tangentWS.xyz), IN.tangentWS.w);
    geoParams.uv = IN.uv;
    geoParams.pixelCoord = IN.positionHCS.xy;
    geoParams.screenUV = geoParams.pixelCoord * _CameraBufferSize.xy;
}

void InitializeAdvancedMaterialParams(in GeometryParams geoParams, out AdvancedMaterialParams advMatParams)
{
    float4 color = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, geoParams.uv).rgba * _BaseColor.rgba;
    advMatParams.albedo = color.rgb;
    advMatParams.alpha = color.a;
    advMatParams.emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, geoParams.uv).rgb * _EmissionColor.rgb;
    
    #if _USE_HYBRIDTEX
        float4 hybrid = SAMPLE_TEXTURE2D(_HybridTex, sampler_HybridTex, geoParams.uv).rgba;
        advMatParams.roughness = saturate(hybrid.r * pow(10, _RoughnessScale));
        advMatParams.alphaRoughness = advMatParams.roughness * advMatParams.roughness;
        advMatParams.metallic = saturate(hybrid.g * pow(10, _MetallicScale));
        advMatParams.ao = saturate(hybrid.a * pow(0.1, _AOScale));
    #else
        advMatParams.roughness = _Roughness;
        advMatParams.alphaRoughness = _Roughness * _Roughness;
        advMatParams.metallic = _Metallic;
        advMatParams.ao = 1.0;
    #endif
    
    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, geoParams.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = geoParams.normalWS;
        float3 t = geoParams.tangentWS.xyz;
        float3 b = normalize(cross(n, t) * geoParams.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        advMatParams.N = normalize(mul(normalTS, tbn));
    #else
        float3 n = geoParams.normalWS;
        float3 t = geoParams.tangentWS.xyz;
        float3 b = normalize(cross(n, t) * geoParams.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        advMatParams.N = n;
    #endif
    
    #if _USE_ADVANCEDTEX
        float4 advanced = SAMPLE_TEXTURE2D(_AdvancedTex, sampler_AdvancedTex, geoParams.uv);
        advMatParams.anisotropy = saturate(advanced.r);
        float anisotropyRotation = saturate(advanced.g) * PI;
        float2 direction = float2(cos(anisotropyRotation), sin(anisotropyRotation));
        advMatParams.anisotropicT = normalize(mul(float3(direction, 0.0), tbn));
        advMatParams.anisotropicB = normalize(cross(advMatParams.N, advMatParams.anisotropicT));
        
        advMatParams.clearCoat = saturate(advanced.b);
        advMatParams.clearCoatRoughness = saturate(advanced.a);
    #else
        float anisotropyRotation = _AnisotropyRotation * PI;
        float2 direction = float2(cos(anisotropyRotation), sin(anisotropyRotation));
        advMatParams.anisotropy = _Anisotropy;
        advMatParams.anisotropicT = normalize(mul(float3(direction, 0.0), tbn));
        advMatParams.anisotropicB = normalize(cross(advMatParams.N, advMatParams.anisotropicT));
    
        advMatParams.clearCoat = _ClearCoat;
        advMatParams.clearCoatRoughness = _ClearCoatRoughness;
    #endif
    
    #if _USE_CLEARCOATNORMALTEX
        float4 packedClearCoatNormal = SAMPLE_TEXTURE2D(_ClearCoatNormalTex, sampler_ClearCoatNormalTex, geoParams.uv);
        float3 clearCoatNormalTS = UnpackNormalScale(packedClearCoatNormal, 1.0);
        advMatParams.clearCoatN = normalize(mul(clearCoatNormalTS, tbn));
    #else
        advMatParams.clearCoatN = advMatParams.N;
    #endif
    
    #if _SCREEN_SPACE_AMBIENT_OCCLUSION
        advMatParams.ao = min(advMatParams.ao, SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionTexture, sampler_PointClamp, geoParams.screenUV, 0).r);
    #endif
    
    advMatParams.F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), advMatParams.albedo, advMatParams.metallic);
    advMatParams.F90 = saturate(dot(advMatParams.F0, 50.0 * 0.3333));
    advMatParams.V = GetWorldSpaceNormalizedViewDir(geoParams.positionWS);
    advMatParams.NoV = saturate(dot(advMatParams.N, advMatParams.V)) + 1e-3; // 防止小黑点
    advMatParams.clearCoatNoV = saturate(dot(advMatParams.clearCoatN, advMatParams.V)) + 1e-3;
}

Varyings ForwardVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
    return OUT;
}

float4 ForwardFrag(Varyings IN) : SV_TARGET
{
    // ------------------------- Initialize Params -------------------------
    
    GeometryParams geoParams = (GeometryParams) 0;
    InitializeGeometryParams(IN, geoParams);
    
    AdvancedMaterialParams advMatParams = (AdvancedMaterialParams) 0;
    InitializeAdvancedMaterialParams(geoParams, advMatParams);
    
    // ------------------------- Clipping -------------------------
    
    #if defined(_CLIPPING)
        clip(advMatParams.alpha - _Cutoff);
    #endif
    
    // ------------------------- LOD Fade -------------------------
    
    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(geoParams.pixelCoord);
    #endif
    
    // ------------------------- Shading -------------------------
    
    RenderingEquationContent content = (RenderingEquationContent) 0;
    AdvancedPBRShading(geoParams, advMatParams, content);
    
    return float4(CombineRenderingEquationContent(content), 1.0);
}

#endif