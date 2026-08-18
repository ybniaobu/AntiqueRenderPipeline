#ifndef YPIPELINE_SPHERICAL_HARMONICS_LIBRARY_INCLUDED
#define YPIPELINE_SPHERICAL_HARMONICS_LIBRARY_INCLUDED

#define SHBasis0  0.28209479177387814347 // sqrt(1/4π)
#define SHBasis1  0.48860251190291992159 // sqrt(3/4π)
#define SHBasis2  1.09254843059207907054 // 0.5 * sqrt(15/π)
#define SHBasis3  0.31539156525252000603 // 0.25 * sqrt(5/π)
#define SHBasis4  0.54627421529603953527 // 0.25 * sqrt(15/π)

// Zonal Harmonics
#define ZHBasis0  0.28209479177387814347 // sqrt(1/4π) * 1 = sqrt(1/4π)
#define ZHBasis1  0.32573500793527994772 // sqrt(3/4π) * 2/3 = sqrt(1/3π)
#define ZHBasis2  0.27313710764801976764 // 0.5 * sqrt(15/π) * 1/4 = 1/8 * sqrt(15/π)
#define ZHBasis3  0.078847891313130001508 // 0.25 * sqrt(5/π) * 1/4 = 1/16 * sqrt(5/π)
#define ZHBasis4  0.13656855382400988382 // 0.25 * sqrt(15/π) * 1/4 = 1/16 * sqrt(15/π)

// ----------------------------------------------------------------------------------------------------
// SH Functions
// ----------------------------------------------------------------------------------------------------

void InitializeSHFunctions(float3 N, out float SHFunctions[9])
{
    SHFunctions[0] = SHBasis0;                              // l = 0, m = 0
    SHFunctions[1] = SHBasis1 * N.y;                        // l = 1, m = -1
    SHFunctions[2] = SHBasis1 * N.z;                        // l = 1, m = 0
    SHFunctions[3] = SHBasis1 * N.x;                        // l = 1, m = 1
    SHFunctions[4] = SHBasis2 * N.x * N.y;                  // l = 2, m = -2
    SHFunctions[5] = SHBasis2 * N.y * N.z;                  // l = 2, m = -1
    SHFunctions[6] = SHBasis3 * (3.0 * N.z * N.z - 1.0);    // l = 2, m = 0
    SHFunctions[7] = SHBasis2 * N.z * N.x;                  // l = 2, m = 1
    SHFunctions[8] = SHBasis4 * (N.x * N.x - N.y * N.y);    // l = 2, m = 2
}

// ----------------------------------------------------------------------------------------------------
// ZH
// ----------------------------------------------------------------------------------------------------

void InitializeZHCoefficients(float3 N, out float ZHCoefficients[9])
{
    ZHCoefficients[0] = ZHBasis0;                              // l = 0, m = 0
    ZHCoefficients[1] = ZHBasis1 * N.y;                        // l = 1, m = -1
    ZHCoefficients[2] = ZHBasis1 * N.z;                        // l = 1, m = 0
    ZHCoefficients[3] = ZHBasis1 * N.x;                        // l = 1, m = 1
    ZHCoefficients[4] = ZHBasis2 * N.x * N.y;                  // l = 2, m = -2
    ZHCoefficients[5] = ZHBasis2 * N.y * N.z;                  // l = 2, m = -1
    ZHCoefficients[6] = ZHBasis3 * (3.0 * N.z * N.z - 1.0);    // l = 2, m = 0
    ZHCoefficients[7] = ZHBasis2 * N.z * N.x;                  // l = 2, m = 1
    ZHCoefficients[8] = ZHBasis4 * (N.x * N.x - N.y * N.y);    // l = 2, m = 2
}

// ----------------------------------------------------------------------------------------------------
// Reconstruction
// ----------------------------------------------------------------------------------------------------

// 7 个 float4 存储 9 * 3 = 27 个球谐系数
float3 SampleSphericalHarmonics(float3 N, in float4 SH[7])
{
    float3 L0L1;
    float4 vA = float4(N, 1.0);
    L0L1.r = dot(SH[0], vA);
    L0L1.g = dot(SH[2], vA);
    L0L1.b = dot(SH[4], vA);

    float3 L2;
    float4 vB = N.xyzz * N.yzzx;
    L2.r = dot(SH[1], vB);
    L2.g = dot(SH[3], vB);
    L2.b = dot(SH[5], vB);
    
    float vC = N.x * N.x - N.y * N.y;
    L2 += SH[6].rgb * vC;

    return L0L1 + L2;
}

#endif