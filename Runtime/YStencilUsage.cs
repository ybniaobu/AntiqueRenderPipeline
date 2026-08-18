namespace YPipeline
{
    internal enum YStencilUsage
    {
        Clear = 0,
        Unlit = 1 << 0,
        StandardPBR = 1 << 1, // deferred lighting
        AdvancedPBR = 1 << 2,
        SubsurfaceScattering = 1 << 3, 
        
        Decals = 1 << 6,
        MotionVector = 1 << 7,
        
        // Stencil is cleared before post-processing
    }
}