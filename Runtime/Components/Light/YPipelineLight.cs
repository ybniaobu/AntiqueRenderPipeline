using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class LightExtensions
    {
        public static YPipelineLight GetYPipelineLight(this Light light)
        {
            GameObject lightObject = light.gameObject;
            bool componentExists = lightObject.TryGetComponent<YPipelineLight>(out YPipelineLight pipelineLight);
            if(!componentExists) pipelineLight = lightObject.AddComponent<YPipelineLight>();
            return pipelineLight;
        }
    }
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [ExecuteAlways]
    public sealed class YPipelineLight : MonoBehaviour, IAdditionalData
    {
        [Range(0f, 1f)] public float rangeAttenuationScale = 0.0f;
        
        public int shadowResolution = 512;
        public Color shadowTint = new Color(0f, 0f, 0f, 1f);
        public Color penumbraTint = new Color(0f, 0f, 0f, 1f);
        
        // Shadow Bias
        [Range(0f, 2f)] public float depthBias = 0.2f;
        [Range(0f, 2f)] public float slopeScaledDepthBias = 0.5f;
        [Range(0f, 2f)] public float normalBias = 0.2f;
        [Range(0f, 2f)] public float slopeScaledNormalBias = 0.5f;
        
        // PCF
        [Range(0f, 1f)] public float penumbraWidth = 0.1f;
        [Range(1, 64)] public int sampleCount = 8;
        
        // PCSS
        [Range(0f, 180f)] public float angularDiameter = 0.5332f;
        [Range(0f, 1f)] public float lightRadius = 0.1f;
        [Range(-2f, 2f)] public float blockerSearchAreaSizeScale = 0.0f;
        [Range(1, 64)] public int blockerSearchSampleCount = 8;
        [Range(-1f, 1f)] public float penumbraScale = 0.0f;
        [Range(0f, 1f)] public float minPenumbraWidth = 0.01f;
        [Range(1, 64)] public int filterSampleCount = 8;
    }
}