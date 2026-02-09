using System;
using UnityEngine;

namespace YPipeline
{
    public class YPipelineReflectionProbesData : IDisposable
    {
        // ----------------------------------------------------------------------------------------------------
        // Constants
        // ----------------------------------------------------------------------------------------------------
        
        public const int k_MaxReflectionProbeCount = 16;
        public const int k_MaxReflectionProbeCountPerTile = 4;
        public const int k_PerTileDataSize = k_MaxReflectionProbeCountPerTile + 1; // 1 for the header (light count)
        
        // ----------------------------------------------------------------------------------------------------
        // Data
        // ----------------------------------------------------------------------------------------------------

        public Vector2Int atlasSize;
        public int probeCount;
        
        public Vector4[] boxCenter = new Vector4[k_MaxReflectionProbeCount]; // xyz: box center, w: importance
        public Vector4[] boxExtent = new Vector4[k_MaxReflectionProbeCount]; // xyz: box extent, w: box projection
        public Vector4[] SH = new Vector4[k_MaxReflectionProbeCount * 7]; // For reflection probe normalization
        public Vector4[] probeSampleParams = new Vector4[k_MaxReflectionProbeCount]; // xy: uv in atlas, z: height
        public Vector4[] probeParams = new Vector4[k_MaxReflectionProbeCount]; // x: intensity, y: blend distance
        // public Vector4[] rotation = new Vector4[k_MaxReflectionProbeCount]; // 暂未支持 Reflection Probe 的旋转
        
        public Texture[] octahedralAtlas = new Texture[k_MaxReflectionProbeCount];
        
        // ----------------------------------------------------------------------------------------------------
        // Standard Dispose Pattern
        // ----------------------------------------------------------------------------------------------------
        
        private bool m_Disposed = false;
        
        ~YPipelineReflectionProbesData()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    boxCenter = null;
                    boxExtent = null;
                }
            }
            m_Disposed = true;
        }
    }
}