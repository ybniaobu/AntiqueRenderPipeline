using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public enum NFGIMode
    {
        None, SSDO, HBIL
    }

    public enum NFGIFallbackMode
    {
        APV = 0, AmbientProbe = 1
    }
    
    [System.Serializable]
    public sealed class NFGIModeParameter : VolumeParameter<NFGIMode>
    {
        public NFGIModeParameter(NFGIMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class NFGIFallbackModeParameter : VolumeParameter<NFGIFallbackMode>
    {
        public NFGIFallbackModeParameter(NFGIFallbackMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Global Illumination/Screen Space Near Field Global Illumination")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class NearFieldGlobalIllumination : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("屏幕空间漫反射全局光照算法 Choose a screen space near-field diffuse global illumination algorithm.")]
        public NFGIModeParameter mode = new NFGIModeParameter(NFGIMode.None, true);
        
        [Tooltip("是否使用半分辨率 If this option is set to true, the effect runs at half resolution. This will increases performance significantly, but also decreases quality.")]
        public BoolParameter halfResolution = new BoolParameter(true);
        
        // HBIL
        [Tooltip("近距离间接光照强度 Controls the strength of the near-field indirect lighting.")]
        public ClampedFloatParameter hbilIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("采样半径 Sampling radius in meters. Bigger the radius, wider the near-field indirect lighting will be achieved.")]
        public MinFloatParameter nearFieldRadius = new MinFloatParameter(5.0f, 0.0f);
        
        [Tooltip("最大采样屏幕比例 Maximum sampling screen ratio, used to limit the sampling radius when the camera is close.")]
        public ClampedFloatParameter maxScreenPercentage = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);
        
        [Tooltip("样本聚集程度 A higher value results in samples being more tightly clustered (concentrated)")]
        public ClampedFloatParameter convergeDegree = new ClampedFloatParameter(1.0f, 1.0f, 2.0f);
        
        [Tooltip("采样方向数量 Number of directions.")]
        public ClampedIntParameter directionCount = new ClampedIntParameter(2, 1, 6);
        
        [Tooltip("步数 Number of steps to take along one direction during horizon search. ")]
        public ClampedIntParameter stepCount = new ClampedIntParameter(4, 2, 12);
        
        // Fallback
        [Tooltip("远距离间接光照模式 Source for the far-field(off-screen) indirect lighting.")]
        public NFGIFallbackModeParameter fallbackMode = new NFGIFallbackModeParameter(NFGIFallbackMode.APV, true);
        
        [Tooltip("远距离间接光照强度 Controls the strength of the far-field(off-screen) indirect lighting.")]
        public ClampedFloatParameter fallbackIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("远距离间接光照遮蔽强度 Controls the strength of the far-field ambient occlusion.")]
        public ClampedFloatParameter farFieldAO = new ClampedFloatParameter(0.75f, 0.0f, 2.0f);
        
        // Denoise
        [Tooltip("绝对深度阈值 Rejects pixel averaging when the depth difference is above the value. Lower value achieves a better effect in edge preservation but could introduces false edges.")]
        public ClampedFloatParameter absoluteDepthThreshold = new ClampedFloatParameter(0.25f, 0.0f, 2.0f);
        
        [Tooltip("相对深度阈值 Rejects pixel averaging when the depth difference is above the percentage. Lower percentage achieves a better effect in edge preservation but could introduces false edges.")]
        public ClampedFloatParameter relativeDepthThreshold = new ClampedFloatParameter(0.05f, 0.0f, 0.2f);
        
        public BoolParameter enableTemporalDenoise = new BoolParameter(true, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("方差临界值 Lower value reduces ghosting but produces more noise and flicking, higher value reduces noise but produces more ghosting.")]
        public ClampedFloatParameter criticalValue = new ClampedFloatParameter(1.0f, 0.5f, 1.5f);
        
        public BoolParameter enableBilateralDenoise = new BoolParameter(true, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("过滤核半径 Defines the neighborhood area used for weighted averaging. Larger kernel produces stronger blurring effects.")]
        public ClampedIntParameter kernelRadius = new ClampedIntParameter(8, 0, 16);
        
        [Tooltip("标准差 The standard deviation of the Gaussian function, higher value results in blurrier result.")]
        public ClampedFloatParameter sigma = new ClampedFloatParameter(4.0f, 0.0f, 8.0f);
        
        public bool IsActive() => mode.value != NFGIMode.None;
    }
}