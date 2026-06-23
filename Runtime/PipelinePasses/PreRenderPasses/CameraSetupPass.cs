using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace YPipeline
{
    internal sealed class CameraSetupPass : PipelinePass
    {
        private class CameraSetupPassData
        {
            public Camera camera;
            public YPipelineCamera yCamera;

            public Vector4 cameraSettings;
            public Vector4 bufferSize;
            public Vector4 jitter;
            public Vector4 timeParams;

            public void SetNonBuiltInCameraMatrixShaderVariables(RasterCommandBuffer cmd)
            {
                bool isProjectionMatrixFlipped = SystemInfo.graphicsUVStartsAtTop;

                Matrix4x4 viewMatrix = yCamera.perCameraData.viewMatrix;
                Matrix4x4 inverseViewMatrix = viewMatrix.inverse;
                Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.jitteredProjectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 inverseProjectionMatrix = gpuProjectionMatrix.inverse;
                Matrix4x4 gpuNonJitterProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.projectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 nonJitterInverseProjectionMatrix = gpuNonJitterProjectionMatrix.inverse;
                
                Matrix4x4 inverseViewProjectionMatrix = inverseViewMatrix * inverseProjectionMatrix;
                Matrix4x4 nonJitterViewProjectionMatrix = gpuNonJitterProjectionMatrix * viewMatrix;
                Matrix4x4 nonJitterInverseViewProjectionMatrix = inverseViewMatrix * nonJitterInverseProjectionMatrix;
                
                Matrix4x4 previousViewMatrix = yCamera.perCameraData.previousViewMatrix;
                Matrix4x4 previousInverseViewMatrix = previousViewMatrix.inverse;
                Matrix4x4 previousGPUProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.previousJitteredProjectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 previousInverseProjectionMatrix = previousGPUProjectionMatrix.inverse;
                Matrix4x4 previousGPUNonJitterProjectionMatrix = GL.GetGPUProjectionMatrix(yCamera.perCameraData.previousProjectionMatrix, isProjectionMatrixFlipped);
                Matrix4x4 previousNonJitterInverseProjectionMatrix = previousGPUNonJitterProjectionMatrix.inverse;
                
                Matrix4x4 previousViewProjectionMatrix = previousGPUProjectionMatrix * previousViewMatrix;
                Matrix4x4 previousInverseViewProjectionMatrix = previousInverseViewMatrix * previousInverseProjectionMatrix;
                Matrix4x4 previousNonJitterViewProjectionMatrix = previousGPUNonJitterProjectionMatrix * previousViewMatrix;
                Matrix4x4 previousNonJitterInverseViewProjectionMatrix = previousInverseViewMatrix * previousNonJitterInverseProjectionMatrix;
                
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_InverseProjectionMatrixID, inverseProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_InverseViewProjectionMatrixID, inverseViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredViewProjectionMatrixID, nonJitterViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredInverseViewProjectionMatrixID, nonJitterInverseViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_PreviousViewProjectionMatrixID, previousViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_PreviousInverseViewProjectionMatrixID, previousInverseViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredPreviousViewProjectionMatrixID, previousNonJitterViewProjectionMatrix);
                cmd.SetGlobalMatrix(YPipelineShaderIDs.k_NonJitteredPreviousInverseViewProjectionMatrixID, previousNonJitterInverseViewProjectionMatrix);
            }
        }

        private TAA m_TAA;

        protected override void Initialize(ref YPipelineData data)
        {
            var stack = VolumeManager.instance.stack;
            m_TAA = stack.GetComponent<TAA>();
        }

        protected override void OnDispose()
        {
            m_TAA = null;
        }

        protected override void OnRecord(ref YPipelineData data)
        {
            using (var builder = data.renderGraph.AddRasterRenderPass<CameraSetupPassData>("Set Camera Properties", out var passData))
            {
                passData.camera = data.camera;
                YPipelineCamera yCamera = data.camera.GetYPipelineCamera();
                passData.yCamera = yCamera;
                
                // ----------------------------------------------------------------------------------------------------
                // Camera / Time Params
                // ----------------------------------------------------------------------------------------------------
                
                Vector2Int bufferSize = data.BufferSize;
                passData.bufferSize = new Vector4(1.0f / bufferSize.x, 1.0f / bufferSize.y, bufferSize.x, bufferSize.y);
                float fov = Mathf.Deg2Rad * data.camera.fieldOfView;
                float cotFov = 1.0f / Mathf.Tan(fov * 0.5f);
                passData.cameraSettings = new Vector4(fov, cotFov);
                
                int frameIndex = Time.frameCount;
                Vector2 jitter64 = RandomUtils.k_Halton[frameIndex % 64 + 1] - new Vector2(0.5f, 0.5f);
                passData.jitter = new Vector4(1.0f / jitter64.x, 1.0f / jitter64.y, jitter64.x, jitter64.y);
                passData.timeParams = new Vector4(frameIndex, 1.0f / frameIndex);

                // ----------------------------------------------------------------------------------------------------
                // TAA jitter
                // ----------------------------------------------------------------------------------------------------
                
                bool isOrthographic = data.camera.orthographic;
                Matrix4x4 viewMatrix = data.camera.worldToCameraMatrix;
                Matrix4x4 projectionMatrix = data.camera.projectionMatrix;
                Matrix4x4 jitteredProjectionMatrix;

                if (data.IsTAAEnabled)
                {
                    Vector2 jitter = RandomUtils.k_Halton[frameIndex % 16 + 1] - new Vector2(0.5f, 0.5f);
                    jitter *= 2.0f * m_TAA.jitterScale.value;
                    jitteredProjectionMatrix = CameraUtils.GetJitteredProjectionMatrix(data.BufferSize, projectionMatrix, jitter, isOrthographic);
                }
                else
                {
                    jitteredProjectionMatrix = projectionMatrix;
                }
                
                yCamera.perCameraData.SetPerCameraDataMatrices(viewMatrix, projectionMatrix, jitteredProjectionMatrix);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (CameraSetupPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetupCameraProperties(data.camera);
                    context.cmd.SetViewProjectionMatrices(data.yCamera.perCameraData.viewMatrix, data.yCamera.perCameraData.jitteredProjectionMatrix);
                    data.SetNonBuiltInCameraMatrixShaderVariables(context.cmd);
                    
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_CameraSettingsID, data.cameraSettings);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_BufferSizeID, data.bufferSize);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_JitterID, data.jitter);
                    context.cmd.SetGlobalVector(YPipelineShaderIDs.k_TimeParams,data.timeParams);
                });
            }
        }
    }
}