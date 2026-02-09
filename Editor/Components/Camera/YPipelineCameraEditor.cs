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

        public void OnEnable()
        {
            m_YPipelineCamera = Camera.GetYPipelineCamera();
        }
    }
}
