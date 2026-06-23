using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;

namespace YPipeline
{
    internal sealed class ShadowAtlasPass : PipelinePass
    {
        private class SunLightShadowAtlasPassData
        {
            public YPipelineLightData lightData;
            
            public RendererListHandle[] rendererList = new RendererListHandle[YPipelineLightData.k_MaxCascadeCount];
        }
        
        private class PunctualLightShadowAtlasPassData
        {
            public YPipelineLightData lightData;
            
            public int listCount;
            public int[] listIdxToSliceIdx = new int[YPipelineLightData.k_MaxShadowSliceCount];
            public RendererListHandle[] rendererList = new RendererListHandle[YPipelineLightData.k_MaxShadowSliceCount];
            
            // For recover VP Matrices
            public Matrix4x4 viewMatrix;
            public Matrix4x4 projectionMatrix;
        }
        
        protected override void Initialize(ref YPipelineData data) { }

        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<SunLightShadowAtlasPassData>("Draw Sun Light Shadow Atlas", out var passData))
            {
                passData.lightData = data.lightData;
                
                CreateSunLightShadowAtlas(ref data, builder, passData);
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (SunLightShadowAtlasPassData data, RasterGraphContext context) =>
                {
                    YPipelineLightData lightData = data.lightData;
                    
                    if (lightData.sunLightIndex != -1)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 1.0f);
                        for (int i = 0; i < lightData.cascadeCount; i++)
                        {
                            context.cmd.SetViewport(lightData.sunLightViewports[i]);
                            context.cmd.SetViewProjectionMatrices(lightData.sunLightViewMatrices[i], lightData.sunLightProjectionMatrices[i]);
                            context.cmd.DrawRendererList(data.rendererList[i]);
                        }
                    }
                });
            }
            
            using (var builder = data.renderGraph.AddRasterRenderPass<PunctualLightShadowAtlasPassData>("Draw Punctual Lights Shadow Atlas", out var passData))
            {
                passData.lightData = data.lightData;
                
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                passData.viewMatrix = yCamera.perCameraData.viewMatrix;
                passData.projectionMatrix = yCamera.perCameraData.jitteredProjectionMatrix;

                CreatePunctualLightShadowAtlas(ref data, builder, passData);
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PunctualLightShadowAtlasPassData data, RasterGraphContext context) =>
                {
                    YPipelineLightData lightData = data.lightData;

                    if (data.listCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
                        for (int i = 0; i < data.listCount; i++)
                        {
                            int sliceIdx = data.listIdxToSliceIdx[i];
                            Vector4 sampleParams = lightData.punctualLightSampleParams[sliceIdx];
                            Rect rect = new Rect(sampleParams.x, sampleParams.y, sampleParams.z, sampleParams.z);
                            context.cmd.SetViewport(rect);
                            context.cmd.SetViewProjectionMatrices(lightData.punctualLightViewMatrices[sliceIdx], lightData.punctualLightProjectionMatrices[sliceIdx]);
                            context.cmd.DrawRendererList(data.rendererList[i]);
                        }
                    }
                    
                    context.cmd.SetViewProjectionMatrices(data.viewMatrix, data.projectionMatrix);
                });
            }
        }
        
        private void CreateSunLightShadowAtlas(ref YPipelineData data, IRasterRenderGraphBuilder builder, SunLightShadowAtlasPassData passData)
        {
            int visibleLightIndex = data.lightData.sunLightIndex;
            Vector2Int atlasSize = data.lightData.cascadeAtlasSize;
            ref CullingResults cullingResults = ref data.cullingResults;
            
            if (visibleLightIndex != -1)
            {
                TextureDesc desc = new TextureDesc(atlasSize.x, atlasSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16, // DepthBits.Depth32
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    name = "Sun Light Shadow Atlas"
                };

                data.SunLightShadowAtlas = data.renderGraph.CreateTexture(desc);
                data.isSunLightShadowAtlasCreated = true;
                builder.SetRenderAttachmentDepth(data.SunLightShadowAtlas, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(data.SunLightShadowAtlas, YPipelineShaderIDs.k_SunLightShadowAtlasID);
            
                for (int i = 0; i < data.lightData.cascadeCount; i++)
                {
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, visibleLightIndex);
                    passData.rendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(passData.rendererList[i]);
                }
            }
            else
            {
                data.isSunLightShadowAtlasCreated = false;
            }
        }

        private void CreatePunctualLightShadowAtlas(ref YPipelineData data, IRasterRenderGraphBuilder builder, PunctualLightShadowAtlasPassData passData)
        {
            YPipelineLightData lightData = data.lightData;
            Vector2Int atlasSize = lightData.punctualLightAtlasSize;
            ref CullingResults cullingResults = ref data.cullingResults;
            int listCount = 0;

            if (lightData.punctualLightSliceCount > 0)
            {
                TextureDesc desc = new TextureDesc(atlasSize.x, atlasSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16, // DepthBits.Depth32
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    name = "Punctual Light Shadow Atlas"
                };
                
                data.PunctualLightShadowAtlas = data.renderGraph.CreateTexture(desc);
                data.isPunctualLightShadowAtlasCreated = true;
                builder.SetRenderAttachmentDepth(data.PunctualLightShadowAtlas, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(data.PunctualLightShadowAtlas, YPipelineShaderIDs.k_PunctualLightShadowAtlasID);

                for (int i = 0; i < lightData.punctualLightSliceCount; i++)
                {
                    int visibleIdx = lightData.punctualLightSliceIdxToVisibleIdx[i];
                    int sliceIdx = lightData.punctualLightVisibleIdxToSliceIdx[visibleIdx];
                    if (sliceIdx == -1) continue;
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, visibleIdx);
                    passData.rendererList[listCount] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(passData.rendererList[listCount]);
                    passData.listIdxToSliceIdx[listCount] = i; // This must be done because cullingResults.GetShadowCasterBounds corrupts the number of slices that need to render shadows.
                    listCount++;
                }
            }
            else
            {
                data.isPunctualLightShadowAtlasCreated = false;
            }
            
            passData.listCount = listCount;
        }
    }
}