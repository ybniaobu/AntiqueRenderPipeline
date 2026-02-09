using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class CameraUtility
    {
        // ----------------------------------------------------------------------------------------------------
        // TAA
        // ----------------------------------------------------------------------------------------------------
        
        /// <param name="jitter">(-1, 1)</param>
        public static Matrix4x4 GetJitteredProjectionMatrix(Vector2Int bufferSize, Matrix4x4 projectionMatrix, Vector2 jitter, bool isOrthographic)
        {
            if (isOrthographic)
            {
                projectionMatrix[0, 3] += jitter.x / bufferSize.x;
                projectionMatrix[1, 3] += jitter.y / bufferSize.y;
            }
            else
            {
                projectionMatrix[0, 2] += jitter.x / bufferSize.x;
                projectionMatrix[1, 2] += jitter.y / bufferSize.y;
            }
            return projectionMatrix;
        }
        
        
    }
}