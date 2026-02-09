using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [System.Serializable, VolumeComponentMenu("Post Processing/Color Grading/Lift Gamma Gain")]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public class LiftGammaGain : VolumeComponent, IPostProcessComponent
    {
        public Vector4Parameter lift = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        
        public Vector4Parameter gamma = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        
        public Vector4Parameter gain = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));
        
        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return lift != defaultState || gamma != defaultState || gain != defaultState;
        }
    }
}