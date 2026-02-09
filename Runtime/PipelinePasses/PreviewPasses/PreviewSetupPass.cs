using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class PreviewSetupPass : PipelinePass
    {
        private class SetupPassData
        {
            public Camera camera;
            
            // Resource Setup
            public Vector2Int bufferSize;
            
            // Light Setup
            public SunLightConstantBuffer sunLightData = new SunLightConstantBuffer();
            public BufferHandle punctualLightsBuffer;
            public PunctualLightStructuredBuffer[] punctualLightsData = new PunctualLightStructuredBuffer[YPipelineLightsData.k_MaxPunctualLightCount];
            
            // public BufferHandle tileLightIndicesBuffer;
            // public BufferHandle tileReflectionProbeIndicesBuffer;
            
            public PointLightShadowStructuredBuffer[] pointLightsShadowData = new PointLightShadowStructuredBuffer[YPipelineLightsData.k_MaxShadowingPointLightCount];
            public BufferHandle pointLightShadowBuffer;
            public Matrix4x4[] pointLightShadowMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingPointLightCount * 6];
            public BufferHandle pointLightShadowMatricesBuffer;
            public SpotLightShadowStructuredBuffer[] spotLightsShadowData = new SpotLightShadowStructuredBuffer[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public BufferHandle spotLightShadowBuffer;
            public Matrix4x4[] spotLightShadowMatrices = new Matrix4x4[YPipelineLightsData.k_MaxShadowingSpotLightCount];
            public BufferHandle spotLightShadowMatricesBuffer;
        }
        
        private struct SunLightConstantBuffer
        {
            public Vector4 sunLightColor;
            public Vector4 sunLightDirection;

            public void Setup(YPipelineLightsData lightsData)
            {
                if (lightsData.sunLightCount > 0)
                {
                    sunLightColor = lightsData.sunLightColor;
                    sunLightDirection = lightsData.sunLightDirection;
                }
                else
                {
                    sunLightColor = Vector4.zero;
                    sunLightDirection = Vector4.zero;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PunctualLightStructuredBuffer
        {
            public Vector4 punctualLightColors;
            public Vector4 punctualLightPositions;
            public Vector4 punctualLightDirections;
            public Vector4 punctualLightParams;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct PointLightShadowStructuredBuffer
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
        private struct SpotLightShadowStructuredBuffer
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
        
        private RTHandle m_CameraColorTarget;
        private RTHandle m_CameraDepthTarget;
        
        private RTHandle m_EnvBRDFLut;
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose()
        {
            base.OnDispose();
            RTHandles.Release(m_CameraColorTarget);
            RTHandles.Release(m_CameraDepthTarget);
            RTHandles.Release(m_EnvBRDFLut);
            m_CameraColorTarget = null;
            m_CameraDepthTarget = null;
            m_EnvBRDFLut = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddUnsafePass<SetupPassData>("Preview Resource & Light Setup", out var passData))
            {
                passData.camera = data.camera;
                
                // ----------------------------------------------------------------------------------------------------
                // Imported texture resources
                // ----------------------------------------------------------------------------------------------------
                
                Vector2Int bufferSize = data.BufferSize;
                passData.bufferSize = bufferSize;
                ImportBackBuffers(ref data);
                
                if (m_EnvBRDFLut == null || m_EnvBRDFLut.externalTexture != data.runtimeResources.EnvironmentBRDFLut)
                {
                    m_EnvBRDFLut?.Release();
                    m_EnvBRDFLut = RTHandles.Alloc(data.runtimeResources.EnvironmentBRDFLut);
                }
                TextureHandle envBRDFLut = data.renderGraph.ImportTexture(m_EnvBRDFLut);
                builder.UseTexture(envBRDFLut, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(envBRDFLut, YPipelineShaderIDs.k_EnvBRDFLutID);
                
                // ----------------------------------------------------------------------------------------------------
                // Setup Light & Shadow Data
                // ----------------------------------------------------------------------------------------------------
                
                RecordLightsData(ref data);
                
                passData.sunLightData.Setup(data.lightsData);
                
                // Punctual Light Data
                data.PunctualLightBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxPunctualLightCount,
                    stride = 16 * 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Punctual Lights Data"
                });
                passData.punctualLightsBuffer = builder.UseBuffer(data.PunctualLightBufferHandle, AccessFlags.Write);
                
                // Shadow Buffer
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
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((SetupPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetupCameraProperties(data.camera);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ScreenSpaceAmbientOcclusion, false);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, new Vector4(1f / data.bufferSize.x, 1f / data.bufferSize.y, data.bufferSize.x, data.bufferSize.y));
                    
                    // Sun Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, data.sunLightData.sunLightColor);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, data.sunLightData.sunLightDirection);
                    
                    // Punctual Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightCountID, new Vector4(0, 0));
                    context.cmd.SetBufferData(data.punctualLightsBuffer, data.punctualLightsData);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PunctualLightDataID, data.punctualLightsBuffer);
                    
                    // Shadow Buffer
                    context.cmd.SetBufferData(data.pointLightShadowBuffer, data.pointLightsShadowData);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PointLightShadowDataID, data.pointLightShadowBuffer);
                    context.cmd.SetBufferData(data.pointLightShadowMatricesBuffer, data.pointLightShadowMatrices);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PointLightShadowMatricesID, data.pointLightShadowMatricesBuffer);
                    context.cmd.SetBufferData(data.spotLightShadowBuffer, data.spotLightsShadowData);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_SpotLightShadowDataID, data.spotLightShadowBuffer);
                    context.cmd.SetBufferData(data.spotLightShadowMatricesBuffer, data.spotLightShadowMatrices);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_SpotLightShadowMatricesID, data.spotLightShadowMatricesBuffer);
                    
                    // Reflection Probe
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_ReflectionProbeCountID, new Vector4(0, 0));
                });
            }
        }

        private void RecordLightsData(ref YPipelineData data)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
            int sunLightCount = 0;
            int shadowingSunLightCount = 0;
            int punctualLightCount = 0;
            int shadowingSpotLightCount = 0;
            int shadowingPointLightCount = 0;

            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                // YPipelineLight yLight = light.GetYPipelineLight();

                if (visibleLight.lightType == LightType.Directional)
                {
                    if (sunLightCount >= YPipelineLightsData.k_MaxDirectionalLightCount) continue;
                
                    data.lightsData.sunLightIndex = i;
                    data.lightsData.sunLightNearPlaneOffset = light.shadowNearPlane;
                    data.lightsData.sunLightColor = visibleLight.finalColor * Mathf.PI * 1.5f; // 乘以 pi * 1.5 是为了 preview 看起来正常点
                    data.lightsData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    data.lightsData.sunLightDirection.w = 0;
                    sunLightCount++;
                }
            }
            
            data.lightsData.sunLightCount = sunLightCount;
            data.lightsData.shadowingSunLightCount = shadowingSunLightCount;
            data.lightsData.punctualLightCount = punctualLightCount;
            data.lightsData.shadowingSpotLightCount = shadowingSpotLightCount;
            data.lightsData.shadowingPointLightCount = shadowingPointLightCount;
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
                clearColor = new Color(0.01033f, 0.01033f, 0.01033f, 1.0f), // Blender 的背景颜色
                discardOnLastUse = false
            };
            
            data.CameraColorTarget = data.renderGraph.ImportTexture(m_CameraColorTarget, importInfoColor, importBackbufferParams);
            data.CameraDepthTarget = data.renderGraph.ImportTexture(m_CameraDepthTarget, importInfoDepth, importBackbufferParams);
        }
    }
}