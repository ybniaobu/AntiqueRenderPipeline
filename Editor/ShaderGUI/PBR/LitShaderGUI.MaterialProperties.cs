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
        protected MaterialProperty m_HybridTexProperty;
        protected MaterialProperty m_RoughnessTexProperty;
        protected MaterialProperty m_MetallicTexProperty;
        protected MaterialProperty m_AOTexProperty;
        
        protected MaterialProperty m_NormalTexProperty;
        protected MaterialProperty m_NormalIntensityProperty;
        
        protected MaterialProperty m_AdvancedTexProperty;
        protected MaterialProperty m_AnisotropyProperty;
        protected MaterialProperty m_AnisotropyRotationProperty;
        protected MaterialProperty m_CoatProperty;
        protected MaterialProperty m_CoatRoughnessProperty;
        protected MaterialProperty m_CoatTexProperty;
        protected MaterialProperty m_CoatColorProperty;
        protected MaterialProperty m_CoatColorTexProperty;
        protected MaterialProperty m_CoatIORProperty;
        protected MaterialProperty m_CoatNormalTexProperty;
        protected MaterialProperty m_CoatNormalIntensityProperty;
        
        protected override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            m_SpecularIntensityProperty = FindProperty(PropertiesName.k_SpecularIntensity, properties, false);

            m_RoughnessProperty = FindProperty(PropertiesName.k_Roughness, properties, false);
            m_MetallicProperty = FindProperty(PropertiesName.k_Metallic, properties, false);
            m_HybridTexProperty = FindProperty(PropertiesName.k_HybridTex, properties, false);
            m_RoughnessTexProperty = FindProperty(PropertiesName.k_RoughnessTex, properties, false);
            m_MetallicTexProperty = FindProperty(PropertiesName.k_MetallicTex, properties, false);
            m_AOTexProperty = FindProperty(PropertiesName.k_AOTex, properties, false);

            m_NormalTexProperty = FindProperty(PropertiesName.k_NormalTex, properties, false);
            m_NormalIntensityProperty = FindProperty(PropertiesName.k_NormalIntensity, properties, false);

            m_AdvancedTexProperty = FindProperty(PropertiesName.k_AdvancedTex, properties, false);
            m_AnisotropyProperty = FindProperty(PropertiesName.k_Anisotropy, properties, false);
            m_AnisotropyRotationProperty = FindProperty(PropertiesName.k_AnisotropyRotation, properties, false);
            m_CoatProperty = FindProperty(PropertiesName.k_Coat, properties, false);
            m_CoatRoughnessProperty = FindProperty(PropertiesName.k_CoatRoughness, properties, false);
            m_CoatTexProperty = FindProperty(PropertiesName.k_CoatTex, properties, false);
            m_CoatColorProperty = FindProperty(PropertiesName.k_CoatColor, properties, false);
            m_CoatColorTexProperty = FindProperty(PropertiesName.k_CoatColorTex, properties, false);
            m_CoatIORProperty = FindProperty(PropertiesName.k_CoatIOR, properties, false);
            m_CoatNormalTexProperty = FindProperty(PropertiesName.k_CoatNormalTex, properties, false);
            m_CoatNormalIntensityProperty = FindProperty(PropertiesName.k_CoatNormalIntensity, properties, false);
        }
    }
}