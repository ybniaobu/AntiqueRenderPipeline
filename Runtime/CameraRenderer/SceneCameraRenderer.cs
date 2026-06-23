using UnityEngine.Rendering;

namespace YPipeline
{
    internal sealed class SceneCameraRenderer : CameraRenderer
    {
        protected override void Initialize(ref YPipelineData data)
        {
            m_CameraPipelineNodes.Clear();
            
            switch (data.asset.renderPath)
            {
                case RenderPath.ForwardPlus: 
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardResourcesPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ReflectionProbeAtlasPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CameraSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardThinGBufferPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CopyDepthPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<MotionVectorPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<LightDataPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ShadowAtlasPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<TiledLightCullingPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DownsamplePass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceAmbientOcclusionPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<NearFieldGlobalIlluminationPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceIrradiancePass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ForwardGeometryPass>(ref data));
                    break;
                case RenderPath.DeferredPlus:
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DeferredResourcesPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ReflectionProbeAtlasPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CameraSetupPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DepthOnlyPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<CopyDepthPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DeferredGeometryPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<MotionVectorPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<LightDataPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ShadowAtlasPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<TiledLightCullingPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DownsamplePass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceAmbientOcclusionPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<NearFieldGlobalIlluminationPass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<ScreenSpaceIrradiancePass>(ref data));
                    m_CameraPipelineNodes.Add(PipelinePass.Create<DeferredLightingPass>(ref data));
                    break;
            }
            
            m_CameraPipelineNodes.Add(PipelinePass.Create<ErrorMaterialPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<SkyboxPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<CopyColorPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<TransparencyPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<PreGizmosPass>(ref data));
            m_CameraPipelineNodes.Add(PipelinePass.Create<PostProcessingPass>(ref data));
#if UNITY_ASSERTIONS
            m_CameraPipelineNodes.Add(PipelinePass.Create<DebugPass>(ref data));
#endif
            m_CameraPipelineNodes.Add(PipelinePass.Create<PostGizmosPass>(ref data));
        }

        public override void Render(ref YPipelineData data)
        {
            if (!CullingUtils.SetupCullingParameter(ref data, out ScriptableCullingParameters cullingParameters))
                return;
            
            CullingUtils.EmitUIGeometry(data.camera);
            APVUtils.SetupAdaptiveProbeVolume(ref data);
            
            CullingUtils.Cull(ref data, ref cullingParameters);
            CullingUtils.CullShadowCasters(ref data);
            
            data.context.ExecuteCommandBuffer(data.cmd);
            data.context.Submit();
            data.cmd.Clear();
            
            RecordAndExecuteRenderGraph(ref data);
        }
    }
}