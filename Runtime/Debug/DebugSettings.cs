#if UNITY_ASSERTIONS
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public sealed class DebugSettings
    {
        private YPipelineDebugResources m_DebugResources;
        public LightingDebugSettings lightingDebugSettings;
        
        public DebugSettings()
        {
            m_DebugResources = GraphicsSettings.GetRenderPipelineSettings<YPipelineDebugResources>();
            
            lightingDebugSettings = new LightingDebugSettings();
            lightingDebugSettings.Initialize(m_DebugResources);
        }
        
        public void Dispose()
        {
            lightingDebugSettings.OnDispose();
        }
    }
}
#endif