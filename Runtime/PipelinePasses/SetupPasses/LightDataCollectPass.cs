using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;

namespace YPipeline
{
    public class LightDataCollectPass : PipelinePass
    {
        private NativeArray<LightShadowCasterCullingInfo> m_CullingInfoPerLight;
        private NativeArray<ShadowSplitData> m_ShadowSplitDataPerLight;
        
        private TextureAtlasPacker m_Packer;

        protected override void Initialize(ref YPipelineData data)
        {
            m_Packer = new TextureAtlasPacker();
        }

        protected override void OnDispose()
        {
            m_Packer.Dispose();
            m_Packer = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            // Light Data Collection
            CollectLightData(ref data);
            
            // Shadow Culling
            m_CullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(data.cullingResults.visibleLights.Length, Allocator.Temp);
            m_ShadowSplitDataPerLight = new NativeArray<ShadowSplitData>(m_CullingInfoPerLight.Length * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            CollectSunLightShadowData(ref data);
            CollectPointLightShadowData(ref data);
            CollectSpotLightShadowData(ref data);
            
            if (data.lightsData.shadowingSunLightCount + data.lightsData.shadowingPointLightCount + data.lightsData.shadowingSpotLightCount > 0)
            {
                data.context.CullShadowCasters(data.cullingResults, new ShadowCastersCullingInfos
                {
                    perLightInfos = m_CullingInfoPerLight,
                    splitBuffer = m_ShadowSplitDataPerLight
                });
            }
            
            // Reflection Probe Collection
            CollectReflectionProbeData(ref data);
            
            data.context.ExecuteCommandBuffer(data.cmd);
            data.context.Submit();
            data.cmd.Clear();
        }

        private void CollectLightData(ref YPipelineData data)
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
                YPipelineLight yLight = light.GetYPipelineLight();

                if (visibleLight.lightType == LightType.Directional)
                {
                    if (sunLightCount >= YPipelineLightsData.k_MaxDirectionalLightCount) continue;
                
                    data.lightsData.sunLightIndex = i;
                    data.lightsData.sunLightNearPlaneOffset = light.shadowNearPlane;
                    data.lightsData.sunLightColor = visibleLight.finalColor;
                    data.lightsData.sunLightDirection = -visibleLight.localToWorldMatrix.GetColumn(2);
                    data.lightsData.sunLightDirection.w = 0; // Used to determine whether sun light is shadowing (should calculate shadow attenuation)
                
                    if (light.shadows != LightShadows.None && light.shadowStrength > 0f && data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                    {
                        data.lightsData.sunLightDirection.w = 1;
                        data.lightsData.sunLightShadowColor = yLight.shadowTint;
                        data.lightsData.sunLightShadowColor.w = light.shadowStrength;
                        data.lightsData.sunLightPenumbraColor = yLight.penumbraTint;
                        data.lightsData.sunLightShadowBias = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                        data.lightsData.sunLightShadowParams = data.asset.shadowMode == ShadowMode.PCSS ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleNumber) : new Vector4(yLight.penumbraWidth, yLight.sampleNumber);
                        data.lightsData.sunLightShadowParams2 = new Vector4(yLight.lightSize, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleNumber, yLight.minPenumbraWidth);
                        shadowingSunLightCount++;
                    }
                    sunLightCount++;
                }
                else if (visibleLight.lightType == LightType.Point)
                {
                    if (punctualLightCount >= YPipelineLightsData.k_MaxPunctualLightCount) continue;
                
                    data.lightsData.punctualLightColors[punctualLightCount] = visibleLight.finalColor;
                    data.lightsData.punctualLightColors[punctualLightCount].w = 1;
                    data.lightsData.punctualLightPositions[punctualLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    data.lightsData.punctualLightPositions[punctualLightCount].w = -1; // when w is -1, light should skip shadow calculation
                    data.lightsData.punctualLightDirections[punctualLightCount] = Vector4.zero;
                    data.lightsData.punctualLightParams[punctualLightCount] = new Vector4(visibleLight.range, yLight.rangeAttenuationScale, 0.0f, 0.0f);
                    
                    bool isPointLightShadowing = light.shadows != LightShadows.None && light.shadowStrength > 0f && shadowingPointLightCount < YPipelineLightsData.k_MaxShadowingPointLightCount;
                
                    if (isPointLightShadowing && data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                    {
                        data.lightsData.shadowingPointLightIndices[shadowingPointLightCount] = i;
                        data.lightsData.punctualLightPositions[punctualLightCount].w = shadowingPointLightCount;
                        data.lightsData.pointLightShadowColors[shadowingPointLightCount] = yLight.shadowTint;
                        data.lightsData.pointLightShadowColors[shadowingPointLightCount].w = light.shadowStrength;
                        data.lightsData.pointLightPenumbraColors[shadowingPointLightCount] = yLight.penumbraTint;
                        data.lightsData.pointLightShadowBias[shadowingPointLightCount] = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                        Vector4 shadowParams = data.asset.shadowMode == ShadowMode.PCSS ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleNumber) : new Vector4(yLight.penumbraWidth, yLight.sampleNumber);
                        data.lightsData.pointLightShadowParams[shadowingPointLightCount] = shadowParams;
                        data.lightsData.pointLightShadowParams2[shadowingPointLightCount] = new Vector4(yLight.lightSize, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleNumber, yLight.minPenumbraWidth);
                        shadowingPointLightCount++;
                    }
                    punctualLightCount++;
                }
                else if (visibleLight.lightType == LightType.Spot)
                {
                    if (punctualLightCount >= YPipelineLightsData.k_MaxPunctualLightCount) continue;
                
                    data.lightsData.punctualLightColors[punctualLightCount] = visibleLight.finalColor;
                    data.lightsData.punctualLightColors[punctualLightCount].w = 2;
                    data.lightsData.punctualLightPositions[punctualLightCount] = visibleLight.localToWorldMatrix.GetColumn(3);
                    data.lightsData.punctualLightPositions[punctualLightCount].w = -1; // when w is -1, light should skip shadow calculation
                    data.lightsData.punctualLightDirections[punctualLightCount] = -visibleLight.localToWorldMatrix.GetColumn(2);
                    
                    float cosInnerAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
                    float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
                    float invAngleRange = 1.0f / Mathf.Max(cosInnerAngle - cosOuterAngle, 0.0001f);
                    data.lightsData.punctualLightParams[punctualLightCount] = new Vector4(visibleLight.range, yLight.rangeAttenuationScale, invAngleRange, cosOuterAngle);

                    bool isSpotLightShadowing = light.shadows != LightShadows.None && light.shadowStrength > 0f && shadowingSpotLightCount < YPipelineLightsData.k_MaxShadowingSpotLightCount;
                
                    if (isSpotLightShadowing && data.cullingResults.GetShadowCasterBounds(i, out Bounds outBounds))
                    {
                        data.lightsData.shadowingSpotLightIndices[shadowingSpotLightCount] = i;
                        data.lightsData.punctualLightPositions[punctualLightCount].w = shadowingSpotLightCount;
                        data.lightsData.spotLightShadowColors[shadowingSpotLightCount] = yLight.shadowTint;
                        data.lightsData.spotLightShadowColors[shadowingSpotLightCount].w = light.shadowStrength;
                        data.lightsData.spotLightPenumbraColors[shadowingSpotLightCount] = yLight.penumbraTint;
                        data.lightsData.spotLightShadowBias[shadowingSpotLightCount] = new Vector4(yLight.depthBias, yLight.slopeScaledDepthBias, yLight.normalBias, yLight.slopeScaledNormalBias);
                        Vector4 shadowParams = data.asset.shadowMode == ShadowMode.PCSS ? new Vector4(Mathf.Pow(10,yLight.penumbraScale), yLight.filterSampleNumber) : new Vector4(yLight.penumbraWidth, yLight.sampleNumber);
                        data.lightsData.spotLightShadowParams[shadowingSpotLightCount] = shadowParams;
                        data.lightsData.spotLightShadowParams2[shadowingSpotLightCount] = new Vector4(yLight.lightSize, Mathf.Pow(10,yLight.blockerSearchAreaSizeScale), yLight.blockerSearchSampleNumber, yLight.minPenumbraWidth);
                        shadowingSpotLightCount++;
                    }
                    punctualLightCount++;
                }
            }

            data.lightsData.cascadeCount = data.asset.cascadeCount;
            data.lightsData.sunLightCount = sunLightCount;
            data.lightsData.shadowingSunLightCount = shadowingSunLightCount;
            data.lightsData.punctualLightCount = punctualLightCount;
            data.lightsData.shadowingSpotLightCount = shadowingSpotLightCount;
            data.lightsData.shadowingPointLightCount = shadowingPointLightCount;
        }
        
