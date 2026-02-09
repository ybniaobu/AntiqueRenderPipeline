using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum FilmGrainKinds
    {
        Thin1, Thin2,
        Medium1, Medium2, Medium3, Medium4, Medium5, Medium6,
        Large01, Large02,
        Custom
    }
    
    [System.Serializable]
    public sealed class FilmGrainKindsParameter : VolumeParameter<FilmGrainKinds>
    {
        public FilmGrainKindsParameter(FilmGrainKinds value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Post Processing/Post Color Grading/Film Grain")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class FilmGrain : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Specifies the type of grain to use. Select a preset or select \"Custom\" to provide your own Texture.")]
        public FilmGrainKindsParameter type = new FilmGrainKindsParameter(FilmGrainKinds.Thin1);
        
        [Tooltip("Specifies a tileable Texture to use for the grain. The neutral value for this Texture is 0.5 which means that render pipeline does not apply grain at this value.")]
        public Texture2DParameter texture = new Texture2DParameter(null);
        
        [Tooltip("Use the slider to set the strength of the Film Grain effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        
        [Tooltip("Controls the noisiness response curve. The higher you set this value, the less noise there is in brighter areas.")]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);
        
        public bool IsActive()
        {
            return intensity.value > 0f && (type.value != FilmGrainKinds.Custom || texture.value != null);
        }
    }
}