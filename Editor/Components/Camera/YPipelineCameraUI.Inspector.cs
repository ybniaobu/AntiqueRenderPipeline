using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    using CED = CoreEditorDrawer<SerializedYPipelineCamera>;
    
    public static partial class YPipelineCameraUI
    {
        private enum Expandable
        {
            Lens = 1 << 0,
            Rendering = 1 << 1,
            Output = 1 << 2,
        }
        
        private static readonly ExpandedState<Expandable, Camera> k_ExpandedState = new ExpandedState<Expandable, Camera>(~-1, "YPipeline");
        
        public static readonly CED.IDrawer Inspector;
        static YPipelineCameraUI()
        {
            Inspector = CED.Group( 
                CED.FoldoutGroup(k_LensSettingsHeader, Expandable.Lens, k_ExpandedState, FoldoutOption.None, DrawLensSettings),
                PhysicalCameraDrawer,
                CED.FoldoutGroup(k_RenderingSettingsHeader, Expandable.Rendering, k_ExpandedState, FoldoutOption.None, DrawRenderingSettings),
                CED.FoldoutGroup(k_OutputSettingsHeader, Expandable.Output, k_ExpandedState, FoldoutOption.None, DrawOutputSettings)
            );
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Lens Settings
        // ----------------------------------------------------------------------------------------------------

        private static void DrawLensSettings(SerializedYPipelineCamera serialized, UnityEditor.Editor owner)
        {
            CameraUI.Drawer_Projection(serialized, owner);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Rendering Settings
        // ----------------------------------------------------------------------------------------------------

        private static void DrawRenderingSettings(SerializedYPipelineCamera serialized, UnityEditor.Editor owner)
        {
            DrawBackGroundProperties(serialized, owner);
            
            // TODO: 添加以下参数, 以及是否考虑将抗锯齿的设置以及以下设置作为 per-camera 的参数
            // CameraUI.Rendering.Drawer_Rendering_StopNaNs(serialized, owner);
            // CameraUI.Rendering.Drawer_Rendering_Dithering(serialized, owner);
            
            CameraUI.Rendering.Drawer_Rendering_CullingMask(serialized, owner);
            CameraUI.Rendering.Drawer_Rendering_OcclusionCulling(serialized, owner);
        }

        private static void DrawBackGroundProperties(SerializedYPipelineCamera serialized, UnityEditor.Editor owner)
        {
            bool clearFlagsMixed = serialized.baseCameraSettings.clearFlags.hasMultipleDifferentValues;
            int selected = serialized.baseCameraSettings.clearFlags.enumValueIndex;
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = clearFlagsMixed;
            int shape = EditorGUILayout.IntPopup(k_BackgroundTypeText, selected, k_BackgroundTypeTitles, k_BackgroundTypeValues);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                serialized.baseCameraSettings.clearFlags.enumValueIndex = shape;
            }

            if (!clearFlagsMixed && shape == 1)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    serialized.baseCameraSettings.DrawBackgroundColor();
                }
            }
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Output Settings
        // ----------------------------------------------------------------------------------------------------

        private static void DrawOutputSettings(SerializedYPipelineCamera serialized, UnityEditor.Editor owner)
        {
            serialized.baseCameraSettings.DrawMultiDisplay();
            EditorGUILayout.PropertyField(serialized.baseCameraSettings.targetTexture, k_TargetTextureText);
            EditorGUILayout.PropertyField(serialized.baseCameraSettings.depth, k_PriorityText);
            CameraUI.Output.Drawer_Output_AllowDynamicResolution(serialized, owner, k_DynamicResolutionText);
            CameraUI.Output.Drawer_Output_NormalizedViewPort(serialized, owner);
        }
    }
}