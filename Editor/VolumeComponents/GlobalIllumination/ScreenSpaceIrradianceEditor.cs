using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(ScreenSpaceIrradiance))]
    internal sealed class ScreenSpaceIrradianceEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_HalfResolution;
        
        private SerializedDataParameter m_AbsoluteDepthThreshold;
        private SerializedDataParameter m_RelativeDepthThreshold;
        private SerializedDataParameter m_EnableTemporalDenoise;
        private SerializedDataParameter m_CriticalValue;
        private SerializedDataParameter m_EnableBilateralDenoise;
        private SerializedDataParameter m_KernelRadius;
        private SerializedDataParameter m_Sigma;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceIrradiance>(serializedObject);
            
            m_HalfResolution = Unpack(o.Find(x => x.halfResolution));
            
            m_AbsoluteDepthThreshold = Unpack(o.Find(x => x.absoluteDepthThreshold));
            m_RelativeDepthThreshold = Unpack(o.Find(x => x.relativeDepthThreshold));
            m_EnableTemporalDenoise = Unpack(o.Find(x => x.enableTemporalDenoise));
            m_CriticalValue = Unpack(o.Find(x => x.criticalValue));
            m_EnableBilateralDenoise = Unpack(o.Find(x => x.enableBilateralDenoise));
            m_KernelRadius = Unpack(o.Find(x => x.kernelRadius));
            m_Sigma = Unpack(o.Find(x => x.sigma));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_HalfResolution);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Denoise Settings", EditorStyles.boldLabel);
            
            PropertyField(m_AbsoluteDepthThreshold);
            PropertyField(m_RelativeDepthThreshold);
            
            PropertyField(m_EnableTemporalDenoise);
            if (m_EnableTemporalDenoise.value.boolValue)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_CriticalValue);
                }
            }
            
            PropertyField(m_EnableBilateralDenoise);
            if (m_EnableBilateralDenoise.value.boolValue)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_KernelRadius);
                    PropertyField(m_Sigma);
                }
            }
        }
    }
}