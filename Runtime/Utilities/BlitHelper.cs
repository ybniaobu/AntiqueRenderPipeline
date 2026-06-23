using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal static class BlitHelper
    {
        public static readonly int k_BlitTextureID = Shader.PropertyToID("_BlitTexture");
        private static readonly int k_ScaleOffsetID = Shader.PropertyToID("_ScaleOffset"); // Source 的 Scale Offset
        
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
        
        #region YPipeline Copy Material Blit
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(UnsafeCommandBuffer, TextureHandle, TextureHandle)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(RasterCommandBuffer, TextureHandle)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(RasterCommandBuffer cmd, TextureHandle source)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(UnsafeCommandBuffer, TextureHandle, TextureHandle, Rect)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect rect)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(RasterCommandBuffer, TextureHandle, Rect)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(RasterCommandBuffer cmd, TextureHandle source, Rect rect)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(RasterCommandBuffer cmd, TextureHandle source)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect rect)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(RasterCommandBuffer cmd, TextureHandle source, Rect rect)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect rect, Vector4 scaleOffset)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            m_PropertyBlock.SetVector(k_ScaleOffsetID, scaleOffset);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(RasterCommandBuffer cmd, TextureHandle source, Rect rect, Vector4 scaleOffset)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            m_PropertyBlock.SetVector(k_ScaleOffsetID, scaleOffset);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(RasterCommandBuffer cmd, Texture source, Rect rect, Vector4 scaleOffset)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            m_PropertyBlock.SetVector(k_ScaleOffsetID, scaleOffset);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        #endregion
        
        #region Custom Material Blit
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(UnsafeCommandBuffer, TextureHandle, TextureHandle, Material, int)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(RasterCommandBuffer, TextureHandle, Material, int)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(RasterCommandBuffer cmd, TextureHandle source, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(UnsafeCommandBuffer, TextureHandle, TextureHandle, Rect, Material, int)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect rect, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        /// <summary>
        /// Performs blit by binding the source texture to a global property "_BlitTexture".
        /// </summary>
        /// <remarks>
        /// Avoid using this method for frequent blits, as it modifies global shader state,
        /// please use <see cref="BlitTexture(RasterCommandBuffer, TextureHandle, Rect, Material, int)"/> instead.
        /// </remarks>
        public static void BlitGlobalTexture(RasterCommandBuffer cmd, TextureHandle source, Rect rect, Material material, int pass)
        {
            cmd.SetGlobalTexture(k_BlitTextureID, source);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Material material, int pass)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(RasterCommandBuffer cmd, TextureHandle source, Material material, int pass)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect rect, Material material, int pass)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(RasterCommandBuffer cmd, TextureHandle source, Rect rect, Material material, int pass)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination, Rect rect, Vector4 scaleOffset, Material material, int pass)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            m_PropertyBlock.SetVector(k_ScaleOffsetID, scaleOffset);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void BlitTexture(RasterCommandBuffer cmd, TextureHandle source, Rect rect, Vector4 scaleOffset, Material material, int pass)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            m_PropertyBlock.SetVector(k_ScaleOffsetID, scaleOffset);
            cmd.SetViewport(rect);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void DrawTexture(UnsafeCommandBuffer cmd, TextureHandle destination, Material material, int pass)
        {
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        public static void DrawTexture(RasterCommandBuffer cmd, TextureHandle destination, Material material, int pass)
        {
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
        }
        
        #endregion

        #region YPipeline Copy Depth
        
        public static void CopyDepth(UnsafeCommandBuffer cmd, TextureHandle source, TextureHandle destination)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        public static void CopyDepth(RasterCommandBuffer cmd, TextureHandle source)
        {
            m_PropertyBlock.Clear();
            m_PropertyBlock.SetTexture(k_BlitTextureID, source);
            cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Triangles, 3, 1, m_PropertyBlock);
        }
        
        #endregion
    }
}