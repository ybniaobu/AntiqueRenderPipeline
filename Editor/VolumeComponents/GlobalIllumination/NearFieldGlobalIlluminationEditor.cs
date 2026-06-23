using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(NearFieldGlobalIllumination))]
    internal sealed class NearFieldGlobalIlluminationEditor : VolumeComponentEditor
    {
        // NFGI (HBIL)
        private SerializedDataParameter m_HalfResolution;
        private SerializedDataParameter m_NearFieldIntensity;
        private SerializedDataParameter m_NearFieldRadius;
        private SerializedDataParameter m_MaxScreenPercentage;
        private SerializedDataParameter m_ConvergeDegree;
        private SerializedDataParameter m_DirectionCount;
        private SerializedDataParameter m_StepCount;
        
        // Fallback
        private SerializedDataParameter m_FallbackMode;
        private SerializedDataParameter m_FarFieldIntensity;
        private SerializedDataParameter m_FarFieldAO;
        
        // Denoise
        private SerializedDataParameter m_AbsoluteDepthThreshold;
        private SerializedDataParameter m_RelativeDepthThreshold;
        private SerializedDataParameter m_EnableTemporalDenoise;
        private SerializedDataParameter m_CriticalValue;
        private SerializedDataParameter m_EnableBilateralDenoise;
        private SerializedDataParameter m_KernelRadius;
        private SerializedDataParameter m_Sigma;

        public override GUIContent GetDisplayTitle()
        {
            return EditorGUIUtility.TrTextContent("Screen Space Near Field Global Illumination");
        }

        public override void OnEnable()
        {
            var o = new PropertyFetcher<NearFieldGlobalIllumination>(serializedObject);
            
            // NFGI (HBIL)
            m_HalfResolution = Unpack(o.Find(x => x.halfResolution));
            m_NearFieldIntensity = Unpack(o.Find(x => x.nearFieldIntensity));
            m_NearFieldRadius = Unpack(o.Find(x => x.nearFieldRadius));
            m_MaxScreenPercentage = Unpack(o.Find(x => x.maxScreenPercentage));
            m_ConvergeDegree = Unpack(o.Find(x => x.convergeDegree));
            m_DirectionCount = Unpack(o.Find(x => x.directionCount));
            m_StepCount = Unpack(o.Find(x => x.stepCount));
            
            // Fallback
            m_FallbackMode = Unpack(o.Find(x => x.fallbackMode));
            m_FarFieldIntensity = Unpack(o.Find(x => x.farFieldIntensity));
            m_FarFieldAO = Unpack(o.Find(x => x.farFieldAO));
            
            // Denoise
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
            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField("Indirect Diffuse Lighting", EditorStyles.boldLabel);
            
            PropertyField(m_HalfResolution);
            PropertyField(m_NearFieldIntensity);
            PropertyField(m_NearFieldRadius);
            PropertyField(m_MaxScreenPercentage);
            PropertyField(m_ConvergeDegree);
            PropertyField(m_DirectionCount);
            PropertyField(m_StepCount);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fallback Settings", EditorStyles.boldLabel);
            
            PropertyField(m_FallbackMode);
            PropertyField(m_FarFieldIntensity);
            PropertyField(m_FarFieldAO);
            
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