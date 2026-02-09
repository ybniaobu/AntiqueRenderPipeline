using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class ShadowUtility
    {
        /// <summary>
        /// 获取从世界空间坐标到光源屏幕空间坐标的矩阵，适用于使用 Shadow Array 的灯光，点光源和聚光灯别忘了在 Shader 中做齐次除法
        /// </summary>
        /// <param name="vp">光源的观察投影矩阵</param>
        /// <returns></returns>
        public static Matrix4x4 GetWorldToLightScreenMatrix(Matrix4x4 vp)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                vp.m20 = -vp.m20;
                vp.m21 = -vp.m21;
                vp.m22 = -vp.m22;
                vp.m23 = -vp.m23;
            }
            
            vp.m00 = 0.5f * (vp.m00 + vp.m30);
            vp.m01 = 0.5f * (vp.m01 + vp.m31);
            vp.m02 = 0.5f * (vp.m02 + vp.m32);
            vp.m03 = 0.5f * (vp.m03 + vp.m33);
            vp.m10 = 0.5f * (vp.m10 + vp.m30);
            vp.m11 = 0.5f * (vp.m11 + vp.m31);
            vp.m12 = 0.5f * (vp.m12 + vp.m32);
            vp.m13 = 0.5f * (vp.m13 + vp.m33);
            vp.m20 = 0.5f * (vp.m20 + vp.m30);
            vp.m21 = 0.5f * (vp.m21 + vp.m31);
            vp.m22 = 0.5f * (vp.m22 + vp.m32);
            vp.m23 = 0.5f * (vp.m23 + vp.m33);
            
            return vp;
        }
        
        /// <summary>
        /// 获取从世界空间坐标到光源屏幕空间切片坐标的矩阵，适用于使用 Shadow Atlas 的灯光，点光源和聚光灯别忘了在 Shader 中做齐次除法
        /// </summary>
        /// <param name="vp">光源的观察投影矩阵</param>
        /// <param name="offset">切片偏移</param>
        /// <param name="scale">切片比例</param>
        /// <returns></returns>
        public static Matrix4x4 GetWorldToTiledLightScreenMatrix(Matrix4x4 vp, Vector2 offset, float scale = 1.0f)
        {
            Matrix4x4 vps = GetWorldToLightScreenMatrix(vp);
            
            vps.m00 = scale * vps.m00 + offset.x * vps.m30;
            vps.m01 = scale * vps.m01 + offset.x * vps.m31;
            vps.m02 = scale * vps.m02 + offset.x * vps.m32;
            vps.m03 = scale * vps.m03 + offset.x * vps.m33;
            vps.m10 = scale * vps.m10 + offset.y * vps.m30;
            vps.m11 = scale * vps.m11 + offset.y * vps.m31;
            vps.m12 = scale * vps.m12 + offset.y * vps.m32;
            vps.m13 = scale * vps.m13 + offset.y * vps.m33;
            
            return vps;
        }

        // public static Matrix4x4 GetShadowJitteredProjectionMatrix(float shadowMapSize, Matrix4x4 projectionMatrix, Vector2 jitter, bool isOrthographic)
        // {
        //     if (isOrthographic)
        //     {
        //         projectionMatrix[0, 3] += jitter.x / shadowMapSize;
        //         projectionMatrix[1, 3] += jitter.y / shadowMapSize;
        //     }
        //     else
        //     {
        //         projectionMatrix[0, 2] += jitter.x / shadowMapSize;
        //         projectionMatrix[1, 2] += jitter.y / shadowMapSize;
        //     }
        //     return projectionMatrix;
        // }
    }
}