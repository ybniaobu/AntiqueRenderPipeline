using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public class SerializedYPipelineReflectionProbe
    {
        public SerializedObject serializedObject;
        public SerializedObject serializedAdditionalDataObject;

        public YPipelineReflectionProbe[] reflectionProbesAdditionalData;
        public YPipelineReflectionProbe reflectionProbeAdditionalData => reflectionProbesAdditionalData[0];
        
        // Unity ReflectionProbe Properties
        public SerializedProperty mode;
        public SerializedProperty refreshMode;
        public SerializedProperty timeSlicingMode;
        public SerializedProperty resolution;
        public SerializedProperty shadowDistance;
        public SerializedProperty importance;
        public SerializedProperty boxSize;
        public SerializedProperty boxOffset;
        public SerializedProperty cullingMask;
        public SerializedProperty clearFlags;
        public SerializedProperty backgroundColor;
        public SerializedProperty HDR;
        public SerializedProperty boxProjection;
        public SerializedProperty intensityMultiplier;
        public SerializedProperty blendDistance;
        public SerializedProperty customBakedTexture;
        public SerializedProperty renderDynamicObjects;
        public SerializedProperty useOcclusionCulling;
        public SerializedProperty nearClip;
        public SerializedProperty farClip;
        
        // YPipeline ReflectionProbe Properties
        public SerializedProperty cubemapPreviewByNormal;
        
        public SerializedProperty isOctahedralAtlasBaked;
        public SerializedProperty octahedralAtlasLow;
        public SerializedProperty octahedralAtlasMedium;
        public SerializedProperty octahedralAtlasHigh;
        public SerializedProperty showOctahedralAtlas;
        
        public SerializedProperty isSHBaked;
        public SerializedProperty SHData;
        public SerializedProperty showSHProbe;
        public SerializedProperty SHPreviewByReflection;
        
        public SerializedYPipelineReflectionProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            reflectionProbesAdditionalData = CoreEditorUtils.GetAdditionalData<YPipelineReflectionProbe>(serializedObject.targetObjects);
            serializedAdditionalDataObject = new SerializedObject(reflectionProbesAdditionalData);
            
            mode = serializedObject.FindProperty("m_Mode");
            refreshMode = serializedObject.FindProperty("m_RefreshMode");
            timeSlicingMode = serializedObject.FindProperty("m_TimeSlicingMode");
            resolution = serializedObject.FindProperty("m_Resolution");
            shadowDistance = serializedObject.FindProperty("m_ShadowDistance");
            importance = serializedObject.FindProperty("m_Importance");
            boxSize = serializedObject.FindProperty("m_BoxSize");
            boxOffset = serializedObject.FindProperty("m_BoxOffset");
            cullingMask = serializedObject.FindProperty("m_CullingMask");
            clearFlags = serializedObject.FindProperty("m_ClearFlags");
            backgroundColor = serializedObject.FindProperty("m_BackGroundColor");
            HDR = serializedObject.FindProperty("m_HDR");
            boxProjection = serializedObject.FindProperty("m_BoxProjection");
            intensityMultiplier = serializedObject.FindProperty("m_IntensityMultiplier");
            blendDistance = serializedObject.FindProperty("m_BlendDistance");
            customBakedTexture = serializedObject.FindProperty("m_CustomBakedTexture");
            renderDynamicObjects = serializedObject.FindProperty("m_RenderDynamicObjects");
            useOcclusionCulling = serializedObject.FindProperty("m_UseOcclusionCulling");
            nearClip = serializedObject.FindProperty("m_NearClip");
            farClip = serializedObject.FindProperty("m_FarClip");
            
            cubemapPreviewByNormal = serializedAdditionalDataObject.FindProperty("cubemapPreviewByNormal");
            
            isOctahedralAtlasBaked = serializedAdditionalDataObject.FindProperty("isOctahedralAtlasBaked");
            octahedralAtlasLow = serializedAdditionalDataObject.FindProperty("octahedralAtlasLow");
            octahedralAtlasMedium = serializedAdditionalDataObject.FindProperty("octahedralAtlasMedium");
            octahedralAtlasHigh = serializedAdditionalDataObject.FindProperty("octahedralAtlasHigh");
            showOctahedralAtlas = serializedAdditionalDataObject.FindProperty("showOctahedralAtlas");
            
            isSHBaked = serializedAdditionalDataObject.FindProperty("isSHBaked");
            SHData = serializedAdditionalDataObject.FindProperty("SHData");
            showSHProbe = serializedAdditionalDataObject.FindProperty("showSHProbe");
            SHPreviewByReflection = serializedAdditionalDataObject.FindProperty("SHPreviewByReflection");
        }
        
        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();
        }
        
        public void ApplyModifiedProperties()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
        }
    }
}