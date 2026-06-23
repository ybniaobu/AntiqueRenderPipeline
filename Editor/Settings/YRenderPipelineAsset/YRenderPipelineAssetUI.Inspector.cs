using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    using CED = CoreEditorDrawer<SerializedYRenderPipelineAsset>;
    
    internal static partial class YRenderPipelineAssetUI
    {
        private enum Expandable
        {
            Rendering = 1 << 0,
            Lighting = 1 << 1,
            PostProcessing = 1 << 2,
        }

        private enum ExpandableLighting
        {
            LightCulling = 1 << 0,
            ReflectionProbe = 1 << 1,
            GlobalIllumination = 1 << 2,
            APV = 1 << 3,
            Shadow = 1 << 4,
        }
        
        private static readonly ExpandedState<Expandable, YRenderPipelineAsset> k_ExpandedState = new ExpandedState<Expandable, YRenderPipelineAsset>(0, "YPipeline");
        private static readonly ExpandedState<ExpandableLighting, YRenderPipelineAsset> k_ExpandedLightingState = new ExpandedState<ExpandableLighting, YRenderPipelineAsset>(0, "YPipeline");

        public static readonly CED.IDrawer Inspector;
        static YRenderPipelineAssetUI()
        {
            Inspector = CED.Group(
                CED.FoldoutGroup(k_RenderingSettingsHeader, Expandable.Rendering, k_ExpandedState, FoldoutOption.None, DrawRenderingSettings),
                CED.FoldoutGroup(k_LightingSettingsHeader, Expandable.Lighting, k_ExpandedState, FoldoutOption.None, 
                    CED.Group(GroupOption.None, DrawLightingSettings),
                    CED.FoldoutGroup(k_LightCullingSettingsHeader, ExpandableLighting.LightCulling, k_ExpandedLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawLightCullingSubFoldout),
                    CED.FoldoutGroup(k_ReflectionProbeSettingsHeader, ExpandableLighting.ReflectionProbe, k_ExpandedLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawReflectionProbeSubFoldout),
                    CED.FoldoutGroup(k_GlobalIlluminationSettingsHeader, ExpandableLighting.GlobalIllumination, k_ExpandedLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawGlobalIlluminationSubFoldout),
                    CED.FoldoutGroup(k_APVSettingsHeader, ExpandableLighting.APV, k_ExpandedLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawAPVSubFoldout),
                    CED.FoldoutGroup(k_ShadowSettingsHeader, ExpandableLighting.Shadow, k_ExpandedLightingState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawShadowSubFoldout)),
                CED.FoldoutGroup(k_PostProcessingSettingsHeader, Expandable.PostProcessing, k_ExpandedState, FoldoutOption.None, DrawPostProcessingSettings)
            );
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Rendering Settings
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

        // ----------------------------------------------------------------------------------------------------
        // Draw Lighting Settings
        // ----------------------------------------------------------------------------------------------------
        
        private static void DrawLightingSettings(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.Space();
        }

        private static void DrawLightCullingSubFoldout(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.enableSplitDepth, k_EnableSplitDepthText);
        }
        
        private static void DrawReflectionProbeSubFoldout(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.reflectionProbeAtlasFormat, k_ReflectionProbeAtlasFormatText);
            EditorGUILayout.PropertyField(serialized.reflectionProbeAtlasSize, k_ReflectionProbeAtlasSizeText);
            EditorGUILayout.PropertyField(serialized.maxReflectionProbesOnScreen, k_MaxReflectionProbesOnScreenText);
            EditorGUILayout.PropertyField(serialized.reflectionProbeQuality, k_ReflectionProbeQualityText);
        }
        
        private static void DrawGlobalIlluminationSubFoldout(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.enableScreenSpaceAmbientOcclusion, k_EnableScreenSpaceAmbientOcclusionText);
            EditorGUILayout.PropertyField(serialized.ssgiMode, k_SSGIModeText);
            EditorGUILayout.PropertyField(serialized.enableScreenSpaceReflection, k_EnableScreenSpaceReflectionText);
        }
        
        private static void DrawAPVSubFoldout(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.enableProbeVolumeScreenSpaceIrradiance, k_EnableProbeVolumeScreenSpaceIrradianceText);
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
        }
        
        private static void DrawShadowSubFoldout(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.shadowMode, k_ShadowModeText);
            
            EditorGUILayout.PropertyField(serialized.sunLightShadowAtlasSize, k_SunLightShadowAtlasSizeText);
            EditorGUILayout.DelayedFloatField(serialized.maxShadowDistance, k_MaxShadowDistanceText);
            EditorGUILayout.PropertyField(serialized.distanceFade, k_DistanceFadeText);
            EditorGUILayout.PropertyField(serialized.cascadeCount, k_CascadeCountText);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.spiltRatio1, k_SpiltRatio1Text);
            EditorGUILayout.PropertyField(serialized.spiltRatio2, k_SpiltRatio2Text);
            EditorGUILayout.PropertyField(serialized.spiltRatio3, k_SpiltRatio3Text);
            EditorGUI.indentLevel--;
            
            EditorGUILayout.PropertyField(serialized.punctualLightShadowAtlasSize, k_PunctualLightShadowAtlasSizeText);
            EditorGUILayout.PropertyField(serialized.punctualLightShadowQuality, k_PunctualLightShadowQualityText);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Post Processing Settings
        // ----------------------------------------------------------------------------------------------------

        private static void DrawPostProcessingSettings(SerializedYRenderPipelineAsset serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.globalVolumeProfile, k_GlobalVolumeProfileText);
            EditorGUILayout.DelayedIntField(serialized.bakedLUTResolution, k_BakedLUTResolutionText);
        }
    }
}