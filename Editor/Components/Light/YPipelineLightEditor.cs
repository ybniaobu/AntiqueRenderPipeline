using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace YPipeline.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Light))]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class YPipelineLightEditor : LightEditor
    {
        public Light Light => target as Light;
        private YPipelineLight m_YPipelineLight;

        protected new void OnEnable()
        {
            base.OnEnable();
            m_YPipelineLight = Light.GetYPipelineLight();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawInnerAndOuterSpotAngle();
        }

        private void DrawInnerAndOuterSpotAngle()
        {
            if (!settings.lightType.hasMultipleDifferentValues &&
                (LightType)settings.lightType.enumValueIndex == LightType.Spot)
            {
                settings.DrawInnerAndOuterSpotAngle();
                settings.ApplyModifiedProperties();
            }
        }

        private void DrawYPipelineLight()
        {

        }
    }
}
