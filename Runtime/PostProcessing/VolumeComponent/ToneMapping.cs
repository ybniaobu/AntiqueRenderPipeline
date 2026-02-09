using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    // TODO: 补充 Custom 模式，详见：http://filmicworlds.com/blog/filmic-tonemapping-with-piecewise-power-curves/
    public enum TonemappingMode
    {
        None,
        Reinhard, 
        Uncharted2Filmic,
        KhronosPBRNeutral,
        ACES,
        AGXApproximation
    }
    
    [System.Serializable]
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode>
    {
        public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    public enum ReinhardMode
    {
        Simple,
        Extended, 
        Luminance,
    }
    
    [System.Serializable]
    public sealed class ReinhardModeParameter : VolumeParameter<ReinhardMode>
    {
        public ReinhardModeParameter(ReinhardMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    public enum ACESMode
    {
        Full,
        StephenHillFit,
        ApproximationFit,
    }
    
    [System.Serializable]
    public sealed class ACESModeParameter : VolumeParameter<ACESMode>
    {
        public ACESModeParameter(ACESMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    public enum AgXMode
    {
        Default,
        Golden,
        Punchy
    }
    
    [System.Serializable]
    public sealed class AgXModeParameter : VolumeParameter<AgXMode>
    {
        public AgXModeParameter(AgXMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Post Processing/Color Grading/Tone Mapping")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class ToneMapping : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("色调映射模式 Select a tonemapping operator to use for the color grading process.")]
        public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.ACES, true);
        
        public ReinhardModeParameter reinhardMode = new ReinhardModeParameter(ReinhardMode.Luminance, true);
        
        [Tooltip("场景中白色的最小亮度值，建议设置为场景中最亮的颜色值或亮度值 The smallest luminance that will be mapped to pure white.")]
        public MinFloatParameter minWhite = new MinFloatParameter(5.0f, 0.0f);
        
        public ClampedFloatParameter exposureBias = new ClampedFloatParameter(2.0f, 0.0f, 50.0f);
        
        [Tooltip("从 Full 到 StephenHillFit 再到 ApproximationFit 计算量从高到低")]
        public ACESModeParameter aCESMode = new ACESModeParameter(ACESMode.StephenHillFit);
        
        public AgXModeParameter aGXMode = new AgXModeParameter(AgXMode.Punchy);
        
        public bool IsActive() => mode.value != TonemappingMode.None;
    }
}