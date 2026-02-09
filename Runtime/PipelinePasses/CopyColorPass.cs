using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace YPipeline
{
    public class CopyColorPass : PipelinePass
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
            // 注意 CopyTexture 的兼容问题，看情况是否改为 AddBlitPass。
            using (var builder = data.renderGraph.AddUnsafePass<CopyColorPassData>("Copy Color", out var passData))
            {
                passData.colorAttachment = data.CameraColorAttachment;
                builder.UseTexture(data.CameraColorAttachment, AccessFlags.Read);
                passData.colorTexture = data.CameraColorTexture;
                builder.UseTexture(data.CameraColorTexture, AccessFlags.Write);
                
                builder.SetGlobalTextureAfterPass(data.CameraColorTexture, YPipelineShaderIDs.k_ColorTextureID);
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((CopyColorPassData data, UnsafeGraphContext context) =>
                {
                    // bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
                    // if (copyTextureSupported) context.cmd.CopyTexture(data.colorAttachment, data.colorTexture);
                    // else BlitUtility.BlitTexture(context.cmd, data.colorAttachment, data.colorTexture);
                    
                    context.cmd.CopyTexture(data.colorAttachment, data.colorTexture);
                });
            }
        }
    }
}