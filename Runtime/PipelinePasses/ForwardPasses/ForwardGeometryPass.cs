using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ForwardGeometryPass : PipelinePass
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

                if (data.isIrradianceTextureCreated) builder.UseTexture(data.IrradianceTexture, AccessFlags.Read);
                if (data.isAmbientOcclusionTextureCreated) builder.UseTexture(data.AmbientOcclusionTexture, AccessFlags.Read);
                if (data.isSunLightShadowMapCreated) builder.UseTexture(data.SunLightShadowMap, AccessFlags.Read);
                if (data.isPointLightShadowMapCreated) builder.UseTexture(data.PointLightShadowMap, AccessFlags.Read);
                if (data.isSpotLightShadowMapCreated) builder.UseTexture(data.SpotLightShadowMap, AccessFlags.Read);

                builder.UseBuffer(data.PunctualLightBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.PointLightShadowBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.PointLightShadowMatricesBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.SpotLightShadowBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.SpotLightShadowMatricesBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.TileLightIndicesBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.TileReflectionProbeIndicesBufferHandle, AccessFlags.Read);
               
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((ForwardGeometryPassData data, RasterGraphContext context) =>
                {
                    // context.cmd.SetRenderTarget(data.colorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    //     data.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    
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