using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Post Processing/Post Color Grading/Vignette")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class Vignette : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Specifies the color of the vignette.")]
        public ColorParameter color = new ColorParameter(Color.black, false, false, true);
        
        [Tooltip("Sets the vignette center point.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));
        
        [Tooltip("Controls the strength of the Vignette effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        
        [Tooltip("Controls the smoothness of the vignette borders.")]
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);

        [Tooltip("Controls how round the vignette is, lower values result in a more square vignette.")]
        public ClampedFloatParameter roundness = new ClampedFloatParameter(1f, 0f, 1f);
        
        [Tooltip("When enabled, the vignette is perfectly round. When disabled, the vignette matches shape with the current aspect ratio.")]
        public BoolParameter rounded = new BoolParameter(false);
        
        public bool IsActive() => intensity.value > 0f;
    }
}