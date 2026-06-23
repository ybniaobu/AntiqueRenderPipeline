using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    /// <summary>
    /// Unity Camera 类的扩展方法
    /// </summary>
    public static class CameraExtensions
    {
        public static YPipelineCamera GetYPipelineCamera(this Camera camera)
        {
            GameObject cameraObject = camera.gameObject;
            bool componentExists = cameraObject.TryGetComponent<YPipelineCamera>(out YPipelineCamera pipelineCamera);
            if(!componentExists) pipelineCamera = cameraObject.AddComponent<YPipelineCamera>();
            return pipelineCamera;
        }
    }
    
    /// <summary>
    /// YPipeline 增加的摄像机额外设置
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways] // NOTE: This is required to get calls to OnDestroy() always. Graphics resources are released in OnDestroy().
    public sealed class YPipelineCamera : MonoBehaviour, IAdditionalData
    {
        public Camera Camera => GetComponent<Camera>();
        [NonSerialized] internal YPipelinePerCameraData perCameraData = new YPipelinePerCameraData();

#if UNITY_EDITOR
        public void OnDisable()
        {
            perCameraData?.Dispose();
            perCameraData = null;
        }
#endif

        public void OnDestroy()
        {
            perCameraData?.Dispose();
            perCameraData = null;
        }
    }
}