using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class ScreenSpaceIrradiancePass : PipelinePass
    {
        private class ScreenSpaceIrradiancePassData
        {
            public bool enableHalfResolution;
            public bool enableTemporalDenoise;
            public bool enableBilateralDenoise;
            
            public ComputeShader ssiCS;
            public ComputeShader denoiseCS;
            
            public Vector2Int threadGroupSizesFull8;
            public Vector2Int threadGroupSizes1;
            public Vector2Int threadGroupSizes8;
            public Vector2Int threadGroupSizes64;
            
            public Vector4 textureSize;
            public Vector4 denoiseParams;
            public Vector4 denoiseParams2;
            
            public TextureHandle irradianceTexture;
            public TextureHandle irradianceHistory;
            public TextureHandle transition0;
            public TextureHandle transition1;
            
            public TextureHandle halfDepthTexture;
            public TextureHandle halfNormalRoughnessTexture;
            public TextureHandle halfMotionVectorTexture;
        }
        
        private ScreenSpaceIrradiance m_SSI;
        
        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_SSI = stack.GetComponent<ScreenSpaceIrradiance>();
        }

        protected override void OnDispose()
        {
            m_SSI = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            bool ssiEnabled = data.IsScreenSpaceIrradianceEnabled && (!data.IsSSGIEnabled);
            if (!ssiEnabled) return;

            using (var builder = data.renderGraph.AddUnsafePass<ScreenSpaceIrradiancePassData>("Screen Space Irradiance", out var passData))
            {
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();

                passData.ssiCS = data.runtimeResources.ScreenSpaceIrradianceCS;
                passData.denoiseCS = data.runtimeResources.SSGIDenoiseCS;
                passData.enableHalfResolution = m_SSI.halfResolution.value;
                passData.enableTemporalDenoise = m_SSI.enableTemporalDenoise.value;
                passData.enableBilateralDenoise = m_SSI.enableBilateralDenoise.value;
                
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
                
                passData.denoiseParams = new Vector4(m_SSI.absoluteDepthThreshold.value, m_SSI.relativeDepthThreshold.value, 0, 0);
                passData.denoiseParams2 = new Vector4(m_SSI.kernelRadius.value, m_SSI.sigma.value, m_SSI.criticalValue.value);
                
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
                    builder.UseTexture(data.HalfDepthTexture, AccessFlags.Read);
                    builder.UseTexture(data.HalfNormalRoughnessTexture, AccessFlags.Read);
                }
                else
                {
                    builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                    if (data.IsDeferredRenderingEnabled) builder.UseTexture(data.GBuffer1, AccessFlags.Read);
                    else builder.UseTexture(data.ThinGBuffer, AccessFlags.Read);
                }
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (ScreenSpaceIrradiancePassData data, UnsafeGraphContext context) =>
                {
                    bool enableDenoise = data.enableTemporalDenoise || data.enableBilateralDenoise;
                    bool enableTemporalDenoise = data.enableTemporalDenoise;
                    bool enableBilateralDenoise = data.enableBilateralDenoise;
                    bool enableHalfResolution = data.enableHalfResolution;
                    
                    // Irradiance
                    context.cmd.BeginSample("Calculate Irradiance");
                    
                    LocalKeyword halfResKeyword = new LocalKeyword(data.ssiCS, YPipelineKeywords.k_HalfResolution);
                    context.cmd.SetKeyword(data.ssiCS, halfResKeyword, enableHalfResolution);
                    context.cmd.SetComputeVectorParam(data.ssiCS, "_TextureSize", data.textureSize);
                    context.cmd.SetComputeIntParam(data.ssiCS, "_IsTemporalDenoiseEnabled", enableTemporalDenoise ? 1 : 0);
                    
                    int kernel = data.ssiCS.FindKernel("APV");
                    TextureHandle output = enableTemporalDenoise ? data.transition0 : data.transition1;
                    output = !enableDenoise && !enableHalfResolution ? data.irradianceTexture : output;

                    if (enableHalfResolution)
                    {
                        context.cmd.SetComputeTextureParam(data.ssiCS, kernel, YPipelineShaderIDs.k_HalfDepthTextureID, data.halfDepthTexture);
                        context.cmd.SetComputeTextureParam(data.ssiCS, kernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                    }
                    
                    context.cmd.SetComputeTextureParam(data.ssiCS, kernel, "_OutputTexture", output);
                    context.cmd.DispatchCompute(data.ssiCS, kernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                    
                    context.cmd.EndSample("Calculate Irradiance");
                    
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
                        context.cmd.BeginSample("Temporal Denoise");
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
                        context.cmd.EndSample("Temporal Denoise");
                    }
                    
                    // Bilateral Denoise
                    if (enableBilateralDenoise)
                    {
                        context.cmd.BeginSample("Bilateral Denoise");
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
                        context.cmd.EndSample("Bilateral Denoise");
                    }
                    
                    // Upsample
                    if (enableHalfResolution)
                    {
                        context.cmd.BeginSample("Upsample");
                        int upsampleKernel = data.denoiseCS.FindKernel("UpsampleKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, "_InputTexture", data.transition1);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, "_OutputTexture", data.irradianceTexture);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                        context.cmd.DispatchCompute(data.denoiseCS, upsampleKernel, data.threadGroupSizesFull8.x, data.threadGroupSizesFull8.y, 1);
                        context.cmd.EndSample("Upsample");
                    }
                });
            }
        }
    }
}