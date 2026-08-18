using UnityEngine.Rendering;

namespace YPipeline
{
    [GenerateHLSL]
    public enum MaterialID
    {
        StandardPBR = 0,
        AdvancedPBR = 1,
        Cloth = 2,
        SubsurfaceScattering = 3,
    }
}