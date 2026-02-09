using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class YPipelineLightsData : IDisposable
    {
        // ----------------------------------------------------------------------------------------------------
        // Constants
        // ----------------------------------------------------------------------------------------------------
        
        public const int k_MaxDirectionalLightCount = 1;  // Only Support One Directional Light - Sunlight
        public const int k_MaxCascadeCount = 4;
        public const int k_MaxPunctualLightCount = 256;
        public const int k_MaxShadowingSpotLightCount = 64;
        public const int k_MaxShadowingPointLightCount = 12;

        public const int k_MaxLightCountPerTile = 32;
        public const int k_PerTileDataSize = k_MaxLightCountPerTile + 1; // 1 for the header (light count)
        public const int k_TileSize = 16;
        
        // ----------------------------------------------------------------------------------------------------
        // Sun Light
        // ----------------------------------------------------------------------------------------------------
        
        public int sunLightCount;
        public int cascadeCount;
        public int shadowingSunLightCount;
        public int sunLightIndex; // store shadowing sun light visible light index
        public float sunLightNearPlaneOffset;
        
        public Vector4 sunLightColor; // xyz: light color * intensity
        public Vector4 sunLightDirection; // xyz: sun light direction, w: whether is shadowing (1 for shadowing)
        
        public Vector4 sunLightShadowColor; // xyz: shadow color, w: shadow strengths
        public Vector4 sunLightPenumbraColor; // xyz: penumbra color
        public Vector4 sunLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
        public Vector4 sunLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
        public Vector4 sunLightShadowParams2; // x: light diameter, y: blocker search area size z: blocker search sample number, w: min penumbra(filter) width
        
        public Vector4[] cascadeCullingSpheres = new Vector4[k_MaxCascadeCount]; // xyz: culling sphere center, w: culling sphere radius
        public Vector4[] sunLightDepthParams = new Vector4[k_MaxCascadeCount]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
        public Matrix4x4[] sunLightShadowMatrices = new Matrix4x4[k_MaxCascadeCount];
        public Matrix4x4[] sunLightViewMatrices = new Matrix4x4[k_MaxCascadeCount];
        public Matrix4x4[] sunLightProjectionMatrices = new Matrix4x4[k_MaxCascadeCount];
        
        // ----------------------------------------------------------------------------------------------------
        // Punctual Lights
        // ----------------------------------------------------------------------------------------------------
        
        public int punctualLightCount;
        public int shadowingPointLightCount;
        public int shadowingSpotLightCount;
        public int[] shadowingPointLightIndices = new int[k_MaxShadowingPointLightCount]; // store shadowing point light visible light index
        public int[] shadowingSpotLightIndices = new int[k_MaxShadowingSpotLightCount]; // store shadowing spot light visible light index
        
        public Vector4[] punctualLightColors = new Vector4[k_MaxPunctualLightCount]; // xyz: light color * intensity, w: light type (point 1, spot 2)
        public Vector4[] punctualLightPositions = new Vector4[k_MaxPunctualLightCount]; // xyz: light position, w: shadowing spot/point light index (non-shadowing is -1)
        public Vector4[] punctualLightDirections = new Vector4[k_MaxPunctualLightCount]; // xyz: spot light direction
        public Vector4[] punctualLightParams = new Vector4[k_MaxPunctualLightCount]; // x: light range, y: range attenuation scale, z: invAngleRange, w: cosOuterAngle
        
        public Vector4[] pointLightShadowColors = new Vector4[k_MaxShadowingPointLightCount]; // xyz: shadow color, w: shadow strengths
        public Vector4[] pointLightPenumbraColors = new Vector4[k_MaxShadowingPointLightCount]; // xyz: penumbra color
        public Vector4[] pointLightShadowBias = new Vector4[k_MaxShadowingPointLightCount]; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
        public Vector4[] pointLightShadowParams = new Vector4[k_MaxShadowingPointLightCount]; // x: penumbra(filter) width or scale, y: filter sample number
        public Vector4[] pointLightShadowParams2 = new Vector4[k_MaxShadowingPointLightCount]; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
        public Vector4[] pointLightDepthParams = new Vector4[k_MaxShadowingPointLightCount]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
        public Matrix4x4[] pointLightShadowMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
        public Matrix4x4[] pointLightViewMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
        public Matrix4x4[] pointLightProjectionMatrices = new Matrix4x4[k_MaxShadowingPointLightCount * 6];
        
        public Vector4[] spotLightShadowColors = new Vector4[k_MaxShadowingSpotLightCount]; // xyz: shadow color, w: shadow strength
        public Vector4[] spotLightPenumbraColors = new Vector4[k_MaxShadowingSpotLightCount]; // xyz: penumbra color
        public Vector4[] spotLightShadowBias = new Vector4[k_MaxShadowingSpotLightCount]; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
        public Vector4[] spotLightShadowParams = new Vector4[k_MaxShadowingSpotLightCount]; // x: penumbra(filter) width or scale, y: filter sample number
        public Vector4[] spotLightShadowParams2 = new Vector4[k_MaxShadowingSpotLightCount]; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
        public Vector4[] spotLightDepthParams = new Vector4[k_MaxShadowingSpotLightCount]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
        public Matrix4x4[] spotLightShadowMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
        public Matrix4x4[] spotLightViewMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
        public Matrix4x4[] spotLightProjectionMatrices = new Matrix4x4[k_MaxShadowingSpotLightCount];
        
        // ----------------------------------------------------------------------------------------------------
        // Standard Dispose Pattern
        // ----------------------------------------------------------------------------------------------------
        
        private bool m_Disposed = false;
        
        ~YPipelineLightsData()
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
                    //Dispose managed resources
                    cascadeCullingSpheres = null;
                    sunLightShadowMatrices = null;
                    sunLightDepthParams = null;
                    sunLightViewMatrices = null;
                    sunLightProjectionMatrices = null;
                    
                    shadowingPointLightIndices = null;
                    shadowingSpotLightIndices = null;
                    
                    punctualLightColors = null;
                    punctualLightPositions = null;
                    punctualLightDirections = null;
                    punctualLightParams = null;
                    
                    pointLightShadowColors = null;
                    pointLightPenumbraColors = null;
                    pointLightShadowBias = null;
                    pointLightShadowParams = null;
                    pointLightShadowParams2 = null;
                    pointLightDepthParams = null;
                    pointLightShadowMatrices = null;
                    pointLightViewMatrices = null;
                    pointLightProjectionMatrices = null;
                    
                    spotLightShadowColors = null;
                    spotLightPenumbraColors = null;
                    spotLightShadowBias = null;
                    spotLightShadowParams = null;
                    spotLightShadowParams2 = null;
                    spotLightDepthParams = null;
                    spotLightShadowMatrices = null;
                    spotLightViewMatrices = null;
                    spotLightProjectionMatrices = null;
                }
                //Dispose unmanaged resources
                
            }
            m_Disposed = true;
        }
    }
}