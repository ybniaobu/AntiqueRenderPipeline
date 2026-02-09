using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class SkyboxPass : PipelinePass
    {
        private class SkyboxPassData
        {
            public RendererListHandle skyboxRendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<SkyboxPassData>("Draw Skybox", out var passData))
            {
                passData.skyboxRendererList = data.renderGraph.CreateSkyboxRendererList(data.camera);
                builder.UseRendererList(passData.skyboxRendererList);
                
                builder.SetRenderAttachment(data.CameraColorAttachment, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(data.CameraDepthAttachment, AccessFlags.Read);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((SkyboxPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.skyboxRendererList);
                });
            }
        }
    }
}