using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum BloomMode
    {
        Additive,
        Scattering
    }
    
    public enum BloomDownscaleMode
    {
        Half,
        Quarter,
        HalfQuarter
    }
    
    [System.Serializable]
    public sealed class BloomModeParameter : VolumeParameter<BloomMode>
    {
        public BloomModeParameter(BloomMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class BloomDownscaleParameter : VolumeParameter<BloomDownscaleMode>
    {
        public BloomDownscaleParameter(BloomDownscaleMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Post Processing/Bloom")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class Bloom : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("泛光模式 Choose classical additive or energy-conserving scattering bloom.")]
        public BloomModeParameter mode = new BloomModeParameter(BloomMode.Scattering, true);
        
        [Tooltip("泛光强度 Strength of bloom during the final blit.")]
        public MinFloatParameter intensity = new MinFloatParameter(0.5f, 0.0f, true);
        
        [Tooltip("泛光强度 Strength of bloom during the final blit")]
        public ClampedFloatParameter finalIntensity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f, true);
        
        [Tooltip("泛光上采样加强系数 Boosts the low-res source intensity during the upsampling stage.")]
        public ClampedFloatParameter additiveStrength = new ClampedFloatParameter(0.5f, 0.0f, 2.0f);
        
        [Tooltip("泛光上采样散开插值系数 Interpolates between the high-res and low-res sources during upsampling stage. 1 means that only the low-res is used")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        
        [Tooltip("决定了像素开始泛光的亮度阈值 Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public MinFloatParameter threshold = new MinFloatParameter(4.0f, 0.0f);
        
        [Tooltip("缓和亮度阈值参数的效果 Smooths cutoff effect of the configured threshold. Higher value makes more transition.")]
        public ClampedFloatParameter thresholdKnee = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        
        [Tooltip("泛光模糊开始的分辨率(决定了模糊过程的最大分辨率) The starting resolution that this effect begins processing.")]
        public BloomDownscaleParameter bloomDownscale = new BloomDownscaleParameter(BloomDownscaleMode.Quarter);
        
        [Tooltip("最大迭代次数或泛光金字塔层数(决定了模糊过程的最小分辨率) The maximum number of iterations/Pyramid Levels.")]
        public ClampedIntParameter maxIterations = new ClampedIntParameter(5, 1, 12);
        
        [Tooltip("是否在向上采样阶段使用 Bicubic 插值以获取更平滑的效果（略微更费性能） Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter bicubicUpsampling = new BoolParameter(false);
        
        [Tooltip("Bloom is a resolution-dependent effect, decreasing the render scale will make Bloom effect larger.")]
        public BoolParameter ignoreRenderScale = new BoolParameter(false);
        
        public bool IsActive()
        {
            if (mode.value == BloomMode.Additive) return intensity.value > 0f;
            else return finalIntensity.value > 0f;
        }
    }
}