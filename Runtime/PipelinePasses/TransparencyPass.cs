using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class TransparencyPass : PipelinePass
    {
        private class TransparencyPassData
        {
            public RendererListHandle transparencyRendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<TransparencyPassData>("Draw Transparency", out var passData))
            {
                RendererListDesc transparencyRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardTransparencyShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe,
                    renderQueueRange = RenderQueueRange.transparent,
                    sortingCriteria = SortingCriteria.CommonTransparent
                };
                
                passData.transparencyRendererList = data.renderGraph.CreateRendererList(transparencyRendererListDesc);
                builder.UseRendererList(passData.transparencyRendererList);
                
                builder.UseTexture(data.CameraColorTexture, AccessFlags.Read);
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(data.CameraColorAttachment, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(data.CameraDepthAttachment, AccessFlags.Read);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((TransparencyPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.transparencyRendererList);
                });
            }
        }
    }
}