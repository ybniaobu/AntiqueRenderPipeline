using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal abstract partial class YBaseShaderGUI
    {
        protected static class PropertiesName
        {
            public const string k_BaseTex = "_BaseTex";
            public const string k_BaseColor = "_BaseColor";
            public const string k_SpecularIntensity = "_Specular";
            
            public const string k_Roughness = "_Roughness";
            public const string k_Metallic = "_Metallic";
            public const string k_RoughnessScale = "_RoughnessScale";
            public const string k_MetallicScale = "_MetallicScale";
            public const string k_AOScale = "_AOScale";
            public const string k_RoughnessTex = "_RoughnessTex";
            public const string k_MetallicTex = "_MetallicTex";
            public const string k_AOTex = "_AOTex";
            public const string k_HybridTex = "_HybridTex";
            
            public const string k_NormalTex = "_NormalTex";
            public const string k_NormalIntensity = "_NormalIntensity";
            
            public const string k_EmissionTex = "_EmissionTex";
            public const string k_EmissionColor = "_EmissionColor";
            
            public const string k_AlphaClipping = "_Clipping";
            public const string k_AlphaCutoff = "_Cutoff";
            
            public const string k_SrcBlend = "_SrcBlend";
            public const string k_DstBlend = "_DstBlend";
            public const string k_SrcBlendAlpha = "_SrcBlendAlpha";
            public const string k_DstBlendAlpha = "_DstBlendAlpha";
            public const string k_BlendOp = "_BlendOp";
            public const string k_AlphaToCoverage = "_AlphaToCoverage";
            
            public const string k_ZWrite = "_ZWrite";
            public const string k_ZTest = "_ZTest";
            
            public const string k_CullMode = "_Cull";
            
            public const string k_AddPrecomputedVelocity = "_AddPrecomputedVelocity";
        }
        
        // Common Material Properties
        protected MaterialProperty m_BaseTexProperty;
        protected MaterialProperty m_BaseColorProperty;
        
        protected MaterialProperty m_EmissionTexProperty;
        protected MaterialProperty m_EmissionColorProperty;
        
        protected MaterialProperty m_AlphaClippingProperty;
        protected MaterialProperty m_AlphaCutoffProperty;
        
        protected MaterialProperty m_SrcBlendProperty;
        protected MaterialProperty m_DstBlendProperty;
        protected MaterialProperty m_SrcBlendAlphaProperty;
        protected MaterialProperty m_DstBlendAlphaProperty;
        protected MaterialProperty m_BlendOpProperty;
        protected MaterialProperty m_AlphaToCoverageProperty;
        
        protected MaterialProperty m_ZWriteProperty;
        protected MaterialProperty m_ZTestProperty;
        
        protected MaterialProperty m_CullModeProperty;
        
        protected MaterialProperty m_AddPrecomputedVelocityProperty;
        
        protected virtual void FindProperties(MaterialProperty[] properties)
        {
            if (m_Material == null) return;
            
            m_BaseTexProperty = FindProperty(PropertiesName.k_BaseTex, properties, false);
            m_BaseColorProperty = FindProperty(PropertiesName.k_BaseColor, properties, false);
            
            m_EmissionTexProperty = FindProperty(PropertiesName.k_EmissionTex, properties, false);
            m_EmissionColorProperty = FindProperty(PropertiesName.k_EmissionColor, properties, false);
            
            m_AlphaClippingProperty = FindProperty(PropertiesName.k_AlphaClipping, properties, false);
            m_AlphaCutoffProperty = FindProperty(PropertiesName.k_AlphaCutoff, properties, false);
            
            m_SrcBlendProperty = FindProperty(PropertiesName.k_SrcBlend, properties, false);
            m_DstBlendProperty = FindProperty(PropertiesName.k_DstBlend, properties, false);
            m_SrcBlendAlphaProperty = FindProperty(PropertiesName.k_SrcBlendAlpha, properties, false);
            m_DstBlendAlphaProperty = FindProperty(PropertiesName.k_DstBlendAlpha, properties, false);
            m_BlendOpProperty = FindProperty(PropertiesName.k_BlendOp, properties, false);
            m_AlphaToCoverageProperty = FindProperty(PropertiesName.k_AlphaToCoverage, properties, false);
            
            m_ZWriteProperty = FindProperty(PropertiesName.k_ZWrite, properties, false);
            m_ZTestProperty = FindProperty(PropertiesName.k_ZTest, properties, false);
            
            m_CullModeProperty = FindProperty(PropertiesName.k_CullMode, properties, false);

            m_AddPrecomputedVelocityProperty = FindProperty(PropertiesName.k_AddPrecomputedVelocity, properties, false);
        }
    }
}