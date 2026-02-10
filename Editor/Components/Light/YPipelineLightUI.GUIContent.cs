using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    public static partial class YPipelineLightUI
    {
        // Expandable Header
        private static readonly GUIContent k_ShadowSettingsHeader = EditorGUIUtility.TrTextContent("Shadow Settings");
        
        // Light Settings
        private static readonly GUIContent k_TypeText = EditorGUIUtility.TrTextContent("Type", "Specifies the current type of light. Possible types are Directional, Point, Spot, and Area lights.");
        private static readonly GUIContent[] k_LightTypeTitles = { EditorGUIUtility.TrTextContent("Directional"), EditorGUIUtility.TrTextContent("Point"), EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Area"), EditorGUIUtility.TrTextContent("Area") };
        private static readonly int[] k_LightTypeValues = { (int) LightType.Directional, (int) LightType.Point, (int) LightType.Spot, (int) LightType.Rectangle,  (int) LightType.Disc };
        
        private static readonly GUIContent k_AreaLightShapeText = EditorGUIUtility.TrTextContent("Shape", "Specifies the shape of the Area light. Possible types are Rectangle and Disc.");
        private static readonly GUIContent[] k_AreaLightShapeTitles = { EditorGUIUtility.TrTextContent("Rectangle"), EditorGUIUtility.TrTextContent("Disk") };
        private static readonly int[] k_AreaLightShapeValues = { (int) LightType.Rectangle, (int) LightType.Disc };
        
        private static readonly GUIContent k_BounceIntensityText = EditorGUIUtility.TrTextContent("Indirect Multiplier", "Determines the intensity of indirect light being contributed to to the scene. Has no effect when Baked Global Illumination is disabled. If this value is 0, Baked and Mixed lights no longer emit indirect lighting.");
        
        private static readonly GUIContent k_RangeAttenuationScaleText = EditorGUIUtility.TrTextContent("Range Attenuation Scale", "控制距离衰减 Scales the inverse squared distance falloff");
        
        private static readonly GUIContent k_CullingMaskText = EditorGUIUtility.TrTextContent("Culling Mask", "Specifies which lights are culled per camera. To control exclude certain lights affecting certain objects, use Rendering Layers instead, which is supported across all rendering paths.");
        private static readonly GUIContent k_RenderingLayersText = EditorGUIUtility.TrTextContent("Rendering Layers", "Select the Rendering Layers that the Light affects. This Light affects objects where at least one Rendering Layer value matches.");
        
        
        // Shadow Settings
        private static readonly GUIContent k_EnableShadowText = EditorGUIUtility.TrTextContent("Enable Shadow", "是否开启阴影 If enabled, the light casts shadow");
        
        private static readonly GUIContent k_ShadowsStrengthText = EditorGUIUtility.TrTextContent("Strength", "阴影强度 Controls how dark the shadows cast by the light will be.");
        private static readonly GUIContent k_ShadowsNearPlaneText = EditorGUIUtility.TrTextContent("Near Plane", "阴影近平面偏移 Controls the value for the near clip plane when rendering shadows.");
        
        private static readonly GUIContent K_ShadowTintText = EditorGUIUtility.TrTextContent("Shadow Tint", "阴影颜色 Specifies the color tint of the light shadow.");
        private static readonly GUIContent K_PenumbraTintText = EditorGUIUtility.TrTextContent("Shadow Penumbra Tint", "阴影的半影颜色 Specifies the color tint of the light shadow's penumbra.");
        
        private static readonly GUIContent K_DepthBiasText = EditorGUIUtility.TrTextContent("Depth Bias", "深度偏移 One of the shadow biases, which is used to address shadow acne by adding a constant offset to shadow map's depth.");
        private static readonly GUIContent k_SlopeScaledDepthBiasText = EditorGUIUtility.TrTextContent("Slope Scaled Depth Bias", "基于斜率的深度偏移 One of the shadow biases, which is used to address shadow acne by adding a offset to shadow map's depth, scaled proportionally to the slope of the light direction.");
        private static readonly GUIContent K_NormalBiasText = EditorGUIUtility.TrTextContent("Normal Bias", "法线偏移 One of the shadow biases, which is used to address shadow acne by moving the surface a constant offset along its normal.");
        private static readonly GUIContent k_SlopeScaledNormalBiasText = EditorGUIUtility.TrTextContent("Slope Scaled Normal Bias", "基于斜率的法线偏移 One of the shadow biases, which is used to address shadow acne by moving the surface a offset along its normal, scaled proportionally to the slope of the light direction.");

        private static readonly GUIContent K_PenumbraWidthText = EditorGUIUtility.TrTextContent("Penumbra Width", "阴影的半影宽度 Controls the width of the shadow's penumbra. Higher value results in higher softness.");
        private static readonly GUIContent K_SampleCountText = EditorGUIUtility.TrTextContent("Sample Count", "阴影采样数量 Controls the number of samples to blur the shadow.");
        
        private static readonly GUIContent K_LightSizeText = EditorGUIUtility.TrTextContent("Light Size", "PCSS 待更改参数！！！");
        private static readonly GUIContent K_BlockerSearchAreaSizeScaleText = EditorGUIUtility.TrTextContent("Blocker Search Area Size Scale", "PCSS 待更改参数！！！");
        private static readonly GUIContent K_BlockerSearchSampleCountText = EditorGUIUtility.TrTextContent("Blocker Search Sample Count", "遮挡物搜索阶段采样数量 Controls the number of samples to determine the depth of blocker.");
        private static readonly GUIContent k_PenumbraScaleText = EditorGUIUtility.TrTextContent("Penumbra Scale", "控制半影宽度 Multiplier on the Light Size. Scales the width of the shadow's penumbra.");
        private static readonly GUIContent k_MinPenumbraWidthText = EditorGUIUtility.TrTextContent("Min Penumbra Width", "最小半影宽度 Controls the minimum width of the shadow's penumbra.");
        private static readonly GUIContent k_FilterSampleCountText = EditorGUIUtility.TrTextContent("Filter Sample Count", "阴影过滤阶段采样数量 Controls the number of samples to blur the shadow.");
    }
}