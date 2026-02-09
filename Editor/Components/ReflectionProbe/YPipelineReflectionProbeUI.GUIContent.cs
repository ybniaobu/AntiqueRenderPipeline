using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    public static partial class YPipelineReflectionProbeUI
    {
        // Mode Settings
        private static readonly int[] k_ModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        private static readonly GUIContent[] k_ModeContents = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        private static readonly GUIContent k_ModeText = EditorGUIUtility.TrTextContent("Mode", "Specify the mode for this reflection probe: Baked, Custom, or Realtime.");
        
        private static readonly GUIContent k_CustomCubemapText = EditorGUIUtility.TrTextContent("Custom Cubemap", "Sets a custom cubemap for this reflection probe.");
        
        private static readonly GUIContent k_TimeSlicingText = EditorGUIUtility.TrTextContent("Time Slicing", "If enabled this probe will update over several frames, to help reduce the impact on the frame rate");
        private static readonly GUIContent k_RefreshModeText = EditorGUIUtility.TrTextContent("Refresh Mode", "Controls how this probe refreshes in the Player");
        
        // Expandable Header
        private static readonly GUIContent k_RuntimeSettingsHeader = EditorGUIUtility.TrTextContent("Runtime Settings");
        private static readonly GUIContent k_CaptureSettingsHeader = EditorGUIUtility.TrTextContent("Capture Settings");
        private static readonly GUIContent k_DebugSettingsHeader = EditorGUIUtility.TrTextContent("Debug Settings");
        
        // Runtime Settings
        private static readonly GUIContent k_ImportanceText = EditorGUIUtility.TrTextContent("Importance", "When reflection probes overlap, Unity uses Importance to determine which probe should take priority.");
        private static readonly GUIContent k_IntensityText = EditorGUIUtility.TrTextContent("Intensity", "The intensity modifier the Editor applies to this probe's texture in its shader.");
        private static readonly GUIContent k_BoxProjectionText = EditorGUIUtility.TrTextContent("Box Projection", "When enabled, Unity assumes that the reflected light is originating from the inside of the probe's box, rather than from infinitely far away. This is useful for box-shaped indoor environments.");
        private static readonly GUIContent k_BlendDistanceText = EditorGUIUtility.TrTextContent("Blend Distance", "Area around the probe where it is blended with other probes.");
        private static readonly GUIContent k_BoxSizeText = EditorGUIUtility.TrTextContent("Box Size", "The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
        private static readonly GUIContent k_BoxOffsetText = EditorGUIUtility.TrTextContent("Box Offset", "The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");
        
        // Capture Settings
        private static readonly GUIContent k_RenderDynamicObjects = EditorGUIUtility.TrTextContent("Dynamic Objects", "If enabled dynamic objects are also rendered into the cubemap");
        
        private static readonly GUIContent[] k_ResolutionContents = { new GUIContent("128"), new GUIContent("256"), new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048") };
        private static readonly int[] k_ResolutionValues = { 128, 256, 512, 1024, 2048 };
        private static readonly GUIContent k_ResolutionText = EditorGUIUtility.TrTextContent("Resolution", "The resolution of the cubemap.");
        
        private static readonly GUIContent k_HDRText = EditorGUIUtility.TrTextContent("HDR", "Enable High Dynamic Range rendering.");
        private static readonly GUIContent k_ShadowDistanceText = EditorGUIUtility.TrTextContent("Shadow Distance", "Maximum distance at which Unity renders shadows associated with this probe.");
        
        private static readonly GUIContent[] k_ClearFlagsContents = { new GUIContent("Skybox"), new GUIContent("Solid Color") };
        private static readonly int[] k_ClearFlagsValues = { 1, 2 }; // from Camera.h
        private static readonly GUIContent k_ClearFlagsText = EditorGUIUtility.TrTextContent("Clear Flags", "Specify how to fill empty areas of the cubemap.");
        
        private static readonly GUIContent k_BackgroundColorText = EditorGUIUtility.TrTextContent("Background Color", "Camera clears the screen to this color before rendering.");
        private static readonly GUIContent k_UseOcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "If this property is enabled, geometries which are blocked from the probe's line of sight are skipped during rendering.");
        private static readonly GUIContent k_CullingMaskText = EditorGUIUtility.TrTextContent("Culling Mask", "Allows objects on specified layers to be included or excluded in the reflection.");
        
        private static readonly GUIContent k_ClippingPlanesLabel = EditorGUIUtility.TrTextContent("Clipping Planes", "The distances from the Camera where rendering starts and stops.");
        private static readonly GUIContent[] k_NearAndFarLabels = new[]
        {
            EditorGUIUtility.TrTextContent("Near", "The closest point to the Camera where drawing occurs."),
            EditorGUIUtility.TrTextContent("Far", "The furthest point from the Camera that drawing occurs.")
        };
        
        // Debug Settings
        private static readonly GUIContent k_CubemapPreviewByNormalText = EditorGUIUtility.TrTextContent("Cubemap Gizom Preview by Normal", "当未激活时，在 Scene 中的 Cubemap 预览的采样的方向是视角反射方向，激活按钮可以按 Normal 方向采样。");
        private static readonly GUIContent k_ShowOctahedralAtlasText = EditorGUIUtility.TrTextContent("Show Octahedral Atlas in Preview");
        private static readonly GUIContent k_ShowSHProbeText = EditorGUIUtility.TrTextContent("Show SH Probe in Scene View");
        private static readonly GUIContent k_SHPreviewByReflectionText = EditorGUIUtility.TrTextContent("SH Probe Gizom Preview by Reflection", "当未激活时，在 Scene 中的 SH Probe 预览采样的方向是 Normal 方向，激活按钮可以按视角反射方向采样。");
        
        // Bake Button
        private static readonly GUIContent k_BakeAllButtonLabel = EditorGUIUtility.TrTextContent("Bake All");
        private static readonly GUIContent k_CubemapBakeButtonLabel = EditorGUIUtility.TrTextContent("Bake Cubemap Only");
        private static readonly GUIContent k_OctahedralAtlasBakeButtonLabel = EditorGUIUtility.TrTextContent("Bake Octahedral Atlas Cache Only");
        private static readonly GUIContent k_SHBakeButtonLabel = EditorGUIUtility.TrTextContent("Bake SH Data Cache Only");
    }
}