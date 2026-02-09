using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class TiledLightCullingPass : PipelinePass
    {
        private class TiledLightCullingPassData
        {
            public ComputeShader cs;
            public int punctualLightCount;
            public bool enableSplitDepth;
            
            // Input Buffer
            public BufferHandle lightInputInfosBuffer;
            public LightInputInfos[] lightInputInfos = new LightInputInfos[YPipelineLightsData.k_MaxPunctualLightCount];
            
            // Output Buffer
            public BufferHandle tileLightIndicesBuffer; // 每个 tile 都包含一个 header（light 的数量）和每个 light 的 index
            public BufferHandle tileReflectionProbeIndicesBuffer; // 每个 tile 都包含一个 header（reflection probe 的数量）和每个 probe 的 index
            
            // Tile Params
            public Vector2Int tileCountXY;
            public Vector2 tileUVSize;
            
            // Intersect Params
            public Vector3 cameraNearPlaneLB;
            public Vector2 tileNearPlaneSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LightInputInfos
        {
            public Vector4 bound;
            public Vector4 spotLightInfos;

            public void Setup(YPipelineLightsData lightsData, int index)
            {
                if (lightsData.punctualLightCount > 0)
                {
                    bound = lightsData.punctualLightPositions[index];
                    bound.w = lightsData.punctualLightParams[index].x;
                    spotLightInfos = -lightsData.punctualLightDirections[index];
                    float angle = Mathf.Acos(lightsData.punctualLightParams[index].w);
                    spotLightInfos.w = lightsData.punctualLightColors[index].w < 1.5f ? -1.0f : angle;
                }
            }
        }
        
        
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            // TODO: 好好整理 TiledLightCulling.compute 里的代码
            // 包含 light 和 reflection probe 的 tile based culling
            using (var builder = data.renderGraph.AddComputePass<TiledLightCullingPassData>("Tiled Based Light Culling", out var passData))
            {
                passData.cs = data.runtimeResources.TiledLightCullingCS;
                passData.punctualLightCount = data.lightsData.punctualLightCount;
                passData.enableSplitDepth = data.asset.enableSplitDepth;
                
                // Input
                for (int i = 0; i < data.lightsData.punctualLightCount; i++)
                {
                    passData.lightInputInfos[i].Setup(data.lightsData, i);
                }

                passData.lightInputInfosBuffer = builder.CreateTransientBuffer(new BufferDesc()
                {
                    count = YPipelineLightsData.k_MaxPunctualLightCount,
                    stride = 8 * sizeof(float),
                    target = GraphicsBuffer.Target.Structured,
                    name = "Light Culling Input Buffer"
                });
                
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                builder.UseBuffer(data.PunctualLightBufferHandle, AccessFlags.Read);
                
                // Tile Params
                float pixelToTileX = data.BufferSize.x / (float) YPipelineLightsData.k_TileSize;
                float pixelToTileY = data.BufferSize.y / (float) YPipelineLightsData.k_TileSize;
                passData.tileCountXY = new Vector2Int(Mathf.CeilToInt(pixelToTileX), Mathf.CeilToInt(pixelToTileY));
                int tileCount = passData.tileCountXY.x * passData.tileCountXY.y;
                passData.tileUVSize = new Vector2(1.0f / pixelToTileX, 1.0f / pixelToTileY);
                
                // Intersect Params
                float nearPlaneZ = data.camera.nearClipPlane;
                float nearPlaneHeight = Mathf.Tan(Mathf.Deg2Rad * data.camera.fieldOfView * 0.5f) * 2 * nearPlaneZ;
                float nearPlaneWidth = data.camera.aspect * nearPlaneHeight;
                passData.cameraNearPlaneLB = new Vector3(-nearPlaneWidth / 2, -nearPlaneHeight / 2, -nearPlaneZ);
                passData.tileNearPlaneSize = new Vector2(YPipelineLightsData.k_TileSize * nearPlaneWidth / data.BufferSize.x, YPipelineLightsData.k_TileSize * nearPlaneHeight / data.BufferSize.y);
                
                // Output
                data.TileLightIndicesBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = tileCount * YPipelineLightsData.k_PerTileDataSize,
                    stride = 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Tile Light Indices Buffer"
                });
                passData.tileLightIndicesBuffer = builder.UseBuffer(data.TileLightIndicesBufferHandle, AccessFlags.Write);

                data.TileReflectionProbeIndicesBufferHandle = data.renderGraph.CreateBuffer(new BufferDesc()
                {
                    count = tileCount * YPipelineReflectionProbesData.k_PerTileDataSize,
                    stride = 4,
                    target = GraphicsBuffer.Target.Structured,
                    name = "Tile Reflection Probe Indices Buffer"
                });
                passData.tileReflectionProbeIndicesBuffer  = builder.UseBuffer(data.TileReflectionProbeIndicesBufferHandle, AccessFlags.Write);
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                builder.SetRenderFunc((TiledLightCullingPassData data, ComputeGraphContext context) =>
                {
                    // LocalKeyword splitDepth = new LocalKeyword(data.cs, YPipelineKeywords.k_TileCullingSplitDepth);
                    CoreUtils.SetKeyword(data.cs, YPipelineKeywords.k_TileCullingSplitDepth, data.enableSplitDepth);
                    
                    int kernel = data.cs.FindKernel("TiledLightCulling");
                    context.cmd.SetBufferData(data.lightInputInfosBuffer, data.lightInputInfos, 0, 0, data.punctualLightCount);
                    context.cmd.SetComputeBufferParam(data.cs, kernel, YPipelineShaderIDs.k_LightInputInfosID, data.lightInputInfosBuffer);
                    
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_TileParamsID, new Vector4(data.tileCountXY.x, data.tileCountXY.y, data.tileUVSize.x, data.tileUVSize.y));
                    context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_CameraNearPlaneLBID, data.cameraNearPlaneLB);
                    context.cmd.SetComputeVectorParam(data.cs, YPipelineShaderIDs.k_TileNearPlaneSizeID, data.tileNearPlaneSize);
                    
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_TilesLightIndicesBufferID, data.tileLightIndicesBuffer);
                    context.cmd.SetGlobalBuffer(YPipelineShaderIDs.k_TileReflectionProbeIndicesBufferID, data.tileReflectionProbeIndicesBuffer);
                    context.cmd.DispatchCompute(data.cs, kernel, data.tileCountXY.x, data.tileCountXY.y, 1);
                });
            }
        }
    }
}