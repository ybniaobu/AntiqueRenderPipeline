using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;

namespace YPipeline
{
    internal sealed class ReflectionProbeAtlasPass : PipelinePass
    {
        private class ReflectionProbeAtlasPassData
        {
            public YPipelineReflectionProbeData reflectionProbeData;
        }
        
        private PenAtlasPacker m_PenAtlasPacker;

        protected override void Initialize(ref YPipelineData data)
        {
            m_PenAtlasPacker = new PenAtlasPacker(YPipelineReflectionProbeData.k_MaxReflectionProbeCount);
        }

        protected override void OnDispose()
        {
            m_PenAtlasPacker.Dispose();
            m_PenAtlasPacker = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            CollectReflectionProbeData(ref data);
            
            using (var builder = data.renderGraph.AddRasterRenderPass<ReflectionProbeAtlasPassData>("Initialize Reflection Probe", out var passData))
            {
                YPipelineReflectionProbeData reflectionProbeData = data.reflectionProbeData;
                passData.reflectionProbeData = reflectionProbeData;

                if (data.reflectionProbeData.probeCount == 0)
                {
                    data.isReflectionProbeAtlasCreated = false;
                }
                else
                {
                    data.isReflectionProbeAtlasCreated = true;
                    Vector2Int size = reflectionProbeData.atlasSize;
                    GraphicsFormat format = data.asset.reflectionProbeAtlasFormat switch
                    {
                        HDRFormat.R11G11B10 => GraphicsFormat.B10G11R11_UFloatPack32,
                        HDRFormat.R16G16B16A16 => GraphicsFormat.R16G16B16A16_SFloat,
                        _ => GraphicsFormat.B10G11R11_UFloatPack32
                    };
                    TextureDesc atlasDesc = new TextureDesc(size.x, size.y)
                    {
                        colorFormat = format,
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        clearBuffer = false,
                        clearColor = Color.clear,
                        name = "Reflection Probe Atlas"
                    };
                    data.ReflectionProbeAtlas = data.renderGraph.CreateTexture(atlasDesc);
                    builder.SetRenderAttachment(data.ReflectionProbeAtlas, 0, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(data.ReflectionProbeAtlas, YPipelineShaderIDs.k_ReflectionProbeAtlasID);
                }
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                builder.SetRenderFunc(static (ReflectionProbeAtlasPassData data, RasterGraphContext context) =>
                {
                    YPipelineReflectionProbeData reflectionProbeData = data.reflectionProbeData;
                    
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_ReflectionProbeCountID, new Vector4(reflectionProbeData.probeCount, 0));
                    if (data.reflectionProbeData.probeCount == 0) return;
                    
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbePositionsID, reflectionProbeData.probePositions);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeBoxCenterID, reflectionProbeData.boxCenter);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeBoxExtentID, reflectionProbeData.boxExtent);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeSHID, reflectionProbeData.SH);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeSampleParamsID, reflectionProbeData.probeSampleParams);
                    context.cmd.SetGlobalVectorArray(YPipelineShaderIDs.k_ReflectionProbeParamsID, reflectionProbeData.probeParams);
                    context.cmd.SetGlobalMatrixArray(YPipelineShaderIDs.k_ReflectionProbeMatricesID, reflectionProbeData.probeMatrices);
                    
                    for (int i = 0; i < reflectionProbeData.probeCount; i++)
                    {
                        Vector4 probeParams = reflectionProbeData.probeSampleParams[i];
                        Vector4 scaleOffset = new Vector4(1, 1, -probeParams.x, -probeParams.y);
                        Rect rect = new Rect(probeParams.x, probeParams.y,  probeParams.z * 1.5f, probeParams.z);
                        BlitHelper.BlitTexture(context.cmd, reflectionProbeData.octahedralAtlas[i], rect, scaleOffset);
                    }
                });
            }
        }
        
        private void CollectReflectionProbeData(ref YPipelineData data)
        {
            NativeArray<VisibleReflectionProbe> visibleReflectionProbes = data.cullingResults.visibleReflectionProbes;
            int reflectionProbeCount = 0;
            int atlasArea = 0;
            int atlasSize = (int) data.asset.reflectionProbeAtlasSize;
            int maxAtlasArea = atlasSize * atlasSize;

            for (int i = 0; i < visibleReflectionProbes.Length; i++)
            {
                if (reflectionProbeCount >= data.asset.maxReflectionProbesOnScreen) break;
                
                ref readonly VisibleReflectionProbe visibleProbe = ref visibleReflectionProbes.UnsafeElementAt(i);
                ReflectionProbe probe = visibleProbe.reflectionProbe;
                YPipelineReflectionProbe yProbe = probe.GetYPipelineReflectionProbe();
                if (!yProbe.IsReady) continue;
                
                Texture octahedralAtlas = data.asset.reflectionProbeQuality switch
                {
                    Quality3Tier.High => yProbe.octahedralAtlasHigh,
                    Quality3Tier.Medium => yProbe.octahedralAtlasMedium,
                    Quality3Tier.Low => yProbe.octahedralAtlasLow,
                    _ => yProbe.octahedralAtlasMedium
                };
                
                atlasArea += octahedralAtlas.height * octahedralAtlas.height;
                if (atlasArea > maxAtlasArea) break;
                
                data.reflectionProbeData.probePositions[reflectionProbeCount] = probe.transform.position;
                data.reflectionProbeData.boxCenter[reflectionProbeCount] = visibleProbe.bounds.center;
                data.reflectionProbeData.boxCenter[reflectionProbeCount].w = visibleProbe.importance;
                data.reflectionProbeData.boxExtent[reflectionProbeCount] = visibleProbe.bounds.extents;
                data.reflectionProbeData.boxExtent[reflectionProbeCount].w = visibleProbe.isBoxProjection ? 1 : 0;
                Array.Copy(yProbe.SHData, 0, data.reflectionProbeData.SH, reflectionProbeCount * 7, 7);
                data.reflectionProbeData.probeSampleParams[reflectionProbeCount] = new Vector4(0, 0, octahedralAtlas.height);
                data.reflectionProbeData.probeParams[reflectionProbeCount] = new Vector4(probe.intensity, probe.blendDistance);
                Matrix4x4 localToWorldMatrix = Matrix4x4.TRS(visibleProbe.bounds.center, probe.transform.rotation, probe.size * 0.5f);
                data.reflectionProbeData.probeMatrices[reflectionProbeCount] = localToWorldMatrix.inverse;
                data.reflectionProbeData.octahedralAtlas[reflectionProbeCount] = octahedralAtlas;
                
                reflectionProbeCount++;
            }
            
            // m_BuddyAtlasPacker.Pack(ref data.reflectionProbesData.probeSampleParams, reflectionProbeCount, 1.5f);
            m_PenAtlasPacker.Pack(ref data.reflectionProbeData.probeSampleParams, reflectionProbeCount, atlasSize, 1.5f);
            data.reflectionProbeData.probeCount = reflectionProbeCount;
            data.reflectionProbeData.atlasSize = new Vector2Int(atlasSize * 3 / 2, atlasSize);
        }
    }
}