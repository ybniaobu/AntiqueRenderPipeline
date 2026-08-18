using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    public sealed class YPipelineDebugResources : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 1;
        public int version => m_Version;
        
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild =>
#if UNITY_ASSERTIONS || UNITY_EDITOR
            true;
#else
            false;
#endif
        
        // ----------------------------------------------------------------------------------------------------
        // Shaders
        // ----------------------------------------------------------------------------------------------------
        
        [SerializeField] [ResourcePath("Shaders/Debug/LightCullingDebug.shader")]
        private Shader m_LightCullingDebugShader;
        public Shader lightCullingDebugShader
        {
            get => m_LightCullingDebugShader;
            set => this.SetValueAndNotify(ref m_LightCullingDebugShader, value, nameof(lightCullingDebugShader));
        }
    }
}