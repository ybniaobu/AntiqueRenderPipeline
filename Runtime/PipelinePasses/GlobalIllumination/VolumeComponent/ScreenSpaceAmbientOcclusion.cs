using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace YPipeline
{
    public enum SSAOMode
    {
        None, SSAO, HBAO, GTAO
    }
    
    [System.Serializable]
    public sealed class AmbientOcclusionModeParameter : VolumeParameter<SSAOMode>
    {
        public AmbientOcclusionModeParameter(SSAOMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Global Illumination/Ambient Occlusion")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class ScreenSpaceAmbientOcclusion : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("屏幕空间环境光遮蔽算法 Choose a screen space ambient occlusion algorithm.")]
        public AmbientOcclusionModeParameter ambientOcclusionMode = new AmbientOcclusionModeParameter(SSAOMode.GTAO, true);
        
        [Tooltip("是否使用半分辨率 If this option is set to true, the effect runs at half resolution. This will increases performance significantly, but also decreases quality.")]
        public BoolParameter halfResolution = new BoolParameter(true);
        
        // SSAO
        [Tooltip("遮蔽强度 Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.")]
        public ClampedFloatParameter ssaoIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("采样次数 Increase the amount of samples will produce higher quality results at a cost of lower performance.")]
        public ClampedIntParameter sampleCount = new ClampedIntParameter(12, 4, 32);
        
        [Tooltip("采样半径 Sampling radius. Bigger the radius, wider ambient occlusion will be achieved.")]
        public ClampedFloatParameter ssaoRadius = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
        
        // HBAO
        [Tooltip("遮蔽强度 Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.")]
        public ClampedFloatParameter hbaoIntensity = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("采样半径 Sampling radius. Bigger the radius, wider ambient occlusion will be achieved.")]
        public ClampedFloatParameter hbaoRadius = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);
        
        [Tooltip("步进方向数 Number of directions on the AO hemisphere.")]
        public ClampedIntParameter hbaoDirectionCount = new ClampedIntParameter(4, 2, 8);
        
        [Tooltip("步进步数 Number of steps during horizon search.")]
        public ClampedIntParameter hbaoStepCount = new ClampedIntParameter(4, 2, 8);
        
        // GTAO
        [Tooltip("遮蔽强度 Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.")]
        public ClampedFloatParameter gtaoIntensity = new ClampedFloatParameter(0.75f, 0.0f, 2.0f);
        
        [Tooltip("采样半径 Sampling radius. Bigger the radius, wider ambient occlusion will be achieved.")]
        public ClampedFloatParameter gtaoRadius = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);
        
        [Tooltip("步进方向数 Number of directions on the AO hemisphere.")]
        public ClampedIntParameter gtaoDirectionCount = new ClampedIntParameter(2, 1, 6);
        
        [Tooltip("步进步数 Number of steps during horizon search.")]
        public ClampedIntParameter gtaoStepCount = new ClampedIntParameter(4, 2, 12);
        
        // Denoise
        public BoolParameter enableBilateralDenoise = new BoolParameter(false, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("过滤核半径 Defines the neighborhood area used for weighted averaging. Larger kernel produces stronger blurring effects.")]
        public ClampedIntParameter kernelRadius = new ClampedIntParameter(4, 0, 16);
        
        [Tooltip("标准差 The standard deviation of the Gaussian function, higher value results in blurrier result.")]
        public ClampedFloatParameter sigma = new ClampedFloatParameter(2.0f, 0.0f, 8.0f);
        
        [Tooltip("深度阈值 Rejects pixel averaging when the depth difference is above depth threshold. Lower value achieves a better effect in edge preservation but could introduces false edges.")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.05f, 0.0f, 0.2f);
        
        public BoolParameter enableTemporalDenoise = new BoolParameter(true, BoolParameter.DisplayType.Checkbox);
        
        [Tooltip("方差临界值 Lower value reduces ghosting but produces more noise and flicking, higher value reduces noise but produces more ghosting.")]
        public ClampedFloatParameter criticalValue = new ClampedFloatParameter(1.0f, 0.5f, 1.5f);
        
        public bool IsActive() => ambientOcclusionMode.value != SSAOMode.None;
    }
}