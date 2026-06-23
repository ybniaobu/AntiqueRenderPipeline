using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;

namespace YPipeline
{
    internal sealed class LightDataPass : PipelinePass
    {
        private class LightDataPassData
        {
            public bool isTAAEnabled;
            public YPipelineLightData lightData;
            public BufferHandle punctualLightsData;
            public BufferHandle punctualLightSlicesData;
        }
        
        private PenAtlasPacker m_PenAtlasPacker;

        protected override void Initialize(ref YPipelineData data)
        {
            m_PenAtlasPacker = new PenAtlasPacker(YPipelineLightData.k_MaxShadowSliceCount);
        }

        protected override void OnDispose()
        {
            m_PenAtlasPacker.Dispose();
            m_PenAtlasPacker = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            PackShadowAtlas(ref data);
            GatherDirectLightData(ref data);
            GatherIndirectLightData(ref data);

            using (var builder = data.renderGraph.AddUnsafePass<LightDataPassData>("Initialize Light Data", out var passData))
            {
                passData.isTAAEnabled = data.IsTAAEnabled;
                passData.lightData = data.lightData;
                
                data.PunctualLightStructuredBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightData.k_MaxPunctualLightCount,
                    stride = 16 * 9,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Punctual Lights Data"
                });
                passData.punctualLightsData = builder.UseBuffer(data.PunctualLightStructuredBufferHandle, AccessFlags.Write);
                
                data.PunctualLightSliceStructuredBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = YPipelineLightData.k_MaxShadowSliceCount,
                    stride = 16 * 6,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Punctual Light Slices Data"
                });
                passData.punctualLightSlicesData = builder.UseBuffer(data.PunctualLightSliceStructuredBufferHandle, AccessFlags.Write);
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (LightDataPassData data, UnsafeGraphContext context) =>
                {
                    YPipelineLightData lightData = data.lightData;
                    
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCSS, lightData.isPCSSEnabled);
                    CoreUtils.SetKeyword(context.cmd, YPipelineKeywords.k_ShadowPCF, !lightData.isPCSSEnabled);
                    
                    // Sun Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_CascadeParams, lightData.cascadeParams);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightColorID, lightData.sunLightColor);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightDirectionID, lightData.sunLightDirection);
                    if (lightData.sunLightIndex != -1)
                    {
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowColorID, lightData.sunLightShadowColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightPenumbraColorID, lightData.sunLightPenumbraColor);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowBiasID, lightData.sunLightShadowBias);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParamsID, lightData.sunLightShadowParams);
                        context.cmd.SetGlobalVector(YPipelineShaderIDs.k_SunLightShadowParams2ID, lightData.sunLightShadowParams2);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_CascadeCullingSpheresID, lightData.cascadeCullingSpheres);
                        context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_SunLightDepthParamsID, lightData.sunLightDepthParams);
                        context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_SunLightShadowMatricesID, lightData.sunLightShadowMatrices);
                    }
                    
                    // Punctual Light Data
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_PunctualLightCountID, new Vector4(lightData.punctualLightCount, 0));
                    context.cmd.SetBufferData(data.punctualLightsData, lightData.punctualLightsData, 0, 0, lightData.punctualLightCount);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PunctualLightsDataID, data.punctualLightsData);
                    context.cmd.SetBufferData(data.punctualLightSlicesData, lightData.punctualLightSlicesData, 0, 0, lightData.punctualLightSliceCount);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_PunctualLightSlicesDataID, data.punctualLightSlicesData);
                    
                    // Global Ambient & Reflection Probe
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_AmbientProbeID, lightData.ambientProbe);
                    context.cmd.SetGlobalTexture(YPipelineShaderIDs.k_GlobalReflectionProbeID, lightData.globalReflectionProbe);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_GlobalReflectionProbeHDRID, lightData.globalHDRDecodeValues);
                    
                    // APV
                    ProbeReferenceVolume.instance.UpdateShaderVariablesProbeVolumes(CommandBufferHelpers.GetNativeCommandBuffer(context.cmd), 
                        VolumeManager.instance.stack.GetComponent<ProbeVolumesOptions>(), data.isTAAEnabled ? Time.frameCount : 0, false);
                });
            }
        }
        
        private void PackShadowAtlas(ref YPipelineData data)
        {
            YPipelineLightData lightData = data.lightData;
            Vector4[] punctualLightSampleParams = lightData.punctualLightSampleParams;
            int sliceCount = lightData.punctualLightSliceCount;
            m_PenAtlasPacker.Pack(ref punctualLightSampleParams, sliceCount, lightData.punctualLightAtlasSize.x);

            for (int i = 0; i < sliceCount; i++)
            {
                ref var sliceData = ref lightData.punctualLightSlicesData[i];
                sliceData.sampleParams = punctualLightSampleParams[i];
            }
        }

        private void GatherDirectLightData(ref YPipelineData data)
        {
            ref CullingResults cullingResults = ref data.cullingResults;
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            YPipelineLightData lightData = data.lightData;
            bool isPCSSEnabled = data.IsPCSSEnabled;
            int sunLightCount = 0;
            int punctualLightCount = 0;

            for (int i = 0; i < visibleLights.Length; i++)
            {
                ref readonly VisibleLight visibleLight = ref visibleLights.UnsafeElementAt(i);
                LightType lightType = visibleLight.lightType;
                Light light = visibleLight.light;
                YPipelineLight yLight = light.GetYPipelineLight();
                bool isShadowCasting = light.shadows != LightShadows.None && light.shadowStrength > 0.0f;
                bool containShadowCasters = cullingResults.GetShadowCasterBounds(i, out Bounds _); // cullResults.GetShadowCasterBounds() does the fence sync for each light shadow culling jobs.
                bool shouldUpdateShadowData = isShadowCasting && containShadowCasters;

                if (lightType == LightType.Directional)
                {
                    if (sunLightCount >= YPipelineLightData.k_MaxDirectionalLightCount) continue;
                    
                    lightData.sunLightColor = visibleLight.finalColor;
                    lightData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    lightData.sunLightDirection.w = 0; // Used to determine whether sun light is shadowing (should calculate shadow attenuation)

                    sunLightCount++;
                    
                    if (!shouldUpdateShadowData)
                    {
                        lightData.sunLightIndex = -1;
                        continue;
                    }
                    
                    lightData.sunLightDirection.w = 1;
                    lightData.sunLightShadowColor = yLight.shadowTint;
                    lightData.sunLightShadowColor.w = light.shadowStrength;
                    lightData.sunLightPenumbraColor = yLight.penumbraTint;
                    lightData.sunLightShadowBias = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                    lightData.sunLightShadowParams = isPCSSEnabled ? new Vector4(Mathf.Pow(10, yLight.penumbraScale), yLight.filterSampleCount) : new Vector4(yLight.penumbraWidth, yLight.sampleCount);
                    lightData.sunLightShadowParams2 = new Vector4(Mathf.Deg2Rad * yLight.angularDiameter, Mathf.Pow(10, yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleCount,
                        yLight.minPenumbraWidth);
                }
                else if (lightType == LightType.Point)
                {
                    if (punctualLightCount >= YPipelineLightData.k_MaxPunctualLightCount) continue;
                    
                    ref var punctualLightData = ref lightData.punctualLightsData[punctualLightCount];
                    punctualLightData.color = visibleLight.finalColor;
                    punctualLightData.color.w = 1;
                    punctualLightData.position = visibleLight.localToWorldMatrix.GetColumn(3);
                    punctualLightData.position.w = -1; // when w is -1, light should skip shadow calculation
                    punctualLightData.direction = Vector4.zero;
                    punctualLightData.lightParams = new Vector4(visibleLight.range, yLight.rangeAttenuationScale, 0.0f, 0.0f);
                    
                    ref var lightCullingInputInfos = ref lightData.lightCullingInputInfos[punctualLightCount];
                    lightCullingInputInfos.bound = punctualLightData.position;
                    lightCullingInputInfos.bound.w = visibleLight.range;
                    lightCullingInputInfos.spotLightInfos.w = -1; // point light
                    
                    punctualLightCount++;

                    if (!shouldUpdateShadowData)
                    {
                        lightData.punctualLightVisibleIdxToSliceIdx[i] = -1;
                        continue;
                    }
                    
                    punctualLightData.position.w = lightData.punctualLightVisibleIdxToSliceIdx[i];
                    punctualLightData.shadowColor = yLight.shadowTint;
                    punctualLightData.shadowColor.w = light.shadowStrength;
                    punctualLightData.penumbraColor = yLight.penumbraTint;
                    punctualLightData.shadowBias = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                    Vector4 shadowParams = isPCSSEnabled ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleCount) : new Vector4(yLight.penumbraWidth, yLight.sampleCount);
                    punctualLightData.shadowParams = shadowParams;
                    punctualLightData.shadowParams2 = new Vector4(yLight.lightRadius, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleCount, yLight.minPenumbraWidth);
                }
                else if (lightType == LightType.Spot)
                {
                    if (punctualLightCount >= YPipelineLightData.k_MaxPunctualLightCount) continue;
                    
                    ref var punctualLightData = ref lightData.punctualLightsData[punctualLightCount];
                    punctualLightData.color = visibleLight.finalColor;
                    punctualLightData.color.w = 2;
                    punctualLightData.position = visibleLight.localToWorldMatrix.GetColumn(3);
                    punctualLightData.position.w = -1; // when w is -1, light should skip shadow calculation
                    punctualLightData.direction = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    float cosInnerAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                    float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                    float invAngleRange = 1.0f / Mathf.Max(cosInnerAngle - cosOuterAngle, 0.0001f);
                    punctualLightData.lightParams = new Vector4(visibleLight.range, yLight.rangeAttenuationScale, invAngleRange, cosOuterAngle);
                    
                    ref var lightCullingInputInfos = ref lightData.lightCullingInputInfos[punctualLightCount];
                    lightCullingInputInfos.bound = punctualLightData.position;
                    lightCullingInputInfos.bound.w = visibleLight.range;
                    lightCullingInputInfos.spotLightInfos = -punctualLightData.direction;
                    lightCullingInputInfos.spotLightInfos.w = Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle;
                    
                    punctualLightCount++;
                    
                    if (!shouldUpdateShadowData)
                    {
                        lightData.punctualLightVisibleIdxToSliceIdx[i] = -1;
                        continue;
                    }
                    
                    punctualLightData.position.w = lightData.punctualLightVisibleIdxToSliceIdx[i];
                    punctualLightData.shadowColor = yLight.shadowTint;
                    punctualLightData.shadowColor.w = light.shadowStrength;
                    punctualLightData.penumbraColor = yLight.penumbraTint;
                    punctualLightData.shadowBias = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                    Vector4 shadowParams = isPCSSEnabled ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleCount) : new Vector4(yLight.penumbraWidth, yLight.sampleCount);
                    punctualLightData.shadowParams = shadowParams;
                    punctualLightData.shadowParams2 = new Vector4(yLight.lightRadius, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleCount, yLight.minPenumbraWidth);
                }
            }
            
            lightData.isSplitDepthEnabled = data.asset.enableSplitDepth;
            lightData.isPCSSEnabled = isPCSSEnabled;
            lightData.punctualLightCount = punctualLightCount;
            
            if (sunLightCount == 0)
            {
                lightData.sunLightColor = Vector4.zero;
                lightData.sunLightDirection = Vector4.zero;
            }
        }
        
        private void GatherIndirectLightData(ref YPipelineData data)
        {
            YPipelineLightData lightData = data.lightData;
            
            SphericalHarmonicsL2 SH = RenderSettings.ambientProbe;
            float intensity = Mathf.GammaToLinearSpace(RenderSettings.ambientIntensity);
            SHUtils.PackSHCoefficientsTo7Vectors(SH, ref lightData.ambientProbe, intensity);
            
            lightData.globalReflectionProbe = ReflectionProbe.defaultTexture;
            lightData.globalHDRDecodeValues = ReflectionProbe.defaultTextureHDRDecodeValues;
        }
    }
}