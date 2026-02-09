using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public struct YPipelineData
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
        public YPipelineLightsData lightsData;
        public YPipelineReflectionProbesData reflectionProbesData;
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public DebugSettings debugSettings;
#endif
        
        // ----------------------------------------------------------------------------------------------------
        // Properties
        // ----------------------------------------------------------------------------------------------------

        public Vector2Int BufferSize => new Vector2Int((int) (camera.pixelWidth * asset.renderScale), (int) (camera.pixelHeight * asset.renderScale));
        public bool IsDeferredRenderingEnabled => asset.renderPath == RenderPath.DeferredPlus;
        public bool IsTAAEnabled => asset.antiAliasingMode == AntiAliasingMode.TAA;
        public bool IsSSAOEnabled => asset.enableScreenSpaceAmbientOcclusion;
        public bool IsSSGIEnabled => asset.enableScreenSpaceGlobalIllumination;
        public bool IsSSREnabled => asset.enableScreenSpaceReflection;
        
        // Store locally the value on the instance due as the Render Pipeline Asset data might change before the disposal of the asset, making some APV Resources leak.
        public bool isAPVEnabled;
        
        // ----------------------------------------------------------------------------------------------------
        // Buffer and Texture Handles
        // ----------------------------------------------------------------------------------------------------
        
        public TextureHandle SunLightShadowMap { set; get; }
        public bool isSunLightShadowMapCreated;
        public TextureHandle SpotLightShadowMap { set; get; }
        public bool isSpotLightShadowMapCreated;
        public TextureHandle PointLightShadowMap { set; get; }
        public bool isPointLightShadowMapCreated;
        
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
        
        public TextureHandle EnvBRDFLut { set; get; }
        public TextureHandle BlueNoise256 { set; get; }
        
        // ----------------------------------------------------------------------------------------------------
        // Structured Buffers
        // ----------------------------------------------------------------------------------------------------
        public BufferHandle PunctualLightBufferHandle { set; get; }
        public BufferHandle PointLightShadowBufferHandle { set; get; }
        public BufferHandle PointLightShadowMatricesBufferHandle { set; get; }
        public BufferHandle SpotLightShadowBufferHandle { set; get; }
        public BufferHandle SpotLightShadowMatricesBufferHandle { set; get; }
        
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
            lightsData.Dispose();
            lightsData = null;
            reflectionProbesData.Dispose();
            reflectionProbesData = null;
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugSettings.Dispose();
            debugSettings = null;
#endif

            SunLightShadowMap = TextureHandle.nullHandle;
            SpotLightShadowMap = TextureHandle.nullHandle;
            PointLightShadowMap =  TextureHandle.nullHandle;
            
            CameraColorTarget = TextureHandle.nullHandle;
            CameraDepthTarget = TextureHandle.nullHandle;
            CameraColorAttachment = TextureHandle.nullHandle;
            CameraDepthAttachment = TextureHandle.nullHandle;
            CameraColorTexture = TextureHandle.nullHandle;
            CameraDepthTexture = TextureHandle.nullHandle;
            ThinGBuffer  = TextureHandle.nullHandle;
            MotionVectorTexture = TextureHandle.nullHandle;
            AmbientOcclusionTexture = TextureHandle.nullHandle;
            TAATarget  = TextureHandle.nullHandle;
            BloomTexture = TextureHandle.nullHandle;
            ColorGradingLutTexture = TextureHandle.nullHandle;
            CameraFinalTexture = TextureHandle.nullHandle;
                
            TAAHistory = TextureHandle.nullHandle;
            EnvBRDFLut = TextureHandle.nullHandle;
            BlueNoise256 = TextureHandle.nullHandle;
            
            PunctualLightBufferHandle = BufferHandle.nullHandle;
            PointLightShadowBufferHandle = BufferHandle.nullHandle;
            PointLightShadowMatricesBufferHandle = BufferHandle.nullHandle;
            SpotLightShadowBufferHandle = BufferHandle.nullHandle;
            SpotLightShadowMatricesBufferHandle = BufferHandle.nullHandle;
            
            TileLightIndicesBufferHandle = BufferHandle.nullHandle;
        }
    }
}