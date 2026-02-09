using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DeferredGeometryPass : PipelinePass
    {
        private class DeferredGeometryPassData
        {
            public RendererListHandle rendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<DeferredGeometryPassData>("GBuffer", out var passData))
            {
                RendererListDesc rendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_GBufferShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = new RenderQueueRange(2000, 2499), // 包括 Opaque 和 AlphaTest
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                passData.rendererList = data.renderGraph.CreateRendererList(rendererListDesc);
                builder.UseRendererList(passData.rendererList);
                
                builder.SetRenderAttachmentDepth(data.CameraDepthAttachment, AccessFlags.Read);
                builder.SetRenderAttachment(data.GBuffer0, 0, AccessFlags.Write);
                builder.SetRenderAttachment(data.GBuffer1, 1, AccessFlags.Write);
                builder.SetRenderAttachment(data.GBuffer2, 2, AccessFlags.Write);
                builder.SetRenderAttachment(data.GBuffer3, 3, AccessFlags.Write);
                
                builder.SetGlobalTextureAfterPass(data.GBuffer0, YPipelineShaderIDs.k_GBuffer0ID);
                builder.SetGlobalTextureAfterPass(data.GBuffer1, YPipelineShaderIDs.k_GBuffer1ID);
                builder.SetGlobalTextureAfterPass(data.GBuffer2, YPipelineShaderIDs.k_GBuffer2ID);
                builder.SetGlobalTextureAfterPass(data.GBuffer3, YPipelineShaderIDs.k_GBuffer3ID);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((DeferredGeometryPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }
    }
}