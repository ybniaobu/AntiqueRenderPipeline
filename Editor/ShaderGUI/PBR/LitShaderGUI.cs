using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal partial class LitShaderGUI : YBaseShaderGUI
    {
        // ----------------------------------------------------------------------------------------------------
        // Drawing Functions
        // ----------------------------------------------------------------------------------------------------
        
        protected void DrawAlbedoTexAndReflectance()
        {
            DrawBaseTexAndColor();
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_SpecularIntensityProperty, k_SpecularIntensityText);
            EditorGUI.indentLevel -= 2;
        }

        protected void DrawRoughnessTex()
        {
            DrawTextureProperty(m_RoughnessTexProperty, k_RoughnessTexText);
            
            EditorGUI.indentLevel += 2;
            if (m_RoughnessTexProperty.textureValue != null)
            {
                DrawShaderProperty(m_RoughnessScaleProperty, k_RoughnessScaleText);
            }
            else
            {
                DrawShaderProperty(m_RoughnessProperty, k_RoughnessText);
            }
            EditorGUI.indentLevel -= 2;
        }
        
        protected void DrawMetallicTex()
        {
            DrawTextureProperty(m_MetallicTexProperty, k_MetallicTexText);
            
            EditorGUI.indentLevel += 2;
            if (m_MetallicTexProperty.textureValue != null)
            {
                DrawShaderProperty(m_MetallicScaleProperty, k_MetallicScaleText);
            }
            else
            {
                DrawShaderProperty(m_MetallicProperty, k_MetallicText);
            }
            EditorGUI.indentLevel -= 2;
        }
        
        protected void DrawAOTex()
        {
            DrawTextureProperty(m_AOTexProperty, k_AOTexText);
            
            bool aoEnabled = m_AOTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(!aoEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_AOScaleProperty, k_AOScaleText);
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
        }
        
        protected void DrawHybridTex()
        {
            DrawTextureProperty(m_HybridTexProperty, k_HybridTexText);
            
            EditorGUI.indentLevel += 2;
            if (m_HybridTexProperty.textureValue != null)
            {
                DrawShaderProperty(m_RoughnessScaleProperty, k_RoughnessScaleText);
                DrawShaderProperty(m_MetallicScaleProperty, k_MetallicScaleText);
                DrawShaderProperty(m_AOScaleProperty, k_AOScaleText);
            }
            else
            {
                DrawShaderProperty(m_RoughnessProperty, k_RoughnessText);
                DrawShaderProperty(m_MetallicProperty, k_MetallicText);
            }
            EditorGUI.indentLevel -= 2;
        }
        
        protected void DrawNormalTex()
        {
            DrawTextureProperty(m_NormalTexProperty, k_NormalTexText);
            
            bool normalEnabled = m_NormalTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(!normalEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_NormalIntensityProperty, k_NormalIntensityText);
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Keywords Setting Functions
        // ----------------------------------------------------------------------------------------------------

        protected override void SetMaterialKeywords(Material material)
        {
            base.SetMaterialKeywords(material);
            
            if (material.HasProperty(PropertiesName.k_HybridTex)) CoreUtils.SetKeyword(material, "_USE_HYBRIDTEX", material.GetTexture(PropertiesName.k_HybridTex));
            if (material.HasProperty(PropertiesName.k_NormalTex)) CoreUtils.SetKeyword(material, "_USE_NORMALTEX", material.GetTexture(PropertiesName.k_NormalTex));
            if (material.HasProperty(PropertiesName.k_RoughnessTex)) CoreUtils.SetKeyword(material, "_USE_ROUGHNESSTEX", material.GetTexture(PropertiesName.k_RoughnessTex));
            if (material.HasProperty(PropertiesName.k_MetallicTex)) CoreUtils.SetKeyword(material, "_USE_METALLICTEX", material.GetTexture(PropertiesName.k_MetallicTex));
        }
    }
}