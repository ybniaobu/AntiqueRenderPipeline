using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using YPipeline;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(YPipelineReflectionProbe))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineReflectionProbeAdditionalDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
        }
    }
}