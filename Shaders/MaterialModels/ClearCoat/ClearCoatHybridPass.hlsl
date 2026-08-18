#ifndef YPIPELINE_CLEAR_COAT_HYBRID_PASS_INCLUDED
#define YPIPELINE_CLEAR_COAT_HYBRID_PASS_INCLUDED

#include "../../ShaderLibrary/Utilities/EncodingLib.hlsl"
#include "../../ShaderLibrary/PBR/RenderingEquationLib.hlsl"

Texture2D<float4> _GBuffer0; // RGBA8_SRGB: albedo, AO
Texture2D<float4> _GBuffer1; // RGBA8_UNORM: normal, roughness
Texture2D<float4> _GBuffer2; // RGBA8_UNORM: reflectance, metallic, material ID (alpha)
Texture2D<float4> _GBuffer3; // RGBA8_UNORM: custom parameters based on project requirements

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
    // TODO：使用 GBuffer 和使用材质贴图哪个好？？暂时使用 GBuffer，后续可以考虑使用材质贴图
    float4 gBuffer0 = LOAD_TEXTURE2D_LOD(_GBuffer0, geoParams.pixelCoord, 0);
    float4 gBuffer1 = LOAD_TEXTURE2D_LOD(_GBuffer1, geoParams.pixelCoord, 0);
    float4 gBuffer2 = LOAD_TEXTURE2D_LOD(_GBuffer2, geoParams.pixelCoord, 0);
    float4 gBuffer3 = LOAD_TEXTURE2D_LOD(_GBuffer3, geoParams.pixelCoord, 0);
    
    coatMatParams.albedo = gBuffer0.rgb;
    coatMatParams.emission = 0.0;
    coatMatParams.ao = gBuffer0.a;
    coatMatParams.alpha = 1.0;
    coatMatParams.N = DecodeNormalFrom888(gBuffer1.rgb);
    coatMatParams.roughness = gBuffer1.a;
    coatMatParams.metallic = gBuffer2.g;
    
    float3 n = geoParams.normalWS;
    float3 t = geoParams.tangentWS.xyz;
    float3 b = normalize(cross(n, t) * geoParams.tangentWS.w);
    float3x3 tbn = float3x3(t, b, n);
    
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
    
    coatMatParams.F0 = lerp(gBuffer2.r * gBuffer2.r * float3(0.16, 0.16, 0.16), coatMatParams.albedo, coatMatParams.metallic);
    coatMatParams.F90 = saturate(dot(coatMatParams.F0, 50.0 * 0.3333));
    coatMatParams.V = GetWorldSpaceNormalizedViewDir(geoParams.positionWS);
    coatMatParams.NoV = saturate(dot(coatMatParams.N, coatMatParams.V)) + 1e-3; // 防止小黑点
    
    coatMatParams.coatF0 = IORToF0(_CoatIOR);
    coatMatParams.coatNoV = saturate(dot(coatMatParams.coatN, coatMatParams.V)) + 1e-3;
    float3 coatColor = SAMPLE_TEXTURE2D(_CoatColorTex, sampler_CoatColorTex, geoParams.uv).rgb * _CoatColor.rgb;
    coatMatParams.coatAbsorption = ClearCoatAbsorption(coatMatParams.coat, _CoatIOR, coatMatParams.coatNoV, coatColor);
}

Varyings HybridVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
    return OUT;
}

float4 HybridFrag(Varyings IN) : SV_TARGET
{
    // ------------------------- Initialize Params -------------------------
    
    GeometryParams geoParams = (GeometryParams) 0;
    InitializeGeometryParams(IN, geoParams);
    
    CoatMaterialParams coatMatParams = (CoatMaterialParams) 0;
    InitializeCoatMaterialParams(geoParams, coatMatParams);
    
    // ------------------------- Shading -------------------------
    
    RenderingEquationContent content = (RenderingEquationContent) 0;
    ClearCoatShading(geoParams, coatMatParams, content);
    
    return float4(CombineRenderingEquationContent(content), 1.0);
}

#endif