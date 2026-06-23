using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Serialization;

namespace YPipeline
{
    [CreateAssetMenu(menuName = "YPipeline/YRenderPipelineAsset")]
    public sealed class YRenderPipelineAsset : RenderPipelineAsset<YRenderPipeline>, IProbeVolumeEnabledRenderPipeline, IRenderGraphEnabledRenderPipeline
    {
        // ----------------------------------------------------------------------------------------------------
        // RenderPipelineAsset
        // ----------------------------------------------------------------------------------------------------
        
        // RenderPipelineAsset<YRenderPipeline>
        public override string renderPipelineShaderTag => string.Empty;
        
        public override Shader defaultShader => 
            GraphicsSettings.TryGetRenderPipelineSettings<YPipelineRuntimeResources>(out var resources) ? 
                resources.DefaultPBRShader : null;
        
#if UNITY_EDITOR
        private YPipelineEditorResources EditorResources => GraphicsSettings.GetRenderPipelineSettings<YPipelineEditorResources>();
        
        public override Material defaultMaterial => EditorResources?.DefaultPBRMaterial;
        
#endif
        protected override RenderPipeline CreatePipeline()
        {
            return new YRenderPipeline(this);
        }

        protected override void EnsureGlobalSettings()
        {
            base.EnsureGlobalSettings();
#if UNITY_EDITOR
            YPipelineGlobalSettings.Ensure();
#endif
        }
        
        // IRenderGraphEnabledRenderPipeline
        public bool isImmediateModeSupported => false;
        
        // IProbeVolumeEnabledRenderPipeline
        public ProbeVolumeSHBands maxSHBands => probeVolumeSHBands;
        public bool supportProbeVolume => true;
        [Obsolete("This property is no longer necessary. #from(2023.3)")]
        public ProbeVolumeSceneData probeVolumeSceneData => null;


        // ----------------------------------------------------------------------------------------------------
        // 渲染配置 Rendering Settings
        // ----------------------------------------------------------------------------------------------------
        
        public RenderPath renderPath = RenderPath.DeferredPlus;
        public bool enableSRPBatcher = true;
        [Range(0.1f, 2f)] public float renderScale = 1.0f;
        
        public AntiAliasingMode antiAliasingMode = AntiAliasingMode.TAA;
        public FXAAMode fxaaMode = FXAAMode.Quality;
        
        // ----------------------------------------------------------------------------------------------------
        // 光照配置 Lighting Settings
        // ----------------------------------------------------------------------------------------------------
        
        // Light Culling
        public bool enableSplitDepth = true; // 2.5D culling
        
        // Reflection Probe
        public HDRFormat reflectionProbeAtlasFormat = HDRFormat.R11G11B10;
        public ReflectionProbeAtlasSize reflectionProbeAtlasSize = ReflectionProbeAtlasSize._4096;
        [Range(4, 16)] public int maxReflectionProbesOnScreen = 8;
        public Quality3Tier reflectionProbeQuality = Quality3Tier.Medium;
        
        // Global Illumination
        public bool enableScreenSpaceAmbientOcclusion = true;
        public SSGIMode ssgiMode = SSGIMode.None;
        public bool enableScreenSpaceReflection = false;
        
        // APV
        public bool enableProbeVolumeScreenSpaceIrradiance = true;
        public ProbeVolumeSHBands probeVolumeSHBands = ProbeVolumeSHBands.SphericalHarmonicsL1;
        public ProbeVolumeTextureMemoryBudget probeVolumeMemoryBudget = ProbeVolumeTextureMemoryBudget.MemoryBudgetMedium;
        public bool supportProbeVolumeGPUStreaming = true;
        public bool supportProbeVolumeDiskStreaming = false;
        public bool supportProbeVolumeScenarios = true;
        public bool supportProbeVolumeScenarioBlending = true;
        public ProbeVolumeBlendingTextureMemoryBudget probeVolumeBlendingMemoryBudget = ProbeVolumeBlendingTextureMemoryBudget.MemoryBudgetMedium;
        
        // ----------------------------------------------------------------------------------------------------
        // 阴影配置 Shadow Settings
        // ----------------------------------------------------------------------------------------------------
        public ShadowMode shadowMode = ShadowMode.PCSS;
        
        public SunLightShadowAtlasSize sunLightShadowAtlasSize = SunLightShadowAtlasSize._4096;
        public float maxShadowDistance = 100.0f;
        [Range(0f, 1f)] public float distanceFade = 0.05f;
        [Range(1, 4)] public int cascadeCount = 4;
        [SerializeField]
        [Range(0f, 1f)] private float spiltRatio1 = 0.05f, spiltRatio2 = 0.15f, spiltRatio3 = 0.45f;
        public Vector3 SpiltRatios => new Vector3(spiltRatio1, spiltRatio2, spiltRatio3);
        
        public PunctualLightShadowAtlasSize punctualLightShadowAtlasSize = PunctualLightShadowAtlasSize._8192x4096;
        public Quality3Tier punctualLightShadowQuality = Quality3Tier.Medium;
        
        // ----------------------------------------------------------------------------------------------------
        // 后处理配置 Post Processing Settings
        // ----------------------------------------------------------------------------------------------------
        
        public VolumeProfile globalVolumeProfile;
        
        public int bakedLUTResolution = 32;
    }
}