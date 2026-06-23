using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    using CED = CoreEditorDrawer<SerializedYPipelineLight>;
    
    internal static partial class YPipelineLightUI
    {
        private enum Expandable
        {
            Shadow = 1 << 0,
        }
        
        private enum ExpandableShadow
        {
            ShadowBiases = 1 << 0,
            PCF = 1 << 1,
            PCSS = 1 << 2,
        }
        
        private static readonly ExpandedState<Expandable, Light> k_ExpandedState = new ExpandedState<Expandable, Light>(~-1, "YPipeline");
        private static readonly ExpandedState<ExpandableShadow, Light> k_ExpandedShadowState = new ExpandedState<ExpandableShadow, Light>(0, "YPipeline");

        public static readonly CED.IDrawer Inspector;
        static YPipelineLightUI()
        {
            Inspector = CED.Group( 
                CED.Group(GroupOption.None, DrawLightSettings),
                CED.FoldoutGroup(k_ShadowSettingsHeader, Expandable.Shadow, k_ExpandedState, FoldoutOption.None, 
                    CED.Group(GroupOption.None, DrawShadowSettings),
                    CED.FoldoutGroup("Shadow Bias", ExpandableShadow.ShadowBiases, k_ExpandedShadowState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawShadowBiasesSubFoldout),
                    CED.Conditional((serialized, owner) => IsShadowMode(ShadowMode.PCF),
                        CED.FoldoutGroup("PCF", ExpandableShadow.PCF, k_ExpandedShadowState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawPCFSubFoldout)),
                    CED.Conditional((serialized, owner) => IsShadowMode(ShadowMode.PCSS),
                        CED.FoldoutGroup("PCSS", ExpandableShadow.PCSS, k_ExpandedShadowState, FoldoutOption.Indent | FoldoutOption.SubFoldout | FoldoutOption.NoSpaceAtEnd, DrawPCSSSubFoldout)))
            );
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
            bool lightTypeMixed = serialized.settings.lightType.hasMultipleDifferentValues;
            int selectedLightType = serialized.settings.lightType.intValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = lightTypeMixed;
            int type = EditorGUILayout.IntPopup(k_TypeText, selectedLightType, k_LightTypeTitles, k_LightTypeValues);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                serialized.settings.lightType.intValue = type;
            }

            using (new EditorGUI.DisabledScope(serialized.settings.isAreaLightType))
            {
                serialized.settings.DrawLightmapping();
                
                if (serialized.settings.isAreaLightType && serialized.settings.lightmapping.intValue != (int)LightmapBakeType.Baked)
                {
                    serialized.settings.lightmapping.intValue = (int)LightmapBakeType.Baked;
                }
            }
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
            bool showMixed = serialized.settings.shadowsType.hasMultipleDifferentValues;
            bool selectedShadow = !showMixed && serialized.settings.shadowsType.enumValueIndex != (int) LightShadows.None;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = showMixed;
            bool shadowEnabled = EditorGUILayout.Toggle(k_EnableShadowText, selectedShadow);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                serialized.settings.shadowsType.enumValueIndex = shadowEnabled ? (int) LightShadows.Soft: (int) LightShadows.None;
            }

            if (serialized.settings.light.type != LightType.Directional && GraphicsSettings.currentRenderPipeline is YRenderPipelineAsset yAsset)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    showMixed = serialized.shadowResolution.hasMultipleDifferentValues;
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = showMixed;
                    int trueOldResolution = serialized.shadowResolution.intValue;
                    int oldResolution = yAsset.punctualLightShadowQuality switch
                    {
                        Quality3Tier.Low => trueOldResolution * 2,
                        Quality3Tier.Medium => trueOldResolution,
                        Quality3Tier.High => trueOldResolution / 2,
                        _ => trueOldResolution
                    };
                    int resolution = EditorGUILayout.IntPopup(k_ShadowResolutionText, oldResolution, k_ShadowResolutionTitles, k_ShadowResolutionValues);
                    EditorGUI.showMixedValue = false;
                    int trueResolution = yAsset.punctualLightShadowQuality switch
                    {
                        Quality3Tier.Low => resolution / 2,
                        Quality3Tier.Medium => resolution,
                        Quality3Tier.High => resolution * 2,
                        _ => resolution
                    };
                    if (EditorGUI.EndChangeCheck())
                    {
                        serialized.shadowResolution.intValue = trueResolution;
                    }
                    EditorGUILayout.LabelField($"Shadow Quality: {yAsset.punctualLightShadowQuality} -> {trueResolution}", GUILayout.ExpandWidth(false));
                }
            }

            EditorGUILayout.Slider(serialized.settings.shadowsStrength, 0.0f, 1.0f, k_ShadowsStrengthText);
            float nearPlaneMinBound = Mathf.Min(0.01f * serialized.settings.range.floatValue, 0.1f);
            EditorGUILayout.Slider(serialized.settings.shadowsNearPlane, nearPlaneMinBound, 10.0f, k_ShadowsNearPlaneText);
            
            EditorGUILayout.PropertyField(serialized.shadowTint, K_ShadowTintText);
            EditorGUILayout.PropertyField(serialized.penumbraTint, K_PenumbraTintText);
        }

        private static void DrawShadowBiasesSubFoldout(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.depthBias, K_DepthBiasText);
            EditorGUILayout.PropertyField(serialized.slopeScaledDepthBias, k_SlopeScaledDepthBiasText);
            EditorGUILayout.PropertyField(serialized.normalBias, K_NormalBiasText);
            EditorGUILayout.PropertyField(serialized.slopeScaledNormalBias, k_SlopeScaledNormalBiasText);
        }

        private static bool IsShadowMode(ShadowMode mode)
        {
            if (GraphicsSettings.currentRenderPipeline is YRenderPipelineAsset yAsset)
            {
                return yAsset.shadowMode == mode;
            }
            return false;
        }
        
        private static void DrawPCFSubFoldout(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.penumbraWidth, K_PenumbraWidthText);
            EditorGUILayout.PropertyField(serialized.sampleCount, K_SampleCountText);
        }

        private static void DrawPCSSSubFoldout(SerializedYPipelineLight serialized, UnityEditor.Editor owner)
        {
            if (serialized.settings.light.type == LightType.Directional)
            {
                EditorGUILayout.PropertyField(serialized.angularDiameter, K_AngularDiameterText);
            }
            else
            {
                EditorGUILayout.PropertyField(serialized.lightRadius, K_LightRadiusText);
            }
            
            EditorGUILayout.PropertyField(serialized.blockerSearchAreaSizeScale, K_BlockerSearchAreaSizeScaleText);
            EditorGUILayout.PropertyField(serialized.blockerSearchSampleCount, K_BlockerSearchSampleCountText);
            EditorGUILayout.PropertyField(serialized.penumbraScale, k_PenumbraScaleText);
            EditorGUILayout.PropertyField(serialized.minPenumbraWidth, k_MinPenumbraWidthText);
            EditorGUILayout.PropertyField(serialized.filterSampleCount, k_FilterSampleCountText);
        }
    }
}