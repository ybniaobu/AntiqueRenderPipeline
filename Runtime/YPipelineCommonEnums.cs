using UnityEngine;

namespace YPipeline
{
    public enum RenderPath
    {
        [InspectorName("Forward+")] ForwardPlus, 
        [InspectorName("Deferred+")] DeferredPlus
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
}