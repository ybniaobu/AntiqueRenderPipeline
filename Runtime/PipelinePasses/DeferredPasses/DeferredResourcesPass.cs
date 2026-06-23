using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    internal sealed class DeferredResourcesPass : PipelinePass
    {
        private class DeferredResourcesPassData
        {
            public bool isIrradianceTextureCreated;
        }
        
        private RTHandle m_CameraColorTarget;
        private RTHandle m_CameraDepthTarget;
        
        private RTHandle m_EnvBRDFLut;
        private RTHandle m_BlueNoise;
        private RTHandle m_BlueNoise3D;

        protected override void Initialize(ref YPipelineData data)
        {
            m_EnvBRDFLut = RTHandles.Alloc(data.runtimeResources.EnvironmentBRDFLut);
            m_BlueNoise = RTHandles.Alloc(data.runtimeResources.BlueNoise);
            m_BlueNoise3D = RTHandles.Alloc(data.runtimeResources.BlueNoise3D);
        }

        protected override void OnDispose()
        {
            RTHandles.Release(m_CameraColorTarget);
            RTHandles.Release(m_CameraDepthTarget);
            m_CameraColorTarget = null;
            m_CameraDepthTarget = null;
            
            RTHandles.Release(m_EnvBRDFLut);
            RTHandles.Release(m_BlueNoise);
            RTHandles.Release(m_BlueNoise3D);
            m_EnvBRDFLut = null;
            m_BlueNoise = null;
            m_BlueNoise3D = null;
        }
        
        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<DeferredResourcesPassData>("Set Global Resources", out var passData))
            {
                ImportBackBuffers(ref data);
            
                // ----------------------------------------------------------------------------------------------------
                // Imported texture resources
                // ----------------------------------------------------------------------------------------------------
                
                TextureHandle envBRDFLut = data.renderGraph.ImportTexture(m_EnvBRDFLut);
                builder.UseTexture(envBRDFLut, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(envBRDFLut, YPipelineShaderIDs.k_EnvBRDFLutID);
                
                TextureHandle blueNoise = data.renderGraph.ImportTexture(m_BlueNoise);
                builder.UseTexture(blueNoise, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(blueNoise, YPipelineShaderIDs.k_BlueNoiseID);
                
                TextureHandle blueNoise3D = data.renderGraph.ImportTexture(m_BlueNoise3D);
                builder.UseTexture(blueNoise3D, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(blueNoise3D, YPipelineShaderIDs.k_BlueNoise3DID);
                
                // ----------------------------------------------------------------------------------------------------
                // Attachments
                // ----------------------------------------------------------------------------------------------------
                
                Vector2Int bufferSize = data.BufferSize;
                
                TextureDesc colorAttachmentDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Color Attachment"
                };
                
                TextureDesc colorTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    // clearBuffer = true,
                    // clearColor = Color.clear,
                    name = "Color Texture"
                };
                
                TextureDesc depthAttachmentDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    depthBufferBits = DepthBits.Depth32,
                    filterMode = FilterMode.Point,
                    clearBuffer = true,
                    name = "Depth Attachment"
                };
                
                TextureDesc depthTextureDesc = new TextureDesc(bufferSize.x,bufferSize.y)
                {
                    depthBufferBits = DepthBits.Depth32,
                    filterMode = FilterMode.Point,
                    // clearBuffer = true,
                    name = "Depth Texture"
                };
                
                data.CameraColorAttachment = data.renderGraph.CreateTexture(colorAttachmentDesc);
                data.CameraDepthAttachment = data.renderGraph.CreateTexture(depthAttachmentDesc);
                data.CameraColorTexture = data.renderGraph.CreateTexture(colorTextureDesc);
                data.CameraDepthTexture = data.renderGraph.CreateTexture(depthTextureDesc);
                
                // ----------------------------------------------------------------------------------------------------
                // GBuffers
                // GBuffer0 -- RGBA8_SRGB: albedo, AO (注意 alpha 是线性的）
                // GBuffer1 -- RGBA8_UNORM: normal, roughness (跟 Forward 统一，并且 SSSR 可以少采样一个纹理）
                // GBuffer2 -- RGBA8_UNORM: reflectance, metallic, material ID (alpha）
                // GBuffer3 -- R11G11B10_FLOAT: emission
                // ----------------------------------------------------------------------------------------------------
                
                TextureDesc gBuffer0Desc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R8G8B8A8_SRGB,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    name = "GBuffer0"
                };
                
                TextureDesc gBuffer1Desc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode = FilterMode.Point,
                    clearBuffer = true,
                    name = "GBuffer1"
                };
                
                TextureDesc gBuffer2Desc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode = FilterMode.Point,
                    clearBuffer = true,
                    name = "GBuffer2"
                };
                
                TextureDesc gBuffer3Desc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.B10G11R11_UFloatPack32,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = true,
                    name = "GBuffer3"
                };
                
                data.GBuffer0 = data.renderGraph.CreateTexture(gBuffer0Desc);
                data.GBuffer1 = data.renderGraph.CreateTexture(gBuffer1Desc);
                data.GBuffer2 = data.renderGraph.CreateTexture(gBuffer2Desc);
                data.GBuffer3 = data.renderGraph.CreateTexture(gBuffer3Desc);
                
                // ----------------------------------------------------------------------------------------------------
                // Irradiance RT
                // ----------------------------------------------------------------------------------------------------
                
                bool isIrradianceTextureCreated = data.IsSSGIEnabled || data.IsScreenSpaceIrradianceEnabled;
                data.isIrradianceTextureCreated = isIrradianceTextureCreated;
                passData.isIrradianceTextureCreated = isIrradianceTextureCreated;
                if (isIrradianceTextureCreated)
                {
                    TextureDesc irradianceTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                    {
                        format = GraphicsFormat.R16G16B16A16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Irradiance Texture"
                    };
                    data.IrradianceTexture = data.renderGraph.CreateTexture(irradianceTextureDesc);
                }
                
                // ----------------------------------------------------------------------------------------------------
                // Scene History
                // ----------------------------------------------------------------------------------------------------
                
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                if (data.IsTAAEnabled)
                {
                    RenderTextureDescriptor taaHistoryDesc = new RenderTextureDescriptor(bufferSize.x, bufferSize.y)
                    {
                        graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        volumeDepth = 1,
                        msaaSamples = 1,
                        mipCount = 0,
                        autoGenerateMips = false,
                    };
                    
                    RTHandle taaHistory = yCamera.perCameraData.GetTAAHistory(ref taaHistoryDesc);
                    data.TAAHistory = data.renderGraph.ImportTexture(taaHistory);
                }
                else
                {
                    yCamera.perCameraData.ReleaseTAAHistory();
                }
                
                if (data is { IsSSGIEnabled: true, IsTAAEnabled: false })
                {
                    RenderTextureDescriptor sceneHistoryDesc = new RenderTextureDescriptor(bufferSize.x, bufferSize.y)
                    {
                        graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                        volumeDepth = 1,
                        msaaSamples = 1,
                        mipCount = 0,
                        autoGenerateMips = false,
                    };
                    
                    RTHandle sceneHistory =  yCamera.perCameraData.GetSceneHistory(ref sceneHistoryDesc);
                    data.SceneHistory = data.renderGraph.ImportTexture(sceneHistory);
                }
                else
                {
                    yCamera.perCameraData.ReleaseSceneHistory();
                }
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                builder.SetRenderFunc(static (DeferredResourcesPassData data, RasterGraphContext context) =>
                {
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_DeferredRendering, true);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ScreenSpaceIrradiance, data.isIrradianceTextureCreated);
                });
            }
        }
        
        private void ImportBackBuffers(ref YPipelineData data)
        {
            RenderTargetIdentifier targetColorId = data.camera.targetTexture != null ? new RenderTargetIdentifier(data.camera.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = data.camera.targetTexture != null ? new RenderTargetIdentifier(data.camera.targetTexture) : BuiltinRenderTextureType.Depth;
            
            if (m_CameraColorTarget == null || m_CameraColorTarget.nameID != targetColorId)
            {
                m_CameraColorTarget?.Release();
                m_CameraColorTarget = RTHandles.Alloc(targetColorId, "Backbuffer Color");
            }

            if (m_CameraDepthTarget == null || m_CameraDepthTarget.nameID != targetDepthId)
            {
                m_CameraDepthTarget?.Release();
                m_CameraDepthTarget = RTHandles.Alloc(targetDepthId, "Backbuffer Depth");
            }
            
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();
            
            if (data.camera.targetTexture == null)
            {
                importInfoColor.width = Screen.width;
                importInfoColor.height = Screen.height;
                importInfoColor.volumeDepth = 1;
                importInfoColor.msaaSamples = 1;

                importInfoColor.format = SystemInfo.GetGraphicsFormat(data.camera.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

                importInfoDepth = importInfoColor;
                importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }
            else
            {
                importInfoColor.width = data.camera.targetTexture.width;
                importInfoColor.height = data.camera.targetTexture.height;
                importInfoColor.volumeDepth = data.camera.targetTexture.volumeDepth;
                importInfoColor.msaaSamples = data.camera.targetTexture.antiAliasing;
                importInfoColor.format = data.camera.targetTexture.graphicsFormat;

                importInfoDepth = importInfoColor;
                importInfoDepth.format = data.camera.targetTexture.depthStencilFormat;
            }
            
            if (importInfoDepth.format == GraphicsFormat.None)
            {
                throw new System.Exception("In the render graph API, the output Render Texture must have a depth buffer.");
            }
            
            ImportResourceParams importBackbufferParams = new ImportResourceParams()
            {
                clearOnFirstUse = true,
                clearColor = Color.clear,
                discardOnLastUse = false
            };
            
            data.CameraColorTarget = data.renderGraph.ImportTexture(m_CameraColorTarget, importInfoColor, importBackbufferParams);
            data.CameraDepthTarget = data.renderGraph.ImportTexture(m_CameraDepthTarget, importInfoDepth, importBackbufferParams);
        }
    }
}