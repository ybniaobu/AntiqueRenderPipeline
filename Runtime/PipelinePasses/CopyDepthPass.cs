using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace YPipeline
{
    internal sealed class CopyDepthPass : PipelinePass
    {
        private class CopyDepthPassData
        {
            public TextureHandle depthAttachment;
            public TextureHandle depthTexture;
        }

        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }
        
        protected override void OnRecord(ref YPipelineData data)
        {
            if (SystemInfo.copyTextureSupport > CopyTextureSupport.None)
            {
                using (var builder = data.renderGraph.AddUnsafePass<CopyDepthPassData>("Copy Depth", out var passData))
                {
                    passData.depthAttachment = data.CameraDepthAttachment;
                    builder.UseTexture(data.CameraDepthAttachment, AccessFlags.Read);
                    passData.depthTexture = data.CameraDepthTexture;
                    builder.UseTexture(data.CameraDepthTexture, AccessFlags.Write);
            
                    builder.SetGlobalTextureAfterPass(data.CameraDepthTexture, YPipelineShaderIDs.k_DepthTextureID);
                    builder.AllowPassCulling(false);
            
                    builder.SetRenderFunc(static (CopyDepthPassData data, UnsafeGraphContext context) =>
                    {
                        context.cmd.CopyTexture(data.depthAttachment, data.depthTexture);
                    });
                }
            }
            else
            {
                using (var builder = data.renderGraph.AddRasterRenderPass<CopyDepthPassData>("Copy Depth", out var passData))
                {
                    passData.depthAttachment = data.CameraDepthAttachment;
                    builder.UseTexture(passData.depthAttachment, AccessFlags.Read);

                    builder.SetRenderAttachmentDepth(data.CameraDepthTexture, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(data.CameraDepthTexture, YPipelineShaderIDs.k_DepthTextureID);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (CopyDepthPassData data, RasterGraphContext context) =>
                    {
                        BlitHelper.CopyDepth(context.cmd, data.depthAttachment);
                    });
                }
            }
        }
    }
}