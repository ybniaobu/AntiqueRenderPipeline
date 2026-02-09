using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DownSamplePass : PipelinePass
    {
        private class DownSamplePassData
        {
            public ComputeShader cs;
            public bool temporalDenoiseEnabled;
            public bool ssgiEnabled;
            
            public Vector2Int threadGroupSizes;
            public Vector4 textureSize;
            
            public TextureHandle halfDepth;
            public TextureHandle halfNormalRoughness;
            public TextureHandle halfMotionVector;
            public TextureHandle halfReprojectedSceneHistory;
            public TextureHandle sceneHistoryInput;
        }
        
        private ScreenSpaceGlobalIllumination m_SSGI;
        private ScreenSpaceAmbientOcclusion m_SSAO;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_SSGI = stack.GetComponent<ScreenSpaceGlobalIllumination>();
            m_SSAO = stack.GetComponent<ScreenSpaceAmbientOcclusion>();
        }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            bool needDownsample = (data.IsSSGIEnabled && m_SSGI.IsActive() && m_SSGI.halfResolution.value) 
                                  || (data.IsSSAOEnabled && m_SSAO.IsActive() && m_SSAO.halfResolution.value);
            if (!needDownsample) return;
            
            using (var builder = data.renderGraph.AddComputePass<DownSamplePassData>("Downsample", out var passData))
            {
                bool temporalDenoiseEnabled = m_SSGI.enableTemporalDenoise.value || m_SSAO.enableTemporalDenoise.value;
                bool ssgiEnabled = m_SSGI.IsActive();
                
                passData.cs = data.runtimeResources.DownSampleCS;
                passData.temporalDenoiseEnabled = temporalDenoiseEnabled;
                passData.ssgiEnabled = ssgiEnabled;
                Vector2Int textureSize = data.BufferSize / 2;
                passData.threadGroupSizes = new Vector2Int(Mathf.CeilToInt(textureSize.x / 8.0f), Mathf.CeilToInt(textureSize.y / 8.0f));
                passData.textureSize = new Vector4(1f / textureSize.x, 1f / textureSize.y, textureSize.x, textureSize.y);
                
                // Half Depth Texture
                TextureDesc halfDepthDesc = new TextureDesc(textureSize.x, textureSize.y)
                {
                    format = GraphicsFormat.R32_SFloat,
                    filterMode = FilterMode.Point,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Half Depth Texture"
                };

                data.HalfDepthTexture = data.renderGraph.CreateTexture(halfDepthDesc);
                passData.halfDepth = data.HalfDepthTexture;
                builder.UseTexture(data.HalfDepthTexture, AccessFlags.Write);
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                
                // Half Normal Rougness Texture
                TextureDesc halfNormalRoughnessDesc = new TextureDesc(textureSize.x, textureSize.y)
                {
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode = FilterMode.Point,
                    clearBuffer = false,
                    enableRandomWrite = true,
                    name = "Half Normal Roughness Texture"
                };
                
                data.HalfNormalRoughnessTexture = data.renderGraph.CreateTexture(halfNormalRoughnessDesc);
                passData.halfNormalRoughness = data.HalfNormalRoughnessTexture;
                builder.UseTexture(data.HalfNormalRoughnessTexture, AccessFlags.Write);
                if (data.IsDeferredRenderingEnabled) builder.UseTexture(data.GBuffer1, AccessFlags.Read);
                else builder.UseTexture(data.ThinGBuffer, AccessFlags.Read);
                
                // Half Motion Vector Texture
                if (temporalDenoiseEnabled)
                {
                    TextureDesc halfMotionVectorDesc = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        colorFormat = GraphicsFormat.R16G16_SFloat,
                        filterMode = FilterMode.Point,
                        clearBuffer = false,
                        enableRandomWrite = true,
                        name = "Half Motion Vector Texture"
                    };

                    data.HalfMotionVectorTexture = data.renderGraph.CreateTexture(halfMotionVectorDesc);
                    passData.halfMotionVector = data.HalfMotionVectorTexture;
                    builder.UseTexture(data.HalfMotionVectorTexture, AccessFlags.Write);
                    builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                }
                
                // Half Reprojected Scene History
                if (ssgiEnabled)
                {
                    TextureDesc halfSceneHistoryDesc = new TextureDesc(textureSize.x, textureSize.y)
                    {
                        format = GraphicsFormat.R16G16B16A16_SFloat,
                        filterMode = FilterMode.Bilinear,
                        clearBuffer = true,
                        enableRandomWrite = true,
                        name = "Half Reprojected Scene History"
                    };
                    
                    data.HalfReprojectedSceneHistory = data.renderGraph.CreateTexture(halfSceneHistoryDesc);
                    passData.halfReprojectedSceneHistory = data.HalfReprojectedSceneHistory;
                    builder.UseTexture(data.HalfReprojectedSceneHistory, AccessFlags.Write);

                    TextureHandle sceneHistoryInput = data.IsTAAEnabled ? data.TAAHistory : data.SceneHistory;
                    passData.sceneHistoryInput = sceneHistoryInput;
                    builder.UseTexture(sceneHistoryInput, AccessFlags.Read);
                }
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                builder.SetRenderFunc((DownSamplePassData data, ComputeGraphContext context) =>
                {
                    LocalKeyword motionVectorKeyword = new LocalKeyword(data.cs, "_OUTPUT_MOTION_VECTOR");
                    LocalKeyword sceneHistoryKeyword = new LocalKeyword(data.cs, "_OUTPUT_SCENE_HISTORY");
                    
                    context.cmd.SetKeyword(data.cs, motionVectorKeyword, data.temporalDenoiseEnabled);
                    context.cmd.SetKeyword(data.cs, sceneHistoryKeyword, data.ssgiEnabled);
                    context.cmd.SetComputeVectorParam(data.cs, "_TextureSize", data.textureSize);
                    
                    int downsampleKernel = data.cs.FindKernel("DownsampleKernel");
                    context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, YPipelineShaderIDs.k_HalfDepthTextureID, data.halfDepth);
                    context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, YPipelineShaderIDs.k_HalfNormalRoughnessTextureID, data.halfNormalRoughness);
                    if (data.temporalDenoiseEnabled) context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, YPipelineShaderIDs.k_HalfMotionVectorTextureID, data.halfMotionVector);
                    if (data.ssgiEnabled)
                    {
                        context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, "_SceneHistoryInput", data.sceneHistoryInput);
                        context.cmd.SetComputeTextureParam(data.cs, downsampleKernel, YPipelineShaderIDs.k_HalfReprojectedSceneHistoryID, data.halfReprojectedSceneHistory);
                    }
                    
                    context.cmd.DispatchCompute(data.cs, downsampleKernel, data.threadGroupSizes.x, data.threadGroupSizes.y, 1);
                });
            }
        }
    }
}