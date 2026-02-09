using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Post Processing/Color Grading/Global Color Corrections")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class GlobalColorCorrections : VolumeComponent, IPostProcessComponent
    {
        [Header("White Balance")]
        [Tooltip("Sets the white balance to a custom color temperature.")]
        public ClampedFloatParameter temperature = new ClampedFloatParameter(0f, -100, 100f);
        
        [Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public ClampedFloatParameter tint = new ClampedFloatParameter(0f, -100, 100f);
        
        [Header("Color Adjustments")]
        [Tooltip("Tint the render by multiplying a color.")]
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);
        
        [Tooltip("Shift the hue of all colors.")]
        public ClampedFloatParameter hue = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        
        [Tooltip("Adjusts the overall exposure of the scene in EV100. This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.")]
        public FloatParameter exposure = new ClampedFloatParameter(0.0f, -10.0f, 10.0f);
        
        [Tooltip("Pushes the intensity of all colors.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Tooltip("Expands or shrinks the overall range of tonal values.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.0f, 0.0f, 2.0f);
        
        [Header("Color Curve")]
        [Tooltip("Affects the luminance across the whole image.")]
        public TextureCurveParameter master = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));
        
        [Tooltip("Affects the red channel intensity across the whole image.")]
        public TextureCurveParameter red = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));
        
        [Tooltip("Affects the green channel intensity across the whole image.")]
        public TextureCurveParameter green = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));
        
        [Tooltip("Affects the blue channel intensity across the whole image.")]
        public TextureCurveParameter blue = new TextureCurveParameter(new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f)));
        
        [Tooltip("Shifts the input hue (x-axis) according to the output hue (y-axis).")]
        public TextureCurveParameter hueVsHue = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, true, new Vector2(0f, 1f)));
        
        [Tooltip("Adjusts saturation (y-axis) according to the input hue (x-axis).")]
        public TextureCurveParameter hueVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, true, new Vector2(0f, 1f)));
        
        [Tooltip("Adjusts saturation (y-axis) according to the input saturation (x-axis).")]
        public TextureCurveParameter satVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, false, new Vector2(0f, 1f)));
        
        [Tooltip("Adjusts saturation (y-axis) according to the input luminance (x-axis).")]
        public TextureCurveParameter lumVsSat = new TextureCurveParameter(new TextureCurve(new Keyframe[] { }, 0.5f, false, new Vector2(0f, 1f)));
        
#pragma warning disable 414
        [SerializeField]
        int m_SelectedCurve = 0; // Only used to track the currently selected curve in the UI
#pragma warning restore 414
        
        public bool IsActive() => true;
    }
}