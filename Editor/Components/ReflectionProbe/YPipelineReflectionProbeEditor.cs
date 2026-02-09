using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using YPipeline;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReflectionProbe))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public partial class YPipelineReflectionProbeEditor : UnityEditor.Editor
    {
        // ----------------------------------------------------------------------------------------------------
        // Fields/Properties
        // ----------------------------------------------------------------------------------------------------
        
        private ReflectionProbe Probe => target as ReflectionProbe;
        private YPipelineReflectionProbe m_YPipelineProbe;
        private SerializedYPipelineReflectionProbe m_SerializedProbe;
        
        public void OnEnable()
        {
            m_YPipelineProbe = Probe.GetYPipelineReflectionProbe();
            m_SerializedProbe = new SerializedYPipelineReflectionProbe(serializedObject);
        }

        public void OnDisable()
        {
            DestroyCubemapEditor();
            DestroyOctahedralCubemapEditor();
        }

        public override void OnInspectorGUI()
        {
            m_SerializedProbe.Update();
            
            YPipelineReflectionProbeUI.Draw(m_SerializedProbe, this);
            
            m_SerializedProbe.ApplyModifiedProperties();
        }
    }
}