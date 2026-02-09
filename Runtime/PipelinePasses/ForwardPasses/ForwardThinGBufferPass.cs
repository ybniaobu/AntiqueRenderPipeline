using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ForwardThinGBufferPass : PipelinePass
    {
        private class ThinGBufferPassData
        {
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<ThinGBufferPassData>("Thin GBuffer", out var passData))
            {
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ThinGBufferShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.CommonOpaque
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ThinGBufferShaderTagId, data.cullingResults, data.camera)
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
                builder.SetRenderAttachment(data.ThinGBuffer, 0, AccessFlags.Write);
                
                builder.SetGlobalTextureAfterPass(data.ThinGBuffer, YPipelineShaderIDs.k_ThinGBufferID);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((ThinGBufferPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                });
            }
        }
    }
}