#ifndef YPIPELINE_CORE_INCLUDED
#define YPIPELINE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#include "UnityInput.hlsl"
#include "UnityMatrix.hlsl"
#include "YPipelineMacros.hlsl"
#include "YPipelineInput.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "SpaceTransforms.hlsl"

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

// ----------------------------------------------------------------------------------------------------
// Cubemap Constants or Functions
// ----------------------------------------------------------------------------------------------------

static const float3 k_CubeMapFaceDir[6] =
{
    float3(1.0, 0.0, 0.0),
    float3(-1.0, 0.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 0.0, 1.0),
    float3(0.0, 0.0, -1.0)
};

float3 PointLightCubeMapping(int faceID, float2 uv)
{
    float3 dir = 0;
    switch (faceID)
    {
        case 0: //+X
            dir.x = 1.0;
            dir.y = uv.y * 2.0 - 1.0;
            dir.z = uv.x * -2.0 + 1.0;
            break;
        
        case 1: //-X
            dir.x = -1.0;
            dir.yz = uv.yx * 2.0 - 1.0;
            break;
        
        case 3: //+Y
            dir.x = uv.x * 2.0 - 1.0;
            dir.z = uv.y * -2.0 + 1.0;
            dir.y = 1.0;
            break;
            
        case 2: //-Y
            dir.xz = uv.xy * 2.0 - 1.0;
            dir.y = -1.0;
            break;
        
        case 4: //+Z
            dir.xy = uv.xy * 2.0 - 1.0;
            dir.z = 1.0;
            break;
        
        case 5: //-Z
            dir.x = uv.x * -2.0 + 1.0;
            dir.y = uv.y * 2.0 - 1.0;
            dir.z = -1.0;
            break;
    }
    dir.y = -dir.y;
    return dir;
}

#endif