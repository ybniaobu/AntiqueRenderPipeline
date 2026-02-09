using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class ScreenSpaceAmbientOcclusionPass : PipelinePass
    {
        private class AmbientOcclusionPassData
        {
            public SSAOMode aoMode;
            public bool enableHalfResolution;
            public bool enableBilateralDenoise;
            public bool enableTemporalDenoise;
            
            public ComputeShader ssaoCS;
            public ComputeShader denoiseCS;
            
            public Vector2Int threadGroupSizesFull8;
            public Vector2Int threadGroupSizes1;
            public Vector2Int threadGroupSizes8;
            public Vector2Int threadGroupSizes64;
            
            public Vector4 textureSize;
            public Vector4 ssaoParams;
            public int temporalDenoiseEnabled;
            public Vector4 denoiseParams;
            
            public TextureHandle aoTexture;
            public TextureHandle aoHistory;
            public TextureHandle transition0;
            public TextureHandle transition1;
            
            public TextureHandle halfDepthTexture;
            public TextureHandle halfNormalRoughnessTexture;
            public TextureHandle halfMotionVectorTexture;
        }

        private ScreenSpaceAmbientOcclusion m_AO;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_AO = stack.GetComponent<ScreenSpaceAmbientOcclusion>();
        }

        protected override void OnDispose()
        {
            m_AO = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            bool ssaoEnabled = data.IsSSAOEnabled && m_AO.IsActive();
            data.isAmbientOcclusionTextureCreated = ssaoEnabled;
            CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ScreenSpaceAmbientOcclusion, ssaoEnabled);
            if (!ssaoEnabled) return;
            
            // TODO：暂时使用 UnsafePass，因为 ComputePass 无法 Copy；
            using (var builder = data.renderGraph.AddUnsafePass<AmbientOcclusionPassData>("Screen Space Ambient Occlusion", out var passData))
            {
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                
                passData.ssaoCS = data.runtimeResources.SSAOCS;
                passData.denoiseCS = data.runtimeResources.SSAODenoiseCS;
                passData.aoMode = m_AO.ambientOcclusionMode.value;
                passData.enableHalfResolution = m_AO.halfResolution.value;
                passData.enableBilateralDenoise = m_AO.enableBilateralDenoise.value;
                passData.enableTemporalDenoise = m_AO.enableTemporalDenoise.value;
                
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
                switch (passData.aoMode)
                {
                    case SSAOMode.SSAO:
                        passData.ssaoParams = new Vector4(m_AO.ssaoIntensity.value, m_AO.sampleCount.value, m_AO.ssaoRadius.value);
                        break;
                    case SSAOMode.HBAO:
                        passData.ssaoParams = new Vector4(m_AO.hbaoIntensity.value, m_AO.hbaoRadius.value, m_AO.hbaoDirectionCount.value, m_AO.hbaoStepCount.value);
                        break;
                    case SSAOMode.GTAO:
                        passData.ssaoParams = new Vector4(m_AO.gtaoIntensity.value, m_AO.gtaoRadius.value, m_AO.gtaoDirectionCount.value, m_AO.gtaoStepCount.value);
                        break;
                    default:
                        passData.ssaoParams = new Vector4(m_AO.gtaoIntensity.value, m_AO.gtaoRadius.value, m_AO.gtaoDirectionCount.value, m_AO.gtaoStepCount.value);
                        break;
                }
                passData.denoiseParams = new Vector4(m_AO.kernelRadius.value, m_AO.sigma.value, m_AO.depthThreshold.value, m_AO.criticalValue.value);
                passData.temporalDenoiseEnabled = passData.enableTemporalDenoise ? 1 : 0;

                // Create Ambient Occlusion Texture
                TextureDesc aoTextureDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R16G16_SFloat,
                    filterMode = FilterMode.Bilinear,
                    clearBuffer = false,
                    // clearColor = Color.white,
                    enableRandomWrite = true,
                    name = "Ambient Occlusion Texture"
                };

                data.AmbientOcclusionTexture = data.renderGraph.CreateTexture(aoTextureDesc);
                passData.aoTexture = data.AmbientOcclusionTexture;
                builder.UseTexture(data.AmbientOcclusionTexture, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.AmbientOcclusionTexture, YPipelineShaderIDs.k_AmbientOcclusionTextureID);
                
                // Ambient Occlusion Transition Texture
                if (passData.enableBilateralDenoise || passData.enableTemporalDenoise)
                {
                    TextureDesc transitionDesc0 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R16G16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion Transition0"
                    };
                    passData.transition0 = builder.CreateTransientTexture(transitionDesc0);
                }

                if (passData.enableBilateralDenoise || passData.enableHalfResolution)
                {
                    TextureDesc transitionDesc1 = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R16G16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Ambient Occlusion Transition1"
                    };
                    passData.transition1 = builder.CreateTransientTexture(transitionDesc1);
                }
                
                // Ambient Occlusion History
                RenderTextureDescriptor aoHistoryDesc = new RenderTextureDescriptor(textureSize.x, textureSize.y)
                {
                    graphicsFormat = GraphicsFormat.R16G16_SFloat,
                    msaaSamples = 1,
                    mipCount = 0,
                    autoGenerateMips = false,
                };

                if (passData.enableTemporalDenoise)
                {
                    RTHandle aoHistory = yCamera.perCameraData.GetAOHistory(ref aoHistoryDesc);
                    yCamera.perCameraData.IsAOHistoryReset = false;
                    passData.aoHistory = data.renderGraph.ImportTexture(aoHistory);
                    builder.UseTexture(passData.aoHistory, AccessFlags.ReadWrite);

                    if (passData.enableHalfResolution)
                    {
                        passData.halfMotionVectorTexture = data.HalfMotionVectorTexture;
                        builder.UseTexture(data.HalfMotionVectorTexture, AccessFlags.Read);
                    }
                    else builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                }
                else
                {
                    yCamera.perCameraData.ReleaseAOHistory();
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

                builder.SetRenderFunc((AmbientOcclusionPassData data, UnsafeGraphContext context) =>
                {
                    bool enableDenoise = data.enableBilateralDenoise || data.enableTemporalDenoise;
                    bool enableTemporalDenoise = data.enableTemporalDenoise;
                    bool enableBilateralDenoise = data.enableBilateralDenoise;
                    bool enableHalfResolution = data.enableHalfResolution;

                    // SSAO
                    context.cmd.BeginSample("SSAO Compute");
                    LocalKeyword halfResKeyword = new LocalKeyword(data.ssaoCS, "_HALF_RESOLUTION");
                    context.cmd.SetKeyword(data.ssaoCS, halfResKeyword, data.enableHalfResolution);
                    context.cmd.SetComputeVectorParam(data.ssaoCS, "_TextureSize", data.textureSize);
                    context.cmd.SetComputeVectorParam(data.ssaoCS, YPipelineShaderIDs.k_SSAOParamsID, data.ssaoParams);
                    context.cmd.SetComputeIntParam(data.ssaoCS, YPipelineShaderIDs.k_TemporalDenoiseEnabledID, data.temporalDenoiseEnabled);

                    int ssaoKernel;
                    switch (data.aoMode)
                    {
                        case SSAOMode.SSAO:
                            ssaoKernel = data.ssaoCS.FindKernel("SSAOKernel");
                            break;
                        case SSAOMode.HBAO:
                            ssaoKernel = data.ssaoCS.FindKernel("HBAOKernel");
                            break;
                        case SSAOMode.GTAO:
                            ssaoKernel = data.ssaoCS.FindKernel("GTAOKernel");
                            break;
                        default:
                            ssaoKernel = data.ssaoCS.FindKernel("GTAOKernel");
                            break;
                    }
                    
                    TextureHandle occlusionOutput = enableTemporalDenoise ? data.transition0 : data.transition1;
                    occlusionOutput = !enableDenoise && !enableHalfResolution ? data.aoTexture : occlusionOutput;
                    context.cmd.SetComputeTextureParam(data.ssaoCS, ssaoKernel, "_OutputTexture", occlusionOutput);
                    if (data.enableHalfResolution)
                    {
                        context.cmd.SetComputeTextureParam(data.ssaoCS, ssaoKernel,YPipelineShaderIDs.k_HalfDepthTextureID, data.halfDepthTexture);
                        context.cmd.SetComputeTextureParam(data.ssaoCS, ssaoKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                    }
                    context.cmd.DispatchCompute(data.ssaoCS, ssaoKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                    context.cmd.EndSample("SSAO Compute");
                    
                    // Denoise
                    if (enableDenoise || enableHalfResolution)
                    {
                        LocalKeyword halfResKeyword2 = new LocalKeyword(data.denoiseCS, "_HALF_RESOLUTION");
                        context.cmd.SetKeyword(data.denoiseCS, halfResKeyword2, enableHalfResolution);
                        context.cmd.SetComputeVectorParam(data.denoiseCS, "_TextureSize", data.textureSize);
                        context.cmd.SetComputeVectorParam(data.denoiseCS, YPipelineShaderIDs.k_SSAODenoiseParamsID, data.denoiseParams);
                    }
                    
                    // Temporal Denoise
                    if (enableTemporalDenoise)
                    {
                        context.cmd.BeginSample("SSAO Temporal Denoise");
                        int temporalKernel = data.denoiseCS.FindKernel("TemporalDenoiseKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, "_InputTexture", data.transition0);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, YPipelineShaderIDs.k_AmbientOcclusionHistoryID, data.aoHistory);
                        if (enableHalfResolution) context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, YPipelineShaderIDs.k_HalfMotionVectorTextureID, data.halfMotionVectorTexture);
                        TextureHandle temporalOutput = enableBilateralDenoise || enableHalfResolution ? data.transition1 : data.aoTexture;
                        context.cmd.SetComputeTextureParam(data.denoiseCS, temporalKernel, "_OutputTexture", temporalOutput);
                        context.cmd.DispatchCompute(data.denoiseCS, temporalKernel, data.threadGroupSizes8.x, data.threadGroupSizes8.y, 1);
                        
                        // TODO: 是否改为使用 CS 复制
                        // 可以考虑在 Bilateral Denoise 后 Copy
                        context.cmd.CopyTexture(temporalOutput, data.aoHistory);
                        context.cmd.EndSample("SSAO Temporal Denoise");
                    }
                    
                    // Bilateral Denoise
                    if (enableBilateralDenoise)
                    {
                        context.cmd.BeginSample("SSAO Bilateral Denoise");
                        int horizontalKernel = data.denoiseCS.FindKernel("BilateralDenoiseHorizontalKernel");
                        if (enableHalfResolution) context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, "_InputTexture", data.transition1);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, horizontalKernel, "_OutputTexture", data.transition0);
                        context.cmd.DispatchCompute(data.denoiseCS, horizontalKernel, data.threadGroupSizes64.x, data.threadGroupSizes1.y, 1);
                        
                        int verticalKernel = data.denoiseCS.FindKernel("BilateralDenoiseVerticalKernel");
                        TextureHandle bilateralOutput = enableHalfResolution ? data.transition1 : data.aoTexture;
                        if (enableHalfResolution) context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughnessTexture);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, "_InputTexture", data.transition0);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, verticalKernel, "_OutputTexture", bilateralOutput);
                        context.cmd.DispatchCompute(data.denoiseCS, verticalKernel, data.threadGroupSizes1.x, data.threadGroupSizes64.y, 1);
                        context.cmd.EndSample("SSAO Bilateral Denoise");
                    }

                    // Upsample
                    if (enableHalfResolution)
                    {
                        context.cmd.BeginSample("SSAO Upsample");
                        int upsampleKernel = data.denoiseCS.FindKernel("UpsampleKernel");
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, "_InputTexture", data.transition1);
                        context.cmd.SetComputeTextureParam(data.denoiseCS, upsampleKernel, "_OutputTexture", data.aoTexture);
                        context.cmd.DispatchCompute(data.denoiseCS, upsampleKernel, data.threadGroupSizesFull8.x, data.threadGroupSizesFull8.y, 1);
                        context.cmd.EndSample("SSAO Upsample");
                    }
                });
            }
        }
    }
}