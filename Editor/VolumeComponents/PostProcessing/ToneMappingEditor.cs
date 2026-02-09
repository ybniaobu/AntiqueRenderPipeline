using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(ToneMapping))]
    public class ToneMappingEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_Mode;
        private SerializedDataParameter m_ReinhardMode;
        private SerializedDataParameter m_MinWhite;
        private SerializedDataParameter m_ExposureBias;
        private SerializedDataParameter m_ACESMode;
        private SerializedDataParameter m_AGXMode;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ToneMapping>(serializedObject);
            
            m_Mode = Unpack(o.Find(x => x.mode));
            m_ReinhardMode = Unpack(o.Find(x => x.reinhardMode));
            m_MinWhite = Unpack(o.Find(x => x.minWhite));
            m_ExposureBias = Unpack(o.Find(x => x.exposureBias));
            m_ACESMode = Unpack(o.Find(x => x.aCESMode));
            m_AGXMode = Unpack(o.Find(x => x.aGXMode));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);
            int index = m_Mode.value.enumValueIndex;

            if (index == 1)
            {
                PropertyField(m_ReinhardMode);
                if (m_ReinhardMode.value.enumValueIndex != 0) PropertyField(m_MinWhite);
            }
            else if (index == 2)
            {
                PropertyField(m_ExposureBias);
            }
            else if (index == 4)
            {
                PropertyField(m_ACESMode);
            }
            else if (index == 5)
            {
                PropertyField(m_AGXMode);
            }
        }
    }
}