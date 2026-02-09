using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineRuntimeResources : IRenderPipelineResources 
    {
        [SerializeField][HideInInspector] private int m_Version = 1;
        public int version => m_Version;
        
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;
        
        // ----------------------------------------------------------------------------------------------------
        // Textures
        // ----------------------------------------------------------------------------------------------------

        #region Textures
        
        [SerializeField] [ResourcePath("PipelineResources/Textures/EnvBRDFLut.exr")]
        private Texture2D m_EnvironmentBRDFLut;
        public Texture2D EnvironmentBRDFLut
        {
            get => m_EnvironmentBRDFLut;
            set => this.SetValueAndNotify(ref m_EnvironmentBRDFLut, value, nameof(m_EnvironmentBRDFLut));
        }
        
        [SerializeField] [ResourcePaths(new[]
        {
            "PipelineResources/Textures/FilmGrain/FilmGrainThin01.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainThin02.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainMedium01.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainMedium02.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainMedium03.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainMedium04.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainMedium05.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainMedium06.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainLarge01.png",
            "PipelineResources/Textures/FilmGrain/FilmGrainLarge02.png"
        })]
        private Texture2D[] m_FilmGrainTex;
        public Texture2D[] FilmGrainTex
        {
            get => m_FilmGrainTex;
            set => this.SetValueAndNotify(ref m_FilmGrainTex, value, nameof(m_FilmGrainTex));
        }
        
        [SerializeField] [ResourcePath("PipelineResources/Textures/BlueNoise/BlueNoise16.png")]
        private Texture2D m_BlueNoise16;
        public Texture2D BlueNoise16
        {
            get => m_BlueNoise16;
            set => this.SetValueAndNotify(ref m_BlueNoise16, value, nameof(m_BlueNoise16));
        }
        
        
        // [SerializeField]
        // private Texture2D m_BlueNoise32;
        // public Texture2D BlueNoise32
        // {
        //     get => m_BlueNoise32;
        //     set => this.SetValueAndNotify(ref m_BlueNoise32, value, nameof(m_BlueNoise32));
        // }
        
        [SerializeField] [ResourcePath("PipelineResources/Textures/BlueNoise/BlueNoise64.png")]
        private Texture2D m_BlueNoise64;
        public Texture2D BlueNoise64
        {
            get => m_BlueNoise64;
            set => this.SetValueAndNotify(ref m_BlueNoise64, value, nameof(m_BlueNoise64));
        }
        
        [SerializeField] [ResourcePath("PipelineResources/Textures/STBN/STBN128_scalar3.png")]
        private Texture2D m_STBN128Scale3;
        public Texture2D STBN128Scale3
        {
            get => m_STBN128Scale3;
            set => this.SetValueAndNotify(ref m_STBN128Scale3, value, nameof(m_STBN128Scale3));
        }
        
        [SerializeField] [ResourcePath("PipelineResources/Textures/STBN/STBN128_unitvec3.png")]
        private Texture2D m_STBN128UnitVec3;
        public Texture2D STBN128UnitVec3
        {
            get => m_STBN128UnitVec3;
            set => this.SetValueAndNotify(ref m_STBN128UnitVec3, value, nameof(m_STBN128UnitVec3));
        }
        
        [SerializeField] [ResourcePath("PipelineResources/Textures/STBN/STBN128_unitvec3_cosine.png")]
        private Texture2D m_STBN128CosineUnitVec3;
        public Texture2D STBN128CosineUnitVec3
        {
            get => m_STBN128CosineUnitVec3;
            set => this.SetValueAndNotify(ref m_STBN128CosineUnitVec3, value, nameof(m_STBN128CosineUnitVec3));
        }
        
        [SerializeField] [ResourcePath("PipelineResources/Textures/STBN/STBN128_vec3.png")]
        private Texture2D m_STBN128Vec3;
        public Texture2D STBN128Vec3
        {
            get => m_STBN128Vec3;
            set => this.SetValueAndNotify(ref m_STBN128Vec3, value, nameof(m_STBN128Vec3));
        }
        
        #endregion
        
        // ----------------------------------------------------------------------------------------------------
        // Shaders 
        // ----------------------------------------------------------------------------------------------------

        #region Shaders

        [SerializeField] [ResourcePath("Shaders/MaterialModels/StandardPBR/StandardPBR.shader")]
        private Shader m_DefaultPBRShader;
        public Shader DefaultPBRShader
        {
            get => m_DefaultPBRShader;
            set => this.SetValueAndNotify(ref m_DefaultPBRShader, value, nameof(m_DefaultPBRShader));
        }

        [SerializeField] [ResourcePath("Shaders/PostProcessing/Copy.shader")]
        private Shader m_CopyShader;
        public Shader CopyShader
        {
            get => m_CopyShader;
            set => this.SetValueAndNotify(ref m_CopyShader, value, nameof(m_CopyShader));
        }
        
        [SerializeField] [ResourcePath("Shaders/Utilities/CopyDepth.shader")]
        private Shader m_CopyDepthShader;
        public Shader CopyDepthShader
        {
            get => m_CopyDepthShader;
            set => this.SetValueAndNotify(ref m_CopyDepthShader, value, nameof(m_CopyDepthShader));
        }
        
        [SerializeField] [ResourcePath("Shaders/PipelineShader/CameraMotionVector/CameraMotionVector.shader")]
        private Shader m_CameraMotionVectorShader;
        public Shader CameraMotionVectorShader
        {
            get => m_CameraMotionVectorShader;
            set => this.SetValueAndNotify(ref m_CameraMotionVectorShader, value, nameof(m_CameraMotionVectorShader));
        }

        [SerializeField] [ResourcePath("Shaders/PipelineShader/DeferredLighting/DeferredLighting.shader")]
        private Shader m_DeferredLightingShader;
        public Shader DeferredLightingShader
        {
            get => m_DeferredLightingShader;
            set => this.SetValueAndNotify(ref m_DeferredLightingShader, value, nameof(m_DeferredLightingShader));
        }

        [SerializeField] [ResourcePath("Shaders/PostProcessing/TAA.shader")]
        private Shader m_TAAShader;
        public Shader TAAShader
        {
            get => m_TAAShader;
            set => this.SetValueAndNotify(ref m_TAAShader, value, nameof(m_TAAShader));
        }

        [SerializeField] [ResourcePath("Shaders/PostProcessing/Bloom.shader")]
        private Shader m_BloomShader;
        public Shader BloomShader
        {
            get => m_BloomShader;
            set => this.SetValueAndNotify(ref m_BloomShader, value, nameof(m_BloomShader));
        }

        [SerializeField] [ResourcePath("Shaders/PostProcessing/ColorGradingLut.shader")]
        private Shader m_ColorGradingLutShader;
        public Shader ColorGradingLutShader
        {
            get => m_ColorGradingLutShader;
            set => this.SetValueAndNotify(ref m_ColorGradingLutShader, value, nameof(m_ColorGradingLutShader));
        }
        
        [SerializeField] [ResourcePath("Shaders/PostProcessing/UberPostProcessing.shader")]
        private Shader m_UberPostProcessingShader;
        public Shader UberPostProcessingShader
        {
            get => m_UberPostProcessingShader;
            set => this.SetValueAndNotify(ref m_UberPostProcessingShader, value, nameof(m_UberPostProcessingShader));
        }

        [SerializeField] [ResourcePath("Shaders/PostProcessing/FinalPostProcessing.shader")]
        private Shader m_FinalPostProcessing;
        public Shader FinalPostProcessing
        {
            get => m_FinalPostProcessing;
            set => this.SetValueAndNotify(ref m_FinalPostProcessing, value, nameof(m_FinalPostProcessing));
        }

        #endregion
        
        // ----------------------------------------------------------------------------------------------------
        // Compute Shaders
        // ----------------------------------------------------------------------------------------------------
        
        #region Compute Shaders

        [SerializeField] [ResourcePath("Shaders/PipelineShader/DownSample/DownSample.compute")]
        private ComputeShader m_DownSampleCS;
        public ComputeShader DownSampleCS
        {
            get => m_DownSampleCS;
            set => this.SetValueAndNotify(ref m_DownSampleCS, value, nameof(m_DownSampleCS));
        }
        
        [SerializeField] [ResourcePath("Shaders/PipelineShader/LightCulling/TiledLightCulling.compute")]
        private ComputeShader m_TiledLightCullingCS;
        public ComputeShader TiledLightCullingCS
        {
            get => m_TiledLightCullingCS;
            set => this.SetValueAndNotify(ref m_TiledLightCullingCS, value, nameof(m_TiledLightCullingCS));
        }
        
        [SerializeField] [ResourcePath("Shaders/PipelineShader/GlobalIllumination/HBIL.compute")]
        private ComputeShader m_HBILCS;
        public ComputeShader HBILCS
        {
            get => m_HBILCS;
            set => this.SetValueAndNotify(ref m_HBILCS, value, nameof(m_HBILCS));
        }

        [SerializeField] [ResourcePath("Shaders/PipelineShader/GlobalIllumination/SSGIDenoise.compute")]
        private ComputeShader m_SSGIDenoiseCS;
        public ComputeShader SSGIDenoiseCS
        {
            get => m_SSGIDenoiseCS;
            set => this.SetValueAndNotify(ref m_SSGIDenoiseCS, value, nameof(m_SSGIDenoiseCS));
        }
        
        [SerializeField] [ResourcePath("Shaders/PipelineShader/GlobalIllumination/SSAO.compute")]
        private ComputeShader m_SSAOCS;
        public ComputeShader SSAOCS
        {
            get => m_SSAOCS;
            set => this.SetValueAndNotify(ref m_SSAOCS, value, nameof(m_SSAOCS));
        }
        
        [SerializeField] [ResourcePath("Shaders/PipelineShader/GlobalIllumination/SSAODenoise.compute")]
        private ComputeShader m_SSAODenoiseCS;
        public ComputeShader SSAODenoiseCS
        {
            get => m_SSAODenoiseCS;
            set => this.SetValueAndNotify(ref m_SSAODenoiseCS, value, nameof(m_SSAOCS));
        }
        
        #endregion
    }
}