using System;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YPipeline
{
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    [DisplayName("YPipeline")]
    public class YPipelineGlobalSettings : RenderPipelineGlobalSettings<YPipelineGlobalSettings, YRenderPipeline>
    {
        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
        
        // internal static int k_LastVersion = 8;
        // [SerializeField] internal int m_AssetVersion = k_LastVersion;
        
        private const string k_GlobalSettingsName = "YPipelineGlobalSettings";
        
#if UNITY_EDITOR
        private static string k_DefaultPath => $"Assets/Settings/{k_GlobalSettingsName}.asset";

        public static YPipelineGlobalSettings Ensure(bool canCreateNewAsset = true)
        {
            var currentInstance = GraphicsSettings.GetSettingsForRenderPipeline<YRenderPipeline>() as YPipelineGlobalSettings;
            
            if (RenderPipelineGlobalSettingsUtils.TryEnsure<YPipelineGlobalSettings, YRenderPipeline>(ref currentInstance, k_DefaultPath, canCreateNewAsset))
            {
                if (currentInstance != null)
                {
                    // AssetDatabase.SaveAssetIfDirty(currentInstance);
                }
                return currentInstance;
            }
            return null;
        }
#endif
    }
}