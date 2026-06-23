using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector4 = UnityEngine.Vector4;

namespace YPipeline
{
    internal sealed class YPipelineLightData : IDisposable
    {
        // ----------------------------------------------------------------------------------------------------
        // Constants
        // ----------------------------------------------------------------------------------------------------
        
        public const int k_MaxDirectionalLightCount = 1;  // Only support one directional light - sunlight
        public const int k_MaxCascadeCount = 4;
        public const int k_MaxPunctualLightCount = 256;
        public const int k_MaxShadowSliceCount = 256; // About 42 point lights or 256 spot lights
        public const int k_MaxVisibleLightCount = k_MaxPunctualLightCount + k_MaxDirectionalLightCount;

        // Tile-Based Light Culling
        public const int k_MaxLightCountPerTile = 32;
        public const int k_PerTileDataSize = k_MaxLightCountPerTile + 1; // 1 for the header (light count)
        public const int k_TileSize = 16;
        
        // ----------------------------------------------------------------------------------------------------
        // Global Keywords
        // ----------------------------------------------------------------------------------------------------
        
        public bool isPCSSEnabled;
        public bool isSplitDepthEnabled; // Tiled based light culling -- 2.5D culling
        
        // ----------------------------------------------------------------------------------------------------
        // Global Ambient & Reflection Probe
        // ----------------------------------------------------------------------------------------------------

        public Vector4[] ambientProbe = new Vector4[7]; // SH
        public Texture globalReflectionProbe;
        public Vector4 globalHDRDecodeValues;
        
        // ----------------------------------------------------------------------------------------------------
        // Sun Light
        // ----------------------------------------------------------------------------------------------------
        
        // Gathered in Light Data Pass
        public Vector4 sunLightColor; // xyz: light color * intensity
        public Vector4 sunLightDirection; // xyz: sun light direction, w: whether is casting shadow (1 for casting)
        public Vector4 sunLightShadowColor; // xyz: shadow color, w: shadow strengths
        public Vector4 sunLightPenumbraColor; // xyz: penumbra color
        public Vector4 sunLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
        public Vector4 sunLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
        public Vector4 sunLightShadowParams2; // x: light angular diameter, y: blocker search area size z: blocker search sample number, w: min penumbra(filter) width
        
        // Gathered in Shadow Caster Culling
        public int sunLightIndex = -1; // store shadowing sun light visible light index
        public int cascadeCount;
        public Rect[] sunLightViewports = new Rect[k_MaxCascadeCount]; // xy: viewport offset, zw: viewport size
        public Matrix4x4[] sunLightViewMatrices = new Matrix4x4[k_MaxCascadeCount];
        public Matrix4x4[] sunLightProjectionMatrices = new Matrix4x4[k_MaxCascadeCount];
        
        public Vector2Int cascadeAtlasSize; // sun light shadow atlas size
        public Vector4 cascadeParams; // x: maxShadowDistance, y: distanceFade, z: cascadeCount, w: slice size
        public Vector4[] cascadeCullingSpheres = new Vector4[k_MaxCascadeCount]; // xyz: culling sphere center, w: culling sphere radius
        public Vector4[] sunLightDepthParams = new Vector4[k_MaxCascadeCount]; // z: frustum size, x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
        public Matrix4x4[] sunLightShadowMatrices = new Matrix4x4[k_MaxCascadeCount];
        
        // ----------------------------------------------------------------------------------------------------
        // Punctual Light Culling
        // ----------------------------------------------------------------------------------------------------
        
        [StructLayout(LayoutKind.Sequential)]
        public struct LightCullingInputInfos
        {
            public Vector4 bound; // xyz: light position, w: light range
            public Vector4 spotLightInfos; // xyz: -spot light direction, w: half outer angle (point light is -1)
        }
        
        public LightCullingInputInfos[] lightCullingInputInfos = new LightCullingInputInfos[k_MaxPunctualLightCount];
        
        // ----------------------------------------------------------------------------------------------------
        // Punctual Lights
        // ----------------------------------------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        public struct PunctualLightStructuredBuffer
        {
            public Vector4 color; // xyz: light color * intensity, w: light type (point 1, spot 2)
            public Vector4 position; // xyz: light position, w: slice index (non-shadowing is -1)
            public Vector4 direction; // xyz: spot light direction
            public Vector4 lightParams; // x: light range, y: range attenuation scale, z: invAngleRange, w: cosOuterAngle
            public Vector4 shadowColor; // xyz: shadow color, w: shadow strengths
            public Vector4 penumbraColor; // xyz: penumbra color
            public Vector4 shadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
            public Vector4 shadowParams; // x: penumbra(filter) width or scale, y: filter sample number
            public Vector4 shadowParams2; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PunctualLightSliceStructuredBuffer
        {
            public Vector4 sampleParams; // xy: pixel coordinate in atlas, z: shadow slice size, w: pack failed = -1 (暂未使用)
            public Vector4 depthParams; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
            public Matrix4x4 shadowMatrix; // shadow matrix for the punctual light shadow slice
        }
        
        // Gathered in Light Data Pass
        public int punctualLightCount;
        
        public PunctualLightStructuredBuffer[] punctualLightsData = new PunctualLightStructuredBuffer[k_MaxPunctualLightCount];
        
        // Gathered in Shadow Caster Culling
        public int punctualLightSliceCount;
        public Vector2Int punctualLightAtlasSize; // punctual light shadow atlas size
        public int[] punctualLightSliceIdxToVisibleIdx = new int[k_MaxShadowSliceCount];
        public int[] punctualLightVisibleIdxToSliceIdx = new int[k_MaxVisibleLightCount]; // -1 for no slice in atlas
        public Vector4[] punctualLightSampleParams = new Vector4[k_MaxShadowSliceCount]; // For texture atlas packing convenience
        public Matrix4x4[] punctualLightViewMatrices = new Matrix4x4[k_MaxShadowSliceCount];
        public Matrix4x4[] punctualLightProjectionMatrices = new Matrix4x4[k_MaxShadowSliceCount];
        
        public PunctualLightSliceStructuredBuffer[] punctualLightSlicesData = new PunctualLightSliceStructuredBuffer[k_MaxShadowSliceCount];
        
        // ----------------------------------------------------------------------------------------------------
        // Dispose
        // ----------------------------------------------------------------------------------------------------
        
        public void Dispose()
        {
            ambientProbe = null;
            globalReflectionProbe = null;
            
            sunLightViewports = null;
            sunLightViewMatrices = null;
            sunLightProjectionMatrices = null;
            cascadeCullingSpheres = null;
            sunLightDepthParams = null;
            sunLightShadowMatrices = null;

            lightCullingInputInfos = null;

            punctualLightsData = null;
            
            punctualLightSliceIdxToVisibleIdx = null;
            punctualLightVisibleIdxToSliceIdx = null;
            punctualLightSampleParams = null;
            punctualLightViewMatrices = null;
            punctualLightProjectionMatrices = null;
            punctualLightSlicesData = null;
        }
    }
}