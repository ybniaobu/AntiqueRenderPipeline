using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineEditorResources : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 1;
        public int version => m_Version;
        
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => false;
        
        // ----------------------------------------------------------------------------------------------------
        // Default Materials
        // ----------------------------------------------------------------------------------------------------
        
        #region Materials
        
        [SerializeField] [ResourcePath("PipelineResources/DefaultMaterials/StandardPBR.mat")]
        private Material m_DefaultPBRMaterial;
        public Material DefaultPBRMaterial
        {
            get => m_DefaultPBRMaterial;
            set => this.SetValueAndNotify(ref m_DefaultPBRMaterial, value, nameof(m_DefaultPBRMaterial));
        }

        #endregion
        
        // ----------------------------------------------------------------------------------------------------
        // Reflection Probe Related
        // ----------------------------------------------------------------------------------------------------
        
        [SerializeField] [ResourcePath("Editor/Components/ReflectionProbe/EditorShader/OctahedralMapping.compute")]
        private ComputeShader m_OctahedralMappingCS;
        public ComputeShader OctahedralMappingCS
        {
            get => m_OctahedralMappingCS;
            set => this.SetValueAndNotify(ref m_OctahedralMappingCS, value, nameof(m_OctahedralMappingCS));
        }

        [SerializeField] [ResourcePath("Editor/Components/ReflectionProbe/EditorShader/CubemapSHCoefficients.compute")]
        private ComputeShader m_CubemapSHCoefficientsCS;
        public ComputeShader CubemapSHCoefficientsCS
        {
            get => m_CubemapSHCoefficientsCS;
            set => this.SetValueAndNotify(ref m_CubemapSHCoefficientsCS, value, nameof(m_CubemapSHCoefficientsCS));
        }
        
    }
}