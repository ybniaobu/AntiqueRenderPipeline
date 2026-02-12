using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    using CED = CoreEditorDrawer<SerializedYPipelineReflectionProbe>;
    
    public static partial class YPipelineReflectionProbeUI
    {
        private enum Expandable
        {
            Runtime = 1 << 0,
            Capture = 1 << 1,
            Debug =  1 << 2
        }
        
        private static readonly ExpandedState<Expandable, ReflectionProbe> k_ExpandedState = new ExpandedState<Expandable, ReflectionProbe>(~-1, "YPipeline");
        
        public static void Draw(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            DrawModeSettings(serialized, owner);
            
            CED.Group( 
                CED.FoldoutGroup(k_RuntimeSettingsHeader, Expandable.Runtime, k_ExpandedState, FoldoutOption.None, DrawRuntimeSettings),
                CED.FoldoutGroup(k_CaptureSettingsHeader, Expandable.Capture, k_ExpandedState, FoldoutOption.None, DrawCaptureSettings),
                CED.Group(GroupOption.None, DrawBakeAllButton),
                CED.Group(GroupOption.None, DrawCubemapBakeButton),
                CED.Group(GroupOption.None, DrawOctahedralBakeButton),
                CED.Group(GroupOption.None, DrawSHBakeButton),
                CED.FoldoutGroup(k_DebugSettingsHeader, Expandable.Debug, k_ExpandedState, FoldoutOption.None, DrawDebugSettings),
                CED.Group(GroupOption.None, DrawInfo)
            ).Draw(serialized, owner);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Draw Settings
        // ----------------------------------------------------------------------------------------------------
        
        private static void DrawModeSettings(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.IntPopup(serialized.mode, k_ModeContents, k_ModeValues, k_ModeText);
            
            switch ((ReflectionProbeMode) serialized.mode.intValue)
            {
                case ReflectionProbeMode.Baked:
                    EditorGUILayout.HelpBox("受限制于 Unity 内置 API，Baked 的功能有一点限制，强烈建议使用 Custom 模式。若要使用 Baked 模式，只能先在 Lighting 里烘焙所有 Reflection Probes，否则下面 Bake 按钮无法使用。", 
                        MessageType.Warning, true);
                    break;
                case ReflectionProbeMode.Custom:
                    Rect lineRect = EditorGUILayout.GetControlRect(true, 64);
                    EditorGUI.BeginChangeCheck();
                    var customTexture = EditorGUI.ObjectField(lineRect, k_CustomCubemapText, serialized.customBakedTexture.objectReferenceValue, typeof(Cubemap), false);
                    if (EditorGUI.EndChangeCheck()) serialized.customBakedTexture.objectReferenceValue = customTexture;
                    break;
                case ReflectionProbeMode.Realtime:
                    EditorGUILayout.HelpBox("Realtime 模式 YPipeline 暂时不支持！！！！！！", 
                        MessageType.Error, true);
                    EditorGUILayout.PropertyField(serialized.refreshMode, k_RefreshModeText);
                    EditorGUILayout.PropertyField(serialized.timeSlicingMode, k_TimeSlicingText);
                    break;
            }
            
            EditorGUILayout.Space();
        }

        private static void DrawRuntimeSettings(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.importance, k_ImportanceText);
            EditorGUILayout.PropertyField(serialized.intensityMultiplier, k_IntensityText);
            EditorGUILayout.PropertyField(serialized.boxProjection, k_BoxProjectionText);
            EditorGUILayout.PropertyField(serialized.blendDistance, k_BlendDistanceText);
            EditorGUILayout.PropertyField(serialized.boxSize, k_BoxSizeText);
            EditorGUILayout.PropertyField(serialized.boxOffset, k_BoxOffsetText);
        }

        private static void DrawCaptureSettings(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.IntPopup(serialized.resolution, k_ResolutionContents, k_ResolutionValues, k_ResolutionText, GUILayout.MinWidth(40));
            EditorGUILayout.PropertyField(serialized.renderDynamicObjects, k_RenderDynamicObjects);
            EditorGUILayout.PropertyField(serialized.HDR, k_HDRText);
            EditorGUILayout.PropertyField(serialized.shadowDistance, k_ShadowDistanceText);
            EditorGUILayout.IntPopup(serialized.clearFlags, k_ClearFlagsContents, k_ClearFlagsValues, k_ClearFlagsText);

            if (serialized.clearFlags.intValue == 2)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.backgroundColor, k_BackgroundColorText);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(serialized.cullingMask, k_CullingMaskText);
            EditorGUILayout.PropertyField(serialized.useOcclusionCulling, k_UseOcclusionCulling);
            
            CoreEditorUtils.DrawMultipleFields(k_ClippingPlanesLabel, new[] { serialized.nearClip, serialized.farClip }, k_NearAndFarLabels);
        }

        private static void DrawDebugSettings(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.cubemapPreviewByNormal, k_CubemapPreviewByNormalText);
            EditorGUILayout.PropertyField(serialized.showOctahedralAtlas, k_ShowOctahedralAtlasText);
            EditorGUILayout.PropertyField(serialized.showSHProbe, k_ShowSHProbeText);
            if (serialized.showSHProbe.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.SHPreviewByReflection, k_SHPreviewByReflectionText);
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawInfo(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, 80);
            GUIStyle customStyle = new GUIStyle(EditorStyles.helpBox);
            customStyle.stretchHeight = true;
            customStyle.fontSize = 12;
            customStyle.richText = true;
            string tick = "<color=green><b>✓</b></color> ;";
            string cross = "<color=red><b>×</b></color> ;";
            bool isReady = serialized.isOctahedralAtlasBaked.boolValue && serialized.isSHBaked.boolValue;
            EditorGUI.SelectableLabel(rect, $"<i><b>Reflection Probe Info</b>: \n 1. Is Probe Ready to use: " + (isReady ? tick : cross) +
                                          $" 2. Is Octahedral Atlas Baked: " + (serialized.isOctahedralAtlasBaked.boolValue ? tick : cross) +
                                          $" 3. Is SH Data Baked: " + (serialized.isSHBaked.boolValue ? tick : cross) + 
                                          $"\n 4. SH Data (7 Vectors): {serialized.SHData.GetArrayElementAtIndex(0).vector4Value} , " +
                                          $"{serialized.SHData.GetArrayElementAtIndex(1).vector4Value} , {serialized.SHData.GetArrayElementAtIndex(2).vector4Value} , " + 
                                          $"{serialized.SHData.GetArrayElementAtIndex(3).vector4Value} , {serialized.SHData.GetArrayElementAtIndex(4).vector4Value} , " +
                                          $"{serialized.SHData.GetArrayElementAtIndex(5).vector4Value} , {serialized.SHData.GetArrayElementAtIndex(6).vector4Value}</i>", customStyle);
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Bake All
        // ----------------------------------------------------------------------------------------------------

        private static void DrawBakeAllButton(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            ReflectionProbe probe = owner.target as ReflectionProbe;
            if (probe == null || owner.targets.Length > 1) return;
            
            GUILayout.BeginHorizontal();
            
            switch (serialized.mode.intValue)
            {
                case (int) ReflectionProbeMode.Baked:
                    using (new EditorGUI.DisabledScope(!probe.enabled))
                    {
                        bool nonbaked = probe.texture == null;
                        Color originalColor = GUI.color;
                        GUI.color = nonbaked ? Color.firebrick : originalColor;
                        if (GUILayout.Button(k_BakeAllButtonLabel, GUILayout.Height(20)))
                        {
                            BakeReflectionProbe(probe);
                            if (!nonbaked)
                            {
                                BakeOctahedralAtlas(serialized, probe);
                                BakeSHData(serialized, probe);
                            }
                            GUIUtility.ExitGUI();
                        }
                        GUI.color = originalColor;
                    }
                    break;
                case (int) ReflectionProbeMode.Custom:
                    using (new EditorGUI.DisabledScope(!probe.enabled))
                    {
                        if (GUILayout.Button(k_BakeAllButtonLabel, GUILayout.Height(20)))
                        {
                            bool baked = BakeCustomReflectionProbe(probe);
                            if (baked)
                            {
                                BakeOctahedralAtlas(serialized, probe);
                                BakeSHData(serialized, probe);
                            }
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                case (int) ReflectionProbeMode.Realtime:
                    break;
            }
            GUILayout.EndHorizontal();
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Bake Cubemap
        // ----------------------------------------------------------------------------------------------------

        private static void DrawCubemapBakeButton(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            ReflectionProbe probe = owner.target as ReflectionProbe;
            if (probe == null || owner.targets.Length > 1) return;
            
            GUILayout.BeginHorizontal();
            
            switch (serialized.mode.intValue)
            {
                case (int) ReflectionProbeMode.Baked:
                    using (new EditorGUI.DisabledScope(!probe.enabled))
                    {
                        Color originalColor = GUI.color;
                        GUI.color = probe.texture == null ? Color.firebrick : originalColor;
                        if (GUILayout.Button(k_CubemapBakeButtonLabel, GUILayout.Height(20)))
                        {
                            BakeReflectionProbe(probe);
                            GUIUtility.ExitGUI();
                        }
                        GUI.color = originalColor;
                    }
                    break;
                case (int) ReflectionProbeMode.Custom:
                    using (new EditorGUI.DisabledScope(!probe.enabled))
                    {
                        if (GUILayout.Button(k_CubemapBakeButtonLabel, GUILayout.Height(20)))
                        {
                            BakeCustomReflectionProbe(probe);
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                case (int) ReflectionProbeMode.Realtime:
                    break;
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 我们没法调用 Lightmapping.BakeAllReflectionProbesSnapshots()、Lightmapping.BakeReflectionProbeSnapshot()，都是 internal 函数。
        /// 本来想着调用 Lightmapping.BakeReflectionProbe() 来模仿 Baked 模式的逻辑，但是发现 Reflection Probe 的 bakedTexture 跟 customBakedTexture 不一样，
        /// bakedTexture 不会被序列化（Scene 用文本打开也看不到该属性），应该是直接由 Unity C++ 控制的，这就很尴尬了。
        /// HDRP 直接另外写了一套烘焙逻辑，自己序列化了 bakedTexture，太复杂了，就不抄了。
        /// 所以在使用 Baked 模式时，要先在 Lighting 里 Generate Lighting，才能正确序列化。
        /// </summary>
        /// <param name="probe"></param>
        private static void BakeReflectionProbe(ReflectionProbe probe)
        {
            string path = AssetDatabase.GetAssetPath(probe.bakedTexture);
            string extension = probe.hdr ? "exr" : "png";

            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + extension)
            {
                Debug.LogWarning("请先在 <b><i>Window -> Lighting</i></b> 里烘焙所有反射探针：<b><i>Generate Lighting -> Bake Reflection Probes</i></b>");
                return;
            }
            
            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!Lightmapping.BakeReflectionProbe(probe, path)) Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
        }
        
        private static bool BakeCustomReflectionProbe(ReflectionProbe probe)
        {
            string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
            string extension = probe.hdr ? "exr" : "png";

            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + extension)
            {
                // 若 scene 文件夹不存在，则创建文件夹
                string scenePath = Path.ChangeExtension(SceneManager.GetActiveScene().path, null);
                if (string.IsNullOrEmpty(scenePath)) scenePath = "Assets";
                else if (Directory.Exists(scenePath) == false) Directory.CreateDirectory(scenePath);
                
                // 文件名
                string fileName = probe.name + "." + extension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(scenePath, fileName)));
                path = EditorUtility.SaveFilePanelInProject("Save Reflection Probe's Cubemap.", fileName, extension, string.Empty, scenePath);
                if (string.IsNullOrEmpty(path)) return false;
                
                // 检查文件路径是否和其他 ReflectionProbe 的 cubemap 冲突
                if (IsCustomReflectionProbeCollidingWithOtherProbes(path, probe, out ReflectionProbe collidingProbe))
                {
                    string message = $"'{path}' path is used by the game object '{collidingProbe.name}', do you really want to overwrite it?";
                    if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe", message, "Yes", "No")) return false;
                }
            }
            
            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!Lightmapping.BakeReflectionProbe(probe, path))
            {
                Debug.LogError("Failed to bake reflection probe to " + path);
                return false;
            }
            EditorUtility.ClearProgressBar();
            return true;
        }
        
        private static bool IsCustomReflectionProbeCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.InstanceID).ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null) continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Bake Octahedral Atlas
        // ----------------------------------------------------------------------------------------------------
    
        private static void DrawOctahedralBakeButton(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            ReflectionProbe probe = owner.target as ReflectionProbe;
            if (probe == null || owner.targets.Length > 1) return;
            
            GUILayout.BeginHorizontal();
            
            Color originalColor = GUI.color;
            GUI.color = probe.texture == null && probe.mode == ReflectionProbeMode.Baked ? Color.firebrick : originalColor;
            if (GUILayout.Button(k_OctahedralAtlasBakeButtonLabel, GUILayout.Height(20)))
            {
                BakeOctahedralAtlas(serialized, probe);
                GUIUtility.ExitGUI();
            }
            GUI.color = originalColor;
            
            GUILayout.EndHorizontal();
        }

        private static void BakeOctahedralAtlas(SerializedYPipelineReflectionProbe serialized, ReflectionProbe probe)
        {
            string path = AssetDatabase.GetAssetPath(probe.texture);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("请先烘焙 Cubemap！");
                return;
            }
            path = Path.GetDirectoryName(path);
            
            GenerateOctahedralAtlas(serialized, probe, path, Quality3Tier.Low);
            GenerateOctahedralAtlas(serialized, probe, path, Quality3Tier.Medium);
            GenerateOctahedralAtlas(serialized, probe, path, Quality3Tier.High);
            serialized.isOctahedralAtlasBaked.boolValue = true;
            serialized.ApplyModifiedProperties();
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Bake Cubemap SH Data
        // ----------------------------------------------------------------------------------------------------
        
        private static void DrawSHBakeButton(SerializedYPipelineReflectionProbe serialized, UnityEditor.Editor owner)
        {
            ReflectionProbe probe = owner.target as ReflectionProbe;
            if (probe == null || owner.targets.Length > 1) return;
            
            GUILayout.BeginHorizontal();
            
            Color originalColor = GUI.color;
            GUI.color = probe.texture == null && probe.mode == ReflectionProbeMode.Baked ? Color.firebrick : originalColor;
            if (GUILayout.Button(k_SHBakeButtonLabel, GUILayout.Height(20)))
            {
                BakeSHData(serialized, probe);
                GUIUtility.ExitGUI();
            }
            GUI.color = originalColor;
            
            GUILayout.EndHorizontal();
        }
        
        private static void BakeSHData(SerializedYPipelineReflectionProbe serialized, ReflectionProbe probe)
        {
            if (probe.texture == null) return;
            
            CalculateSHData(serialized, probe);
            serialized.isSHBaked.boolValue = true;
            serialized.ApplyModifiedProperties();
        }
    }
}