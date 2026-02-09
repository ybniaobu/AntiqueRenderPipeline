using UnityEngine.Rendering;

namespace YPipeline
{
    [GenerateHLSL]
    public enum MaterialID
    {
        Unlit = 0,
        StandardPBR = 1,
        Anisotropic = 2,
        SubsurfaceScattering = 3,
        Cloth = 4,
        ClearCoat = 5,
    }
}