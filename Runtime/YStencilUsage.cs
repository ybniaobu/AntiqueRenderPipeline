namespace YPipeline
{
    internal enum YStencilUsage
    {
        Clear = 0,
        StandardPBR = 1 << 0, // deferred lighting
        Hybrid = 1 << 1, // forward in deferred
        
        Decals = 1 << 6,
        MotionVector = 1 << 7,
        
        // Stencil is cleared before post-processing
    }
}