using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DeferredLightingPass : PipelinePass
    {
        private class DeferredLightingPassData
        {
            public Material material;
        }

        private Material m_DeferredLightingMaterial;

        protected override void Initialize(ref YPipelineData data)
        {
            m_DeferredLightingMaterial = new Material(data.runtimeResources.DeferredLightingShader);
            m_DeferredLightingMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        protected override void OnDispose()
        {
            CoreUtils.Destroy(m_DeferredLightingMaterial);
            m_DeferredLightingMaterial = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<DeferredLightingPassData>("Deferred Lighting", out var passData))
            {
                passData.material = m_DeferredLightingMaterial;
                
                builder.UseTexture(data.GBuffer0, AccessFlags.Read);
                builder.UseTexture(data.GBuffer1, AccessFlags.Read);
                builder.UseTexture(data.GBuffer2, AccessFlags.Read);
                builder.UseTexture(data.GBuffer3, AccessFlags.Read);
                
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
                
                builder.SetRenderAttachment(data.CameraColorAttachment, 0, AccessFlags.Write);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DeferredLightingPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
                });
            }
        }
    }
}