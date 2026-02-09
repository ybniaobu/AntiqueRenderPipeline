using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class DebugPass : PipelinePass
    {
        private class DebugPassData
        {
            // Light Culling Debug
            public Material lightCullingDebugMaterial;
            public Vector4 tilesDebugParams;
            public float tilesDebugOpacity;
        }

        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            if (data.debugSettings.lightingDebugSettings.ShowTiles)
            {
                using (var builder = data.renderGraph.AddRasterRenderPass<DebugPassData>("Debug (Editor)", out var passData))
                {
                    // Light Culling Debug
                    passData.lightCullingDebugMaterial = data.debugSettings.lightingDebugSettings.lightCullingDebugMaterial;
                    passData.tilesDebugOpacity = data.debugSettings.lightingDebugSettings.tileOpacity;
                    Vector4 debugParams = new Vector4(data.debugSettings.lightingDebugSettings.showReflectionProbeTiles ? 1 : 0,
                        data.debugSettings.lightingDebugSettings.showZeroTiles ? 1 : 0);
                    passData.tilesDebugParams = debugParams;

                    builder.SetRenderAttachment(data.CameraColorTarget, 0, AccessFlags.Write);

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((DebugPassData data, RasterGraphContext context) =>
                    {
                        // Light Culling Debug
                        data.lightCullingDebugMaterial.SetVector(LightingDebugSettings.k_TilesDebugParamsID, data.tilesDebugParams);
                        data.lightCullingDebugMaterial.SetFloat(LightingDebugSettings.k_TilesDebugOpacityID, data.tilesDebugOpacity);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.lightCullingDebugMaterial, 0, MeshTopology.Triangles, 3);
                    });
                }
            }
        }
    }
}