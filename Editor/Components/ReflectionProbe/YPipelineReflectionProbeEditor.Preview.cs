using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    public partial class YPipelineReflectionProbeEditor
    {
        UnityEditor.Editor m_CubemapEditor;
        UnityEditor.Editor m_OctahedralCubemapEditor;
        
        public override bool HasPreviewGUI()
        {
            // Only handle one preview for reflection probes
            if (targets.Length > 1)  return false;

            if (ShowOctahedralCubemap())
            {
                if (m_CubemapEditor != null) DestroyCubemapEditor();
                
                if (m_OctahedralCubemapEditor != null && m_OctahedralCubemapEditor.target as Texture != m_YPipelineProbe.octahedralAtlasHigh)
                {
                    DestroyOctahedralCubemapEditor();
                }

                if (HasOctahedralCubemap() && m_OctahedralCubemapEditor == null)
                {
                    m_OctahedralCubemapEditor = CreateEditor(m_YPipelineProbe.octahedralAtlasHigh);
                }
            }
            else
            {
                if (m_OctahedralCubemapEditor != null) DestroyOctahedralCubemapEditor();
                
                if (m_CubemapEditor != null && m_CubemapEditor.target as Texture != Probe.texture)
                {
                    DestroyCubemapEditor();
                }
            
                if (HasCubemap() && m_CubemapEditor == null)
                {
                    m_CubemapEditor = CreateEditor(Probe.texture);
                }
            }
            
            return true;
        }
        
        public override void OnPreviewSettings()
        {
            if (m_CubemapEditor != null) m_CubemapEditor.OnPreviewSettings();
            if (m_OctahedralCubemapEditor != null) m_OctahedralCubemapEditor.OnPreviewSettings();
        }
        
        public override void OnPreviewGUI(Rect position, GUIStyle style)
        {
            if (m_CubemapEditor == null && m_OctahedralCubemapEditor == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color prevColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 1);
                GUILayout.Label("Not Baked/Ready Yet");
                GUI.color = prevColor;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            if (targets.Length == 1)
            {
                m_CubemapEditor?.DrawPreview(position);
                m_OctahedralCubemapEditor?.DrawPreview(position);
            }
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Utility Methods
        // ----------------------------------------------------------------------------------------------------
        
        private bool HasCubemap()
        {
            return (Probe != null && Probe.texture != null);
        }

        private void DestroyCubemapEditor()
        {
            DestroyImmediate(m_CubemapEditor);
            m_CubemapEditor = null;
        }

        private bool ShowOctahedralCubemap()
        {
            return m_YPipelineProbe.showOctahedralAtlas;
        }

        private bool HasOctahedralCubemap()
        {
            return (Probe != null && m_YPipelineProbe != null && m_YPipelineProbe.octahedralAtlasHigh != null);
        }
        
        private void DestroyOctahedralCubemapEditor()
        {
            DestroyImmediate(m_OctahedralCubemapEditor);
            m_OctahedralCubemapEditor = null;
        }
    }
}