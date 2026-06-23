using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class YPipelineData
    {
        // ----------------------------------------------------------------------------------------------------
        // References
        // ----------------------------------------------------------------------------------------------------
        
        public YRenderPipelineAsset asset;
        public YPipelineRuntimeResources runtimeResources;
        
        public RenderGraph renderGraph;
        public ScriptableRenderContext context;
        public Camera camera;
        public CommandBuffer cmd;
        public CullingResults cullingResults;
        
        public YPipelineLightData lightData;
        public YPipelineReflectionProbeData reflectionProbeData;
        
#if UNITY_ASSERTIONS
        public DebugSettings debugSettings;
#endif
        
        // ----------------------------------------------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------------------------------------------

        public Vector2Int BufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
        public bool IsDeferredRenderingEnabled => asset.renderPath == RenderPath.DeferredPlus;
        public bool IsPCSSEnabled => asset.shadowMode == ShadowMode.PCSS;
        public bool IsTAAEnabled => asset.antiAliasingMode == AntiAliasingMode.TAA;
        public bool IsScreenSpaceIrradianceEnabled => asset.enableProbeVolumeScreenSpaceIrradiance && isAPVLoaded;
        public bool IsSSAOEnabled => asset.enableScreenSpaceAmbientOcclusion;
        public bool IsSSGIEnabled => asset.ssgiMode != SSGIMode.None;
        public bool IsSSREnabled => asset.enableScreenSpaceReflection;
        
        // Store locally the value on the instance due as the Render Pipeline Asset data might change before the disposal of the asset, making some APV Resources leak.
        public bool isAPVEnabled;
        public bool isAPVLoaded;
        
        // ----------------------------------------------------------------------------------------------------
        // Buffer and Texture Handles
        // ----------------------------------------------------------------------------------------------------
        
        public TextureHandle ReflectionProbeAtlas { set; get; }
        public bool isReflectionProbeAtlasCreated;
        public TextureHandle SunLightShadowAtlas { set; get; }
        public bool isSunLightShadowAtlasCreated;
        public TextureHandle PunctualLightShadowAtlas { set; get; }
        public bool isPunctualLightShadowAtlasCreated;
        
        public TextureHandle CameraColorTarget { set; get; }
        public TextureHandle CameraDepthTarget { set; get; }
        public TextureHandle CameraColorAttachment { set; get; }
        public TextureHandle CameraDepthAttachment { set; get; }
        public TextureHandle CameraColorTexture { set; get; }
        public TextureHandle CameraDepthTexture { set; get; }
        public TextureHandle GBuffer0 { set; get; } // RGBA8_SRGB: albedo, AO
        public TextureHandle GBuffer1 { set; get; } // RGBA8_UNORM: normal, roughness
        public TextureHandle GBuffer2 { set; get; } // RGBA8_UNORM: reflectance, metallic, material ID (alpha）
        public TextureHandle GBuffer3 { set; get; } // R11G11B10_FLOAT: emission
        public TextureHandle ThinGBuffer { set; get; } // RGBA8_UNORM: normal, roughness
        public TextureHandle MotionVectorTexture { set; get; }
        public TextureHandle HalfDepthTexture { set; get; }
        public TextureHandle HalfNormalRoughnessTexture { set; get; }
        public TextureHandle HalfMotionVectorTexture { set; get; }
        public TextureHandle HalfReprojectedSceneHistory { set; get; }
        public TextureHandle IrradianceTexture { set; get; }
        public bool isIrradianceTextureCreated;
        public TextureHandle AmbientOcclusionTexture { set; get; }
        public bool isAmbientOcclusionTextureCreated;
        public TextureHandle TAATarget { set; get; }
        public TextureHandle BloomTexture { set; get; }
        public TextureHandle ColorGradingLutTexture { set; get; }
        public TextureHandle CameraFinalTexture { set; get; }
        
        // Imported texture resources
        public TextureHandle TAAHistory { set; get; }
        public TextureHandle SceneHistory { set; get; }
        
        // ----------------------------------------------------------------------------------------------------
        // Structured Buffers
        // ----------------------------------------------------------------------------------------------------
        
        public BufferHandle PunctualLightStructuredBufferHandle { set; get; }
        public BufferHandle PunctualLightSliceStructuredBufferHandle { set; get; }
        
        public BufferHandle TileLightIndicesBufferHandle { set; get; }
        public BufferHandle TileReflectionProbeIndicesBufferHandle { set; get; }
        
        // ----------------------------------------------------------------------------------------------------
        // Methods
        // ----------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            asset = null;
            runtimeResources = null;
            renderGraph?.Cleanup();
            renderGraph = null;
            camera = null;
            lightData?.Dispose();
            lightData = null;
            reflectionProbeData?.Dispose();
            reflectionProbeData = null;
            
#if UNITY_ASSERTIONS
            debugSettings?.Dispose();
            debugSettings = null;
#endif

            SunLightShadowAtlas = TextureHandle.nullHandle;
            PunctualLightShadowAtlas = TextureHandle.nullHandle;
            
            CameraColorTarget = TextureHandle.nullHandle;
            CameraDepthTarget = TextureHandle.nullHandle;
            CameraColorAttachment = TextureHandle.nullHandle;
            CameraDepthAttachment = TextureHandle.nullHandle;
            CameraColorTexture = TextureHandle.nullHandle;
            CameraDepthTexture = TextureHandle.nullHandle;
            GBuffer0 = TextureHandle.nullHandle;
            GBuffer1 = TextureHandle.nullHandle;
            GBuffer2 = TextureHandle.nullHandle;
            GBuffer3 = TextureHandle.nullHandle;
            ThinGBuffer  = TextureHandle.nullHandle;
            MotionVectorTexture = TextureHandle.nullHandle;
            HalfDepthTexture = TextureHandle.nullHandle;
            HalfNormalRoughnessTexture = TextureHandle.nullHandle;
            HalfMotionVectorTexture = TextureHandle.nullHandle;
            HalfReprojectedSceneHistory = TextureHandle.nullHandle;
            IrradianceTexture = TextureHandle.nullHandle;
            AmbientOcclusionTexture = TextureHandle.nullHandle;
            TAATarget  = TextureHandle.nullHandle;
            BloomTexture = TextureHandle.nullHandle;
            ColorGradingLutTexture = TextureHandle.nullHandle;
            CameraFinalTexture = TextureHandle.nullHandle;
                
            TAAHistory = TextureHandle.nullHandle;
            SceneHistory = TextureHandle.nullHandle;
            
            PunctualLightStructuredBufferHandle = BufferHandle.nullHandle;
            PunctualLightSliceStructuredBufferHandle = BufferHandle.nullHandle;
            
            TileLightIndicesBufferHandle = BufferHandle.nullHandle;
            TileReflectionProbeIndicesBufferHandle = BufferHandle.nullHandle;
        }
    }
}