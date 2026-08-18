using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal sealed class AdvancedPBRShaderGUI : LitShaderGUI
    {
        protected override bool ShowDefaultGUI => false;
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------
        
        protected override void DrawSurface(Material material)
        {
            EditorGUILayout.Space();

            DrawAlbedoTexAndReflectance();
            DrawHybridTex();
            DrawNormalTex();

            DrawAdvancedTex();
            // DrawCoatNormalTex();
            
            DrawEmission();
            DrawTileOffset();
        }
        
        protected override void DrawTransparency(Material material)
        {
            EditorGUILayout.Space();
            
            DrawAlphaClippingAndCutoff();
        }
        
        protected override void DrawOptions(Material material)
        {
            EditorGUILayout.Space();
            
            DrawRenderQueue();
            DrawGPUInstancing();
            DrawDoubleSidedGI();
            
            DrawCullMode();
            DrawAlembicMotionVectorsToggle();
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Set Keywords
        // ----------------------------------------------------------------------------------------------------

        protected override void SetMaterialKeywords(Material material)
        {
            base.SetMaterialKeywords(material);
            
            if (material.HasProperty(PropertiesName.k_AdvancedTex)) CoreUtils.SetKeyword(material, "_USE_ADVANCEDTEX", material.GetTexture(PropertiesName.k_AdvancedTex));
            // if (material.HasProperty(PropertiesName.k_CoatNormalTex)) CoreUtils.SetKeyword(material, "_USE_COATNORMALTEX", material.GetTexture(PropertiesName.k_CoatNormalTex));
        }
    }
}