using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(YRenderPipelineAsset)), CanEditMultipleObjects]
    internal sealed class YRenderPipelineAssetEditor : UnityEditor.Editor
    {
        private SerializedYRenderPipelineAsset m_SerializedAsset;

        public void OnEnable()
        {
            m_SerializedAsset = new SerializedYRenderPipelineAsset(serializedObject);
        }

        public void OnDisable()
        {
            
        }
        
        public override void OnInspectorGUI()
        {
            m_SerializedAsset.Update();
            
            YRenderPipelineAssetUI.Inspector.Draw(m_SerializedAsset, this);
            
            m_SerializedAsset.ApplyModifiedProperties();
        }
    }
}