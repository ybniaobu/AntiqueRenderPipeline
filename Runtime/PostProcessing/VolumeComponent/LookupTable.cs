using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Post Processing/Post Color Grading/LUT")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class LookupTable : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("A 2D Lookup Texture (LUT) to use for color grading.")]
        public TextureParameter texture = new TextureParameter(null);
        
        [Tooltip("How much of the lookup texture will contribute to the color grading effect.")]
        public ClampedFloatParameter contribution = new ClampedFloatParameter(0f, 0f, 1f);
        
        public bool IsActive() => contribution.value > 0f &&  texture.value != null;
    }
}