using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class ReflectionProbeExtensions
    {
        public static YPipelineReflectionProbe GetYPipelineReflectionProbe(this ReflectionProbe probe)
        {
            GameObject probeObject = probe.gameObject;
            bool componentExists = probeObject.TryGetComponent<YPipelineReflectionProbe>(out YPipelineReflectionProbe pipelineProbe);
            if(!componentExists) pipelineProbe = probeObject.AddComponent<YPipelineReflectionProbe>();
            return pipelineProbe;
        }
    }
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReflectionProbe))]
    [ExecuteAlways]
    public class YPipelineReflectionProbe : MonoBehaviour, IAdditionalData
    {
        public bool cubemapPreviewByNormal;
        
        public bool isOctahedralAtlasBaked;
        public Texture octahedralAtlasLow;
        public Texture octahedralAtlasMedium;
        public Texture octahedralAtlasHigh;
        public bool showOctahedralAtlas;
        
        public bool isSHBaked;
        public Vector4[] SHData = new Vector4[7];
        public bool showSHProbe;
        public bool SHPreviewByReflection = true;
        
        // Properties
        public ReflectionProbe Probe => GetComponent<ReflectionProbe>();
        public bool IsReady => isOctahedralAtlasBaked && isSHBaked;
    }
}