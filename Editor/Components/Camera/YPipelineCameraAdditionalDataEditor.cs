using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(YPipelineCamera))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineCameraAdditionalDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
        }
    }
}