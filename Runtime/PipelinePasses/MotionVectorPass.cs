using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class MotionVectorPass : PipelinePass
    {
        private class MotionVectorPassData
        {
            public Material cameraMotionVectorMaterial;
            
            public RendererListHandle opaqueRendererList;
            public RendererListHandle alphaTestRendererList;
        }
        
        private Material m_CameraMotionVectorMaterial;

        protected override void Initialize(ref YPipelineData data)
        {
            m_CameraMotionVectorMaterial = new Material(data.runtimeResources.CameraMotionVectorShader);
            m_CameraMotionVectorMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        protected override void OnDispose()
        {
            CoreUtils.Destroy(m_CameraMotionVectorMaterial);
            m_CameraMotionVectorMaterial = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<MotionVectorPassData>("Camera & Object Motion Vector", out var passData))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                data.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
                
                // Motion Vector Texture
                Vector2Int bufferSize = data.BufferSize;
                TextureDesc motionVectorDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    colorFormat = GraphicsFormat.R16G16_SFloat,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = "Motion Vector Texture"
                };

                data.MotionVectorTexture = data.renderGraph.CreateTexture(motionVectorDesc);
                builder.SetRenderAttachment(data.MotionVectorTexture, 0, AccessFlags.Write);
                
                // Object Motion Vector
                builder.SetRenderAttachmentDepth(data.CameraDepthAttachment, AccessFlags.ReadWrite);
                
                RendererListDesc opaqueRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_MotionVectorsShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.MotionVectors,
                    renderQueueRange = new RenderQueueRange(2000, 2449),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                RendererListDesc alphaTestRendererListDesc = new RendererListDesc(YPipelineShaderTagIDs.k_MotionVectorsShaderTagId, data.cullingResults, data.camera)
                {
                    rendererConfiguration = PerObjectData.MotionVectors,
                    renderQueueRange = new RenderQueueRange(2450, 2499),
                    sortingCriteria = SortingCriteria.OptimizeStateChanges
                };
                
                passData.opaqueRendererList = data.renderGraph.CreateRendererList(opaqueRendererListDesc);
                passData.alphaTestRendererList = data.renderGraph.CreateRendererList(alphaTestRendererListDesc);
                builder.UseRendererList(passData.opaqueRendererList);
                builder.UseRendererList(passData.alphaTestRendererList);
                
                // Camera Motion Vector
                passData.cameraMotionVectorMaterial = m_CameraMotionVectorMaterial;
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                
                // Render Graph Setting
                builder.SetGlobalTextureAfterPass(data.MotionVectorTexture, YPipelineShaderIDs.k_MotionVectorTextureID);
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((MotionVectorPassData data, RasterGraphContext context) =>
                {
                    // context.cmd.SetRenderTarget(data.motionVectorTexture, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                    //     data.depthAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                    
                    // Object Motion Vector
                    context.cmd.BeginSample("Object Motion Vector");
                    context.cmd.DrawRendererList(data.opaqueRendererList);
                    context.cmd.DrawRendererList(data.alphaTestRendererList);
                    context.cmd.EndSample("Object Motion Vector");
                    
                    // Camera Motion Vector
                    context.cmd.BeginSample("Camera Motion Vector");
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.cameraMotionVectorMaterial, 0, MeshTopology.Triangles, 3);
                    context.cmd.EndSample("Camera Motion Vector");
                });
            }
        }
    }
}