using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal sealed class ClearCoatShaderGUI : LitShaderGUI
    {
        protected override bool ShowDefaultGUI => false;
        
        private enum CExpandable
        {
            Base = 1 << 0,
            Coat = 1 << 1,
            Transparency = 1 << 2,
            Options = 1 << 3
        }

        protected override void OnOpenGUI()
        {
            var filter = (CExpandable) MaterialFilter;
            if (filter.HasFlag(CExpandable.Base)) m_MaterialScopeList.RegisterHeaderScope(k_BaseSubstrateHeader, (uint) CExpandable.Base, DrawBaseSubstrate);
            if (filter.HasFlag(CExpandable.Coat)) m_MaterialScopeList.RegisterHeaderScope(k_CoatLayerHeader, (uint) CExpandable.Coat, DrawCoatLayer);
            if (filter.HasFlag(CExpandable.Transparency)) m_MaterialScopeList.RegisterHeaderScope(k_TransparencyHeader, (uint) CExpandable.Transparency, DrawTransparency);
            if (filter.HasFlag(CExpandable.Options)) m_MaterialScopeList.RegisterHeaderScope(k_OptionsHeader, (uint) CExpandable.Options, DrawOptions);
        }

        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------
        
        private void DrawBaseSubstrate(Material material)
        {
            EditorGUILayout.Space();

            DrawAlbedoTexAndReflectance();
            DrawHybridTex();
            DrawNormalTex();
            
            DrawEmission();
            DrawTileOffset();
        }
        
        private void DrawCoatLayer(Material material)
        {
            EditorGUILayout.Space();
            
            DrawCoatColorTex();
            DrawCoatTex();
            DrawCoatNormalTex();
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
            
            if (material.HasProperty(PropertiesName.k_CoatTex)) CoreUtils.SetKeyword(material, "_USE_COATTEX", material.GetTexture(PropertiesName.k_CoatTex));
            if (material.HasProperty(PropertiesName.k_CoatNormalTex)) CoreUtils.SetKeyword(material, "_USE_COATNORMALTEX", material.GetTexture(PropertiesName.k_CoatNormalTex));
        }
    }
}