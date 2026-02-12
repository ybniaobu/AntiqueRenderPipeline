using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    using CED = CoreEditorDrawer<SerializedYPipelineLight>;
    
    public static partial class YPipelineLightUI
    {
        private enum Expandable
        {
            Shadow = 1 << 0,
        }
        
        private static readonly ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(~-1, "YPipeline");

        public static void Draw(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            DrawLightSettings(serialized, owner);
            
            CED.Group( 
                CED.FoldoutGroup(k_ShadowSettingsHeader, Expandable.Shadow, k_ExpandedState, FoldoutOption.None, DrawShadowSettings)
            ).Draw(serialized, owner);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Light General Settings
        // ----------------------------------------------------------------------------------------------------

        private static void DrawLightSettings(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            DrawLightType(serialized, owner);
            EditorGUILayout.Space();
            DrawColorOrTemperature(serialized, owner);
            EditorGUILayout.Space();
            DrawShape(serialized, owner);
            DrawMaskAndLayers(serialized, owner);
            EditorGUILayout.Space();
        }

        private static void DrawLightType(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            int selectedLightType = serialized.settings.lightType.intValue;
            int type = EditorGUILayout.IntPopup(k_TypeText, selectedLightType, k_LightTypeTitles, k_LightTypeValues);
            serialized.settings.lightType.intValue = type;

            using (new EditorGUI.DisabledScope(serialized.settings.isAreaLightType))
            {
                serialized.settings.DrawLightmapping();
                
                if (serialized.settings.isAreaLightType && serialized.settings.lightmapping.intValue != (int)LightmapBakeType.Baked)
                {
                    serialized.settings.lightmapping.intValue = (int)LightmapBakeType.Baked;
                }
            }
            serialized.settings.ApplyModifiedProperties();
        }
        
        private static void DrawColorOrTemperature(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            LightUI.DrawColor(serialized, owner);
            
            // 以后修改为基于物理单位的灯光时，再使用
            // LightUI.DrawIntensity(serialized, owner);
            // LightUI.DrawIntensityModifiers(serialized);
            
            serialized.settings.DrawIntensity();
            // serialized.settings.DrawBounceIntensity(); // 不知道为什么有时候不会显示出来
            EditorGUILayout.PropertyField(serialized.settings.bounceIntensity, k_BounceIntensityText);
        }

        private static void DrawShape(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            if (serialized.settings.light.type == LightType.Directional) return;
            serialized.settings.DrawRange();
            EditorGUILayout.PropertyField(serialized.rangeAttenuationScale, k_RangeAttenuationScaleText);
            DrawInnerAndOuterSpotAngle(serialized, owner);
            DrawAreaLightShape(serialized, owner);
            EditorGUILayout.Space();
        }
        
        private static void DrawAreaLightShape(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            if (!serialized.settings.lightType.hasMultipleDifferentValues && serialized.settings.isAreaLightType)
            {
                int selectedShape = serialized.settings.lightType.intValue;
                int shape = EditorGUILayout.IntPopup(k_AreaLightShapeText, selectedShape, k_AreaLightShapeTitles, k_AreaLightShapeValues);
                serialized.settings.lightType.intValue = shape;

                using (new EditorGUI.IndentLevelScope())
                {
                    serialized.settings.DrawArea();
                }
            
                serialized.settings.ApplyModifiedProperties();
            }
        }
        
        private static void DrawInnerAndOuterSpotAngle(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            if (!serialized.settings.lightType.hasMultipleDifferentValues && serialized.settings.light.type == LightType.Spot)
            {
                serialized.settings.DrawInnerAndOuterSpotAngle();
            }
        }

        private static void DrawMaskAndLayers(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            serialized.settings.DrawCullingMask();
            serialized.settings.DrawRenderingLayerMask();
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Shadow Settings
        // ----------------------------------------------------------------------------------------------------

        private static void DrawShadowSettings(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            bool selectedShadow = serialized.settings.shadowsType.enumValueIndex != (int) LightShadows.None;
            bool enabled = EditorGUILayout.Toggle(k_EnableShadowText, selectedShadow);
            serialized.settings.shadowsType.enumValueIndex = enabled ? (int) LightShadows.Soft: (int) LightShadows.None;
            serialized.settings.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(!enabled))
            {
                // Resolution 暂时由 Asset 控制，后续修改时再在这里添加
                
                EditorGUILayout.Slider(serialized.settings.shadowsStrength, 0.0f, 1.0f, k_ShadowsStrengthText);
                float nearPlaneMinBound = Mathf.Min(0.01f * serialized.settings.range.floatValue, 0.1f);
                EditorGUILayout.Slider(serialized.settings.shadowsNearPlane, nearPlaneMinBound, 10.0f, k_ShadowsNearPlaneText);
                
                EditorGUILayout.PropertyField(serialized.shadowTint, K_ShadowTintText);
                EditorGUILayout.PropertyField(serialized.penumbraTint, K_PenumbraTintText);
                EditorGUILayout.Space();

                DrawShadowBiases(serialized, owner);
                EditorGUILayout.Space();

                DrawSoftShadowProperties(serialized, owner);
            }
        }

        private static void DrawShadowBiases(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            IMGUIUtils.DrawTitle("Shadow Bias");
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.depthBias, K_DepthBiasText);
                EditorGUILayout.PropertyField(serialized.slopeScaledDepthBias, k_SlopeScaledDepthBiasText);
                EditorGUILayout.PropertyField(serialized.normalBias, K_NormalBiasText);
                EditorGUILayout.PropertyField(serialized.slopeScaledNormalBias, k_SlopeScaledNormalBiasText);
            }
        }
        
        private static void DrawSoftShadowProperties(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            if (GraphicsSettings.currentRenderPipeline is YRenderPipelineAsset yAsset)
            {
                if (yAsset.shadowMode == ShadowMode.PCF)
                {
                    IMGUIUtils.DrawTitle("PCF");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serialized.penumbraWidth, K_PenumbraWidthText);
                        EditorGUILayout.PropertyField(serialized.sampleCount, K_SampleCountText);
                    }
                }
                else
                {
                    IMGUIUtils.DrawTitle("PCSS");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(serialized.lightSize, K_LightSizeText);
                        EditorGUILayout.PropertyField(serialized.blockerSearchAreaSizeScale, K_BlockerSearchAreaSizeScaleText);
                        EditorGUILayout.PropertyField(serialized.blockerSearchSampleCount, K_BlockerSearchSampleCountText);
                        EditorGUILayout.PropertyField(serialized.penumbraScale, k_PenumbraScaleText);
                        EditorGUILayout.PropertyField(serialized.minPenumbraWidth, k_MinPenumbraWidthText);
                        EditorGUILayout.PropertyField(serialized.filterSampleCount, k_FilterSampleCountText);
                    }
                }
            }
        }
    }
}