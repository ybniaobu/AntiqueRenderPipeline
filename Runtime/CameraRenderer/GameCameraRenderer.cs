using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class GameCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            m_CameraPipelineNodes.Clear();
            
            m_CameraPipelineNodes.Add(PipelinePass.Create<CullingPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<LightDataCollectPass>(ref data));
            
            switch (data.asset.renderPath)
            {
                case RenderPath.ForwardPlus: 
                    m_CameraPipelineNodes.Add(PipelinePass.Create<LightSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ReflectionProbeSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CameraSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardResourcesPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardThinGBufferPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CopyDepthPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<MotionVectorPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ShadowPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DownSamplePass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceGlobalIlluminationPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceAmbientOcclusionPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<TiledLightCullingPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardGeometryPass>(ref data));
                    break;
                case RenderPath.DeferredPlus:
                    m_CameraPipelineNodes.Add(PipelinePass.Create<LightSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ReflectionProbeSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CameraSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DeferredResourcesPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DepthOnlyPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CopyDepthPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DeferredGeometryPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<MotionVectorPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ShadowPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DownSamplePass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceGlobalIlluminationPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceAmbientOcclusionPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<TiledLightCullingPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DeferredLightingPass>(ref data));
                    break;
            }
            
#if UNITY_EDITOR
            m_CameraPipelineNodes.Add(PipelinePass.Create<ErrorMaterialPass>(ref data));
#endif
            m_CameraPipelineNodes.Add(PipelinePass.Create<SkyboxPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<CopyColorPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<TransparencyPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<PostProcessingPass>(ref data));
                    
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_CameraPipelineNodes.Add(PipelinePass.Create<DebugPass>(ref data));
#endif
            
#if UNITY_EDITOR
            m_CameraPipelineNodes.Add(PipelinePass.Create<GizmosPass>(ref data));
#endif
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