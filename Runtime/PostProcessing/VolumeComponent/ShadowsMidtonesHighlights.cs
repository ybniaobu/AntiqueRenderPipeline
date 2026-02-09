using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Post Processing/Color Grading/Shadows Midtones Highlights")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class ShadowsMidtonesHighlights : VolumeComponent, IPostProcessComponent
    {
        public Vector4Parameter shadows = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        public Vector4Parameter midtones = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        public Vector4Parameter highlights = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        
        [Tooltip("Start point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsStart = new MinFloatParameter(0f, 0f);
        
        [Tooltip("End point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsEnd = new MinFloatParameter(0.5f, 0f);
        
        [Tooltip("Start point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsStart = new MinFloatParameter(0.5f, 0f);
        
        [Tooltip("End point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsEnd = new MinFloatParameter(1f, 0f);
        
        public bool IsActive()
        {
            Vector4 defaultState = new Vector4(1f, 1f, 1f, 0f);
            return shadows != defaultState || midtones != defaultState || highlights != defaultState;
        }
    }
}