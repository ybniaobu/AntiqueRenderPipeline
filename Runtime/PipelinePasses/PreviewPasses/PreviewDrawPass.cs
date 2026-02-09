using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class PreviewDrawPass : PipelinePass
    {
        private class DrawPassData
        {
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
            public RendererListHandle errorRendererList;
            public RendererListHandle skyboxRendererList;
            public RendererListHandle transparencyRendererList;
        }
        
        private Material m_ErrorMaterial;

        protected override void Initialize(ref YPipelineData data) { }

        protected override void OnDispose()
        {
            base.OnDispose();
            CoreUtils.Destroy(m_ErrorMaterial);
            m_ErrorMaterial = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<DrawPassData>("Preview Draw", out var passData))
            {
                var stateBlock = new RenderStateBlock(RenderStateMask.Depth);
                stateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
                
                // opaque & alphaTest
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardOpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.LightProbe,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges,
                    stateBlock = stateBlock
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardOpaqueShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.LightProbe,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges,
                    stateBlock = stateBlock
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);
                
                // Error Material
                if (m_ErrorMaterial == null)
                {
                    m_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
                    m_ErrorMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                
                RendererListDesc rendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_LegacyShaderTagIds, data.cullingResults, data.camera)
                {
                    overrideMaterial = m_ErrorMaterial,
                    renderQueueRange = RenderQueueRange.all,
                };

                passData.errorRendererList = data.renderGraph.CreateRendererList(rendererListDesc);
                builder.UseRendererList(passData.errorRendererList);
                
                // Skybox
                passData.skyboxRendererList = data.renderGraph.CreateSkyboxRendererList(data.camera);
                builder.UseRendererList(passData.skyboxRendererList);
                
                // Transparency
                RendererListDesc transparencyRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_ForwardTransparencyShaderTagIds, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.ReflectionProbes | PerObjectData.LightProbe,
                    renderQueueRange = RenderQueueRange.transparent,
                    sortingCriteria = SortingCriteria.CommonTransparent,
                };
                
                passData.transparencyRendererList = data.renderGraph.CreateRendererList(transparencyRendererListDesc);
                builder.UseRendererList(passData.transparencyRendererList);
                
                // Set Color & Depth Buffer
                builder.SetRenderAttachment(data.CameraColorTarget, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(data.CameraDepthTarget, AccessFlags.ReadWrite);
                
                builder.UseBuffer(data.PunctualLightBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.PointLightShadowBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.PointLightShadowMatricesBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.SpotLightShadowBufferHandle, AccessFlags.Read);
                builder.UseBuffer(data.SpotLightShadowMatricesBufferHandle, AccessFlags.Read);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DrawPassData data, RasterGraphContext context) =>
                {
                    // context.cmd.SetRenderTarget(data.colorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    //     data.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    // context.cmd.ClearRenderTarget(true, true, Color.clear);
                    
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.cmd.DrawRendererList(data.errorRendererList);
                    context.cmd.DrawRendererList(data.skyboxRendererList);
                    context.cmd.DrawRendererList(data.transparencyRendererList);
                });
            }
        }
    }
}