        private void CollectSunLightShadowData(ref YPipelineData data)
        {
            if (data.lightsData.shadowingSunLightCount > 0)
            {
                int size = (int) data.asset.sunLightShadowMapSize;
                int visibleLightIndex = data.lightsData.sunLightIndex;
                int cascadeCount = data.lightsData.cascadeCount;
                int splitOffset = visibleLightIndex * 6;
                m_CullingInfoPerLight[visibleLightIndex] = new LightShadowCasterCullingInfo
                {
                    projectionType = BatchCullingProjectionType.Orthographic,
                    splitRange = new RangeInt(splitOffset, cascadeCount)
                };
            
                for (int i = 0; i < cascadeCount; i++)
                {
                    data.cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(visibleLightIndex, i, cascadeCount, data.asset.SpiltRatios
                        , size, data.lightsData.sunLightNearPlaneOffset + 0.8f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                    splitData.shadowCascadeBlendCullingFactor = 1f;
                    m_ShadowSplitDataPerLight[splitOffset + i] = splitData;
                
                    data.lightsData.cascadeCullingSpheres[i] = splitData.cullingSphere;
                    data.lightsData.sunLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    data.lightsData.sunLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    data.lightsData.sunLightViewMatrices[i] = viewMatrix;
                    data.lightsData.sunLightProjectionMatrices[i] = projectionMatrix;
                }
            }
        }

        private void CollectPointLightShadowData(ref YPipelineData data)
        {
            if (data.lightsData.shadowingPointLightCount > 0)
            {
                for (int i = 0; i < data.lightsData.shadowingPointLightCount; i++)
                {
                    int visibleLightIndex = data.lightsData.shadowingPointLightIndices[i];
                    int splitOffset = visibleLightIndex * 6;
                    m_CullingInfoPerLight[visibleLightIndex] = new LightShadowCasterCullingInfo
                    {
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitRange = new RangeInt(splitOffset, 6)
                    };

                    for (int j = 0; j < 6; j++)
                    {
                        data.cullingResults.ComputePointShadowMatricesAndCullingPrimitives(visibleLightIndex, (CubemapFace)j, 0.0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

                        m_ShadowSplitDataPerLight[splitOffset + j] = splitData;
                        
                        // TODO: 解决 ComputePointShadowMatricesAndCullingPrimitives 导致的正面剔除（winding order）的问题
                        // viewMatrix.m11 = -viewMatrix.m11;
                        // viewMatrix.m12 = -viewMatrix.m12;
                        // viewMatrix.m13 = -viewMatrix.m13;

                        // projectionMatrix.m11 = -projectionMatrix.m11;

                        data.lightsData.pointLightShadowMatrices[i * 6 + j] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                        data.lightsData.pointLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                        data.lightsData.pointLightViewMatrices[i * 6 + j] = viewMatrix;
                        data.lightsData.pointLightProjectionMatrices[i * 6 + j] = projectionMatrix;
                    }
                }
            }
        }
        
        private void CollectSpotLightShadowData(ref YPipelineData data)
        {
            if (data.lightsData.shadowingSpotLightCount > 0)
            {
                for (int i = 0; i < data.lightsData.shadowingSpotLightCount; i++)
                {
                    int visibleLightIndex = data.lightsData.shadowingSpotLightIndices[i];
                    data.cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                    
                    int splitOffset = visibleLightIndex * 6;
                    m_ShadowSplitDataPerLight[splitOffset] = splitData;
                    m_CullingInfoPerLight[visibleLightIndex] = new LightShadowCasterCullingInfo
                    {
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitRange = new RangeInt(splitOffset, 1)
                    };
                    
                    data.lightsData.spotLightShadowMatrices[i] = ShadowUtility.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    data.lightsData.spotLightDepthParams[i] = SystemInfo.usesReversedZBuffer ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    data.lightsData.spotLightViewMatrices[i] = viewMatrix;
                    data.lightsData.spotLightProjectionMatrices[i] = projectionMatrix;
                }
            }
        }

        private void CollectReflectionProbeData(ref YPipelineData data)
        {
            NativeArray<VisibleReflectionProbe> visibleReflectionProbes = data.cullingResults.visibleReflectionProbes;
            int reflectionProbeCount = 0;
            int atlasArea = 0;

            for (int i = 0; i < visibleReflectionProbes.Length; i++)
            {
                if (reflectionProbeCount >= data.asset.maxReflectionProbesOnScreen) break;
                
                VisibleReflectionProbe visibleProbe = visibleReflectionProbes[i];
                ReflectionProbe probe = visibleProbe.reflectionProbe;
                YPipelineReflectionProbe yProbe = probe.GetYPipelineReflectionProbe();
                if (!yProbe.IsReady) continue;

                data.reflectionProbesData.boxCenter[reflectionProbeCount] = visibleProbe.bounds.center;
                data.reflectionProbesData.boxCenter[reflectionProbeCount].w = visibleProbe.importance;
                data.reflectionProbesData.boxExtent[reflectionProbeCount] = visibleProbe.bounds.extents;
                data.reflectionProbesData.boxExtent[reflectionProbeCount].w = visibleProbe.isBoxProjection ? 1 : 0;
                Array.Copy(yProbe.SHData, 0, data.reflectionProbesData.SH, reflectionProbeCount * 7, 7);
                Texture octahedralAtlas = data.asset.reflectionProbeQuality switch
                {
                    Quality3Tier.High => yProbe.octahedralAtlasHigh,
                    Quality3Tier.Medium => yProbe.octahedralAtlasMedium,
                    Quality3Tier.Low => yProbe.octahedralAtlasLow,
                    _ => yProbe.octahedralAtlasMedium
                };
                data.reflectionProbesData.probeSampleParams[reflectionProbeCount] = new Vector4(0, 0, octahedralAtlas.height);
                data.reflectionProbesData.probeParams[reflectionProbeCount] = new Vector4(probe.intensity, probe.blendDistance);
                data.reflectionProbesData.octahedralAtlas[reflectionProbeCount] = octahedralAtlas;

                atlasArea += octahedralAtlas.height * octahedralAtlas.height;
                reflectionProbeCount++;
            }

            int atlasSize = Mathf.NextPowerOfTwo(Mathf.RoundToInt(Mathf.Sqrt(atlasArea)));
            data.reflectionProbesData.atlasSize = atlasSize * atlasSize / 2 >= atlasArea ? new Vector2Int(atlasSize * 3 / 2, atlasSize / 2) : new Vector2Int(atlasSize * 3 / 2, atlasSize);
            data.reflectionProbesData.probeCount = reflectionProbeCount;
            
            m_Packer.Pack(ref data.reflectionProbesData.probeSampleParams, reflectionProbeCount, atlasSize, 1.5f);
        }
    }
}