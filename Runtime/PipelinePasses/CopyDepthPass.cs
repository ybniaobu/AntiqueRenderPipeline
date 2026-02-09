using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace YPipeline
{
    public class CopyDepthPass : PipelinePass
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
            // 当前版本 AddCopyPass 无法复制深度格式贴图，暂时使用 UnsafePass
            using (var builder = data.renderGraph.AddUnsafePass<CopyDepthPassData>("Copy Depth", out var passData))
            {
                passData.depthAttachment = data.CameraDepthAttachment;
                builder.UseTexture(data.CameraDepthAttachment, AccessFlags.Read);
                passData.depthTexture = data.CameraDepthTexture;
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Write);
                
                builder.SetGlobalTextureAfterPass(data.CameraDepthTexture, YPipelineShaderIDs.k_DepthTextureID);
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((CopyDepthPassData data, UnsafeGraphContext context) =>
                {
                    // bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
                    // if (copyTextureSupported) context.cmd.CopyTexture(data.depthAttachment, data.depthTexture);
                    // else BlitUtility.CopyDepth(context.cmd, data.depthAttachment, data.depthTexture);
                    
                    context.cmd.CopyTexture(data.depthAttachment, data.depthTexture);
                });
            }
        }
    }
}