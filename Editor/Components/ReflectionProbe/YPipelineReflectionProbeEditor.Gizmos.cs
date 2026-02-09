using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;

namespace YPipeline.Editor
{
    public partial class YPipelineReflectionProbeEditor
    {
        private static Mesh m_Sphere;
        private static Material m_PreviewMaterial;
        
        public void OnSceneGUI()
        {
            
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Tool Bar
        // ----------------------------------------------------------------------------------------------------
        
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawReflectionProbeGizmoSelected(ReflectionProbe probe, GizmoType gizmoType)
        {
            Color oldColor = Gizmos.color;
            // 绘制 Reflection Probe 影响范围
            Gizmos.color = new Color(1.0f, 0.667f, 0.333f, 1.0f);
            Matrix4x4 probeMatrix = Matrix4x4.TRS(probe.transform.position, Quaternion.identity, Vector3.one);
            Gizmos.matrix = probeMatrix;
            Gizmos.DrawWireCube(probe.center, probe.size);
            
            // 绘制 Blend 影响范围
            Gizmos.DrawWireCube(probe.center, probe.size - new Vector3(probe.blendDistance, probe.blendDistance, probe.blendDistance) * 2.0f);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
            
            // 绘制 Reflection Probe 在 Gizmo 的预览
            if (m_Sphere == null) m_Sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            if (m_PreviewMaterial == null) m_PreviewMaterial = new Material(Shader.Find("Hidden/Preview/ReflectionProbePreview"));
            
            m_PreviewMaterial.SetTexture("_Cubemap", probe.texture);
            m_PreviewMaterial.SetVector("_Cubemap_HDR", probe.textureHDRDecodeValues);
            m_PreviewMaterial.SetInteger("_SampleByNormal", probe.GetYPipelineReflectionProbe().cubemapPreviewByNormal ? 1 : 0);
            m_PreviewMaterial.SetPass(0);
            Graphics.DrawMeshNow(m_Sphere, probeMatrix);

            if (probe.GetYPipelineReflectionProbe().showSHProbe)
            {
                m_PreviewMaterial.SetVectorArray("_SH", probe.GetYPipelineReflectionProbe().SHData);
                m_PreviewMaterial.SetInteger("_SampleByReflection", probe.GetYPipelineReflectionProbe().SHPreviewByReflection ? 1 : 0);
                m_PreviewMaterial.SetPass(1);
                probeMatrix = Matrix4x4.TRS(probe.transform.position + new Vector3(0, 1, 0), Quaternion.identity, Vector3.one);
                Graphics.DrawMeshNow(m_Sphere, probeMatrix);
            }
        }
    }
}