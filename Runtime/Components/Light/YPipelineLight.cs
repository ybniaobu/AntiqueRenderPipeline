using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

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
    public class YPipelineLight : MonoBehaviour, IAdditionalData
    {
        [Space(5f)] 
        [Range(0f, 1f)] public float rangeAttenuationScale = 0.0f;
        
        [Space(5f)]
        public Color shadowTint = new Color(0f, 0f, 0f, 1f);
        public Color penumbraTint = new Color(0f, 0f, 0f, 1f);
        
        [Space(5f)]
        [Header("Shadow Bias Settings")]
        [Range(0f, 2f)] public float depthBias = 0.1f;
        [Range(0f, 2f)] public float slopeScaledDepthBias = 0.25f;
        [Range(0f, 2f)] public float normalBias = 0.1f;
        [Range(0f, 2f)] public float slopeScaledNormalBias = 0.25f;
        
        [Space(5f)]
        [Header("PCF Settings")]
        [Range(0f, 0.25f)] public float penumbraWidth = 0.01f;
        [Range(1, 64)] public int sampleNumber = 4;
        
        [Space(5f)]
        [Header("PCSS Settings")]
        [Range(0f, 2f)] public float lightSize = 0.1f;
        [Range(-2f, 2f)] public float blockerSearchAreaSizeScale = 0.0f;
        [Range(-1f, 1f)] public float penumbraScale = 0.0f;
        [Range(0f, 1f)] public float minPenumbraWidth = 0.01f;

        [Range(1, 64)] public int blockerSearchSampleNumber = 8;
        [Range(1, 64)] public int filterSampleNumber = 8;
        
    }
}