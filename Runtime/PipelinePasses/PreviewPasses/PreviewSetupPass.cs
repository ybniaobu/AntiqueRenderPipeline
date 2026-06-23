using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    internal sealed class PreviewSetupPass : PipelinePass
    {
        private class SetupPassData
        {
            public Camera camera;
            public YPipelineLightData lightData;
            
            public Vector2Int bufferSize;
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
            RecordLightsData(ref data);
            
            using (var builder = data.renderGraph.AddUnsafePass<SetupPassData>("Preview Resource & Light Setup", out var passData))
            {
                passData.camera = data.camera;
                passData.lightData = data.lightData;
                
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
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (SetupPassData data, UnsafeGraphContext context) =>
                {
                    YPipelineLightData lightData = data.lightData;
                    
                    context.cmd.SetupCameraProperties(data.camera);
                    
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_EditorPreview, true);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_TAA, false);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ScreenSpaceAmbientOcclusion, false);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ScreenSpaceIrradiance, false);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ProbeVolumeL1, false);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ProbeVolumeL2, false);
                    
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, new Vector4(1f / data.bufferSize.x, 1f / data.bufferSize.y, data.bufferSize.x, data.bufferSize.y));
                    
                    // Sun Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, lightData.sunLightColor);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, lightData.sunLightDirection);
                    
                    // Punctual Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightCountID, new Vector4(0, 0));
                    
                    // Reflection Probe
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_ReflectionProbeCountID, new Vector4(0, 0));
                });
            }
        }

        private void RecordLightsData(ref YPipelineData data)
        {
            NativeArray<VisibleLight> visibleLights = data.cullingResults.visibleLights;
            YPipelineLightData lightData = data.lightData;
            int sunLightCount = 0;

            for (int i = 0; i < visibleLights.Length; i++)
            {
                ref readonly VisibleLight visibleLight = ref visibleLights.UnsafeElementAt(i);
                Light light = visibleLight.light;

                if (visibleLight.lightType == LightType.Directional)
                {
                    if (sunLightCount >= YPipelineLightData.k_MaxDirectionalLightCount) continue;
                
                    lightData.sunLightIndex = i;
                    lightData.sunLightColor = visibleLight.finalColor * Mathf.PI * 1.5f; // 乘以 pi * 1.5 是为了 preview 看起来正常点
                    lightData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    lightData.sunLightDirection.w = 0;
                    sunLightCount++;
                }
            }
            
            lightData.punctualLightCount = 0;
            lightData.punctualLightSliceCount = 0;
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