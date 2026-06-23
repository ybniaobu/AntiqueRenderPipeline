using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(YRenderPipelineAsset))]
    [UnityEngine.Categorization.CategoryInfo(Name = "Default Volume Profile", Order = 0)]
    public sealed class YPipelineDefaultVolumeProfileResource : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 1;
        public int version => m_Version;
        
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;
        
        [SerializeField] [ResourcePath("PipelineResources/YDefaultVolumeProfile.asset")]
        private VolumeProfile m_VolumeProfile;
        public VolumeProfile volumeProfile
        {
            get => m_VolumeProfile;
            set => this.SetValueAndNotify(ref m_VolumeProfile, value);
        }
    }
}