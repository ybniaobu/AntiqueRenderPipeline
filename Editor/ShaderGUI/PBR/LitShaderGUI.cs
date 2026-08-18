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
            
            bool roughnessEnabled = m_RoughnessTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(roughnessEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_RoughnessProperty, k_RoughnessText);
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
        }
        
        protected void DrawMetallicTex()
        {
            DrawTextureProperty(m_MetallicTexProperty, k_MetallicTexText);
            
            bool metallicEnabled = m_MetallicTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(metallicEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_MetallicProperty, k_MetallicText);
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
        }
        
        protected void DrawAOTex()
        {
            DrawTextureProperty(m_AOTexProperty, k_AOTexText);
        }
        
        protected void DrawHybridTex()
        {
            DrawTextureProperty(m_HybridTexProperty, k_HybridTexText);
            
            bool hybridEnabled = m_HybridTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(hybridEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_RoughnessProperty, k_RoughnessText);
            DrawShaderProperty(m_MetallicProperty, k_MetallicText);
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
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

        protected void DrawAdvancedTex()
        {
            DrawTextureProperty(m_AdvancedTexProperty, k_AdvancedTexText);
            
            bool advancedEnabled = m_AdvancedTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(advancedEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_AnisotropyProperty, k_AnisotropyText);
            DrawShaderProperty(m_AnisotropyRotationProperty, k_AnisotropyRotationText);
            DrawShaderProperty(m_CoatProperty, k_CoatText);
            DrawShaderProperty(m_CoatRoughnessProperty, k_CoatRoughnessText);
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
        }
        
        protected void DrawCoatTex()
        {
            DrawTextureProperty(m_CoatTexProperty, k_CoatTexText);
            
            bool coatEnabled = m_CoatTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(coatEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_CoatProperty, k_CoatText);
            DrawShaderProperty(m_CoatRoughnessProperty, k_CoatRoughnessText);
            EditorGUI.indentLevel -= 2;
            EditorGUI.EndDisabledGroup();
        }
        
        protected void DrawCoatColorTex()
        {
            m_MaterialEditor.TexturePropertySingleLine(k_CoatColorTexText, m_CoatColorTexProperty, m_CoatColorProperty);
            
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_CoatIORProperty, k_CoatIORText);
            EditorGUI.indentLevel -= 2;
        }
        
        protected void DrawCoatNormalTex()
        {
            DrawTextureProperty(m_CoatNormalTexProperty, k_CoatNormalTexText);
            
            bool coatNormalEnabled = m_CoatNormalTexProperty.textureValue != null;
            EditorGUI.BeginDisabledGroup(!coatNormalEnabled);
            EditorGUI.indentLevel += 2;
            DrawShaderProperty(m_CoatNormalIntensityProperty, k_CoatNormalIntensityText);
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