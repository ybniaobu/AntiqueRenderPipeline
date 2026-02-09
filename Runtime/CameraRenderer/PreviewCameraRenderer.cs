using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class PreviewCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            // TODO：专门给 Preview 写个 Pass 得了
            m_CameraPipelineNodes.Clear();
            m_CameraPipelineNodes.Add(PipelinePass.Create<CullingPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreviewSetupPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreviewDrawPass>(ref data));
            // m_CameraPipelineNodes.Add(PipelinePass.Create<CopyColorPass>(ref data)); // 绘制透明物体可能需要用到 color texture，之后可能需要添加
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreviewFinalPass>(ref data)); // 是否加个 FXAA
        }
        
        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Render(ref YPipelineData data)
        {
            base.Render(ref data);
        }
    }
}