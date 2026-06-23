using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace YPipeline
{
    internal sealed class CopyColorPass : PipelinePass
    {
        private class CopyColorPassData
        {
            public TextureHandle colorAttachment;
            public TextureHandle colorTexture;
        }

        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            if (SystemInfo.copyTextureSupport > CopyTextureSupport.None)
            {
                using (var builder = data.renderGraph.AddUnsafePass<CopyColorPassData>("Copy Color", out var passData))
                {
                    passData.colorAttachment = data.CameraColorAttachment;
                    builder.UseTexture(data.CameraColorAttachment, AccessFlags.Read);
                    passData.colorTexture = data.CameraColorTexture;
                    builder.UseTexture(data.CameraColorTexture, AccessFlags.Write);
                    
                    builder.SetGlobalTextureAfterPass(data.CameraColorTexture, YPipelineShaderIDs.k_ColorTextureID);
                    builder.AllowPassCulling(false);
                    
                    builder.SetRenderFunc(static (CopyColorPassData data, UnsafeGraphContext context) =>
                    {
                        context.cmd.CopyTexture(data.colorAttachment, data.colorTexture);
                    });
                }
            }
            else
            {
                using (var builder = data.renderGraph.AddRasterRenderPass<CopyColorPassData>("Copy Color", out var passData))
                {
                    passData.colorAttachment = data.CameraColorAttachment;
                    builder.UseTexture(passData.colorAttachment, AccessFlags.Read);
                    
                    builder.SetRenderAttachment(data.CameraColorTexture, 0, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(data.CameraColorTexture, YPipelineShaderIDs.k_ColorTextureID);
                    builder.AllowPassCulling(false);
                
                    builder.SetRenderFunc(static (CopyColorPassData data, RasterGraphContext context) => 
                    {
                        BlitHelper.BlitTexture(context.cmd, data.colorAttachment);
                    });
                }
            }
        }
    }
}