using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Camera))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineCameraEditor : UnityEditor.Editor
    {
        public Camera Camera => target as Camera;
        private YPipelineCamera m_YPipelineCamera;
        private SerializedYPipelineCamera m_SerializedCamera;
        private CameraEditor.Settings m_Settings;
        private CameraEditor.Settings settings => m_Settings ??= new CameraEditor.Settings(serializedObject);

        public void OnEnable()
        {
            m_YPipelineCamera = Camera.GetYPipelineCamera();
            m_SerializedCamera = new SerializedYPipelineCamera(serializedObject, settings);
        }
        
        public void OnDisable()
        {
            
        }
        
        public override void OnInspectorGUI()
        {
            m_SerializedCamera.Update();
            
            YPipelineCameraUI.Draw(m_SerializedCamera, this);
            
            m_SerializedCamera.ApplyModifiedProperties();
        }
    }
}
