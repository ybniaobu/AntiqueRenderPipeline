using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(TAA))]
    public class TAAEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_JitterScale;
        private SerializedDataParameter m_HistoryBlendFactor;
        private SerializedDataParameter m_Neighborhood;
        private SerializedDataParameter m_ColorSpace;
        private SerializedDataParameter m_AABB;
        private SerializedDataParameter m_VarianceCriticalValue;
        private SerializedDataParameter m_ColorRectifyMode;
        private SerializedDataParameter m_CurrentFilter;
        private SerializedDataParameter m_HistoryFilter;
        private SerializedDataParameter m_RelativeContrastThreshold;
        private SerializedDataParameter m_FixedContrastThreshold;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<TAA>(serializedObject);
            
            m_JitterScale = Unpack(o.Find(x => x.jitterScale));
            m_HistoryBlendFactor = Unpack(o.Find(x => x.historyBlendFactor));
            m_Neighborhood = Unpack(o.Find(x => x.neighborhood));
            m_ColorSpace = Unpack(o.Find(x => x.colorSpace));
            m_AABB = Unpack(o.Find(x => x.AABB));
            m_VarianceCriticalValue = Unpack(o.Find(x => x.varianceCriticalValue));
            m_ColorRectifyMode = Unpack(o.Find(x => x.colorRectifyMode));
            m_CurrentFilter = Unpack(o.Find(x => x.currentFilter));
            m_HistoryFilter = Unpack(o.Find(x => x.historyFilter));
            m_FixedContrastThreshold = Unpack(o.Find(x => x.fixedContrastThreshold));
            m_RelativeContrastThreshold = Unpack(o.Find(x => x.relativeContrastThreshold));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_JitterScale);
            PropertyField(m_HistoryBlendFactor);
            PropertyField(m_Neighborhood);
            PropertyField(m_ColorSpace);
            PropertyField(m_AABB);

            if (m_AABB.value.enumValueIndex == 1)
            {
                PropertyField(m_VarianceCriticalValue);
            }
            
            PropertyField(m_ColorRectifyMode);
            PropertyField(m_CurrentFilter);
            PropertyField(m_HistoryFilter);
            PropertyField(m_FixedContrastThreshold);
            PropertyField(m_RelativeContrastThreshold);
        }
    }
}