using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(YPipelineLight))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    internal sealed class YPipelineLightAdditionalDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
        }
    }
}