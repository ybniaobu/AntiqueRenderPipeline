using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class NearFieldGlobalIlluminationPass : PipelinePass
    {
        private class NFGIPassData
        {
            public bool enableHalfResolution;
            public bool enableTemporalDenoise;
            public bool enableBilateralDenoise;
            
            public ComputeShader nfgiCS;
            public ComputeShader denoiseCS;
            
            public Vector2Int threadGroupSizesFull8;
            public Vector2Int threadGroupSizes1;
            public Vector2Int threadGroupSizes8;
            public Vector2Int threadGroupSizes64;
            
            public Vector4 textureSize;
            public Vector4 nfgiParams;
            public Vector4 nfgiParams2;
            public Vector4 fallbackParams;
            public Vector4 denoiseParams;
            public Vector4 denoiseParams2;
            
            public TextureHandle irradianceTexture;
            public TextureHandle irradianceHistory;
            public TextureHandle transition0;
            public TextureHandle transition1;
            
            public TextureHandle sceneHistory; // TAAHistory or SceneHistory
            public TextureHandle reprojectedSceneHistory;
            
            public TextureHandle halfDepthTexture;
            public TextureHandle halfNormalRoughnessTexture;
            public TextureHandle halfMotionVectorTexture;
            public TextureHandle halfReprojectedSceneHistory;
        }

        private NearFieldGlobalIllumination m_NFGI;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_NFGI = stack.GetComponent<NearFieldGlobalIllumination>();
        }

        protected override void OnDispose()
        {
            m_NFGI = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            bool nfgiEnabled = data.asset.ssgiMode == SSGIMode.NearField;
            nfgiEnabled = nfgiEnabled && Time.frameCount != 0;
            if (!nfgiEnabled) return;

            // TODO：暂时使用 UnsafePass，因为 ComputePass 无法 Copy；
            using (var builder = data.renderGraph.AddUnsafePass<NFGIPassData>("Screen Space Near Field Global Illumination", out var passData))
            {
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                
                passData.nfgiCS = data.runtimeResources.HBILCS;
                passData.denoiseCS = data.runtimeResources.SSGIDenoiseCS;
                passData.enableHalfResolution = m_NFGI.halfResolution.value;
                passData.enableTemporalDenoise = m_NFGI.enableTemporalDenoise.value;
                passData.enableBilateralDenoise = m_NFGI.enableBilateralDenoise.value;
                
                Vector2Int bufferSize = data.BufferSize;
                passData.threadGroupSizesFull8 = new Vector2Int(Mathf.CeilToInt(bufferSize.x / 8.0f),  Mathf.CeilToInt(bufferSize.y / 8.0f));
                Vector2Int textureSize = passData.enableHalfResolution ? bufferSize / 2 : bufferSize;
                passData.textureSize = new Vector4(1f / textureSize.x, 1f / textureSize.y, textureSize.x, textureSize.y);
                passData.threadGroupSizes1 = textureSize;
                int threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 8.0f);
                int threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 8.0f);
                passData.threadGroupSizes8 = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                threadGroupSizeX = Mathf.CeilToInt(textureSize.x / 64.0f);
                threadGroupSizeY = Mathf.CeilToInt(textureSize.y / 64.0f);
                passData.threadGroupSizes64 = new Vector2Int(threadGroupSizeX, threadGroupSizeY);
                
                // Pass Data
                passData.nfgiParams = new Vector4(m_NFGI.nearFieldIntensity.value, m_NFGI.nearFieldRadius.value, m_NFGI.maxScreenPercentage.value, 0);
                passData.nfgiParams2 = new Vector4(m_NFGI.convergeDegree.value, m_NFGI.directionCount.value, m_NFGI.stepCount.value, 0);
                passData.fallbackParams = new Vector4((int)m_NFGI.fallbackMode.value, m_NFGI.farFieldIntensity.value, m_NFGI.farFieldAO.value, m_NFGI.enableTemporalDenoise.value ? 1 : 0);
                passData.denoiseParams = new Vector4(m_NFGI.absoluteDepthThreshold.value, m_NFGI.relativeDepthThreshold.value, 0, 0);
                passData.denoiseParams2 = new Vector4(m_NFGI.kernelRadius.value, m_NFGI.sigma.value, m_NFGI.criticalValue.value, 0);
                
                // Irradiance Texture
                passData.irradianceTexture = data.IrradianceTexture;
                builder.UseTexture(data.IrradianceTexture, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.IrradianceTexture, YPipelineShaderIDs.k_IrradianceTextureID);
                
                // Irradiance Transition Texture
                GraphicsFormat format = GraphicsFormat.R16G16B16A16_SFloat;
                
                if (passData.enableBilateralDenoise || passData.enableTemporalDenoise)
                {
                    TextureDesc transitionDesc0 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = format,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Irradiance Transition0"
                    };
                    passData.transition0 = builder.CreateTransientTexture(transitionDesc0);
                }

                if (passData.enableBilateralDenoise || passData.enableHalfResolution)
                {
                    TextureDesc transitionDesc1 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = format,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Irradiance Transition1"
                    };
                    passData.transition1 = builder.CreateTransientTexture(transitionDesc1);
                }
                
                // Irradiance History
                RenderTextureDescriptor irradianceHistoryDesc = new RenderTextureDescriptor(textureSize.x, textureSize.y)
                {
                    graphicsFormat = format,
                    msaaSamples = 1,
                    mipCount = 0,
                    autoGenerateMips = false,
                };

                if (passData.enableTemporalDenoise)
                {
                    RTHandle irradianceHistory = yCamera.perCameraData.GetIrradianceHistory(ref irradianceHistoryDesc);
                    yCamera.perCameraData.IsIrradianceHistoryReset = false;
                    passData.irradianceHistory = data.renderGraph.ImportTexture(irradianceHistory);
                    builder.UseTexture(passData.irradianceHistory, AccessFlags.ReadWrite);

                    if (passData.enableHalfResolution)
                    {
                        passData.halfMotionVectorTexture = data.HalfMotionVectorTexture;
                        builder.UseTexture(data.HalfMotionVectorTexture, AccessFlags.Read);
                    }
                    else builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                }
                else
                {
                    yCamera.perCameraData.ReleaseIrradianceHistory();
                }
                
                // Other Render Textures
                if (passData.enableHalfResolution)
                {
                    passData.halfDepthTexture = data.HalfDepthTexture;
                    passData.halfNormalRoughnessTexture = data.HalfNormalRoughnessTexture;
                    passData.halfReprojectedSceneHistory = data.HalfReprojectedSceneHistory;
                    builder.UseTexture(data.HalfDepthTexture, AccessFlags.Read);
                    builder.UseTexture(data.HalfNormalRoughnessTexture, AccessFlags.Read);
                    builder.UseTexture(data.HalfReprojectedSceneHistory, AccessFlags.Read);
                }
                else
                {
                    builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                    if (data.IsDeferredRenderingEnabled) builder.UseTexture(data.GBuffer1, AccessFlags.Read);
                    else builder.UseTexture(data.ThinGBuffer, AccessFlags.Read);
                    
                    TextureHandle sceneHistory = data.IsTAAEnabled ? data.TAAHistory : data.SceneHistory;
                    passData.sceneHistory = sceneHistory;
                    builder.UseTexture(sceneHistory, AccessFlags.Read);
                    builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                    
                    TextureDesc reprojectedSceneHistoryDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                    {
                        format = format,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Reprojected Scene History"
                    };
                    
                    passData.reprojectedSceneHistory = builder.CreateTransientTexture(reprojectedSceneHistoryDesc);
                }
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (NFGIPassData data, UnsafeGraphContext context) =>
                {
                    bool enableDenoise = data.enableTemporalDenoise || data.enableBilateralDenoise;
                    bool enableTemporalDenoise = data.enableTemporalDenoise;
                    bool enableBilateralDenoise = data.enableBilateralDenoise;
                    bool enableHalfResolution = data.enableHalfResolution;
                    
                    // Reprojection
                    // 若开启了 Half Resolution, 在 Downsample Pass 中会将 Scene History Reprojection
                    if (!enableHalfResolution)
                    {
                        context.cmd.BeginSample("Scene History Reprojection");
                        int reprojectionKernel = data.nfgiCS.FindKernel("HBILReprojectionKernel");
                        context.cmd.SetComputeTextureParam(data.nfgiCS, reprojectionKernel, "_InputTexture", data.sceneHistory);
                        context.cmd.SetComputeTextureParam(data.nfgiCS, reprojectionKernel, "_OutputTexture", data.reprojectedSceneHistory);
                        context.cmd.DispatchCompute(data.nfgiCS, reprojectionKernel, data.threadGroupSizesFull8.x, data.threadGroupSizesFull8.y, 1);
                        context.cmd.EndSample("Scene History Reprojection");
                    }
                    
                    // HBIL
                    context.cmd.BeginSample("NFGI Compute");
                    
                    LocalKeyword halfResKeyword = new LocalKeyword(data.nfgiCS, YPipelineKeywords.k_HalfResolution);
                    context.cmd.SetKeyword(data.nfgiCS, halfResKeyword, enableHalfResolution);
                    context.cmd.SetComputeVectorParam(data.nfgiCS, "_TextureSize", data.textureSize);
                    context.cmd.SetComputeVectorParam(data.nfgiCS, YPipelineShaderIDs.k_NFGIParamsID, data.nfgiParams);
                    context.cmd.SetComputeVectorParam(data.nfgiCS, YPipelineShaderIDs.k_NFGIParams2ID, data.nfgiParams2);
                    context.cmd.SetComputeVectorParam(data.nfgiCS, YPipelineShaderIDs.k_NFGIFallbackParamsID, data.fallbackParams);
                    
                    int hbgiKernel = data.nfgiCS.FindKernel("HBILAlternateKernel");
                    // int hbgiKernel = data.nfgiCS.FindKernel("HBILKernel");
                    TextureHandle hbilOutput = enableTemporalDenoise ? data.transition0 : data.transition1;
                    hbilOutput = !enableDenoise && !enableHalfResolution ? data.irradianceTexture : hbilOutput;

                    if (enableHalfResolution)
                    {
                        context.cmd.SetComputeTextureParam(data.nfgiCS, hbgiKernel, YPipelineShaderIDs.k_HalfDepthTextureID, data.halfDepthTexture);
                        context.cmd.SetComputeTextureParam(data.nfgiCS, hbgiKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                        context.cmd.SetComputeTextureParam(data.nfgiCS, hbgiKernel, "_InputTexture", data.halfReprojectedSceneHistory);
                    }
                    else
                    {
                        context.cmd.SetComputeTextureParam(data.nfgiCS, hbgiKernel, "_InputTexture", data.reprojectedSceneHistory);
                    }
                    
                    context.cmd.SetComputeTextureParam(data.nfgiCS, hbgiKernel, "_OutputTexture", hbilOutput);
                    context.cmd.DispatchCompute(data.nfgiCS, hbgiKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                    context.cmd.EndSample("NFGI Compute");
                    
                    // Denoise
                    if (enableDenoise || enableHalfResolution)
                    {
                        LocalKeyword halfResKeyword2 = new LocalKeyword(data.denoiseCS, YPipelineKeywords.k_HalfResolution);
                        context.cmd.SetKeyword(data.denoiseCS, halfResKeyword2, enableHalfResolution);
                        context.cmd.SetComputeVectorParam(data.denoiseCS, "_TextureSize", data.textureSize);
                        context.cmd.SetComputeVectorParam(data.denoiseCS, YPipelineShaderIDs.k_SSGIDenoiseParamsID, data.denoiseParams);
                        context.cmd.SetComputeVectorParam(data.denoiseCS, YPipelineShaderIDs.k_SSGIDenoiseParams2ID, data.denoiseParams2);
                    }
                    
                    // Temporal Denoise
                    if (enableTemporalDenoise)
                    {
                        context.cmd.BeginSample("NFGI Temporal Denoise");
                        int temporalKernel = data.denoiseCS.FindKernel("TemporalDenoiseKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, YPipelineShaderIDs.k_IrradianceHistoryID, data.irradianceHistory);
                        if (enableHalfResolution) context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, YPipelineShaderIDs.k_HalfMotionVectorTextureID, data.halfMotionVectorTexture);
                        TextureHandle temporalOutput = enableBilateralDenoise || enableHalfResolution ? data.transition1 : data.irradianceTexture;
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, "_InputTexture", data.transition0);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, "_OutputTexture", temporalOutput);
                        context.cmd.DispatchCompute(data.denoiseCS, temporalKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                        
                        // TODO: 是否改为使用 CS 复制
                        // 可以考虑在 Bilateral Denoise 后 Copy
                        context.cmd.CopyTexture(temporalOutput, data.irradianceHistory);
                        context.cmd.EndSample("NFGI Temporal Denoise");
                    }
                    
                    // Bilateral Denoise
                    if (enableBilateralDenoise)
                    {
                        context.cmd.BeginSample("NFGI Bilateral Denoise");
                        int horizontalKernel = data.denoiseCS.FindKernel("BilateralDenoiseHorizontalKernel");
                        if (enableHalfResolution) context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, "_InputTexture", data.transition1);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, "_OutputTexture", data.transition0);
                        context.cmd.DispatchCompute(data.denoiseCS, horizontalKernel, data.threadGroupSizes64.x, data.threadGroupSizes1.y, 1);

                        int verticalKernel = data.denoiseCS.FindKernel("BilateralDenoiseVerticalKernel");
                        TextureHandle bilateralOutput = enableHalfResolution ? data.transition1 : data.irradianceTexture;
                        if (enableHalfResolution) context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, "_InputTexture", data.transition0);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, "_OutputTexture", bilateralOutput);
                        context.cmd.DispatchCompute(data.denoiseCS, verticalKernel, data.threadGroupSizes1.x, data.threadGroupSizes64.y, 1);
                        context.cmd.EndSample("NFGI Bilateral Denoise");
                    }
                    
                    // Upsample
                    if (enableHalfResolution)
                    {
                        context.cmd.BeginSample("NFGI Upsample");
                        int upsampleKernel = data.denoiseCS.FindKernel("UpsampleKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, "_InputTexture", data.transition1);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, "_OutputTexture", data.irradianceTexture);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                        context.cmd.DispatchCompute(data.denoiseCS, upsampleKernel, data.threadGroupSizesFull8.x, data.threadGroupSizesFull8.y, 1);
                        context.cmd.EndSample("NFGI Upsample");
                    }
                });
            }
        }
    }
}