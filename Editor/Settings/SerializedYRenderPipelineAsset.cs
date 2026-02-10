using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public class SerializedYRenderPipelineAsset
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
        public SerializedProperty reflectionProbeQuality;
        public SerializedProperty maxReflectionProbesOnScreen;
        
        public SerializedProperty enableScreenSpaceAmbientOcclusion;
        public SerializedProperty enableScreenSpaceGlobalIllumination;
        public SerializedProperty enableScreenSpaceReflection;
        
        public SerializedProperty probeVolumeSHBands;
        public SerializedProperty probeVolumeMemoryBudget;
        public SerializedProperty probeVolumeBlendingMemoryBudget;
        public SerializedProperty supportProbeVolumeGPUStreaming;
        public SerializedProperty supportProbeVolumeDiskStreaming;
        public SerializedProperty supportProbeVolumeScenarios;
        public SerializedProperty supportProbeVolumeScenarioBlending;
        
        public SerializedProperty shadowMode;
        public SerializedProperty maxShadowDistance;
        public SerializedProperty distanceFade;
        public SerializedProperty cascadeCount;
        public SerializedProperty spiltRatio1;
        public SerializedProperty spiltRatio2;
        public SerializedProperty spiltRatio3;
        public SerializedProperty cascadeEdgeFade;
        public SerializedProperty sunLightShadowMapSize;
        public SerializedProperty spotLightShadowMapSize;
        public SerializedProperty pointLightShadowMapSize;
        
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
            reflectionProbeQuality = serializedObject.FindProperty("reflectionProbeQuality");
            maxReflectionProbesOnScreen = serializedObject.FindProperty("maxReflectionProbesOnScreen");
            
            enableScreenSpaceAmbientOcclusion = serializedObject.FindProperty("enableScreenSpaceAmbientOcclusion");
            enableScreenSpaceGlobalIllumination = serializedObject.FindProperty("enableScreenSpaceGlobalIllumination");
            enableScreenSpaceReflection = serializedObject.FindProperty("enableScreenSpaceReflection");
            
            probeVolumeSHBands = serializedObject.FindProperty("probeVolumeSHBands");
            probeVolumeMemoryBudget = serializedObject.FindProperty("probeVolumeMemoryBudget");
            supportProbeVolumeGPUStreaming = serializedObject.FindProperty("supportProbeVolumeGPUStreaming");
            supportProbeVolumeDiskStreaming = serializedObject.FindProperty("supportProbeVolumeDiskStreaming");
            supportProbeVolumeScenarios = serializedObject.FindProperty("supportProbeVolumeScenarios");
            supportProbeVolumeScenarioBlending = serializedObject.FindProperty("supportProbeVolumeScenarioBlending");
            probeVolumeBlendingMemoryBudget = serializedObject.FindProperty("probeVolumeBlendingMemoryBudget");
            
            shadowMode = serializedObject.FindProperty("shadowMode");
            maxShadowDistance = serializedObject.FindProperty("maxShadowDistance");
            distanceFade = serializedObject.FindProperty("distanceFade");
            cascadeCount = serializedObject.FindProperty("cascadeCount");
            spiltRatio1 = serializedObject.FindProperty("spiltRatio1");
            spiltRatio2 = serializedObject.FindProperty("spiltRatio2");
            spiltRatio3 = serializedObject.FindProperty("spiltRatio3");
            cascadeEdgeFade = serializedObject.FindProperty("cascadeEdgeFade");
            sunLightShadowMapSize = serializedObject.FindProperty("sunLightShadowMapSize");
            spotLightShadowMapSize = serializedObject.FindProperty("spotLightShadowMapSize");
            pointLightShadowMapSize = serializedObject.FindProperty("pointLightShadowMapSize");
            
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