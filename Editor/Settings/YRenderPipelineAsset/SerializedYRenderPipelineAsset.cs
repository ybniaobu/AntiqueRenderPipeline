using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    internal sealed class SerializedYRenderPipelineAsset
    {
        public SerializedObject serializedObject;
        
        // Properties -- Rendering Settings
        public SerializedProperty renderPath;
        public SerializedProperty enableSRPBatcher;
        public SerializedProperty renderScale;
        
        public SerializedProperty antiAliasingMode;
        public SerializedProperty fxaaMode;
        
        // Properties -- Lighting Settings
        public SerializedProperty enableSplitDepth;
        
        public SerializedProperty reflectionProbeAtlasFormat;
        public SerializedProperty reflectionProbeAtlasSize;
        public SerializedProperty maxReflectionProbesOnScreen;
        public SerializedProperty reflectionProbeQuality;
        
        public SerializedProperty enableScreenSpaceAmbientOcclusion;
        public SerializedProperty ssgiMode;
        public SerializedProperty enableScreenSpaceReflection;

        public SerializedProperty enableProbeVolumeScreenSpaceIrradiance;
        public SerializedProperty probeVolumeSHBands;
        public SerializedProperty probeVolumeMemoryBudget;
        public SerializedProperty probeVolumeBlendingMemoryBudget;
        public SerializedProperty supportProbeVolumeGPUStreaming;
        public SerializedProperty supportProbeVolumeDiskStreaming;
        public SerializedProperty supportProbeVolumeScenarios;
        public SerializedProperty supportProbeVolumeScenarioBlending;
        
        public SerializedProperty shadowMode;
        public SerializedProperty sunLightShadowAtlasSize;
        public SerializedProperty maxShadowDistance;
        public SerializedProperty distanceFade;
        public SerializedProperty cascadeCount;
        public SerializedProperty spiltRatio1;
        public SerializedProperty spiltRatio2;
        public SerializedProperty spiltRatio3;
        public SerializedProperty punctualLightShadowAtlasSize;
        public SerializedProperty punctualLightShadowQuality;
        
        // Properties -- Post Processing Settings
        public SerializedProperty globalVolumeProfile;
        public SerializedProperty bakedLUTResolution;
        

        public SerializedYRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            
            renderPath = serializedObject.FindProperty("renderPath");
            enableSRPBatcher = serializedObject.FindProperty("enableSRPBatcher");
            renderScale = serializedObject.FindProperty("renderScale");
            
            antiAliasingMode = serializedObject.FindProperty("antiAliasingMode");
            fxaaMode = serializedObject.FindProperty("fxaaMode");
            
            enableSplitDepth = serializedObject.FindProperty("enableSplitDepth");
            
            reflectionProbeAtlasFormat = serializedObject.FindProperty("reflectionProbeAtlasFormat");
            reflectionProbeAtlasSize = serializedObject.FindProperty("reflectionProbeAtlasSize");
            maxReflectionProbesOnScreen = serializedObject.FindProperty("maxReflectionProbesOnScreen");
            reflectionProbeQuality = serializedObject.FindProperty("reflectionProbeQuality");
            
            enableScreenSpaceAmbientOcclusion = serializedObject.FindProperty("enableScreenSpaceAmbientOcclusion");
            ssgiMode = serializedObject.FindProperty("ssgiMode");
            enableScreenSpaceReflection = serializedObject.FindProperty("enableScreenSpaceReflection");
            
            enableProbeVolumeScreenSpaceIrradiance = serializedObject.FindProperty("enableProbeVolumeScreenSpaceIrradiance");
            probeVolumeSHBands = serializedObject.FindProperty("probeVolumeSHBands");
            probeVolumeMemoryBudget = serializedObject.FindProperty("probeVolumeMemoryBudget");
            supportProbeVolumeGPUStreaming = serializedObject.FindProperty("supportProbeVolumeGPUStreaming");
            supportProbeVolumeDiskStreaming = serializedObject.FindProperty("supportProbeVolumeDiskStreaming");
            supportProbeVolumeScenarios = serializedObject.FindProperty("supportProbeVolumeScenarios");
            supportProbeVolumeScenarioBlending = serializedObject.FindProperty("supportProbeVolumeScenarioBlending");
            probeVolumeBlendingMemoryBudget = serializedObject.FindProperty("probeVolumeBlendingMemoryBudget");
            
            shadowMode = serializedObject.FindProperty("shadowMode");
            sunLightShadowAtlasSize = serializedObject.FindProperty("sunLightShadowAtlasSize");
            maxShadowDistance = serializedObject.FindProperty("maxShadowDistance");
            distanceFade = serializedObject.FindProperty("distanceFade");
            cascadeCount = serializedObject.FindProperty("cascadeCount");
            spiltRatio1 = serializedObject.FindProperty("spiltRatio1");
            spiltRatio2 = serializedObject.FindProperty("spiltRatio2");
            spiltRatio3 = serializedObject.FindProperty("spiltRatio3");
            punctualLightShadowAtlasSize = serializedObject.FindProperty("punctualLightShadowAtlasSize");
            punctualLightShadowQuality = serializedObject.FindProperty("punctualLightShadowQuality");
            
            globalVolumeProfile = serializedObject.FindProperty("globalVolumeProfile");
            bakedLUTResolution = serializedObject.FindProperty("bakedLUTResolution");
        }
        
        public void Update()
        {
            serializedObject.Update();
        }
        
        public void ApplyModifiedProperties()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}