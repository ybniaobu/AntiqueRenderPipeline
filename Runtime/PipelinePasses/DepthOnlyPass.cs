using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DepthOnlyPass : PipelinePass
    {
        private class DepthOnlyPassData
        {
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<DepthOnlyPassData>("Depth PrePass", out var passData))
            {
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_DepthShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_DepthShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);

                builder.SetRenderAttachmentDepth(data.CameraDepthAttachment, AccessFlags.ReadWrite);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DepthOnlyPassData data, RasterGraphContext context) =>
                {
                    // context.cmd.SetRenderTarget(data.depthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                });
            }
        }
    }
}