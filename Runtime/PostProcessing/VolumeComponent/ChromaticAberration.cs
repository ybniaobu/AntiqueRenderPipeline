using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Post Processing/Post Color Grading/Chromatic Aberration")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class ChromaticAberration : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Use the slider to set the strength of the Chromatic Aberration effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        
        [Tooltip("Controls the maximum number of samples render pipeline uses to render the effect. A lower sample number results in better performance.")]
        public ClampedIntParameter maxSamples = new ClampedIntParameter(6, 3, 24);
        
        public bool IsActive() => intensity.value > 0f;
    }
}