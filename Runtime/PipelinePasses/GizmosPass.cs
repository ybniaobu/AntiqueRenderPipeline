using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
namespace YPipeline
{
    public class GizmosPass : PipelinePass
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
            if (Handles.ShouldRenderGizmos())
            {
                using (var builder = data.renderGraph.AddUnsafePass<GizmosPassData>("Gizmos (Editor)", out var passData))
                {
                    passData.depthAttachment = data.CameraDepthAttachment;
                    passData.cameraColorTarget = data.CameraColorTarget;
                    passData.cameraDepthTarget = data.CameraDepthTarget;
                    builder.UseTexture(passData.depthAttachment, AccessFlags.Read);
                    builder.UseTexture(passData.cameraDepthTarget, AccessFlags.Write);
                    
                    passData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                    builder.UseRendererList(passData.preGizmosRendererList);
                    passData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                    builder.UseRendererList(passData.postGizmosRendererList);
                    
                    builder.SetRenderFunc((GizmosPassData data, UnsafeGraphContext context) =>
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
#endif