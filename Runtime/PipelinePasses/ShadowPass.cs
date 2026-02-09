using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class ShadowPass : PipelinePass
    {
        private class ShadowPassData
        {
            public Matrix4x4 viewMatrix;
            public Matrix4x4 projectionMatrix;
            
            public bool isPCSSEnabled;

            public TextureHandle sunLightShadowMap;
            public TextureHandle spotLightShadowMap;
            public TextureHandle pointLightShadowMap;
            
            // Sun Light Shadow
            public int cascadeCount;
            public int shadowingSunLightCount;
            public Matrix4x4[] sunLightViewMatrices;
            public Matrix4x4[] sunLightProjectionMatrices;

            public SunLightShadowConstantBuffer sunLightShadowData = new SunLightShadowConstantBuffer();
            public RendererListHandle[] sunLightShadowRendererList = new RendererListHandle[YPipelineLightsData.k_MaxCascadeCount];
            
            // Point Light Shadow
            public int shadowingPointLightCount;
            public Matrix4x4[] pointLightViewMatrices;
            public Matrix4x4[] pointLightProjectionMatrices;
            
            public PointLightShadowStructuredBuffer[] pointLightsShadowData = new PointLightShadowStructuredBuffer[YPipelineLightsData.k_MaxShadowingPointLightCount];
            public RendererListHandle[] pointLightShadowRendererList = new RendererListHandle[YPipelineLightsData.k_MaxShadowingPointLightCount * 6];
            public BufferHandle pointLightShadowBuffer;
            
            public Matrix4x4[] pointLightShadowMatrices;
            public BufferHandle pointLightShadowMatricesBuffer;
            
            // Spot Light Shadow
            public int shadowingSpotLightCount;
            public Matrix4x4[] spotLightViewMatrices;
            public Matrix4x4[] spotLightProjectionMatrices;

            public SpotLightShadowStructuredBuffer[] spotLightsShadowData = new SpotLightShadowStructuredBuffer[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public RendererListHandle[] spotLightShadowRendererList = new RendererListHandle[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public BufferHandle spotLightShadowBuffer;
            
            public Matrix4x4[] spotLightShadowMatrices;
            public BufferHandle spotLightShadowMatricesBuffer;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Buffers
        // ----------------------------------------------------------------------------------------------------
        struct SunLightShadowConstantBuffer
        {
            public Vector4[] cascadeCullingSpheres;
            public Matrix4x4[] sunLightShadowMatrices;
            public Vector4[] sunLightDepthParams;

            public void Setup(YPipelineLightsData lightsData)
            {
                if (lightsData.shadowingSunLightCount > 0)
                {
                    cascadeCullingSpheres = lightsData.cascadeCullingSpheres;
                    sunLightShadowMatrices = lightsData.sunLightShadowMatrices;
                    sunLightDepthParams = lightsData.sunLightDepthParams;
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct PointLightShadowStructuredBuffer
        {
            public Vector4 pointLightShadowColors;
            public Vector4 pointLightPenumbraColors;
            public Vector4 pointLightShadowBias;
            public Vector4 pointLightShadowParams;
            public Vector4 pointLightShadowParams2;
            public Vector4 pointLightDepthParams;

            public void Setup(YPipelineLightsData lightsData, int index)
            {
                if (lightsData.shadowingPointLightCount > 0)
                {
                    pointLightShadowColors = lightsData.pointLightShadowColors[index];
                    pointLightPenumbraColors = lightsData.pointLightPenumbraColors[index];
                    pointLightShadowBias = lightsData.pointLightShadowBias[index];
                    pointLightShadowParams = lightsData.pointLightShadowParams[index];
                    pointLightShadowParams2 = lightsData.pointLightShadowParams2[index];
                    pointLightDepthParams = lightsData.pointLightDepthParams[index];
                }
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct SpotLightShadowStructuredBuffer
        {
            public Vector4 spotLightShadowColors;
            public Vector4 spotLightPenumbraColors;
            public Vector4 spotLightShadowBias;
            public Vector4 spotLightShadowParams;
            public Vector4 spotLightShadowParams2;
            public Vector4 spotLightDepthParams;

            public void Setup(YPipelineLightsData lightsData, int index)
            {
                if (lightsData.shadowingSpotLightCount > 0)
                {
                    spotLightShadowColors = lightsData.spotLightShadowColors[index];
                    spotLightPenumbraColors = lightsData.spotLightPenumbraColors[index];
                    spotLightShadowBias = lightsData.spotLightShadowBias[index];
                    spotLightShadowParams = lightsData.spotLightShadowParams[index];
                    spotLightShadowParams2 = lightsData.spotLightShadowParams2[index];
                    spotLightDepthParams = lightsData.spotLightDepthParams[index];
                }
            }
        }
        
        
        // ----------------------------------------------------------------------------------------------------
        // Shadow Culling Related Fields
        // ----------------------------------------------------------------------------------------------------
        
        private NativeArray<LightShadowCasterCullingInfo> m_CullingInfoPerLight;
        private NativeArray<ShadowSplitData> m_ShadowSplitDataPerLight;
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            // TODO: RasterRenderPass 无法 SetRenderTarget，URP 是将直接光和间接光的阴影贴图分开进行的。
            
            using (var builder = data.renderGraph.AddUnsafePass<ShadowPassData>("Render Shadow Maps", out var passData))
            {
                m_CullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(data.cullingResults.visibleLights.Length, Allocator.Temp);
                m_ShadowSplitDataPerLight = new NativeArray<ShadowSplitData>(m_CullingInfoPerLight.Length * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                passData.viewMatrix = yCamera.perCameraData.viewMatrix;
                passData.projectionMatrix = yCamera.perCameraData.jitteredProjectionMatrix;
                passData.isPCSSEnabled = data.asset.shadowMode == ShadowMode.PCSS;
                
                // Shadow Culling & Shadow Map Creation & Renderer List Preparation
                CreateSunLightShadowMap(ref data, builder, passData);
                CreateSpotLightShadowMap(ref data, builder, passData);
                CreatePointLightShadowMap(ref data, builder, passData);
                
                // Buffers Preparation
                passData.sunLightShadowData.Setup(data.lightsData);
                for (int i = 0; i < data.lightsData.shadowingPointLightCount; i++)
                {
                    passData.pointLightsShadowData[i].Setup(data.lightsData, i);
                }

                for (int i = 0; i < data.lightsData.shadowingSpotLightCount; i++)
                {
                    passData.spotLightsShadowData[i].Setup(data.lightsData, i);
                }

                data.PointLightShadowBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingPointLightCount,
                    stride = 16 * 6,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Point Lights Shadows Data"
                });
                passData.pointLightShadowBuffer = builder.UseBuffer(data.PointLightShadowBufferHandle, AccessFlags.Write);
                
                data.PointLightShadowMatricesBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingPointLightCount * 6,
                    stride = 16 * 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Point Lights Shadow Matrices Data"
                });
                passData.pointLightShadowMatricesBuffer = builder.UseBuffer(data.PointLightShadowMatricesBufferHandle, AccessFlags.Write);
                
                data.SpotLightShadowBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingSpotLightCount,
                    stride = 16 * 6,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Spot Lights Shadows Data"
                });
                passData.spotLightShadowBuffer = builder.UseBuffer(data.SpotLightShadowBufferHandle, AccessFlags.Write);
                
                data.SpotLightShadowMatricesBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxShadowingSpotLightCount,
                    stride = 16 * 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Spot Lights Shadow Matrices Data"
                });
                passData.spotLightShadowMatricesBuffer = builder.UseBuffer(data.SpotLightShadowMatricesBufferHandle, AccessFlags.Write);
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((ShadowPassData data, UnsafeGraphContext context) =>
                {
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCSS, data.isPCSSEnabled);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCF, !data.isPCSSEnabled);
                    
                    context.cmd.BeginSample("Sun Light Shadows");
                    if (data.shadowingSunLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 1.0f);
                        for (int i = 0; i < data.cascadeCount; i++)
                        { 
                            context.cmd.SetViewProjectionMatrices(data.sunLightViewMatrices[i], data.sunLightProjectionMatrices[i]);
                            context.cmd.SetRenderTarget(new RenderTargetIdentifier(data.sunLightShadowMap, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            context.cmd.ClearRenderTarget(true, false, Color.clear);
                            context.cmd.DrawRendererList(data.sunLightShadowRendererList[i]);
                        }
                        
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_CascadeCullingSpheresID, data.sunLightShadowData.cascadeCullingSpheres);
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_SunLightShadowMatricesID, data.sunLightShadowData.sunLightShadowMatrices);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SunLightDepthParamsID, data.sunLightShadowData.sunLightDepthParams);
                    }
                    context.cmd.EndSample("Sun Light Shadows");
                    
                    context.cmd.BeginSample("Point Light Shadows");
                    if (data.shadowingPointLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
                        for (int i = 0; i < data.shadowingPointLightCount; i++)
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                int cubeIndex = i * 6 + j;
                                context.cmd.SetViewProjectionMatrices(data.pointLightViewMatrices[cubeIndex], data.pointLightProjectionMatrices[cubeIndex]);
                                context.cmd.SetRenderTarget(new RenderTargetIdentifier(data.pointLightShadowMap, 0, CubemapFace.Unknown, cubeIndex), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                                context.cmd.ClearRenderTarget(true, false, Color.clear);
                                context.cmd.DrawRendererList(data.pointLightShadowRendererList[cubeIndex]);
                            }
                        }
                    }
                    context.cmd.SetBufferData(data.pointLightShadowBuffer, data.pointLightsShadowData, 0, 0, data.shadowingPointLightCount);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PointLightShadowDataID, data.pointLightShadowBuffer);
                    context.cmd.SetBufferData(data.pointLightShadowMatricesBuffer, data.pointLightShadowMatrices, 0, 0, data.shadowingPointLightCount * 6);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PointLightShadowMatricesID, data.pointLightShadowMatricesBuffer);
                    context.cmd.EndSample("Point Light Shadows");
                    
                    context.cmd.BeginSample("Spot Light Shadows");
                    if (data.shadowingSpotLightCount > 0)
                    {
                        context.cmd.SetGlobalFloat(YPipelineShaderIDs.k_ShadowPancakingID, 0.0f);
                        for (int i = 0; i < data.shadowingSpotLightCount; i++)
                        {
                            context.cmd.SetViewProjectionMatrices(data.spotLightViewMatrices[i], data.spotLightProjectionMatrices[i]);
                            context.cmd.SetRenderTarget(new RenderTargetIdentifier(data.spotLightShadowMap, 0, CubemapFace.Unknown, i), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                            context.cmd.ClearRenderTarget(true, false, Color.clear);
                            context.cmd.DrawRendererList(data.spotLightShadowRendererList[i]);
                        }
                    }
                    context.cmd.SetBufferData(data.spotLightShadowBuffer, data.spotLightsShadowData, 0, 0, data.shadowingSpotLightCount);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_SpotLightShadowDataID, data.spotLightShadowBuffer);
                    context.cmd.SetBufferData(data.spotLightShadowMatricesBuffer, data.spotLightShadowMatrices, 0, 0, data.shadowingSpotLightCount);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_SpotLightShadowMatricesID, data.spotLightShadowMatricesBuffer);
                    context.cmd.EndSample("Spot Light Shadows");
                    
                    context.cmd.SetViewProjectionMatrices(data.viewMatrix, data.projectionMatrix);
                });
            }
        }

        private void CreateSunLightShadowMap(ref YPipelineData data, IUnsafeRenderGraphBuilder builder, ShadowPassData passData)
        {
            data.isSunLightShadowMapCreated = false;
            int shadowingSunLightCount = data.lightsData.shadowingSunLightCount;
            passData.shadowingSunLightCount = shadowingSunLightCount;
            int cascadeCount = data.lightsData.cascadeCount;
            passData.cascadeCount = cascadeCount;
            
            passData.sunLightViewMatrices = data.lightsData.sunLightViewMatrices;
            passData.sunLightProjectionMatrices = data.lightsData.sunLightProjectionMatrices;
            
            if (shadowingSunLightCount > 0)
            {
                int size = (int) data.asset.sunLightShadowMapSize;
                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16, // DepthBits.Depth32
                    dimension = TextureDimension.Tex2DArray,
                    slices = shadowingSunLightCount * cascadeCount,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    msaaSamples = MSAASamples.None,
                    name = "Sun Light Shadow Map"
                };

                data.SunLightShadowMap = data.renderGraph.CreateTexture(desc);
                passData.sunLightShadowMap = data.SunLightShadowMap;
                builder.UseTexture(data.SunLightShadowMap, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.SunLightShadowMap, YPipelineShaderIDs.k_SunLightShadowMapID);
                data.isSunLightShadowMapCreated = true;

                int visibleLightIndex = data.lightsData.sunLightIndex;
                ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, visibleLightIndex);
            
                for (int i = 0; i < cascadeCount; i++)
                {
                    passData.sunLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(passData.sunLightShadowRendererList[i]);
                }
            }
        }

        private void CreateSpotLightShadowMap(ref YPipelineData data, IUnsafeRenderGraphBuilder builder, ShadowPassData passData)
        {
            data.isSpotLightShadowMapCreated = false;
            int shadowingSpotLightCount = data.lightsData.shadowingSpotLightCount;
            passData.shadowingSpotLightCount = shadowingSpotLightCount;
            
            passData.spotLightShadowMatrices = data.lightsData.spotLightShadowMatrices;
            passData.spotLightViewMatrices = data.lightsData.spotLightViewMatrices;
            passData.spotLightProjectionMatrices = data.lightsData.spotLightProjectionMatrices;
            
            if (shadowingSpotLightCount > 0)
            {
                int size = (int) data.asset.spotLightShadowMapSize;
                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16,
                    dimension = TextureDimension.Tex2DArray,
                    slices = shadowingSpotLightCount,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    discardBuffer = true,
                    msaaSamples = MSAASamples.None,
                    name = "Spot Light Shadow Map"
                };
                
                data.SpotLightShadowMap = data.renderGraph.CreateTexture(desc);
                passData.spotLightShadowMap = data.SpotLightShadowMap;
                builder.UseTexture(data.SpotLightShadowMap, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.SpotLightShadowMap, YPipelineShaderIDs.k_SpotLightShadowMapID);
                data.isSpotLightShadowMapCreated = true;
                
                for (int i = 0; i < shadowingSpotLightCount; i++)
                {
                    int visibleLightIndex = data.lightsData.shadowingSpotLightIndices[i];
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, visibleLightIndex);
                    passData.spotLightShadowRendererList[i] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                    builder.UseRendererList(passData.spotLightShadowRendererList[i]);
                }
            }
        }

        private void CreatePointLightShadowMap(ref YPipelineData data, IUnsafeRenderGraphBuilder builder, ShadowPassData passData)
        {
            data.isPointLightShadowMapCreated = false;
            int shadowingPointLightCount = data.lightsData.shadowingPointLightCount;
            passData.shadowingPointLightCount = shadowingPointLightCount;
            
            passData.pointLightShadowMatrices = data.lightsData.pointLightShadowMatrices;
            passData.pointLightViewMatrices = data.lightsData.pointLightViewMatrices;
            passData.pointLightProjectionMatrices = data.lightsData.pointLightProjectionMatrices;
            
            if (shadowingPointLightCount > 0)
            {
                int size = (int) data.asset.pointLightShadowMapSize;
                TextureDesc desc = new TextureDesc(size,size)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.Shadow),
                    depthBufferBits = DepthBits.Depth16,
                    dimension = TextureDimension.CubeArray,
                    slices = shadowingPointLightCount * 6,
                    filterMode = FilterMode.Bilinear,
                    isShadowMap = true,
                    clearBuffer = false,
                    discardBuffer = true,
                    msaaSamples = MSAASamples.None,
                    name = "Point Light Shadow Map"
                };
                
                data.PointLightShadowMap = data.renderGraph.CreateTexture(desc);
                passData.pointLightShadowMap = data.PointLightShadowMap;
                builder.UseTexture(data.PointLightShadowMap, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(data.PointLightShadowMap, YPipelineShaderIDs.k_PointLightShadowMapID);
                data.isPointLightShadowMapCreated = true;
                
                for (int i = 0; i < shadowingPointLightCount; i++)
                {
                    int visibleLightIndex = data.lightsData.shadowingPointLightIndices[i];
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(data.cullingResults, visibleLightIndex);

                    for (int j = 0; j < 6; j++)
                    {
                        passData.pointLightShadowRendererList[i * 6 + j] = data.renderGraph.CreateShadowRendererList(ref shadowDrawingSettings);
                        builder.UseRendererList(passData.pointLightShadowRendererList[i * 6 + j]);
                    }
                }
            }
        }
    }
}