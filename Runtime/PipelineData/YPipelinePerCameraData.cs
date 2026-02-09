using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public class YPipelinePerCameraData : IDisposable
    {
        private bool m_IsPerCameraDataReset;
        
        // ----------------------------------------------------------------------------------------------------
        // Matrices
        // ----------------------------------------------------------------------------------------------------
        
        public Matrix4x4 viewMatrix;
        public Matrix4x4 projectionMatrix;
        public Matrix4x4 jitteredProjectionMatrix;
        public Matrix4x4 previousViewMatrix;
        public Matrix4x4 previousProjectionMatrix;
        public Matrix4x4 previousJitteredProjectionMatrix;

        public void SetPerCameraDataMatrices(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 jitteredProjectionMatrix)
        {
            if (Time.frameCount == 0 || m_IsPerCameraDataReset)
            {
                this.previousViewMatrix = viewMatrix;
                this.previousProjectionMatrix = projectionMatrix;
                this.previousJitteredProjectionMatrix = jitteredProjectionMatrix;
                m_IsPerCameraDataReset = false;
            }
            else
            {
                this.previousViewMatrix = this.viewMatrix;
                this.previousProjectionMatrix = this.projectionMatrix;
                this.previousJitteredProjectionMatrix = this.jitteredProjectionMatrix;
            }
            
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
            this.jitteredProjectionMatrix = jitteredProjectionMatrix;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // TAA History
        // ----------------------------------------------------------------------------------------------------

        public bool IsTAAHistoryReset { get; set; }
        private RTHandle m_TAAHistory;

        public RTHandle GetTAAHistory(ref RenderTextureDescriptor desc, string name = "TAA History")
        {
            if (m_TAAHistory == null || m_TAAHistory.rt.width != desc.width || m_TAAHistory.rt.height != desc.height)
            {
                IsTAAHistoryReset = true;
                m_TAAHistory?.Release();
                m_TAAHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_TAAHistory;
        }

        public void ReleaseTAAHistory()
        {
            m_TAAHistory?.Release();
            m_TAAHistory = null;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Scene History
        // ----------------------------------------------------------------------------------------------------

        public bool IsSceneHistoryReset { get; set; }
        private RTHandle m_SceneHistory;

        public RTHandle GetSceneHistory(ref RenderTextureDescriptor desc, string name = "Scene History")
        {
            if (m_SceneHistory == null || m_SceneHistory.rt.width != desc.width || m_SceneHistory.rt.height != desc.height)
            {
                IsSceneHistoryReset = true;
                m_SceneHistory?.Release();
                m_SceneHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_SceneHistory;
        }

        public void ReleaseSceneHistory()
        {
            m_SceneHistory?.Release();
            m_SceneHistory = null;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Irradiance History
        // ----------------------------------------------------------------------------------------------------
        
        public bool IsIrradianceHistoryReset { get; set; }
        private RTHandle m_IrradianceHistory;

        public RTHandle GetIrradianceHistory(ref RenderTextureDescriptor desc, string name = "Irradiance History")
        {
            if (m_IrradianceHistory == null || m_IrradianceHistory.rt.width != desc.width || m_IrradianceHistory.rt.height != desc.height)
            {
                IsIrradianceHistoryReset = true;
                m_IrradianceHistory?.Release();
                m_IrradianceHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_IrradianceHistory;
        }

        public void ReleaseIrradianceHistory()
        {
            m_IrradianceHistory?.Release();
            m_IrradianceHistory = null;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Ambient Occlusion History
        // ----------------------------------------------------------------------------------------------------
        
        public bool IsAOHistoryReset { get; set; }
        private RTHandle m_AOHistory;

        public RTHandle GetAOHistory(ref RenderTextureDescriptor desc, string name = "Ambient Occlusion History")
        {
            if (m_AOHistory == null || m_AOHistory.rt.width != desc.width || m_AOHistory.rt.height != desc.height)
            {
                IsAOHistoryReset = true;
                m_AOHistory?.Release();
                m_AOHistory = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, anisoLevel: 0, name: name);
            }
            return m_AOHistory;
        }

        public void ReleaseAOHistory()
        {
            m_AOHistory?.Release();
            m_AOHistory = null;
        }
        
        // ----------------------------------------------------------------------------------------------------
        // Standard Dispose Pattern
        // ----------------------------------------------------------------------------------------------------
        
        bool m_Disposed = false;
        
        public YPipelinePerCameraData()
        {
            m_IsPerCameraDataReset = true;
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~YPipelinePerCameraData()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    //Dispose managed resources
                }
                //Dispose unmanaged resources
                ReleaseTAAHistory();
                ReleaseSceneHistory();
                ReleaseIrradianceHistory();
                ReleaseAOHistory();
            }
            
            m_IsPerCameraDataReset = true;
            m_Disposed = true;
        }
    }
}