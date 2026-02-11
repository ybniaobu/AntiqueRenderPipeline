using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Light))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineLightEditor : LightEditor
    {
        private Light Light => target as Light;
        private YPipelineLight m_YPipelineLight;
        private SerializedYPipelineLight m_SerializedLight;
        

        protected override void OnEnable()
        {
            m_YPipelineLight = Light.GetYPipelineLight();
            m_SerializedLight = new SerializedYPipelineLight(serializedObject, settings);
        }
        
        public void OnDisable()
        {
            
        }

        public override void OnInspectorGUI()
        {
            m_SerializedLight.Update();
            
            YPipelineLightUI.Draw(m_SerializedLight, this);
            
            m_SerializedLight.ApplyModifiedProperties();
        }
        
        protected override void OnSceneGUI()
        {
            if (!(target is Light light) || light == null)  return;

            switch (light.type)
            {
                case LightType.Directional:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawDirectionalLightGizmo(light);
                    }
                    break;
                case LightType.Spot:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawSpotLightGizmo(light);
                    }
                    break;
                case LightType.Point:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, Quaternion.identity, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawPointLightGizmo(light);
                    }
                    break;
                case LightType.Rectangle:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawRectangleLightGizmo(light);
                    }
                    break;
                case LightType.Disc:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawDiscLightGizmo(light);
                    }
                    break;
                default:
                    base.OnSceneGUI();
                    break;
            }
        }
    }
}
