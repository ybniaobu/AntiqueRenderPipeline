using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class ForwardGeometryPass : PipelinePass
    {
        private class ForwardGeometryPassData
        {
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }

        protected override void Initialize(ref YPipelineData data) { }

        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<ForwardGeometryPassData>("Draw Opaque & AlphaTest", out var passData))
            {
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardOpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.Lightmaps,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardOpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.Lightmaps,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);

                builder.SetRenderAttachment(data.CameraColorAttachment, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(data.CameraDepthAttachment, AccessFlags.Read);

                if (data.isReflectionProbeAtlasCreated) builder.UseTexture(data.ReflectionProbeAtlas, AccessFlags.Read);
                if (data.isSunLightShadowAtlasCreated) builder.UseTexture(data.SunLightShadowAtlas, AccessFlags.Read);
                if (data.isPunctualLightShadowAtlasCreated) builder.UseTexture(data.PunctualLightShadowAtlas, AccessFlags.Read);
                if (data.isIrradianceTextureCreated) builder.UseTexture(data.IrradianceTexture, AccessFlags.Read);
                if (data.isAmbientOcclusionTextureCreated) builder.UseTexture(data.AmbientOcclusionTexture, AccessFlags.Read);

                builder.UseBuffer(data.PunctualLightStructuredBufferHandle , AccessFlags.Read);
                builder.UseBuffer(data.PunctualLightSliceStructuredBufferHandle , AccessFlags.Read);
                builder.UseBuffer(data.TileLightIndicesBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.TileReflectionProbeIndicesBufferHandle, AccessFlags.Read);
               
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (ForwardGeometryPassData data, RasterGraphContext context) =>
                {
                    context.cmd.BeginSample("Draw Opaque");
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.EndSample("Draw Opaque");
                    
                    context.cmd.BeginSample("Draw AlphaTest");
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.cmd.EndSample("Draw AlphaTest");
                });
            }
        }
    }
}