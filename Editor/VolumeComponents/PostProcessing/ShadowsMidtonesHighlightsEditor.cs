using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    [CustomEditor(typeof(ShadowsMidtonesHighlights))]
    public class ShadowsMidtonesHighlightsEditor : VolumeComponentEditor
    {
        private static class Styles
        {
            public static readonly GUIContent shadowsLabel = EditorGUIUtility.TrTextContent("Shadows", "Use this to control and apply a hue to the shadows.");
            public static readonly GUIContent midtonesLabel = EditorGUIUtility.TrTextContent("Midtones", "Use this to control and apply a hue to the shadows.");
            public static readonly GUIContent highlightsLabel = EditorGUIUtility.TrTextContent("Highlights", "Use this to control and apply a hue to the shadows.");
        }
        
        private SerializedDataParameter m_Shadows;
        private SerializedDataParameter m_Midtones;
        private SerializedDataParameter m_Highlights;
        private SerializedDataParameter m_ShadowsStart;
        private SerializedDataParameter m_ShadowsEnd;
        private SerializedDataParameter m_HighlightsStart;
        private SerializedDataParameter m_HighlightsEnd;
        
        private readonly TrackballUIDrawer m_TrackballUIDrawer = new TrackballUIDrawer();
        private Rect m_CurveRect;
        private Material m_Material;
        private RenderTexture m_CurveTex;
        
        public override void OnEnable()
        {
            var o = new PropertyFetcher<ShadowsMidtonesHighlights>(serializedObject);
            
            m_Shadows = Unpack(o.Find(x => x.shadows));
            m_Midtones = Unpack(o.Find(x => x.midtones));
            m_Highlights = Unpack(o.Find(x => x.highlights));
            m_ShadowsStart = Unpack(o.Find(x => x.shadowsStart));
            m_ShadowsEnd = Unpack(o.Find(x => x.shadowsEnd));
            m_HighlightsStart = Unpack(o.Find(x => x.highlightsStart));
            m_HighlightsEnd = Unpack(o.Find(x => x.highlightsEnd));
            
            m_Material = new Material(Shader.Find("Hidden/Editor/Shadows Midtones Highlights Curve"));
        }
        
        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_TrackballUIDrawer.OnGUI(m_Shadows.value, enableOverrides ? m_Shadows.overrideState : null, Styles.shadowsLabel, GetWheelValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Midtones.value, enableOverrides ? m_Midtones.overrideState : null, Styles.midtonesLabel, GetWheelValue);
                GUILayout.Space(4f);
                m_TrackballUIDrawer.OnGUI(m_Highlights.value, enableOverrides ? m_Highlights.overrideState : null, Styles.highlightsLabel, GetWheelValue);
            }
            EditorGUILayout.Space(10);
            
            // Reserve GUI space
            m_CurveRect = GUILayoutUtility.GetRect(128, 100);
            m_CurveRect.xMin += EditorGUI.indentLevel * 15f;

            if (Event.current.type == EventType.Repaint)
            {
                float alpha = GUI.enabled ? 1f : 0.4f;
                var limits = new Vector4(m_ShadowsStart.value.floatValue, m_ShadowsEnd.value.floatValue, m_HighlightsStart.value.floatValue, m_HighlightsEnd.value.floatValue);

                m_Material.SetVector("_ShaHiLimits", limits);
                m_Material.SetVector("_Variants", new Vector4(alpha, Mathf.Max(m_HighlightsEnd.value.floatValue, 1f), 0f, 0f));

                CheckCurveRT((int)m_CurveRect.width, (int)m_CurveRect.height);

                var oldRt = RenderTexture.active;
                Graphics.Blit(null, m_CurveTex, m_Material, EditorGUIUtility.isProSkin ? 0 : 1);
                RenderTexture.active = oldRt;

                GUI.DrawTexture(m_CurveRect, m_CurveTex);

                Handles.DrawSolidRectangleWithOutline(m_CurveRect, Color.clear, Color.white * 0.4f);
            }
            
            EditorGUILayout.Space(10);
            PropertyField(m_ShadowsStart);
            m_ShadowsStart.value.floatValue = Mathf.Min(m_ShadowsStart.value.floatValue, m_ShadowsEnd.value.floatValue);
            PropertyField(m_ShadowsEnd);
            m_ShadowsEnd.value.floatValue = Mathf.Max(m_ShadowsStart.value.floatValue, m_ShadowsEnd.value.floatValue);

            PropertyField(m_HighlightsStart);
            m_HighlightsStart.value.floatValue = Mathf.Min(m_HighlightsStart.value.floatValue, m_HighlightsEnd.value.floatValue);
            PropertyField(m_HighlightsEnd);
            m_HighlightsEnd.value.floatValue = Mathf.Max(m_HighlightsStart.value.floatValue, m_HighlightsEnd.value.floatValue);
        }
        
        private void CheckCurveRT(int width, int height)
        {
            if (m_CurveTex == null || !m_CurveTex.IsCreated() || m_CurveTex.width != width || m_CurveTex.height != height)
            {
                CoreUtils.Destroy(m_CurveTex);
                m_CurveTex = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_SRGB);
                m_CurveTex.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        
        private Vector3 GetWheelValue(Vector4 v)
        {
            float w = v.w * (Mathf.Sign(v.w) < 0f ? 1f : 4f);
            return new Vector3(
                Mathf.Max(v.x + w, 0f),
                Mathf.Max(v.y + w, 0f),
                Mathf.Max(v.z + w, 0f)
            );
        }
    }
}