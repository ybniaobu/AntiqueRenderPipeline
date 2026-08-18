using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal partial class LitShaderGUI
    {
        protected MaterialProperty m_SpecularIntensityProperty;
        
        protected MaterialProperty m_RoughnessProperty;
        protected MaterialProperty m_MetallicProperty;
        protected MaterialProperty m_RoughnessScaleProperty;
        protected MaterialProperty m_MetallicScaleProperty;
        protected MaterialProperty m_AOScaleProperty;
        protected MaterialProperty m_RoughnessTexProperty;
        protected MaterialProperty m_MetallicTexProperty;
        protected MaterialProperty m_AOTexProperty;
        protected MaterialProperty m_HybridTexProperty;
        
        protected MaterialProperty m_NormalTexProperty;
        protected MaterialProperty m_NormalIntensityProperty;
        
        protected override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            m_SpecularIntensityProperty = FindProperty(PropertiesName.k_SpecularIntensity, properties, false);

            m_RoughnessProperty = FindProperty(PropertiesName.k_Roughness, properties, false);
            m_MetallicProperty = FindProperty(PropertiesName.k_Metallic, properties, false);
            m_RoughnessScaleProperty = FindProperty(PropertiesName.k_RoughnessScale, properties, false);
            m_MetallicScaleProperty = FindProperty(PropertiesName.k_MetallicScale, properties, false);
            m_AOScaleProperty = FindProperty(PropertiesName.k_AOScale, properties, false);
            m_RoughnessTexProperty = FindProperty(PropertiesName.k_RoughnessTex, properties, false);
            m_MetallicTexProperty = FindProperty(PropertiesName.k_MetallicTex, properties, false);
            m_AOTexProperty = FindProperty(PropertiesName.k_AOTex, properties, false);
            m_HybridTexProperty = FindProperty(PropertiesName.k_HybridTex, properties, false);

            m_NormalTexProperty = FindProperty(PropertiesName.k_NormalTex, properties, false);
            m_NormalIntensityProperty = FindProperty(PropertiesName.k_NormalIntensity, properties, false);
        }
    }
}