using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    public class CullingPass : PipelinePass
    {
        protected override void Initialize(ref YPipelineData data) { }
        
        protected override void OnDispose() { }

        protected override void OnRecord(ref YPipelineData data)
        {
            // Setup Culling Parameters
            data.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
            cullingParameters.shadowDistance = Mathf.Min(data.asset.maxShadowDistance, data.camera.farClipPlane);
            cullingParameters.maximumVisibleLights = YPipelineLightsData.k_MaxPunctualLightCount + 1;
            cullingParameters.reflectionProbeSortingCriteria = ReflectionProbeSortingCriteria.ImportanceThenSize;
            cullingParameters.conservativeEnclosingSphere = true;
            cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling; // 取消 per-object culling for Lights and Reflection Probes
            
            // Emit scene view UI
#if UNITY_EDITOR
            if (data.camera.cameraType == CameraType.Reflection || data.camera.cameraType == CameraType.Preview)
                ScriptableRenderContext.EmitGeometryForCamera(data.camera);

            if (data.camera.cameraType == CameraType.SceneView) 
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(data.camera);
            }
#endif
            // APV
            SetupAPV(ref data);
            
            // Cull
            data.cullingResults = data.context.Cull(ref cullingParameters);
            
            data.context.ExecuteCommandBuffer(data.cmd);
            // data.context.Submit(); // LightCollectPass 中提交
            data.cmd.Clear();
        }
        
        private void SetupAPV(ref YPipelineData data)
        {
            if (ProbeReferenceVolume.instance.isInitialized)
            {
                var stack = VolumeManager.instance.stack;
                ProbeVolumesOptions apvOptions = stack.GetComponent<ProbeVolumesOptions>();
                
                ProbeReferenceVolume.instance.PerformPendingOperations();
                if (data.camera.cameraType != CameraType.Reflection && data.camera.cameraType != CameraType.Preview)
                {
                    ProbeReferenceVolume.instance.UpdateCellStreaming(data.cmd, data.camera, apvOptions);
                }
                
                ProbeReferenceVolume.instance.BindAPVRuntimeResources(data.cmd, true);
                
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Must be called before culling because it emits intermediate renderers via Graphics.DrawInstanced.
                ProbeReferenceVolume.instance.RenderDebug(data.camera, apvOptions, Texture2D.whiteTexture);
#endif
                
                bool isProbeVolumeL1Enabled = data.asset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1;
                bool isProbeVolumeL2Enabled = data.asset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2;
                bool isTAAEnabled = data.asset.antiAliasingMode == AntiAliasingMode.TAA;
                
                bool enableProbeVolumes = ProbeReferenceVolume.instance.UpdateShaderVariablesProbeVolumes(data.cmd, apvOptions, isTAAEnabled ? Time.frameCount : 0, false);
                CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ProbeVolumeL1, isProbeVolumeL1Enabled && enableProbeVolumes);
                CoreUtils.SetKeyword(data.cmd, YPipelineKeywords.k_ProbeVolumeL2, isProbeVolumeL2Enabled && enableProbeVolumes);
            }
        }
    }
}