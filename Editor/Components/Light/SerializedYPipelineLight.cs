using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public class SerializedYPipelineLight : ISerializedLight
    {
        public LightEditor.Settings settings { get; }
        public SerializedObject serializedObject { get; }
        public SerializedObject serializedAdditionalDataObject { get; private set; }
        public YPipelineLight[] lightsAdditionalData;
        public YPipelineLight lightAdditionalData => lightsAdditionalData[0];
        
        // Unity Light Properties
        public SerializedProperty intensity { get; }
        public SerializedProperty shadowsStrength;
        public SerializedProperty shadowsNearPlane;
        
        // YPipeline Light Properties
        public SerializedProperty rangeAttenuationScale;
        
        public SerializedProperty shadowTint;
        public SerializedProperty penumbraTint;
        public SerializedProperty depthBias;
        public SerializedProperty slopeScaledDepthBias;
        public SerializedProperty normalBias;
        public SerializedProperty slopeScaledNormalBias;
        
        public SerializedProperty penumbraWidth;
        public SerializedProperty sampleCount;
        
        public SerializedProperty lightSize;
        public SerializedProperty blockerSearchAreaSizeScale;
        public SerializedProperty penumbraScale;
        public SerializedProperty minPenumbraWidth;
        public SerializedProperty blockerSearchSampleCount;
        public SerializedProperty filterSampleCount;
        

        public SerializedYPipelineLight(SerializedObject serializedObject, LightEditor.Settings settings)
        {
            this.settings = settings;
            settings.OnEnable();
            
            this.serializedObject = serializedObject;
            lightsAdditionalData = CoreEditorUtils.GetAdditionalData<YPipelineLight>(serializedObject.targetObjects);
            serializedAdditionalDataObject = new SerializedObject(lightsAdditionalData);
            
            intensity = serializedObject.FindProperty("m_Intensity");
            shadowsStrength = serializedObject.FindProperty("m_Shadows.m_Strength");
            shadowsNearPlane = serializedObject.FindProperty("m_Shadows.m_NearPlane");
            
            // YPipeline Light Properties
            rangeAttenuationScale = serializedAdditionalDataObject.FindProperty("rangeAttenuationScale");
            
            shadowTint = serializedAdditionalDataObject.FindProperty("shadowTint");
            penumbraTint = serializedAdditionalDataObject.FindProperty("penumbraTint");
            
            depthBias = serializedAdditionalDataObject.FindProperty("depthBias");
            slopeScaledDepthBias = serializedAdditionalDataObject.FindProperty("slopeScaledDepthBias");
            normalBias = serializedAdditionalDataObject.FindProperty("normalBias");
            slopeScaledNormalBias = serializedAdditionalDataObject.FindProperty("slopeScaledNormalBias");
            
            penumbraWidth = serializedAdditionalDataObject.FindProperty("penumbraWidth");
            sampleCount = serializedAdditionalDataObject.FindProperty("sampleCount");
            
            lightSize = serializedAdditionalDataObject.FindProperty("lightSize");
            blockerSearchAreaSizeScale = serializedAdditionalDataObject.FindProperty("blockerSearchAreaSizeScale");
            penumbraScale = serializedAdditionalDataObject.FindProperty("penumbraScale");
            minPenumbraWidth = serializedAdditionalDataObject.FindProperty("minPenumbraWidth");
            blockerSearchSampleCount = serializedAdditionalDataObject.FindProperty("blockerSearchSampleCount");
            filterSampleCount = serializedAdditionalDataObject.FindProperty("filterSampleCount");
        }
        
        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();
            settings.Update();
        }
        
        public void ApplyModifiedProperties()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();
        }

        public void Apply()
        {
            ApplyModifiedProperties();
        }
    }
}