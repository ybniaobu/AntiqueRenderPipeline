using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public static class BlitHelper
    {
        public static readonly int k_BlitTextureID = Shader.PropertyToID("_BlitTexture");
        public static readonly int k_ScaleOffsetID = Shader.PropertyToID("_ScaleOffset"); // Source 的 Scale Offset
        
        // ----------------------------------------------------------------------------------------------------
        // Materials
        // ----------------------------------------------------------------------------------------------------
        
        private static Material m_CopyMaterial;
        private static Material m_CopyDepthMaterial;

        private static MaterialPropertyBlock m_PropertyBlock;

        public static void Initialize()
        {
            var runtimeResources = GraphicsSettings.GetRenderPipelineSettings<YPipelineRuntimeResources>();
            m_CopyMaterial = CoreUtils.CreateEngineMaterial(runtimeResources.CopyShader);
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(runtimeResources.CopyDepthShader);

            m_PropertyBlock = new MaterialPropertyBlock();
        }

        public static void Dispose()
        {
            CoreUtils.Destroy(m_CopyMaterial);
            m_CopyMaterial = null;
            CoreUtils.Destroy(m_CopyDepthMaterial);
            m_CopyDepthMaterial = null;
            
            m_PropertyBlock.Clear();
            m_PropertyBlock = null;
        }

        // ----------------------------------------------------------------------------------------------------
        // Functions
        // ----------------------------------------------------------------------------------------------------

        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect cameraRect)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            m_CopyMaterial.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        /// <summary>
        /// 无 destination，需提前 SetRenderTarget，主要用于 Blit 多次。
        /// </summary>
        public static void BlitTexture(CommandBuffer cmd, Texture source, Rect rect)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        /// <summary>
        /// 无 destination，需提前 SetRenderTarget，主要用于 Blit 多次。
        /// </summary>
        public static void BlitTexture(UnsafeCommandBuffer cmd, Texture source, Rect rect)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        /// <summary>
        /// 无 destination，需提前 SetRenderTarget，主要用于 Blit 多次。
        /// </summary>
        public static void BlitTexture(RasterCommandBuffer cmd, Texture source, Rect rect, Vector4 scaleOffset)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            m_PropertyBlock.SetVector(k_ScaleOffsetID, scaleOffset);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            m_CopyMaterial.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, Texture source, TextureHandle destination, Rect cameraRect)
        {
            m_CopyMaterial.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitGlobalTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect cameraRect, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(CommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect cameraRect, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect cameraRect, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, Texture source, TextureHandle destination, Rect cameraRect, Material material, int pass)
        {
            material.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(cameraRect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void DrawTexture(CommandBuffer cmd, TextureHandle destination, Material material, int pass)
        {
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }

        public static void CopyDepth(CommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            //cmd.SetGlobalTexture(k_BlitTextureId, source);
            m_CopyDepthMaterial.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void CopyDepth(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            //cmd.SetGlobalTexture(k_BlitTextureId, source);
            m_CopyDepthMaterial.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Triangles, 3);
        }
    }
}