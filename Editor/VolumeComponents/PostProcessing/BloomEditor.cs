using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(Bloom))]
    public class BloomEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_Mode;
        private SerializedDataParameter m_Intensity;
        private SerializedDataParameter m_FinalIntensity;
        private SerializedDataParameter m_AdditiveStrength;
        private SerializedDataParameter m_Scatter;
        private SerializedDataParameter m_Threshold;
        private SerializedDataParameter m_ThresholdKnee;
        private SerializedDataParameter m_BloomDownscale;
        private SerializedDataParameter m_MaxIterations;
        private SerializedDataParameter m_BicubicUpsampling;
        private SerializedDataParameter m_IgnoreRenderScale;
        
        public override void OnEnable()
        {
            var o = new PropertyFetcher<Bloom>(serializedObject);
            
            m_Mode = Unpack(o.Find(x => x.mode));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_FinalIntensity = Unpack(o.Find(x => x.finalIntensity));
            m_AdditiveStrength = Unpack(o.Find(x => x.additiveStrength));
            m_Scatter = Unpack(o.Find(x => x.scatter));
            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_ThresholdKnee = Unpack(o.Find(x => x.thresholdKnee));
            m_BloomDownscale = Unpack(o.Find(x => x.bloomDownscale));
            m_MaxIterations = Unpack(o.Find(x => x.maxIterations));
            m_BicubicUpsampling = Unpack(o.Find(x => x.bicubicUpsampling));
            m_IgnoreRenderScale = Unpack(o.Find(x => x.ignoreRenderScale));
        }
    
        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);
            
            if (m_Mode.value.enumValueIndex == 0)
            {
                PropertyField(m_Intensity);
                PropertyField(m_AdditiveStrength);
            }
            
            if (m_Mode.value.enumValueIndex == 1)
            {
                PropertyField(m_FinalIntensity, EditorGUIUtility.TrTextContent("Intensity"));
                PropertyField(m_Scatter);
            }
            PropertyField(m_Threshold);
            PropertyField(m_ThresholdKnee);
            PropertyField(m_BloomDownscale);
            PropertyField(m_MaxIterations);
            PropertyField(m_BicubicUpsampling);
            PropertyField(m_IgnoreRenderScale);
        }
    }
}