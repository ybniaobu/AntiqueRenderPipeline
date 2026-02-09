using UnityEngine;
using UnityEngine.Rendering;

namespace YPipeline
{
    public static class SHUtils
    {
        /// sqrt(1/4π), sqrt(3/4π), 0.5 * sqrt(15/π), 0.25 * sqrt(5/π), 0.25 * sqrt(15/π)
        public static readonly float[] k_SHBasis = new float[5] { 0.28209479177387814347f, 0.48860251190291992159f, 1.09254843059207907054f, 0.31539156525252000603f, 0.54627421529603953527f };
        /// sqrt(1/4π) * 1 = sqrt(1/4π), sqrt(3/4π) * 2/3 = sqrt(1/3π), 0.5 * sqrt(15/π) * 1/4 = 1/8 * sqrt(15/π), 0.25 * sqrt(5/π) * 1/4 = 1/16 * sqrt(5/π), 0.25 * sqrt(15/π) * 1/4 = 1/16 * sqrt(15/π)
        public static readonly float[] k_ZHCoefficients = new float[5] { 0.28209479177387814347f, 0.32573500793527994772f, 0.27313710764801976764f, 0.078847891313130001508f, 0.13656855382400988382f };
        
        /// <summary>
        /// 将 9 个球谐系数打包进 7 个 Vector4 当中
        /// </summary>
        /// <param name="coefficients">球谐系数</param>
        /// <param name="vectors">7 个 Vector4 </param>
        /// <param name="intensity">额外乘上的强度</param>
        public static void PackSHCoefficientsTo7Vectors(SphericalHarmonicsL2 coefficients, ref Vector4[] vectors, float intensity = 1)
        {
            vectors[0] = intensity * new Vector4(coefficients[0, 3], coefficients[0, 1], coefficients[0, 2], coefficients[0, 0] - coefficients[0, 6]);
            vectors[1] = intensity * new Vector4(coefficients[0, 4], coefficients[0, 5], coefficients[0, 6] * 3, coefficients[0, 7]);
            vectors[2] = intensity * new Vector4(coefficients[1, 3], coefficients[1, 1], coefficients[1, 2], coefficients[1, 0] - coefficients[1, 6]);
            vectors[3] = intensity * new Vector4(coefficients[1, 4], coefficients[1, 5], coefficients[1, 6] * 3, coefficients[1, 7]);
            vectors[4] = intensity * new Vector4(coefficients[2, 3], coefficients[2, 1], coefficients[2, 2], coefficients[2, 0] - coefficients[2, 6]);
            vectors[5] = intensity * new Vector4(coefficients[2, 4], coefficients[2, 5], coefficients[2, 6] * 3, coefficients[2, 7]);
            vectors[6] = intensity * new Vector4(coefficients[0, 8], coefficients[1, 8], coefficients[2, 8]);
        }
        
        /// <summary>
        /// 将 9 个球谐系数打包进 7 个 Vector4 当中
        /// </summary>
        /// <param name="coefficients">球谐系数</param>
        /// <param name="vectors">7 个 Vector4 </param>
        /// <param name="intensity">额外乘上的强度</param>
        public static void PackSHCoefficientsTo7Vectors(Vector4[] coefficients, ref Vector4[] vectors, float intensity = 1)
        {
            vectors[0] = intensity * new Vector4(coefficients[3].x, coefficients[1].x, coefficients[2].x, coefficients[0].x - coefficients[6].x);
            vectors[1] = intensity * new Vector4(coefficients[4].x, coefficients[5].x, coefficients[6].x * 3.0f, coefficients[7].x);
            vectors[2] = intensity * new Vector4(coefficients[3].y, coefficients[1].y, coefficients[2].y, coefficients[0].y - coefficients[6].y);
            vectors[3] = intensity * new Vector4(coefficients[4].y, coefficients[5].y, coefficients[6].y * 3.0f, coefficients[7].y);
            vectors[4] = intensity * new Vector4(coefficients[3].z, coefficients[1].z, coefficients[2].z, coefficients[0].z - coefficients[6].z);
            vectors[5] = intensity * new Vector4(coefficients[4].z, coefficients[5].z, coefficients[6].z * 3.0f, coefficients[7].z);
            vectors[6] = intensity * new Vector4(coefficients[8].x, coefficients[8].y, coefficients[8].z);
        }
    }
}