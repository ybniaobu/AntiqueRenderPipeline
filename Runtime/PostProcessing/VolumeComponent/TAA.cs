using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace YPipeline
{
    public enum TAANeighborhood
    {
        [InspectorName("3X3 (9 taps)")] _3X3, [InspectorName("Cross (5 taps)")] Cross
    }
    
    public enum TAAColorSpace
    {
        [InspectorName("YCoCg")] YCoCg, RGB
    }

    public enum AABBMode
    {
        MinMax, Variance
    }

    public enum ColorRectifyMode
    {
        Clamp, ClipToAABBCenter, ClipToFiltered
    }
    
    public enum CurrentFilter
    {
        None, Gaussian
    }
    
    public enum HistoryFilter
    {
        Linear, [InspectorName("Catmull-Rom Bicubic")] CatmullRomBicubic
    }
    
    [System.Serializable]
    public sealed class TAANeighborhoodParameter : VolumeParameter<TAANeighborhood>
    {
        public TAANeighborhoodParameter(TAANeighborhood value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class TAAColorSpaceParameter : VolumeParameter<TAAColorSpace>
    {
        public TAAColorSpaceParameter(TAAColorSpace value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class AABBModeParameter : VolumeParameter<AABBMode>
    {
        public AABBModeParameter(AABBMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class ColorRectifyModeParameter : VolumeParameter<ColorRectifyMode>
    {
        public ColorRectifyModeParameter(ColorRectifyMode value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class CurrentFilterParameter : VolumeParameter<CurrentFilter>
    {
        public CurrentFilterParameter(CurrentFilter value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable]
    public sealed class HistoryFilterParameter : VolumeParameter<HistoryFilter>
    {
        public HistoryFilterParameter(HistoryFilter value, bool overrideState = false) : base(value, overrideState) { }
    }
    
    [System.Serializable, VolumeComponentMenu("Anti-Aliasing/Temporal Anti-Aliasing (TAA)")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class TAA : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("抖动范围参数，默认 1.0 即抖动一个像素，值越大越模糊 Smaller value results in less flicker but more alias")]
        public ClampedFloatParameter jitterScale = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("历史帧混合系数 Determines how much the history is blended with the current frame")]
        public ClampedFloatParameter historyBlendFactor = new ClampedFloatParameter(0.9f, 0.85f, 0.95f);
        
        [Tooltip("采样模式 Using a 3X3 or crossed(5 taps) neighborhood samples")]
        public TAANeighborhoodParameter neighborhood = new TAANeighborhoodParameter(TAANeighborhood._3X3);
        
        [Tooltip("修正历史颜色的颜色空间 YCoCg color space leads to less ghosting")]
        public TAAColorSpaceParameter colorSpace = new TAAColorSpaceParameter(TAAColorSpace.YCoCg);
        
        [Tooltip("建立 AABB 的方式 Variance builds an improved AABB using a statistical method, reducing ghosting artifacts")]
        public AABBModeParameter AABB = new AABBModeParameter(AABBMode.Variance);
        
        [Tooltip("标准差乘数 The critical value of confidence intervals, determining the volume of AABB box. Lower value reduces ghosting but produces more flicking, higher value reduces flicking but produces more ghosting")]
        public ClampedFloatParameter varianceCriticalValue = new ClampedFloatParameter(1.25f, 0.5f, 1.5f);
        
        [Tooltip("修正历史颜色的方式 Rectify invalid history by clamp or clip to the range of neighborhood samples")]
        public ColorRectifyModeParameter colorRectifyMode = new ColorRectifyModeParameter(ColorRectifyMode.ClipToFiltered);
        
        [Tooltip("过滤当前帧以减少闪烁 Filtering current color to reduce flicking")]
        public CurrentFilterParameter currentFilter = new CurrentFilterParameter(CurrentFilter.Gaussian);
        
        [Tooltip("过滤历史以减少模糊 Filtering history to reduce reprojection blur")]
        public HistoryFilterParameter historyFilter = new HistoryFilterParameter(HistoryFilter.CatmullRomBicubic);
        
        [Tooltip("闪烁绝对阈值 A fixed/absolute luma contrast threshold to judge whether a pixel is flicking")]
        public ClampedFloatParameter fixedContrastThreshold = new ClampedFloatParameter(0.0625f, 0.03125f, 0.08333f);
        
        [Tooltip("闪烁相对阈值 A relative luma contrast threshold to judge whether a pixel is flicking")]
        public ClampedFloatParameter relativeContrastThreshold = new ClampedFloatParameter(0.25f, 0.125f, 0.5f);
        
        public bool IsActive() => true;
    }
}