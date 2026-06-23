using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace YPipeline
{
    internal static class CullingUtils
    {
        public static bool SetupCullingParameter(ref YPipelineData data, out ScriptableCullingParameters cullingParameters)
        {
            if (!data.camera.TryGetCullingParameters(out cullingParameters)) return false;
            
            cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling; // cancel per-object culling for Lights and Reflection Probes
            // cullingParameters.cullingOptions |= CullingOptions.SkipTexturelessReflectionProbes;
            cullingParameters.maximumVisibleLights = YPipelineLightData.k_MaxVisibleLightCount;
            cullingParameters.reflectionProbeSortingCriteria = ReflectionProbeSortingCriteria.ImportanceThenSize;
            cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
            cullingParameters.conservativeEnclosingSphere = true;
            
            return true;
        }
        
        public static void EmitUIGeometry(Camera camera)
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                ScriptableRenderContext.EmitGeometryForCamera(camera);

            if (camera.cameraType == CameraType.SceneView) 
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif
        }

        public static void Cull(ref YPipelineData data, ref ScriptableCullingParameters cullingParameters)
        {
            data.cullingResults = data.context.Cull(ref cullingParameters);
        }
        
        public static void CullShadowCasters(ref YPipelineData data)
        {
            ref CullingResults cullingResults = ref data.cullingResults;
            YPipelineLightData lightData = data.lightData;
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            int visibleLightCount = visibleLights.Length;
            NativeArray<LightShadowCasterCullingInfo> cullingInfoPerLight = new NativeArray<LightShadowCasterCullingInfo>(visibleLightCount, Allocator.Temp);
            NativeArray<ShadowSplitData> shadowSplitData = new NativeArray<ShadowSplitData>(visibleLightCount * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            int sunLightShadowCount = 0;
            int cascadeCount = data.asset.cascadeCount;
            int sunLightShadowAtlasSize = (int) data.asset.sunLightShadowAtlasSize;
            
            int punctualLightSliceCount = 0;
            int totalPunctualLightSliceArea = 0;
            Vector2Int punctualLightAtlasSize = ShadowUtils.GetPunctualLightAtlasSize((uint) data.asset.punctualLightShadowAtlasSize);
            int punctualLightAtlasArea = punctualLightAtlasSize.x * punctualLightAtlasSize.y;
            
            bool reversedZ = SystemInfo.usesReversedZBuffer;
            
            for (int i = 0; i < visibleLightCount; i++)
            {
                ref readonly VisibleLight visibleLight = ref visibleLights.UnsafeElementAt(i);
                LightType lightType = visibleLight.lightType;
                Light light = visibleLight.light;
                YPipelineLight yLight = light.GetYPipelineLight();
                int splitDataIdx = i * 6;

                if (lightType == LightType.Directional)
                {
                    sunLightShadowCount++; // Only determine if the first sun light casts a shadow.
                    if (sunLightShadowCount > YPipelineLightData.k_MaxDirectionalLightCount) continue;
                    
                    bool noShadowCasting = light.shadows == LightShadows.None || light.shadowStrength == 0.0f;
                    lightData.sunLightIndex = noShadowCasting ? -1 : i;
                    if (noShadowCasting) continue;
                    
                    cullingInfoPerLight[i] = new LightShadowCasterCullingInfo
                    {
                        projectionType = BatchCullingProjectionType.Orthographic,
                        splitRange = new RangeInt(splitDataIdx, cascadeCount)
                    };
                    
                    int size = sunLightShadowAtlasSize >> 1;
                    Vector2 sliceScale = new Vector2(cascadeCount == 1 ? 1 : 0.5f, cascadeCount <= 2 ? 1 : 0.5f);
                    Vector3 spiltRatios = data.asset.SpiltRatios;
                    float nearPlaneOffset = light.shadowNearPlane + 0.8f;

                    for (int j = 0; j < cascadeCount; j++)
                    {
                        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(i, j, cascadeCount, spiltRatios,
                            size, nearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                        splitData.shadowCascadeBlendCullingFactor = 1.0f;
                        shadowSplitData[splitDataIdx + j] = splitData;
                        
                        lightData.sunLightViewMatrices[j] = viewMatrix;
                        lightData.sunLightProjectionMatrices[j] = projectionMatrix;
                        lightData.cascadeCullingSpheres[j] = splitData.cullingSphere;
                        float frustumSize = 2.0f / projectionMatrix.m11;
                        lightData.sunLightDepthParams[j] = reversedZ ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23, frustumSize) 
                                                                     : new Vector4(projectionMatrix.m22, projectionMatrix.m23, frustumSize);
                        Vector2 sliceOffset = new Vector2(j & 1, j >> 1); // j % 2, j / 2
                        lightData.sunLightViewports[j] = new Rect(sliceOffset.x * size, sliceOffset.y * size, size, size);
                        lightData.sunLightShadowMatrices[j] = ShadowUtils.GetWorldToSlicedLightScreenMatrix(projectionMatrix * viewMatrix, sliceOffset * 0.5f, sliceScale);
                    }
                }
                else if (lightType == LightType.Point)
                {
                    bool outOfRange = punctualLightSliceCount + 6 > YPipelineLightData.k_MaxShadowSliceCount;
                    int sliceSize = yLight.shadowResolution;
                    int neededArea = sliceSize * sliceSize * 6;
                    bool noSpace = totalPunctualLightSliceArea + neededArea > punctualLightAtlasArea;
                    bool noShadowCasting = light.shadows == LightShadows.None || light.shadowStrength == 0.0f;
                    bool skipCulling = outOfRange || noSpace || noShadowCasting;

#if UNITY_ASSERTIONS
                    if (noSpace)
                    {
                        Debug.LogWarning($"No room in shadow atlas for {light.name}, skipping shadow rendering.");
                    }
#endif
                    
                    lightData.punctualLightVisibleIdxToSliceIdx[i] = skipCulling ? -1 : punctualLightSliceCount;
                    if (skipCulling) continue;
                    
                    cullingInfoPerLight[i] = new LightShadowCasterCullingInfo
                    {
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitRange = new RangeInt(splitDataIdx, 6)
                    };
                    
                    for (int j = 0; j < 6; j++)
                    {
                        cullingResults.ComputePointShadowMatricesAndCullingPrimitives(i, (CubemapFace) j, 0.0f, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                        shadowSplitData[splitDataIdx + j] = splitData;
                        
                        // Native API CullingResults.ComputePointShadowMatricesAndCullingPrimitives invert point light shadow face to deal with the shadow acne.
                        // To ensure numerical consistency for the shadow bias between spot and point light, the 2nd row of the view matrix is flipped to counter the effect.
                        viewMatrix.m11 = -viewMatrix.m11;
                        viewMatrix.m12 = -viewMatrix.m12;
                        viewMatrix.m13 = -viewMatrix.m13;
                        
                        lightData.punctualLightSliceIdxToVisibleIdx[punctualLightSliceCount] = i;
                        lightData.punctualLightViewMatrices[punctualLightSliceCount] = viewMatrix;
                        lightData.punctualLightProjectionMatrices[punctualLightSliceCount] = projectionMatrix;
                        lightData.punctualLightSampleParams[punctualLightSliceCount] = new Vector4(0.0f, 0.0f, sliceSize, 0.0f);
                        
                        ref var sliceData = ref lightData.punctualLightSlicesData[punctualLightSliceCount];
                        sliceData.depthParams = reversedZ ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                        sliceData.shadowMatrix = ShadowUtils.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                        
                        punctualLightSliceCount++;
                    }
                    totalPunctualLightSliceArea += neededArea;
                }
                else if (lightType == LightType.Spot)
                {
                    bool outOfRange = punctualLightSliceCount + 1 > YPipelineLightData.k_MaxShadowSliceCount;
                    int sliceSize = yLight.shadowResolution;
                    int neededArea = sliceSize * sliceSize;
                    bool noSpace = totalPunctualLightSliceArea + neededArea > punctualLightAtlasArea;
                    bool noShadowCasting = light.shadows == LightShadows.None || light.shadowStrength == 0.0f;
                    bool skipCulling = outOfRange || noSpace || noShadowCasting;
                    
#if UNITY_ASSERTIONS
                    if (noSpace)
                    {
                        Debug.LogWarning($"No room in shadow atlas for {light.name}, skipping shadow rendering.");
                    }
#endif
                    
                    lightData.punctualLightVisibleIdxToSliceIdx[i] = skipCulling ? -1 : punctualLightSliceCount;
                    if (skipCulling) continue;
                    
                    cullingInfoPerLight[i] = new LightShadowCasterCullingInfo
                    {
                        projectionType = BatchCullingProjectionType.Perspective,
                        splitRange = new RangeInt(splitDataIdx, 1)
                    };
                    
                    cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(i, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                    shadowSplitData[splitDataIdx] = splitData;
                    
                    lightData.punctualLightSliceIdxToVisibleIdx[punctualLightSliceCount] = i;
                    lightData.punctualLightViewMatrices[punctualLightSliceCount] = viewMatrix;
                    lightData.punctualLightProjectionMatrices[punctualLightSliceCount] = projectionMatrix;
                    lightData.punctualLightSampleParams[punctualLightSliceCount] = new Vector4(0.0f, 0.0f, sliceSize, 0.0f);
                    
                    ref var sliceData = ref lightData.punctualLightSlicesData[punctualLightSliceCount];
                    sliceData.depthParams = reversedZ ? new Vector4(-projectionMatrix.m22, -projectionMatrix.m23) : new Vector4(projectionMatrix.m22, projectionMatrix.m23);
                    sliceData.shadowMatrix = ShadowUtils.GetWorldToLightScreenMatrix(projectionMatrix * viewMatrix);
                    
                    punctualLightSliceCount++;
                    totalPunctualLightSliceArea += neededArea;
                }
            }
            
            data.context.CullShadowCasters(cullingResults, new ShadowCastersCullingInfos
            {
                perLightInfos = cullingInfoPerLight,
                splitBuffer = shadowSplitData
            });
            
            lightData.cascadeCount = cascadeCount;
            lightData.punctualLightSliceCount = punctualLightSliceCount;
            lightData.punctualLightAtlasSize = punctualLightAtlasSize;
            if (sunLightShadowCount == 0) lightData.sunLightIndex = -1;
            lightData.cascadeAtlasSize = ShadowUtils.GetCascadeAtlasSize(sunLightShadowAtlasSize, cascadeCount);
            lightData.cascadeParams = new Vector4(data.asset.maxShadowDistance, data.asset.distanceFade, cascadeCount, sunLightShadowAtlasSize >> 1);
        }
    }
}