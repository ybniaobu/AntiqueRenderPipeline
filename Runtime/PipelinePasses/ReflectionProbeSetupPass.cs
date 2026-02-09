using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ReflectionProbeSetupPass : PipelinePass
    {
        private class ReflectionProbeSetupPassData
        {
            public TextureHandle atlas;
            
            public int probeCount;
            
            public Vector4[] boxCenter;
            public Vector4[] boxExtent;
            public Vector4[] SH;
            public Vector4[] probeSampleParams;
            public Vector4[] probeParams;
            public Texture[] octahedralAtlas;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            // 当前版本 AddBlit/CopyPass 无法多次复制，暂时使用 RasterRenderPass
            using (var builder = data.renderGraph.AddRasterRenderPass<ReflectionProbeSetupPassData>("Set Reflection Probe Data", out var passData))
            {
                Vector2Int size = data.reflectionProbesData.atlasSize;
                bool hasAtlas = size.x != 0 && size.y != 0;
                
                if (hasAtlas)
                {
                    TextureDesc atlasDesc = new TextureDesc(size.x, size.y)
                    {
                        colorFormat = GraphicsFormat.B10G11R11_UFloatPack32,
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        clearBuffer = true,
                        clearColor = Color.clear,
                        name = "Reflection Probe Atlas"
                    };

                    passData.atlas = data.renderGraph.CreateTexture(atlasDesc);
                    builder.SetRenderAttachment(passData.atlas, 0, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(passData.atlas, YPipelineShaderIDs.k_ReflectionProbeAtlasID);
                }
                
                passData.probeCount = data.reflectionProbesData.probeCount;
                passData.boxCenter = data.reflectionProbesData.boxCenter;
                passData.boxExtent = data.reflectionProbesData.boxExtent;
                passData.SH = data.reflectionProbesData.SH;
                passData.probeSampleParams = data.reflectionProbesData.probeSampleParams;
                passData.probeParams = data.reflectionProbesData.probeParams;
                passData.octahedralAtlas = data.reflectionProbesData.octahedralAtlas;
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                builder.SetRenderFunc((ReflectionProbeSetupPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_ReflectionProbeCountID, new Vector4(data.probeCount, 0));
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeBoxCenterID, data.boxCenter);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeBoxExtentID, data.boxExtent);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeSHID, data.SH);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeSampleParamsID, data.probeSampleParams);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeParamsID, data.probeParams);
                    
                    for (int i = 0; i < data.probeCount; i++)
                    {
                        Vector4 probeParams = data.probeSampleParams[i];
                        Vector4 scaleOffset = new Vector4(1, 1, -probeParams.x, -probeParams.y);
                        Rect rect = new Rect(probeParams.x, probeParams.y,  probeParams.z * 1.5f, probeParams.z);
                        BlitHelper.BlitTexture(context.cmd, data.octahedralAtlas[i], rect, scaleOffset);
                    }
                });
            }
        }
    }
}