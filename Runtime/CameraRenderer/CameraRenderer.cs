using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal abstract class CameraRenderer : IDisposable
    {
        protected List<PipelinePass> m_CameraPipelineNodes = new List<PipelinePass>();
        
        public static T Create<T>(ref YPipelineData data) where T : CameraRenderer, new()
        {
            T node = new T();
            node.Initialize(ref data);
            return node;
        }

        protected abstract void Initialize(ref YPipelineData data);
        
        public abstract void Render(ref YPipelineData data);

        public void Dispose()
        {
            PipelinePass.Dispose(m_CameraPipelineNodes);
            m_CameraPipelineNodes.Clear();
            m_CameraPipelineNodes = null;
        }

        protected void RecordAndExecuteRenderGraph(ref YPipelineData data)
        {
            RenderGraphParameters renderGraphParams = new RenderGraphParameters
            {
                executionId = data.camera.GetEntityId(),
                generateDebugData = true,
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
                renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.BottomLeft,
            };
            
            try
            {
                data.renderGraph.BeginRecording(renderGraphParams);
                
                PipelinePass.Record(m_CameraPipelineNodes, ref data);
                
                data.renderGraph.EndRecordingAndExecute();
            }
            catch (Exception e)
            {
                if (data.renderGraph.ResetGraphAndLogException(e))
                    throw;
            }
        }
    }
}