#ifndef YPIPELINE_MATERIAL_BLENDING_LIBRARY_INCLUDED
#define YPIPELINE_MATERIAL_BLENDING_LIBRARY_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Normal Blending in tangent space, From https://blog.selfshadow.com/publications/blending-in-detail/
// ----------------------------------------------------------------------------------------------------

float3 NormalBlending_Whiteout(float3 normal, float3 detailedNormal)
{
    return normalize(float3(normal.xy + detailedNormal.xy, normal.z * detailedNormal.z));
}

float3 NormalBlending_UDN(float3 normal, float3 detailedNormal)
{
    return normalize(float3(normal.xy + detailedNormal.xy, normal.z));
}

// Reoriented normal mapping
float3 NormalBlending_RNM(float3 normal, float3 detailedNormal)
{
    float3 t = normal * float3(2, 2, 2) + float3(-1, -1, 0);
    float3 u = detailedNormal * float3(-2, -2, 2) + float3(1, 1, -1);
    return normalize(t * dot(t, u) - u * t.z);
}


#endif