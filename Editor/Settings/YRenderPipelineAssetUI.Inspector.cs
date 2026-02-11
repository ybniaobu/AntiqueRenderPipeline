using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    using CED = CoreEditorDrawer<SerializedYRenderPipelineAsset>;
    
    public static partial class YRenderPipelineAssetUI
    {
        private enum Expandable
        {
            Rendering = 1 << 1,
            Lighting = 1 << 2,
            PostProcessing = 1 << 3,
        }
        
        private static readonly ExpandedState<Expandable, YRenderPipelineAsset> k_ExpandedState = new ExpandedState<Expandable, YRenderPipelineAsset>(Expandable.Rendering, "YPipeline");

        public static void Draw(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            CED.Group( 
                CED.FoldoutGroup(k_RenderingSettingsHeader, Expandable.Rendering, k_ExpandedState, DrawRenderingSettings),
                CED.FoldoutGroup(k_LightingSettingsHeader, Expandable.Lighting, k_ExpandedState, DrawLightingSettings),
                CED.FoldoutGroup(k_PostProcessingSettingsHeader, Expandable.PostProcessing, k_ExpandedState, DrawPostProcessingSettings)
            ).Draw(serialized, owner);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Settings
        // ----------------------------------------------------------------------------------------------------

        private static void DrawRenderingSettings(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPath, k_RenderPathText);
            EditorGUILayout.PropertyField(serialized.enableSRPBatcher, k_EnableSRPBatcherText);
            EditorGUILayout.PropertyField(serialized.renderScale, k_RenderScaleText);
            EditorGUILayout.PropertyField(serialized.antiAliasingMode, k_AntiAliasingModeText);

            if (serialized.antiAliasingMode.enumValueIndex == (int)AntiAliasingMode.FXAA)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.fxaaMode, k_FXAAModeText);
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawLightingSettings(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.Space();
            IMGUIUtils.DrawTitle(k_LightCullingSettingsHeader);
            EditorGUILayout.PropertyField(serialized.enableSplitDepth, k_EnableSplitDepthText);
            EditorGUILayout.Space();
            
            IMGUIUtils.DrawTitle(k_ReflectionProbeSettingsHeader);
            EditorGUILayout.PropertyField(serialized.reflectionProbeQuality, k_ReflectionProbeQualityText);
            EditorGUILayout.PropertyField(serialized.maxReflectionProbesOnScreen, k_MaxReflectionProbesOnScreenText);
            EditorGUILayout.Space();
            
            IMGUIUtils.DrawTitle(k_GlobalIlluminationSettingsHeader);
            EditorGUILayout.PropertyField(serialized.enableScreenSpaceAmbientOcclusion, k_EnableScreenSpaceAmbientOcclusionText);
            EditorGUILayout.PropertyField(serialized.enableScreenSpaceGlobalIllumination, k_EnableScreenSpaceGlobalIlluminationText);
            EditorGUILayout.PropertyField(serialized.enableScreenSpaceReflection, k_EnableScreenSpaceReflectionText);
            EditorGUILayout.Space();
            
            IMGUIUtils.DrawTitle(k_APVSettingsHeader);
            EditorGUILayout.PropertyField(serialized.probeVolumeSHBands, k_ProbeVolumeSHBandsText);
            EditorGUILayout.PropertyField(serialized.probeVolumeMemoryBudget, k_ProbeVolumeMemoryBudgetText);
            EditorGUILayout.PropertyField(serialized.supportProbeVolumeScenarios, k_SupportProbeVolumeScenariosText);
            EditorGUI.BeginDisabledGroup(!serialized.supportProbeVolumeScenarios.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.supportProbeVolumeScenarioBlending, k_SupportProbeVolumeScenarioBlendingText);
            EditorGUILayout.PropertyField(serialized.probeVolumeBlendingMemoryBudget, k_ProbeVolumeBlendingMemoryBudgetText);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(serialized.supportProbeVolumeGPUStreaming, k_SupportProbeVolumeGPUStreamingText);
            EditorGUI.BeginDisabledGroup(!serialized.supportProbeVolumeGPUStreaming.boolValue);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.supportProbeVolumeDiskStreaming, k_SupportProbeVolumeDiskStreamingText);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
            int estimatedVMemCost = ProbeReferenceVolume.instance.GetVideoMemoryCost();
            if (estimatedVMemCost == 0)
            {
                EditorGUILayout.HelpBox($"Estimated GPU Memory cost: 0.\nProbe reference volume is not used in the scene and resources haven't been allocated yet.", MessageType.Info, wide: true);
            }
            else
            {
                EditorGUILayout.HelpBox($"Estimated GPU Memory cost: {estimatedVMemCost / (1024 * 1024)} MB.", MessageType.Info, wide: true);
            }
            EditorGUILayout.Space();
            
            IMGUIUtils.DrawTitle(k_ShadowSettingsHeader);
            EditorGUILayout.PropertyField(serialized.shadowMode, k_ShadowModeText);
            EditorGUILayout.DelayedFloatField(serialized.maxShadowDistance, k_MaxShadowDistanceText);
            EditorGUILayout.PropertyField(serialized.distanceFade, k_DistanceFadeText);
            EditorGUILayout.PropertyField(serialized.cascadeCount, k_CascadeCountText);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.spiltRatio1, k_SpiltRatio1Text);
            EditorGUILayout.PropertyField(serialized.spiltRatio2, k_SpiltRatio2Text);
            EditorGUILayout.PropertyField(serialized.spiltRatio3, k_SpiltRatio3Text);
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(serialized.cascadeEdgeFade, k_CascadeEdgeFadeText);
            EditorGUILayout.PropertyField(serialized.sunLightShadowMapSize, k_SunLightShadowMapSizeText);
            EditorGUILayout.PropertyField(serialized.spotLightShadowMapSize, k_SpotLightShadowMapSizeText);
            EditorGUILayout.PropertyField(serialized.pointLightShadowMapSize, k_PointLightShadowMapSizeText);
        }

        private static void DrawPostProcessingSettings(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.globalVolumeProfile, k_GlobalVolumeProfileText);
            EditorGUILayout.DelayedIntField(serialized.bakedLUTResolution, k_BakedLUTResolutionText);
        }
    }
}