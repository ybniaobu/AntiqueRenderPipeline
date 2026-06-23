using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    internal static class APVUtils
    {
        public static void InitializeAPV(ref YPipelineData data)
        {
            data.isAPVEnabled = data.asset.supportProbeVolume;
            SupportedRenderingFeatures.active.overridesLightProbeSystem = data.isAPVEnabled;
            SupportedRenderingFeatures.active.skyOcclusion = data.isAPVEnabled;
            if (data.isAPVEnabled)
            {
                ProbeVolumeSystemParameters apvParams = new ProbeVolumeSystemParameters()
                {
                    memoryBudget = data.asset.probeVolumeMemoryBudget,
                    blendingMemoryBudget = data.asset.probeVolumeBlendingMemoryBudget,
                    shBands = data.asset.probeVolumeSHBands,
                    supportGPUStreaming = data.asset.supportProbeVolumeGPUStreaming,
                    supportDiskStreaming = data.asset.supportProbeVolumeDiskStreaming,
                    supportScenarios = data.asset.supportProbeVolumeScenarios,
                    supportScenarioBlending = data.asset.supportProbeVolumeScenarioBlending,
                };
                ProbeReferenceVolume.instance.Initialize(apvParams);
                ProbeReferenceVolume.instance.SetEnableStateFromSRP(true);
                ProbeReferenceVolume.instance.SetVertexSamplingEnabled(false);
            }
        }
        
        /// <summary>
        /// Configures the Unity Adaptive Probe Volume system
        /// </summary>
        /// <remarks>
        /// Do not invoke this method inside PreviewCameraRenderer
        /// </remarks>
        public static void SetupAdaptiveProbeVolume(ref YPipelineData data)
        {
            if (ProbeReferenceVolume.instance.isInitialized)
            {
                var stack = VolumeManager.instance.stack;
                ProbeVolumesOptions apvOptions = stack.GetComponent<ProbeVolumesOptions>();
                
                ProbeReferenceVolume.instance.PerformPendingOperations();
                
#if UNITY_EDITOR
                if (data.camera.cameraType != CameraType.Reflection && data.camera.cameraType != CameraType.Preview)
#endif
                    ProbeReferenceVolume.instance.UpdateCellStreaming(data.cmd, data.camera, apvOptions);
                
                ProbeReferenceVolume.instance.BindAPVRuntimeResources(data.cmd, true);
                
#if UNITY_ASSERTIONS
                // Must be called before culling because it emits intermediate renderers via Graphics.DrawInstanced.
                ProbeReferenceVolume.instance.RenderDebug(data.camera, apvOptions, Texture2D.whiteTexture);
#endif

                bool isProbeVolumesLoaded = ProbeReferenceVolume.instance.DataHasBeenLoaded();
                bool isProbeVolumeL1Enabled = data.asset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1;
                bool isProbeVolumeL2Enabled = data.asset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2;
                // bool isProbeVolumesLoaded = ProbeReferenceVolume.instance.UpdateShaderVariablesProbeVolumes(data.cmd, apvOptions, data.IsTAAEnabled ? Time.frameCount : 0, false);
                CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ProbeVolumeL1, isProbeVolumeL1Enabled && isProbeVolumesLoaded);
                CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ProbeVolumeL2, isProbeVolumeL2Enabled && isProbeVolumesLoaded);
                data.isAPVLoaded = isProbeVolumesLoaded;
            }
            else
            {
                data.isAPVLoaded = false;
            }
        }
    }
}