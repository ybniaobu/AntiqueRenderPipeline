using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public abstract class CameraRenderer
    {
        protected List<PipelinePass> m_CameraPipelineNodes = new List<PipelinePass>();
        
        public static T Create<T>(ref YPipelineData data) where T : CameraRenderer, new()
        {
            T node = new T();
            node.Initialize(ref data);
            return node;
        }

        protected abstract void Initialize(ref YPipelineData data);

        public virtual void Dispose()
        {
            PipelinePass.Dispose(m_CameraPipelineNodes);
            m_CameraPipelineNodes.Clear();
            m_CameraPipelineNodes = null;
        }

        public virtual void Render(ref YPipelineData data)
        {
            RenderGraphParameters renderGraphParams = new RenderGraphParameters()
            {
                executionId = data.camera.GetEntityId(),
                generateDebugData = true,
                scriptableRenderContext = data.context,
                commandBuffer = data.cmd,
                currentFrameIndex = Time.frameCount,
                renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.BottomLeft,
                rendererListCulling = true
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