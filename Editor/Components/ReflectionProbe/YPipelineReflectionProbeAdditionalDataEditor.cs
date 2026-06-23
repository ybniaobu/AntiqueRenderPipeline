using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(YPipelineReflectionProbe))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    internal sealed class YPipelineReflectionProbeAdditionalDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
        }
    }
}