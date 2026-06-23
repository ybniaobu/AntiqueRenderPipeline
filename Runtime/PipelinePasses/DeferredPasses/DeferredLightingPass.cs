using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class DeferredLightingPass : PipelinePass
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
                
                if (data.isReflectionProbeAtlasCreated) builder.UseTexture(data.ReflectionProbeAtlas, AccessFlags.Read);
                if (data.isSunLightShadowAtlasCreated) builder.UseTexture(data.SunLightShadowAtlas, AccessFlags.Read);
                if (data.isPunctualLightShadowAtlasCreated) builder.UseTexture(data.PunctualLightShadowAtlas, AccessFlags.Read);
                if (data.isIrradianceTextureCreated) builder.UseTexture(data.IrradianceTexture, AccessFlags.Read);
                if (data.isAmbientOcclusionTextureCreated) builder.UseTexture(data.AmbientOcclusionTexture, AccessFlags.Read);

                builder.UseBuffer(data.PunctualLightStructuredBufferHandle , AccessFlags.Read);
                builder.UseBuffer(data.PunctualLightSliceStructuredBufferHandle , AccessFlags.Read);
                builder.UseBuffer(data.TileLightIndicesBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.TileReflectionProbeIndicesBufferHandle, AccessFlags.Read);
                
                builder.SetRenderAttachment(data.CameraColorAttachment, 0, AccessFlags.Write);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (DeferredLightingPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
                });
            }
        }
    }
}