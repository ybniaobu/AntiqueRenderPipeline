using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class PreviewFinalPass : PipelinePass
    {
        private class FinalPassData
        {
            public RendererListHandle preGizmosRendererList;
            public RendererListHandle postGizmosRendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
#if UNITY_EDITOR
            using (var builder = data.renderGraph.AddUnsafePass<FinalPassData>("Preview Final", out var passData))
            {
                builder.SetRenderAttachment(data.CameraColorTarget, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(data.CameraDepthTarget, AccessFlags.Read);
                
                passData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                builder.UseRendererList(passData.preGizmosRendererList);
                passData.postGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PostImageEffects);
                builder.UseRendererList(passData.postGizmosRendererList);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((FinalPassData data, UnsafeGraphContext context) =>
                {
                    if (Handles.ShouldRenderGizmos()) context.cmd.DrawRendererList(data.preGizmosRendererList);
                    if (Handles.ShouldRenderGizmos()) context.cmd.DrawRendererList(data.postGizmosRendererList);
                });
            }
#endif
        }
    }
}