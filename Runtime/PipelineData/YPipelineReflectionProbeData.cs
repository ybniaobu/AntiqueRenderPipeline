using System;
using UnityEngine;

namespace YPipeline
{
    internal sealed class YPipelineReflectionProbeData : IDisposable
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
        
        public Vector4[] probePositions = new Vector4[k_MaxReflectionProbeCount]; // xyz: position
        public Vector4[] boxCenter = new Vector4[k_MaxReflectionProbeCount]; // xyz: box center, w: importance
        public Vector4[] boxExtent = new Vector4[k_MaxReflectionProbeCount]; // xyz: box extent, w: box projection
        public Vector4[] SH = new Vector4[k_MaxReflectionProbeCount * 7]; // for reflection probe normalization
        public Vector4[] probeSampleParams = new Vector4[k_MaxReflectionProbeCount]; // xy: pixel coordinate in atlas, z: height, w: pack failed = -1 (暂未使用)
        public Vector4[] probeParams = new Vector4[k_MaxReflectionProbeCount]; // x: intensity, y: blend distance
        // public Vector4[] rotation = new Vector4[k_MaxReflectionProbeCount]; // quaternion
        public Matrix4x4[] probeMatrices = new Matrix4x4[k_MaxReflectionProbeCount]; // world to local matrix
        
        public Texture[] octahedralAtlas = new Texture[k_MaxReflectionProbeCount];
        
        // ----------------------------------------------------------------------------------------------------
        // Standard Dispose Pattern
        // ----------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            probePositions = null;
            boxCenter = null;
            boxExtent = null;
            SH = null;
            probeSampleParams = null;
            probeParams = null;
            probeMatrices = null;
            
            octahedralAtlas = null;
        }
    }
}