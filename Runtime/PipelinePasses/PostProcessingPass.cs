using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    public class PostProcessingPass : PipelinePass
    {
        private class PostProcessingPassData
        {
            // TODO: 更改到 SceneCameraRenderer 内后删除
            public CameraType cameraType;
            
            public TextureHandle colorAttachment;
            public TextureHandle cameraColorTarget;
        }
        
        private TAASubPass m_TAASubPass;
        private BloomSubPass m_BloomSubPass;
        private ColorGradingLutSubPass m_ColorGradingLutSubPass;
        private UberPostProcessingSubPass m_UberPostProcessingSubPass;
        private FinalPostProcessingSubPass m_FinalPostProcessingSubPass;
        
        private ProfilingSampler m_Sampler;
        
        protected override void Initialize(ref YPipelineData data)
        {
            m_TAASubPass = PostProcessingSubPass.Create<TAASubPass>(ref data);
            m_BloomSubPass = PostProcessingSubPass.Create<BloomSubPass>(ref data);
            m_ColorGradingLutSubPass = PostProcessingSubPass.Create<ColorGradingLutSubPass>(ref data);
            m_UberPostProcessingSubPass = PostProcessingSubPass.Create<UberPostProcessingSubPass>(ref data);
            m_FinalPostProcessingSubPass = PostProcessingSubPass.Create<FinalPostProcessingSubPass>(ref data);
            
            m_Sampler = new ProfilingSampler("Post Processing");
        }

        protected override void OnDispose()
        {
            m_Sampler = null;
            
            m_TAASubPass.OnDispose();
            m_BloomSubPass.OnDispose();
            m_ColorGradingLutSubPass.OnDispose();
            m_UberPostProcessingSubPass.OnDispose();
            m_FinalPostProcessingSubPass.OnDispose();
            
            m_TAASubPass = null;
            m_BloomSubPass = null;
            m_ColorGradingLutSubPass = null;
            m_UberPostProcessingSubPass = null;
            m_FinalPostProcessingSubPass = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
#if UNITY_EDITOR
            using (var builder = data.renderGraph.AddUnsafePass<PostProcessingPassData>("Disable Post Processing (Editor Preview)", out var passData))
            {
                passData.cameraType = data.camera.cameraType;

                passData.colorAttachment = data.CameraColorAttachment;
                builder.UseTexture(data.CameraColorAttachment, AccessFlags.Read);
                passData.cameraColorTarget = data.CameraColorTarget;
                builder.UseTexture(data.CameraColorTarget, AccessFlags.Write);
                
                builder.SetRenderFunc((PostProcessingPassData data, UnsafeGraphContext context) =>
                {
                    // disable post-processing in material preview and reflection probe preview
                    if (data.cameraType > CameraType.SceneView)
                    {
                        // TODO: 改变逻辑
                        BlitHelper.BlitTexture(context.cmd, data.colorAttachment, data.cameraColorTarget);
                    }
            
                    // enable or disable post-processing in the scene window via its effects dropdown menu in its toolbar
                    if (data.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
                    {
                        BlitHelper.BlitTexture(context.cmd, data.colorAttachment, data.cameraColorTarget);
                    }
                });
            }
            
            // TODO: 更改到 SceneCameraRenderer 内
            if (data.camera.cameraType > CameraType.SceneView || data.camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            {
                return;
            }
#endif
            
            data.renderGraph.BeginProfilingSampler(m_Sampler);
            
            m_TAASubPass.OnRecord(ref data);
            m_BloomSubPass.OnRecord(ref data);
            m_ColorGradingLutSubPass.OnRecord(ref data);
            m_UberPostProcessingSubPass.OnRecord(ref data);
            m_FinalPostProcessingSubPass.OnRecord(ref data);
            
            data.renderGraph.EndProfilingSampler(m_Sampler);
        }
    }
}