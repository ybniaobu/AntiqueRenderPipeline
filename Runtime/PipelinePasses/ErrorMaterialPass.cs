using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ErrorMaterialPass : PipelinePass
    {
        private class ErrorMaterialPassData
        {
            public RendererListHandle rendererList;
        }
        
        private Material m_ErrorMaterial;
        
        protected override void Initialize(ref YPipelineData data) { }

        protected override void OnDispose()
        {
            CoreUtils.Destroy(m_ErrorMaterial);
            m_ErrorMaterial = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<ErrorMaterialPassData>("Draw Error Material", out var passData))
            {
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

                passData.rendererList = data.renderGraph.CreateRendererList(rendererListDesc);
                builder.UseRendererList(passData.rendererList);
                
                builder.SetRenderAttachment(data.CameraColorAttachment, 0);
                builder.SetRenderAttachmentDepth(data.CameraDepthAttachment, AccessFlags.Read);
                
                builder.SetRenderFunc((ErrorMaterialPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }
    }
}