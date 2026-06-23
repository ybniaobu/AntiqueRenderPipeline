using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class GameGizmosPass : PipelinePass
    {
        private class GizmosPassData
        {
            public TextureHandle depthAttachment;
            public TextureHandle cameraDepthTarget;
            public TextureHandle cameraColorTarget;
            
            public RendererListHandle preGizmosRendererList;
            public RendererListHandle postGizmosRendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
#endif
            {
                using (var builder = data.renderGraph.AddUnsafePass<GizmosPassData>("Draw Gizmos", out var passData))
                {
                    passData.depthAttachment = data.CameraDepthAttachment;
                    passData.cameraColorTarget = data.CameraColorTarget;
                    passData.cameraDepthTarget = data.CameraDepthTarget;
                    builder.UseTexture(passData.depthAttachment, AccessFlags.Read);
                    builder.UseTexture(passData.cameraColorTarget, AccessFlags.Write);
                    builder.UseTexture(passData.cameraDepthTarget, AccessFlags.Write);
                    
                    passData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects); // grid
                    builder.UseRendererList(passData.preGizmosRendererList);
                    passData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects); // icons
                    builder.UseRendererList(passData.postGizmosRendererList);
                    
                    builder.SetRenderFunc(static (GizmosPassData data, UnsafeGraphContext context) =>
                    {
                        BlitHelper.CopyDepth(context.cmd, data.depthAttachment, data.cameraDepthTarget);
                        
                        context.cmd.SetRenderTarget(data.cameraColorTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                            data.cameraDepthTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                        context.cmd.DrawRendererList(data.preGizmosRendererList);
                        context.cmd.DrawRendererList(data.postGizmosRendererList);
                    });
                }
            }
        }
    }
}