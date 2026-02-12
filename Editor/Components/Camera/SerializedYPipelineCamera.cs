using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public class SerializedYPipelineCamera : ISerializedCamera
    {
        public CameraEditor.Settings baseCameraSettings { get; }
        public SerializedObject serializedObject { get; }
        public SerializedObject serializedAdditionalDataObject { get; }
        public YPipelineCamera[] camerasAdditionalData;
        
        // Unity Camera Properties
        public SerializedProperty projectionMatrixMode { get; }
        public SerializedProperty dithering { get; }
        public SerializedProperty stopNaNs { get; }
        public SerializedProperty allowDynamicResolution { get; }
        public SerializedProperty volumeLayerMask { get; }
        public SerializedProperty clearDepth { get; }
        public SerializedProperty antialiasing { get; }
        
        // YPipeline Camera Properties
        // 暂无

        public SerializedYPipelineCamera(SerializedObject serializedObject, CameraEditor.Settings settings)
        {
            baseCameraSettings = settings;
            baseCameraSettings.OnEnable();
            
            this.serializedObject = serializedObject;
            camerasAdditionalData = CoreEditorUtils.GetAdditionalData<YPipelineCamera>(serializedObject.targetObjects);
            serializedAdditionalDataObject = new SerializedObject(camerasAdditionalData);
            
            // Unity Camera Properties
            projectionMatrixMode = serializedObject.FindProperty("m_projectionMatrixMode");
            allowDynamicResolution = serializedObject.FindProperty("m_AllowDynamicResolution");
            
            // 待添加 Properties
            // dithering = serializedAdditionalDataObject.FindProperty("m_Dithering");
            // stopNaNs = serializedAdditionalDataObject.FindProperty("m_StopNaN");
            // volumeLayerMask = serializedAdditionalDataObject.FindProperty("m_VolumeLayerMask");
            // clearDepth = serializedAdditionalDataObject.FindProperty("m_ClearDepth");
            // antialiasing = serializedAdditionalDataObject.FindProperty("m_Antialiasing");
            
            // YPipeline Camera Properties
        }
        
        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();
            baseCameraSettings.Update();
        }
        
        public void ApplyModifiedProperties()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
            baseCameraSettings.ApplyModifiedProperties();
        }
        
        public void Apply()
        {
            ApplyModifiedProperties();
        }
        
        public void Refresh()
        {
            
        }
    }
}