#ifndef YPIPELINE_AMBIENT_OCCLUSION_LIBRARY_INCLUDED
#define YPIPELINE_AMBIENT_OCCLUSION_LIBRARY_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Multiple Bounces
// ----------------------------------------------------------------------------------------------------

// From SIGGRAPH 2016, Practical Realtime Strategies for Accurate Indirect Occlusion
// https://blog.selfshadow.com/publications/s2016-shading-course/activision/s2016_pbs_activision_occlusion.pdf
// 暂未使用该函数（感觉效果并不太好）
float3 AOMultiBounce(float ao, float3 albedo)
{
    float3 a =  2.0404 * albedo - 0.3324;
    float3 b = -4.7951 * albedo + 0.6417;
    float3 c =  2.7552 * albedo + 0.6903;

    return max(ao, ((ao * a + b) * ao + c) * ao);
}

// ----------------------------------------------------------------------------------------------------
// Horizon Specular Occlusion
// ----------------------------------------------------------------------------------------------------

// could also apply to direct lighting.
// geoN here MUST be the geometric vertex normal, not the normal map modified normal.
// the reflection vector (R) internally uses the normal-map value.
// so it cannot be applied in deferred pipeline, as the geometric normal is not available in the deferred lighting pass
float ComputeHorizonSpecularOcclusion(float3 R, float3 geoN)
{
    float horizon = saturate(1.0 + dot(R, geoN));
    return horizon * horizon;
}

// ----------------------------------------------------------------------------------------------------
// Approximate Specular Occlusion
// ----------------------------------------------------------------------------------------------------

// From SIGGRAPH 2014, Moving Frostbite to Physically Based Rendering, Chapter 4.10 Shadow and occlusion
// https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/course-notes-moving-frostbite-to-pbr-v32.pdf
float ComputeSpecularAO(float NoV, float ao, float alphaRoughness)
{
    return saturate(pow(abs(NoV + ao), exp2(-16.0 * alphaRoughness - 1.0)) - 1.0 + ao);
}




#endif