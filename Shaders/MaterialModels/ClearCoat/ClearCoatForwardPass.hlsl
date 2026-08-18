#ifndef YPIPELINE_CLEAR_COAT_FORWARD_PASS_INCLUDED
#define YPIPELINE_CLEAR_COAT_FORWARD_PASS_INCLUDED

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

void InitializeCoatMaterialParams(in GeometryParams geoParams, out CoatMaterialParams coatMatParams)
{
    float4 color = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, geoParams.uv).rgba * _BaseColor.rgba;
    coatMatParams.albedo = color.rgb;
    coatMatParams.alpha = color.a;
    coatMatParams.emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, geoParams.uv).rgb * _EmissionColor.rgb;
    
    #if _USE_HYBRIDTEX
        float4 hybrid = SAMPLE_TEXTURE2D(_HybridTex, sampler_HybridTex, geoParams.uv).rgba;
        coatMatParams.roughness = hybrid.r;
        coatMatParams.metallic = hybrid.g;
        coatMatParams.ao = hybrid.a;
    #else
        coatMatParams.roughness = _Roughness;
        coatMatParams.metallic = _Metallic;
        coatMatParams.ao = 1.0;
    #endif
    
    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, geoParams.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = geoParams.normalWS;
        float3 t = geoParams.tangentWS.xyz;
        float3 b = normalize(cross(n, t) * geoParams.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        coatMatParams.N = normalize(mul(normalTS, tbn));
    #else
        float3 n = geoParams.normalWS;
        float3 t = geoParams.tangentWS.xyz;
        float3 b = normalize(cross(n, t) * geoParams.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        coatMatParams.N = n;
    #endif
    
    #if _USE_COATTEX
        float2 coatParams = SAMPLE_TEXTURE2D(_CoatTex, sampler_CoatTex, geoParams.uv).rg;
        coatMatParams.coat = coatParams.r;
        coatMatParams.coatRoughness = coatParams.g;
    #else
        coatMatParams.coat = _Coat;
        coatMatParams.coatRoughness = _CoatRoughness;
    #endif
    
    #if _USE_COATNORMALTEX
        float4 packedCoatNormal = SAMPLE_TEXTURE2D(_CoatNormalTex, sampler_CoatNormalTex, geoParams.uv);
        float3 coatNormalTS = UnpackNormalScale(packedCoatNormal, _CoatNormalIntensity);
        coatMatParams.coatN = normalize(mul(coatNormalTS, tbn));
    #else
        coatMatParams.coatN = coatMatParams.N;
    #endif
    
    #if _SCREEN_SPACE_AMBIENT_OCCLUSION
        coatMatParams.ao = min(coatMatParams.ao, SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionTexture, sampler_PointClamp, geoParams.screenUV, 0).r);
    #endif
    
    coatMatParams.F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), coatMatParams.albedo, coatMatParams.metallic);
    coatMatParams.F90 = saturate(dot(coatMatParams.F0, 50.0 * 0.3333));
    coatMatParams.V = GetWorldSpaceNormalizedViewDir(geoParams.positionWS);
    coatMatParams.NoV = saturate(dot(coatMatParams.N, coatMatParams.V)) + 1e-3; // 防止小黑点
    
    coatMatParams.coatF0 = IORToF0(_CoatIOR);
    coatMatParams.coatNoV = saturate(dot(coatMatParams.coatN, coatMatParams.V)) + 1e-3;
    float3 coatColor = SAMPLE_TEXTURE2D(_CoatColorTex, sampler_CoatColorTex, geoParams.uv).rgb * _CoatColor.rgb;
    coatMatParams.coatAbsorption = ClearCoatAbsorption(coatMatParams.coat, _CoatIOR, coatMatParams.coatNoV, coatColor);
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
    
    CoatMaterialParams coatMatParams = (CoatMaterialParams) 0;
    InitializeCoatMaterialParams(geoParams, coatMatParams);
    
    // ------------------------- Clipping -------------------------
    
    #if defined(_CLIPPING)
        clip(coatMatParams.alpha - _Cutoff);
    #endif
    
    // ------------------------- LOD Fade -------------------------
    
    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(geoParams.pixelCoord);
    #endif
    
    // ------------------------- Shading -------------------------
    
    RenderingEquationContent content = (RenderingEquationContent) 0;
    ClearCoatShading(geoParams, coatMatParams, content);
    
    return float4(CombineRenderingEquationContent(content), 1.0);
}

#endif