using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Global Illumination/Screen Space Irradiance")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public sealed class ScreenSpaceIrradiance : VolumeComponent
    {
        [Tooltip("是否使用半分辨率 If this option is set to true, the effect runs at half resolution. This will increases performance significantly, but also decreases quality.")]
        public BoolParameter halfResolution = new BoolParameter(true);
        
        [Tooltip("绝对深度阈值 Rejects pixel averaging when the depth difference is above the value. Lower value achieves a better effect in edge preservation but could introduces false edges.")]
        public ClampedFloatParameter absoluteDepthThreshold = new ClampedFloatParameter(0.5f, 0.0f, 2.0f);
        
        [Tooltip("相对深度阈值 Rejects pixel averaging when the depth difference is above the percentage. Lower percentage achieves a better effect in edge preservation but could introduces false edges.")]
        public ClampedFloatParameter relativeDepthThreshold = new ClampedFloatParameter(0.05f, 0.0f, 0.2f);
        
        public BoolParameter enableTemporalDenoise = new BoolParameter(false, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("方差临界值 Lower value reduces ghosting but produces more noise and flicking, higher value reduces noise but produces more ghosting.")]
        public ClampedFloatParameter criticalValue = new ClampedFloatParameter(1.0f, 0.5f, 1.5f);
        
        public BoolParameter enableBilateralDenoise = new BoolParameter(true, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("过滤核半径 Defines the neighborhood area used for weighted averaging. Larger kernel produces stronger blurring effects.")]
        public ClampedIntParameter kernelRadius = new ClampedIntParameter(4, 0, 16);
        
        [Tooltip("标准差 The standard deviation of the Gaussian function, higher value results in blurrier result.")]
        public ClampedFloatParameter sigma = new ClampedFloatParameter(2.0f, 0.0f, 8.0f);
    }
}