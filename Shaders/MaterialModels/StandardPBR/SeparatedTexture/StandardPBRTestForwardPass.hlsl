#ifndef YPIPELINE_STANDARD_PBR_FOR_TEST_FORWARD_PASS_INCLUDED
#define YPIPELINE_STANDARD_PBR_FOR_TEST_FORWARD_PASS_INCLUDED

#include "../../../ShaderLibrary/PBR/RenderingEquationLib.hlsl"

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

void InitializeStandardMaterialParams(in GeometryParams geoParams, out StandardMaterialParams stdMatParams)
{
    float4 color = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, geoParams.uv).rgba * _BaseColor.rgba;
    stdMatParams.albedo = color.rgb;
    stdMatParams.alpha = color.a;
    stdMatParams.emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, geoParams.uv).rgb * _EmissionColor.rgb;

    #if _USE_ROUGHNESSTEX
        stdMatParams.roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_RoughnessTex, geoParams.uv).r;
        stdMatParams.roughness *= pow(10, _RoughnessScale);
        stdMatParams.roughness = saturate(stdMatParams.roughness);
        stdMatParams.alphaRoughness = stdMatParams.roughness * stdMatParams.roughness;
    #else
        stdMatParams.roughness = _Roughness;
        stdMatParams.alphaRoughness = _Roughness * _Roughness;
    #endif

    #if _USE_METALLICTEX
        stdMatParams.metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, geoParams.uv).r;
        stdMatParams.metallic *= pow(10, _MetallicScale);
        stdMatParams.metallic = saturate(stdMatParams.metallic);
    #else
        stdMatParams.metallic = _Metallic;
    #endif
    
    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, geoParams.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = geoParams.normalWS;
        float3 t = geoParams.tangentWS.xyz;
        float3 b = normalize(cross(n, t) * geoParams.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        stdMatParams.N = normalize(mul(normalTS, tbn));
    #else
        stdMatParams.N = geoParams.normalWS;
    #endif

    stdMatParams.ao = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, geoParams.uv).r;
    stdMatParams.ao *= pow(0.1, _AOScale);
    stdMatParams.ao = saturate(stdMatParams.ao);
    
    #if _SCREEN_SPACE_AMBIENT_OCCLUSION
        stdMatParams.ao = min(stdMatParams.ao, SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionTexture, sampler_PointClamp, geoParams.screenUV, 0).r);
    #endif
    
    stdMatParams.F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), stdMatParams.albedo, stdMatParams.metallic);
    stdMatParams.F90 = saturate(dot(stdMatParams.F0, 50.0 * 0.3333));
    stdMatParams.V = GetWorldSpaceNormalizedViewDir(geoParams.positionWS);
    stdMatParams.NoV = saturate(dot(stdMatParams.N, stdMatParams.V)) + 1e-3; //防止小黑点
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
    
    StandardMaterialParams stdMatParams = (StandardMaterialParams) 0;
    InitializeStandardMaterialParams(geoParams, stdMatParams);
    
    // ------------------------- Clipping -------------------------
    
    #if defined(_CLIPPING)
        clip(stdMatParams.alpha - _Cutoff);
    #endif
    
    // ------------------------- LOD Fade -------------------------
    
    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(geoParams.pixelCoord);
    #endif
    
    // ------------------------- Shading -------------------------
    
    RenderingEquationContent content = (RenderingEquationContent) 0;
    StandardPBRShading(geoParams, stdMatParams, content);
    
    return float4(CombineRenderingEquationContent(content), 1.0);
}

#endif