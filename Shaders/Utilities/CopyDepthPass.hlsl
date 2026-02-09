#ifndef YPIPELINE_COPY_DEPTH_PASS_INCLUDED
#define YPIPELINE_COPY_DEPTH_PASS_INCLUDED

#include "../PostProcessing/CopyPass.hlsl"

float CopyDepthFrag(Varyings IN) : SV_DEPTH
{
    return LOAD_TEXTURE2D_LOD(_BlitTexture, IN.positionHCS.xy, 0).r;
    // return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_PointClamp, IN.uv, 0).r;
}

#endif