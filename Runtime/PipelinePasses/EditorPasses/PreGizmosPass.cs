using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class PreGizmosPass : PipelinePass
    {
        private class GizmosPassData
        {
            public TextureHandle colorAttachment;
            public TextureHandle depthAttachment;
            public RendererListHandle preGizmosRendererList;
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
#endif
            {
                using (var builder = data.renderGraph.AddUnsafePass<GizmosPassData>("Draw Pre Image Gizmo", out var passData))
                {
                    passData.colorAttachment = data.CameraColorAttachment;
                    passData.depthAttachment = data.CameraDepthAttachment;
                    builder.UseTexture(passData.colorAttachment, AccessFlags.Write);
                    builder.UseTexture(passData.depthAttachment, AccessFlags.Read);
                    
                    passData.preGizmosRendererList = data.renderGraph.CreateGizmoRendererList(data.camera, GizmoSubset.PreImageEffects);
                    builder.UseRendererList(passData.preGizmosRendererList);
                    
                    builder.AllowPassCulling(false);
                    
                    builder.SetRenderFunc(static (GizmosPassData data, UnsafeGraphContext context) =>
                    {
                        context.cmd.SetRenderTarget(data.colorAttachment, data.depthAttachment);
                        context.cmd.DrawRendererList(data.preGizmosRendererList);
                    });
                }
            }
        }
    }
}