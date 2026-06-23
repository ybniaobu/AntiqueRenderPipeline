using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public sealed partial class YRenderPipeline : RenderPipeline
    {
        private YPipelineData m_Data;
        private GameCameraRenderer m_GameCameraRenderer;
        
#if UNITY_EDITOR
        private SceneCameraRenderer m_SceneCameraRenderer;
        private ReflectionCameraRenderer m_ReflectionCameraRenderer;
        private PreviewCameraRenderer m_PreviewCameraRenderer;
#endif
        
        public YRenderPipeline(YRenderPipelineAsset asset)
        {
            // YPipeline Data
            m_Data = new YPipelineData();
            m_Data.asset = asset;
            m_Data.runtimeResources = GraphicsSettings.GetRenderPipelineSettings<YPipelineRuntimeResources>();
            m_Data.renderGraph = new RenderGraph("YPipeline Render Graph");
            m_Data.lightData = new YPipelineLightData();
            m_Data.reflectionProbeData = new YPipelineReflectionProbeData();
            
#if UNITY_ASSERTIONS
            m_Data.debugSettings = new DebugSettings();
#endif
            
            // Initialization & Settings
            SetGraphicsAndQualitySettings();
            RTHandles.Initialize(Screen.width, Screen.height);
            var defaultVolumeProfileResource = GraphicsSettings.GetRenderPipelineSettings<YPipelineDefaultVolumeProfileResource>();
            VolumeManager.instance.Initialize(defaultVolumeProfileResource.volumeProfile, asset.globalVolumeProfile);
            BlitHelper.Initialize();

            // Camera Renderer
            m_GameCameraRenderer = CameraRenderer.Create<GameCameraRenderer>(ref m_Data);
            
#if UNITY_EDITOR
            m_SceneCameraRenderer = CameraRenderer.Create<SceneCameraRenderer>(ref m_Data);
            m_PreviewCameraRenderer = CameraRenderer.Create<PreviewCameraRenderer>(ref m_Data);
            m_ReflectionCameraRenderer = CameraRenderer.Create<ReflectionCameraRenderer>(ref m_Data);
            
            // Editor
            SetSupportedRenderingFeatures();
            SetLightmapper();
#endif
            
            // APV
            APVUtils.InitializeAPV(ref m_Data);
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (m_Data.isAPVEnabled) ProbeReferenceVolume.instance.Cleanup();
            
#if UNITY_EDITOR
            UnityEngine.Experimental.GlobalIllumination.Lightmapping.ResetDelegate();
            m_SceneCameraRenderer?.Dispose();
            m_SceneCameraRenderer = null;
            m_PreviewCameraRenderer?.Dispose();
            m_PreviewCameraRenderer = null;
            m_ReflectionCameraRenderer?.Dispose();
            m_ReflectionCameraRenderer = null;
#endif
            m_GameCameraRenderer?.Dispose();
            m_GameCameraRenderer = null;
            
            ConstantBuffer.ReleaseAll();
            VolumeManager.instance.Deinitialize();
            m_Data.Dispose();
            BlitHelper.Dispose();
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            using var profilingScope = new ProfilingScope(ProfilingSampler.Get(YPipelineProfileIDs.YPipelineTotal));
            m_Data.context = context;
            
            foreach(Camera camera in cameras)
            {
                m_Data.camera = camera;
                m_Data.cmd = CommandBufferPool.Get();
                VolumeManager.instance.Update(camera.transform, 1); // TODO：是否改为每个 camera 维持一个？

                switch (camera.cameraType)
                {
#if UNITY_EDITOR
                    case CameraType.SceneView: 
                        m_SceneCameraRenderer.Render(ref m_Data);
                        break;
                    case CameraType.Preview:
                        m_PreviewCameraRenderer.Render(ref m_Data);
                        break;
                    case CameraType.Reflection:  // TODO：反射探针不能用 depth prepass 渲染，效果不好 ！！！！！！！！！！！！！！
                        m_GameCameraRenderer.Render(ref m_Data);
                        break;
#endif
                    case CameraType.Game:
                        m_GameCameraRenderer.Render(ref m_Data);
                        break;
                    default:
                        m_GameCameraRenderer.Render(ref m_Data);
                        break;
                }
                
                m_Data.context.ExecuteCommandBuffer(m_Data.cmd);
                m_Data.context.Submit();
                m_Data.cmd.Clear();
                CommandBufferPool.Release(m_Data.cmd);
                m_Data.cmd = null;
            }
            
            m_Data.renderGraph.EndFrame();
        }

        private void SetGraphicsAndQualitySettings()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = m_Data.asset.enableSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            
            SupportedRenderingFeatures.active.rendersUIOverlay = true;
        }
    }
}