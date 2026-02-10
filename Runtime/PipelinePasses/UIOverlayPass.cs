using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class UIOverlayPass : PipelinePass
    {
        private class UGUIPassData
        {
            public RendererListHandle uguiRendererList;
        }

        private class IMGUIPassData
        {
            public TextureHandle cameraColorTarget;
            public RendererListHandle imguiRendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            // TODO: Scene 和 Game 分开后删除
            if (data.camera.cameraType != CameraType.Game) return;
            
            using (var builder = data.renderGraph.AddRasterRenderPass<UGUIPassData>("Draw UGUI & UIToolkit", out var passData))
            {
                passData.uguiRendererList = data.renderGraph.CreateUIOverlayRendererList(data.camera, UISubset.UIToolkit_UGUI);
                builder.UseRendererList(passData.uguiRendererList);
                
                builder.SetRenderAttachment(data.CameraColorTarget, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(data.CameraDepthTarget, AccessFlags.ReadWrite);

                builder.SetRenderFunc((UGUIPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.uguiRendererList);
                });
            }

            // IMGUI 必须在 UnsafePass 里，不然会报错。
            // Render IMGUI overlay and software cursor in a UnsafePass
            // Doing so allow us to safely cover cases when graphics commands called through onGUI() in user scripts are not supported by RenderPass API
            // Besides, Vulkan backend doesn't support SetSRGWrite() in RenderPass API and we have some of them at IMGUI levels
            // Note, these specific UI calls doesn't need depth buffer unlike UIToolkit/uGUI
            using (var builder = data.renderGraph.AddUnsafePass<IMGUIPassData>("Draw IMGUI", out var passData))
            {
                passData.imguiRendererList = data.renderGraph.CreateUIOverlayRendererList(data.camera, UISubset.LowLevel);
                builder.UseRendererList(passData.imguiRendererList);
                
                builder.SetRenderAttachment(data.CameraColorTarget, 0, AccessFlags.Write);

                builder.SetRenderFunc((IMGUIPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.imguiRendererList);
                });
            }
        }
    }
}