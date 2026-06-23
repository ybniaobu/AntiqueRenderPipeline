using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    internal static partial class YRenderPipelineAssetUI
    {
        // Expandable Header
        private static readonly GUIContent k_RenderingSettingsHeader = EditorGUIUtility.TrTextContent("Rendering Settings");
        private static readonly GUIContent k_LightingSettingsHeader = EditorGUIUtility.TrTextContent("Lighting Settings");
        private static readonly GUIContent k_PostProcessingSettingsHeader = EditorGUIUtility.TrTextContent("Post-processing Settings");
        
        // Rendering Settings
        private static readonly GUIContent k_RenderPathText = EditorGUIUtility.TrTextContent("Render Path", "请选择一种渲染路径 Select a rendering path");
        private static readonly GUIContent k_EnableSRPBatcherText = EditorGUIUtility.TrTextContent("SRP Batcher", "是否使用 SRP batcher 进行合批 If enabled, the render pipeline uses the SRP batcher.");
        private static readonly GUIContent k_RenderScaleText = EditorGUIUtility.TrTextContent("Render Scale", "渲染缩放比例，用于调整渲染分辨率 Scales the camera render target allowing the game to render at a resolution different than native resolution.");
        
        private static readonly GUIContent k_AntiAliasingModeText = EditorGUIUtility.TrTextContent("Anti-Aliasing Mode", "抗锯齿模式 Choose an anti-aliasing mode, which smooths jagged edges.");
        private static readonly GUIContent k_FXAAModeText = EditorGUIUtility.TrTextContent("FXAA Mode", "快速近似抗锯齿模式，其中 console 模式性能开销相对较低但质量更差 Choose a FXAA mode, where the console mode has relatively lower performance overhead but inferior quality");
        
        // Lighting Settings
        private static readonly GUIContent k_LightCullingSettingsHeader = EditorGUIUtility.TrTextContent("Light Culling");
        private static readonly GUIContent k_EnableSplitDepthText = EditorGUIUtility.TrTextContent("2.5D Culling", "是否激活分块灯光剔除中额外的深度剔除 If enabled, the 2.5D light culling method splits depth into cells to better handle depth discontinuities.");

        private static readonly GUIContent k_ReflectionProbeSettingsHeader = EditorGUIUtility.TrTextContent("Reflection Probe");
        private static readonly GUIContent k_ReflectionProbeAtlasFormatText = EditorGUIUtility.TrTextContent("Reflection Probe Atlas Format", "反射探针贴图格式 The format of the reflection probe atlas.");
        private static readonly GUIContent k_ReflectionProbeAtlasSizeText = EditorGUIUtility.TrTextContent("Reflection Probe Atlas Size", "反射探针贴图尺寸 The size of the reflection probe atlas.");
        private static readonly GUIContent k_MaxReflectionProbesOnScreenText = EditorGUIUtility.TrTextContent("Max Reflection Probe Count On Screen", "屏幕空间中最大反射探针数量 Maximum amount of reflection probes in screen space.");
        private static readonly GUIContent k_ReflectionProbeQualityText = EditorGUIUtility.TrTextContent("Reflection Probe Quality", "反射探针质量，用于控制运行时每个探针的八面体贴图分辨率 Controls the octahedral map resolution of each reflection probe at runtime.");

        private static readonly GUIContent k_GlobalIlluminationSettingsHeader = EditorGUIUtility.TrTextContent("Global Illumination");
        private static readonly GUIContent k_EnableScreenSpaceAmbientOcclusionText = EditorGUIUtility.TrTextContent("Screen Space Ambient Occlusion");
        private static readonly GUIContent k_SSGIModeText = EditorGUIUtility.TrTextContent("Screen Space Global Illumination");
        private static readonly GUIContent k_EnableScreenSpaceReflectionText =  EditorGUIUtility.TrTextContent("Screen Space Reflection");
        
        private static readonly GUIContent k_APVSettingsHeader = EditorGUIUtility.TrTextContent("APV");
        private static readonly GUIContent k_EnableProbeVolumeScreenSpaceIrradianceText = EditorGUIUtility.TrTextContent("Screen Space Irradiance", "开启屏幕空间计算 Enable screen space irradiance For Adaptive Probe Volumes.");
        private static readonly GUIContent k_ProbeVolumeSHBandsText = EditorGUIUtility.TrTextContent("SH Bands", "APV 使用的球谐光照阶数 The number of Spherical Harmonic bands used by Adaptive Probe Volumes to store lighting data. Choosing L2 provides better quality but with higher memory and runtime costs.");
        private static readonly GUIContent k_ProbeVolumeMemoryBudgetText = EditorGUIUtility.TrTextContent("Memory Budget", "球谐 3D 纹理的大小 Determines the width and height of the 3D textures used to store lighting data from probes. Depth is fixed.");
        private static readonly GUIContent k_SupportProbeVolumeGPUStreamingText = EditorGUIUtility.TrTextContent("GPU Streaming", "开启 GPU 流式传输 Enable streaming of Cells for Adaptive Probe Volumes.");
        private static readonly GUIContent k_SupportProbeVolumeDiskStreamingText = EditorGUIUtility.TrTextContent("Disk Streaming", "开启硬盘流式传输 Enable streaming of Cells from disk for Adaptive Probe Volumes.");
        private static readonly GUIContent k_SupportProbeVolumeScenariosText = EditorGUIUtility.TrTextContent("Lighting Scenarios", "开启光照场景 Enable Lighting Scenario Baking for Adaptive Probe Volumes.");
        private static readonly GUIContent k_SupportProbeVolumeScenarioBlendingText = EditorGUIUtility.TrTextContent("Lighting Scenario Blending", "开启光照场景混合 Enable Lighting Scenario Blending for Adaptive Probe Volumes.");
        private static readonly GUIContent k_ProbeVolumeBlendingMemoryBudgetText = EditorGUIUtility.TrTextContent("Blending Memory Budget", "光照场景混合数据 3D 纹理的大小 Determines the width and height of the 3D textures used to store light scenario blending data from probes. Depth is fixed.");
        
        private static readonly GUIContent k_ShadowSettingsHeader = EditorGUIUtility.TrTextContent("Shadow");
        private static readonly GUIContent k_ShadowModeText = EditorGUIUtility.TrTextContent("Shadow Mode", "选择阴影采样滤波模式 Selects the shadow filter mode: PCF for uniform edge smoothing, or PCSS for distance-dependent penumbra, yielding realistic soft shadows.");
        private static readonly GUIContent k_SunLightShadowAtlasSizeText = EditorGUIUtility.TrTextContent("Sun Light Shadow Atlas Size", "太阳光阴影贴图尺寸 The size of the sun light shadow atlas.");
        private static readonly GUIContent k_MaxShadowDistanceText = EditorGUIUtility.TrTextContent("Max Shadow Distance", "实时阴影最大渲染距离 Maximum realtime shadow rendering distance.");
        private static readonly GUIContent k_DistanceFadeText = EditorGUIUtility.TrTextContent("Distance Fade", "阴影淡出 Controls the fade distance ratio when the distance approaches the maximum shadow distance.");
        private static readonly GUIContent k_CascadeCountText = EditorGUIUtility.TrTextContent("Cascade Count", "级联阴影数量 Number of cascade splits used for directional light shadow.");
        private static readonly GUIContent k_SpiltRatio1Text = EditorGUIUtility.TrTextContent("Spilt Ratio 1", "级联阴影切分比例1 Distance from the camera to the first cascade spilt.");
        private static readonly GUIContent k_SpiltRatio2Text = EditorGUIUtility.TrTextContent("Spilt Ratio 2", "级联阴影切分比例2 Distance from the camera to the second cascade spilt.");
        private static readonly GUIContent k_SpiltRatio3Text = EditorGUIUtility.TrTextContent("Spilt Ratio 3", "级联阴影切分比例3 Distance from the camera to the third cascade spilt.");
        private static readonly GUIContent k_PunctualLightShadowAtlasSizeText = EditorGUIUtility.TrTextContent("Punctual Light Shadow Atlas Size", "精确光阴影贴图尺寸 The size of the punctual light shadow atlas.");
        private static readonly GUIContent k_PunctualLightShadowQualityText = EditorGUIUtility.TrTextContent("Punctual Light Shadow Quality", "精确光阴影质量，用于控制运行时每个精确光源的阴影贴图分辨率 Controls the shadow map resolution of each punctual light at runtime.");
        
        // Post-Processing Settings
        private static readonly GUIContent k_GlobalVolumeProfileText = EditorGUIUtility.TrTextContent("Volume Profile", "全局后处理体积 Settings that will override the values defined in the Default Volume Profile set in the Render Pipeline Global settings. Local Volumes inside scenes may override these settings further.");
        private static readonly GUIContent k_BakedLUTResolutionText = EditorGUIUtility.TrTextContent("Color Grading LUT size", "色彩分级查找表高度 Sets the size of the internal color grading lookup textures (LUTs).");
    }
}