using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class ShadowUtils
    {
        /// <summary>
        /// Retrieve the size of the sun light cascaded shadow map.
        /// Assuming the cascaded shadow map is set to a resolution of 4096, each slice will have a resolution of 2048×2048.
        /// For cascade count of 4, 3, 2, and 1, the returned cascaded shadow map sizes are 4096×4096, 4096×4096, 4096×2048, and 2048×2048, respectively.
        /// </summary>
        /// <param name="cascadeAtlasSize">the configured sun light cascaded shadow map size</param>
        /// <param name="cascadeCount">the configured CSM cascade count</param>
        /// <returns></returns>
        public static Vector2Int GetCascadeAtlasSize(int cascadeAtlasSize, int cascadeCount)
        {
            int width = cascadeCount == 1 ? cascadeAtlasSize >> 1 : cascadeAtlasSize;
            int height = cascadeCount <= 2 ? cascadeAtlasSize >> 1 : cascadeAtlasSize;
            return new Vector2Int(width, height);
        }

        /// <summary>
        /// Extracts width and height from the packed punctual light atlas size.
        /// </summary>
        /// <param name="packedNum">32-bit value with width in high 16 bits, height in low 16 bits.</param>
        /// <returns></returns>
        public static Vector2Int GetPunctualLightAtlasSize(uint packedNum)
        {
            int width = (int) (packedNum >> 16);
            int height = (int) (packedNum & 0xFFFF);
            return new Vector2Int(width, height);
        }
        
        /// <summary>
        /// Get the matrix that transforms coordinates from world space to the light's screen space coordinates.
        /// For point lights and spot lights, remember to perform homogeneous division in the shader.
        /// </summary>
        /// <param name="vp">the light’s view-projection matrix</param>
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
        /// Get the matrix that transforms coordinates from world space to the light's screen-space slice coordinates.
        /// For point lights and spot lights, remember to perform homogeneous division in the shader.
        /// </summary>
        /// <param name="vp">the light’s view-projection matrix</param>
        /// <param name="offset">slice offset</param>
        /// <param name="scale">slice scale</param>
        /// <returns></returns>
        public static Matrix4x4 GetWorldToSlicedLightScreenMatrix(Matrix4x4 vp, Vector2 offset, Vector2 scale)
        {
            Matrix4x4 vps = GetWorldToLightScreenMatrix(vp);
            
            vps.m00 = scale.x * vps.m00 + offset.x * vps.m30;
            vps.m01 = scale.x * vps.m01 + offset.x * vps.m31;
            vps.m02 = scale.x * vps.m02 + offset.x * vps.m32;
            vps.m03 = scale.x * vps.m03 + offset.x * vps.m33;
            vps.m10 = scale.y * vps.m10 + offset.y * vps.m30;
            vps.m11 = scale.y * vps.m11 + offset.y * vps.m31;
            vps.m12 = scale.y * vps.m12 + offset.y * vps.m32;
            vps.m13 = scale.y * vps.m13 + offset.y * vps.m33;
            
            return vps;
        }

        // https://i.ibb.co/wpW5Mnf/Calc-Guard-Angle.png
        // public static float CalculateGuardAngle(float guardBandTexel, float resolution)
        // {
        //     float realHalfFOV = Mathf.Atan(1.0f + 2.0f * guardBandTexel / resolution);
        //     return realHalfFOV * Mathf.Rad2Deg - 45.0f;
        // }
    }
}