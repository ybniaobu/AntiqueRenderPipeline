using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    internal sealed class UnlitShaderGUI : YBaseShaderGUI
    {
        protected override bool ShowDefaultGUI => false;
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Properties
        // ----------------------------------------------------------------------------------------------------
        
        protected override void DrawProperties(Material material)
        {
            EditorGUILayout.Space();
            
            DrawBaseTexAndColor();
            DrawEmission();
            DrawTileOffset();
        }
        
        protected override void DrawTransparency(Material material)
        {
            EditorGUILayout.Space();
            
            DrawAlphaClippingAndCutoff();
            DrawAlphaBlending();
            DrawZWrite();
            DrawZTest();
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
        }
    }
}