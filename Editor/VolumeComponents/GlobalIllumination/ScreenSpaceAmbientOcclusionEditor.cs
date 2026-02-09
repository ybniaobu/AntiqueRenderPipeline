using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusion))]
    public class ScreenSpaceAmbientOcclusionEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_AmbientOcclusionMode;
        private SerializedDataParameter m_HalfResolution;
        
        // SSAO
        private SerializedDataParameter m_SSAOIntensity;
        private SerializedDataParameter m_SampleCount;
        private SerializedDataParameter m_SSAORadius;
        
        // HBAO
        private SerializedDataParameter m_HBAOIntensity;
        private SerializedDataParameter m_HBAORadius;
        private SerializedDataParameter m_HBAODirectionCount;
        private SerializedDataParameter m_HBAOStepCount;
        
        // GTAO
        private SerializedDataParameter m_GTAOIntensity;
        private SerializedDataParameter m_GTAORadius;
        private SerializedDataParameter m_GTAODirectionCount;
        private SerializedDataParameter m_GTAOStepCount;
        
        // Denoise
        private SerializedDataParameter m_DepthThreshold;
        private SerializedDataParameter m_EnableTemporalDenoise;
        private SerializedDataParameter m_CriticalValue;
        private SerializedDataParameter m_EnableBilateralDenoise;
        private SerializedDataParameter m_KernelRadius;
        private SerializedDataParameter m_Sigma;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceAmbientOcclusion>(serializedObject);
            
            m_AmbientOcclusionMode = Unpack(o.Find(x => x.ambientOcclusionMode));
            m_HalfResolution = Unpack(o.Find(x => x.halfResolution));
            
            // SSAO
            m_SSAOIntensity = Unpack(o.Find(x => x.ssaoIntensity));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_SSAORadius = Unpack(o.Find(x => x.ssaoRadius));
            
            // HBAO
            m_HBAOIntensity = Unpack(o.Find(x => x.hbaoIntensity));
            m_HBAORadius = Unpack(o.Find(x => x.hbaoRadius));
            m_HBAODirectionCount = Unpack(o.Find(x => x.hbaoDirectionCount));
            m_HBAOStepCount = Unpack(o.Find(x => x.hbaoStepCount));
            
            // GTAO
            m_GTAOIntensity = Unpack(o.Find(x => x.gtaoIntensity));
            m_GTAORadius = Unpack(o.Find(x => x.gtaoRadius));
            m_GTAODirectionCount = Unpack(o.Find(x => x.gtaoDirectionCount));
            m_GTAOStepCount = Unpack(o.Find(x => x.gtaoStepCount));
            
            // Denoise
            m_DepthThreshold = Unpack(o.Find(x => x.depthThreshold));
            m_EnableTemporalDenoise = Unpack(o.Find(x => x.enableTemporalDenoise));
            m_CriticalValue = Unpack(o.Find(x => x.criticalValue));
            m_EnableBilateralDenoise = Unpack(o.Find(x => x.enableBilateralDenoise));
            m_KernelRadius = Unpack(o.Find(x => x.kernelRadius));
            m_Sigma = Unpack(o.Find(x => x.sigma));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_AmbientOcclusionMode);
            
            if (m_AmbientOcclusionMode.value.enumValueIndex == (int) SSAOMode.GTAO)
            {
                EditorGUILayout.HelpBox("注：目前 GTAO 在着色时未使用 Multiple Bounces，并且未实现 Specular Occlusion。", MessageType.Info);
            }

            switch (m_AmbientOcclusionMode.value.enumValueIndex)
            {
                case (int) SSAOMode.None:
                    break;
                case (int) SSAOMode.SSAO:
                    PropertyField(m_HalfResolution);
                    PropertyField(m_SSAOIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                    PropertyField(m_SampleCount);
                    PropertyField(m_SSAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    break;
                case (int) SSAOMode.HBAO:
                    PropertyField(m_HalfResolution);
                    PropertyField(m_HBAOIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                    PropertyField(m_HBAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    PropertyField(m_HBAODirectionCount, EditorGUIUtility.TrTextContent("Direction Count"));
                    PropertyField(m_HBAOStepCount, EditorGUIUtility.TrTextContent("Step Count"));
                    break;
                case (int) SSAOMode.GTAO:
                    PropertyField(m_HalfResolution);
                    PropertyField(m_GTAOIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                    PropertyField(m_GTAORadius, EditorGUIUtility.TrTextContent("Radius"));
                    PropertyField(m_GTAODirectionCount, EditorGUIUtility.TrTextContent("Direction Count"));
                    PropertyField(m_GTAOStepCount, EditorGUIUtility.TrTextContent("Step Count"));
                    break;
                default:
                    break;
            }

            if (m_AmbientOcclusionMode.value.enumValueIndex == (int)SSAOMode.None) return;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Denoise Settings", EditorStyles.boldLabel);
            
            PropertyField(m_DepthThreshold);
            
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