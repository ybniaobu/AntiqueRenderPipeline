using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class FinalPostProcessingSubPass : PostProcessingSubPass
    {
        private class FinalPostPassData
        {
            public Material material;
            public TextureHandle finalTexture;
                
            public bool isFXAAEnabled;
            public bool isFXAAQualityEnabled;
            
            public bool isFilmGrainEnabled;
            public TextureHandle filmGrainTexture;
            public Vector4 filmGrainParams;
            public Vector4 filmGrainTexParams;
            
            public Rect cameraPixelRect;
        }
        
        private FilmGrain m_FilmGrain;
        
        private RTHandle m_FilmGrainTexture;
        private System.Random m_Random;
        
        private Material m_FinalPostProcessingMaterial;
        
        protected override void Initialize(ref YPipelineData data)
        {
            m_FinalPostProcessingMaterial = new Material(data.runtimeResources.FinalPostProcessing);
            m_FinalPostProcessingMaterial.hideFlags = HideFlags.HideAndDontSave;
            
            m_Random = new System.Random();
        }

        public override void OnDispose()
        {
            m_Random = null;
            m_FilmGrain = null;
            
            RTHandles.Release(m_FilmGrainTexture);
            m_FilmGrainTexture = null;
            
            CoreUtils.Destroy(m_FinalPostProcessingMaterial);
            m_FinalPostProcessingMaterial = null;
        }

        public override void OnRecord(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            
            using (var builder = data.renderGraph.AddRasterRenderPass<FinalPostPassData>("Final Post Processing", out var passData))
            {
                passData.material = m_FinalPostProcessingMaterial;
                passData.finalTexture = data.CameraFinalTexture;
                builder.UseTexture(data.CameraFinalTexture, AccessFlags.Read);
                builder.SetRenderAttachment(data.CameraColorTarget, 0, AccessFlags.Write);
                
                passData.isFXAAEnabled = data.asset.antiAliasingMode == AntiAliasingMode.FXAA;
                passData.isFXAAQualityEnabled = data.asset.fxaaMode == FXAAMode.Quality;
                passData.isFilmGrainEnabled = m_FilmGrain.IsActive();
                passData.cameraPixelRect = data.camera.pixelRect;
                
                builder.AllowPassCulling(false);
                
                if (m_FilmGrain.IsActive())
                {
                    if (m_FilmGrain.type.value != FilmGrainKinds.Custom)
                    {
                        if (m_FilmGrainTexture == null || m_FilmGrainTexture.externalTexture != data.runtimeResources.FilmGrainTex[(int)m_FilmGrain.type.value])
                        {
                            m_FilmGrainTexture?.Release();
                            m_FilmGrainTexture = RTHandles.Alloc(data.runtimeResources.FilmGrainTex[(int)m_FilmGrain.type.value]);
                        }
                    }
                    else
                    {
                        if (m_FilmGrainTexture == null || m_FilmGrainTexture.externalTexture != m_FilmGrain.texture.value)
                        {
                            m_FilmGrainTexture?.Release();
                            m_FilmGrainTexture = RTHandles.Alloc(m_FilmGrain.texture.value);
                        }
                    }
                    
                    passData.filmGrainTexture = data.renderGraph.ImportTexture(m_FilmGrainTexture);
                    builder.UseTexture(passData.filmGrainTexture, AccessFlags.Read);
                    
                    float uvScaleX = data.camera.pixelWidth / (float) m_FilmGrainTexture.externalTexture.width;
                    float uvScaleY = data.camera.pixelHeight / (float) m_FilmGrainTexture.externalTexture.height;
                    float offsetX = (float) m_Random.NextDouble();
                    float offsetY = (float) m_Random.NextDouble();
                    
                    passData.filmGrainParams = new Vector4(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value);
                    passData.filmGrainTexParams = new Vector4(uvScaleX, uvScaleY, offsetX, offsetY);
                }
                else
                {
                    m_FilmGrainTexture?.Release();
                }
                
                builder.SetRenderFunc((FinalPostPassData data, RasterGraphContext context) =>
                {
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_FXAAQuality, data.isFXAAEnabled && data.isFXAAQualityEnabled);
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_FXAAConsole, data.isFXAAEnabled && !data.isFXAAQualityEnabled);
                    
                    CoreUtils.SetKeyword(data.material, YPipelineKeywords.k_FilmGrain, data.isFilmGrainEnabled);
                    if (data.isFilmGrainEnabled)
                    {
                        data.material.SetVector(YPipelineShaderIDs.k_FilmGrainParamsID, data.filmGrainParams);
                        data.material.SetVector(YPipelineShaderIDs.k_FilmGrainTexParamsID, data.filmGrainTexParams);
                        data.material.SetTexture(YPipelineShaderIDs.k_FilmGrainTexID, data.filmGrainTexture);
                    }
                    
                    data.material.SetTexture(BlitHelper.k_BlitTextureID, data.finalTexture);
                    context.cmd.SetViewport(data.cameraPixelRect);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
                });
            }
        }
    }
}