using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    public static partial class YPipelineCameraUI
    {
        // Expandable Header
        private static readonly GUIContent k_LensSettingsHeader = EditorGUIUtility.TrTextContent("Lens Settings");
        private static readonly GUIContent k_RenderingSettingsHeader = EditorGUIUtility.TrTextContent("Rendering Settings");
        private static readonly GUIContent k_OutputSettingsHeader = EditorGUIUtility.TrTextContent("Output Settings");
        
        // Rendering Settings
        private static readonly GUIContent k_BackgroundTypeText = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.");
        private static readonly GUIContent[] k_BackgroundTypeTitles = { EditorGUIUtility.TrTextContent("Sky"), EditorGUIUtility.TrTextContent("Color"), EditorGUIUtility.TrTextContent("None") };
        private static readonly int[] k_BackgroundTypeValues = { 0, 1, 2 }; // CameraClearFlags 的数字是有问题的
        
        // Output Settings
        private static readonly GUIContent k_TargetTextureText = EditorGUIUtility.TrTextContent("Target Texture", "The texture to render this camera into, if none then this camera renders to screen.");
        private static readonly GUIContent k_PriorityText = EditorGUIUtility.TrTextContent("Priority", "A camera with a higher priority is drawn on top of a camera with a lower priority [-100, 100].");
        private static readonly GUIContent k_DynamicResolutionText = EditorGUIUtility.TrTextContent("Dynamic Resolution", "Whether to support dynamic resolution.");
        
    }
}