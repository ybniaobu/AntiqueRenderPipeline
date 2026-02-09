#ifndef YPIPELINE_UNITY_META_PASS_LIBRARY_INCLUDED
#define YPIPELINE_UNITY_META_PASS_LIBRARY_INCLUDED

struct UnityMetaParams
{
    float3 albedo;
    float3 emission;
};

CBUFFER_START(UnityMetaPass)
    // x = use uv1 as raster position, lightmap
    // y = use uv2 as raster position, DynamicLightmap
    bool4 unity_MetaVertexControl;

    // x = return albedo
    // y = return emission
    bool4 unity_MetaFragmentControl;
CBUFFER_END

float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

float4 TransformMetaPosition(float3 positionOS, float2 lightMapUV, float2 dynamicLightMapUV)
{
    if (unity_MetaVertexControl.x)
    {
        positionOS.xy = lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
        positionOS.z = positionOS.z > 0.0 ? FLT_MIN : 0.0;
    }
    
    if (unity_MetaFragmentControl.y)
    {
        positionOS.xy = dynamicLightMapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
        positionOS.z = positionOS.z > 0.0 ? FLT_MIN : 0.0;
    }
    
    return TransformWorldToHClip(positionOS);
}

float4 TransportMetaColor(UnityMetaParams params)
{
    float4 color;
    if (unity_MetaFragmentControl.x)
    {
        color = float4(params.albedo, 1.0);
        // Apply Albedo Boost from LightmapSettings.
        color.rgb = clamp(pow(abs(color.rgb), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }

    if (unity_MetaFragmentControl.y)
    {
        color = float4(params.emission, 1.0);
    }
    
    return color;
}


#endif