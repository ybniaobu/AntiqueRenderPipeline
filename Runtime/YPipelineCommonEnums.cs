using UnityEngine;

namespace YPipeline
{
    public enum RenderPath
    {
        [InspectorName("Forward+")] ForwardPlus, 
        [InspectorName("Deferred+")] DeferredPlus
    }

    public enum HDRFormat
    {
        R11G11B10, R16G16B16A16
    }
    
    public enum AntiAliasingMode
    {
        None, FXAA, TAA
    }
    
    public enum FXAAMode
    {
        Quality, Console
    }

    public enum SSGIMode
    {
        None, NearField, HZBTracing
    }
    
    public enum ShadowMode
    {
        PCF, PCSS
    }
    
    public enum Quality3Tier
    {
        Low, Medium, High
    }
    
    public enum Quality4Tier
    {
        Low, Medium, High, Epic
    }
    
    public enum ResolutionSize
    {
        [InspectorName("512")] _512 = 512,
        [InspectorName("1024")] _1024 = 1024,
        [InspectorName("2048")] _2048 = 2048,
        [InspectorName("4096")] _4096 = 4096,
    }
    
    public enum SunLightShadowAtlasSize
    {
        [InspectorName("2048")] _2048 = 2048,
        [InspectorName("4096")] _4096 = 4096,
        [InspectorName("8192")] _8192 = 8192,
    }

    public enum PunctualLightShadowAtlasSize : uint
    {
        // A 32-bit uint packs width (high 16 bits) and height (low 16 bits)
        [InspectorName("2048×2048")]    _2048x2048 = 0x08000800,
        [InspectorName("4096×2048")]    _4096x2048 = 0x10000800,
        [InspectorName("4096×4096")]    _4096x4096 = 0x10001000,
        [InspectorName("8192×4096")]    _8192x4096 = 0x20001000,
        [InspectorName("8192×8192")]    _8192x8192 = 0x20002000,
        [InspectorName("16384×8192")]   _16384x8192 = 0x40002000,
        [InspectorName("16384×16384")]  _16384x16384 = 0x40004000,
    }

    public enum ReflectionProbeAtlasSize
    {
        [InspectorName("1536×1024")] _1024 = 1024,
        [InspectorName("3072×2048")] _2048 = 2048,
        [InspectorName("6144×4096")] _4096 = 4096,
        [InspectorName("12288×8192")] _8192 = 8192,
    }
}