using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class LightSetupPass : PipelinePass
    {
        private class LightSetupPassData
        {
            // Direct Light：Sun Light, Punctual Light
            public bool isSunLightShadowing;
            public SunLightConstantBuffer sunLightData = new SunLightConstantBuffer();

            public BufferHandle punctualLightsBuffer;
            public int punctualLightCount;
            public PunctualLightStructuredBuffer[] punctualLightsData = new PunctualLightStructuredBuffer[YPipelineLightsData.k_MaxPunctualLightCount];
            
            // Ambient Probe(SH)
            public Vector4[] ambientProbe = new Vector4[7];
            
            // Global Reflection Probe
            public Texture defaultReflectionProbe;
            public Vector4 defaultHDRDecodeValues;
        }
        
        private struct SunLightConstantBuffer
        {
            public Vector4 sunLightColor;
            public Vector4 sunLightDirection;
            public Vector4 sunLightShadowColor;
            public Vector4 sunLightPenumbraColor; 
            public Vector4 sunLightShadowBias;
            public Vector4 sunLightShadowParams; 
            public Vector4 sunLightShadowParams2;

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

                if (lightsData.shadowingSunLightCount > 0)
                {
                    sunLightShadowColor = lightsData.sunLightShadowColor;
                    sunLightPenumbraColor = lightsData.sunLightPenumbraColor;
                    sunLightShadowBias = lightsData.sunLightShadowBias;
                    sunLightShadowParams = lightsData.sunLightShadowParams;
                    sunLightShadowParams2 = lightsData.sunLightShadowParams2;
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

            public void Setup(YPipelineLightsData lightsData, int index)
            {
                if (lightsData.punctualLightCount > 0)
                {
                    punctualLightColors = lightsData.punctualLightColors[index];
                    punctualLightPositions = lightsData.punctualLightPositions[index];
                    punctualLightDirections = lightsData.punctualLightDirections[index];
                    punctualLightParams = lightsData.punctualLightParams[index];
                }
            }
        }
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddUnsafePass<LightSetupPassData>("Set Global Light Data", out var passData))
            {
                // Direct Light：Sun Light, Punctual Light
                passData.sunLightData.Setup(data.lightsData);
                for (int i = 0; i < data.lightsData.punctualLightCount; i++)
                {
                    passData.punctualLightsData[i].Setup(data.lightsData, i);
                }
                
                passData.isSunLightShadowing = data.lightsData.shadowingSunLightCount > 0;
                passData.punctualLightCount = data.lightsData.punctualLightCount;
                
                data.PunctualLightBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxPunctualLightCount,
                    stride = 16 * 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Punctual Lights Data"
                });
                passData.punctualLightsBuffer = builder.UseBuffer(data.PunctualLightBufferHandle, AccessFlags.Write);
                
                // Ambient Probe(SH)
                GatherAmbientProbeSH(ref passData.ambientProbe);
                
                // Global Reflection Probe
                passData.defaultReflectionProbe = ReflectionProbe.defaultTexture;
                passData.defaultHDRDecodeValues = ReflectionProbe.defaultTextureHDRDecodeValues;
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((LightSetupPassData data, UnsafeGraphContext context) =>
                {
                    // Sun Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, data.sunLightData.sunLightColor);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, data.sunLightData.sunLightDirection);
                    
                    if (data.isSunLightShadowing)
                    {
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowColorID, data.sunLightData.sunLightShadowColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightPenumbraColorID, data.sunLightData.sunLightPenumbraColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowBiasID, data.sunLightData.sunLightShadowBias);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParamsID, data.sunLightData.sunLightShadowParams);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParams2ID, data.sunLightData.sunLightShadowParams2);
                    }
                    
                    // Punctual Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightCountID, new Vector4(data.punctualLightCount, 0));
                    context.cmd.SetBufferData(data.punctualLightsBuffer, data.punctualLightsData, 0, 0, data.punctualLightCount);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PunctualLightDataID, data.punctualLightsBuffer);
                    
                    // Ambient Probe(SH)
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_AmbientProbeID, data.ambientProbe);
                    
                    // Global Reflection Probe
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_GlobalReflectionProbeID, data.defaultReflectionProbe);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_GlobalReflectionProbeHDRID, data.defaultHDRDecodeValues);
                });
            }
        }
        
        private void GatherAmbientProbeSH(ref Vector4[] ambientProbe)
        {
            SphericalHarmonicsL2 SH = RenderSettings.ambientProbe;
            float intensity = Mathf.LinearToGammaSpace(RenderSettings.ambientIntensity);
            ambientProbe[0] = intensity * new Vector4(SH[0, 3], SH[0, 1], SH[0, 2], SH[0, 0] - SH[0, 6]);
            ambientProbe[1] = intensity * new Vector4(SH[0, 4], SH[0, 5], SH[0, 6] * 3, SH[0, 7]);
            ambientProbe[2] = intensity * new Vector4(SH[1, 3], SH[1, 1], SH[1, 2], SH[1, 0] - SH[1, 6]);
            ambientProbe[3] = intensity * new Vector4(SH[1, 4], SH[1, 5], SH[1, 6] * 3, SH[1, 7]);
            ambientProbe[4] = intensity * new Vector4(SH[2, 3], SH[2, 1], SH[2, 2], SH[2, 0] - SH[2, 6]);
            ambientProbe[5] = intensity * new Vector4(SH[2, 4], SH[2, 5], SH[2, 6] * 3, SH[2, 7]);
            ambientProbe[6] = intensity * new Vector4(SH[0, 8], SH[1, 8], SH[2, 8]);
        }
    }
}