using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace YPipeline
{
    public class TAASubPass : PostProcessingSubPass
    {
        private class TAAPassData
        {
            public Material material;
            public bool isTAAHistoryReset;
            public bool isFirstFrame;
            
            public TextureHandle colorAttachment;
            public TextureHandle motionVectorTexture;
            public TextureHandle taaTarget;
            public TextureHandle taaHistory;
            public TextureHandle motionVectorHistory;
            
            // Shader Variables
            public Vector4 taaParams;
            
            // Shader Keywords Related
            public bool is3X3;
            public bool isYCoCg;
            public bool isVarianceAABB;
            public ColorRectifyMode rectifyMode;
            public CurrentFilter currentFilter;
            public HistoryFilter historyFilter;
        }
        
        private TAA m_TAA;
        
        private Material m_TAAMaterial;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_TAA = stack.GetComponent<TAA>();
            
            m_TAAMaterial = new Material(data.runtimeResources.TAAShader);
            m_TAAMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        public override void OnDispose()
        {
            m_TAA = null;
            
            CoreUtils.Destroy(m_TAAMaterial);
            m_TAAMaterial = null;
        }

        public override void OnRecord(ref YPipelineData data)
        {
            CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_TAA, data.IsTAAEnabled);
            if (data is { IsSSGIEnabled: true, IsTAAEnabled: false }) CopySceneColor(ref data);
            if (!data.IsTAAEnabled) return;
            
            // TODO: 暂时使用 UnsafePass，因为 ComputePass 无法 Copy；
            using (var builder = data.renderGraph.AddUnsafePass<TAAPassData>("TAA", out var passData))
            {
                passData.material = m_TAAMaterial;
                passData.isFirstFrame = Time.frameCount == 0;

                passData.colorAttachment = data.CameraColorAttachment;
                builder.UseTexture(data.CameraColorAttachment, AccessFlags.Read);
                passData.motionVectorTexture = data.MotionVectorTexture;
                builder.UseTexture(data.MotionVectorTexture, AccessFlags.Read);
                builder.UseTexture(data.CameraDepthTexture, AccessFlags.Read);
                
                // Record shader variables & keywords
                passData.taaParams = new Vector4(m_TAA.historyBlendFactor.value, m_TAA.varianceCriticalValue.value, 
                    m_TAA.fixedContrastThreshold.value, m_TAA.relativeContrastThreshold.value);

                passData.is3X3 = m_TAA.neighborhood.value == TAANeighborhood._3X3;
                passData.isYCoCg = m_TAA.colorSpace.value == TAAColorSpace.YCoCg;
                passData.isVarianceAABB = m_TAA.AABB.value == AABBMode.Variance;
                passData.rectifyMode = m_TAA.colorRectifyMode.value;
                passData.currentFilter = m_TAA.currentFilter.value;
                passData.historyFilter = m_TAA.historyFilter.value;
                
                // TAA history
                Vector2Int bufferSize = data.BufferSize;
                
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                passData.isTAAHistoryReset = yCamera.perCameraData.IsTAAHistoryReset;
                yCamera.perCameraData.IsTAAHistoryReset = false;
                passData.taaHistory = data.TAAHistory;
                builder.UseTexture(data.TAAHistory, AccessFlags.ReadWrite);

                // Create TAA target
                TextureDesc taaTargetDesc = new TextureDesc(bufferSize.x, bufferSize.y)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    clearBuffer = true,
                    anisoLevel = 0,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "TAA Target"
                };
                
                data.TAATarget = data.renderGraph.CreateTexture(taaTargetDesc);
                passData.taaTarget = data.TAATarget;
                builder.UseTexture(data.TAATarget, AccessFlags.Write);
                
                builder.AllowPassCulling(false);
            
                builder.SetRenderFunc((TAAPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.BeginSample("TAABlendHistory");
                    data.material.SetVector(YPipelineShaderIDs.k_TAAParamsID, data.taaParams);
                    // data.material.SetTexture(YPipelineShaderIDs.k_MotionVectorTextureID, data.isFirstFrame || data.isTAAHistoryReset ? context.defaultResources.blackTexture : data.motionVectorTexture);
                    
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAASample3X3, data.is3X3);
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAAYCOCG, data.isYCoCg);
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAAVariance, data.isVarianceAABB);
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAACurrentFilter, data.currentFilter == CurrentFilter.Gaussian);
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_TAAHistoryFilter, data.historyFilter == HistoryFilter.CatmullRomBicubic);
                    
                    if (data.isFirstFrame || data.isTAAHistoryReset) BlitHelper.BlitTexture(context.cmd, data.colorAttachment, data.taaHistory);
                    data.material.SetTexture(YPipelineShaderIDs.k_TAAHistoryID, data.taaHistory);
                    
                    BlitHelper.BlitTexture(context.cmd, data.colorAttachment, data.taaTarget, data.material, (int) data.rectifyMode);
                    context.cmd.EndSample("TAABlendHistory");
                    
                    context.cmd.BeginSample("TAACopyHistory");
                    // bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
                    // if (copyTextureSupported) context.cmd.CopyTexture(data.taaTarget, data.taaHistory);
                    // else BlitUtility.BlitTexture(context.cmd, data.taaTarget, data.taaHistory);

                    context.cmd.CopyTexture(data.taaTarget, data.taaHistory);
                    context.cmd.EndSample("TAACopyHistory");
                });
            }
        }

        public void CopySceneColor(ref YPipelineData data)
        {
            // 看情况是否改为 AddCopyPass 或 AddBlitPass。
            using (var builder = data.renderGraph.AddUnsafePass<TAAPassData>("Copy Scene Color", out var passData))
            {
                passData.colorAttachment = data.CameraColorAttachment;
                builder.UseTexture(data.CameraColorAttachment, AccessFlags.Read);
                passData.taaHistory = data.SceneHistory;
                builder.UseTexture(data.SceneHistory, AccessFlags.ReadWrite);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((TAAPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.CopyTexture(data.colorAttachment, data.taaHistory);
                });
            }
        }
    }
}