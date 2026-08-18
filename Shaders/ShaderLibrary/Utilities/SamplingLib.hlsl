#ifndef YPIPELINE_SAMPLING_LIBRARY_INCLUDED
#define YPIPELINE_SAMPLING_LIBRARY_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Coordinate conversion
// ----------------------------------------------------------------------------------------------------

// Convention: Cartesian coordinate and Spherical coordinate are both left-handed.
// the order of Spherical coordinate is r, theta, phi.
// phi: angle of rotation from x in xz plane, the range of phi is [-π, π];
// theta: angle of rotation with respect to y-axis, the range of theta is [0, π];
// phi can be derived from atan2(z/x), the range of atan2() is also [-π, π];
// theta can be derived from acos(y), the range of acos() is also [0, π];
float3 SphericalCoordToCartesianCoord(float theta, float phi)
{
    return float3(sin(theta) * cos(phi), cos(theta), sin(theta) * sin(phi));
}

float3 SphericalCoordToCartesianCoord(float2 sphericalCoord)
{
    return SphericalCoordToCartesianCoord(sphericalCoord.x, sphericalCoord.y);
}

float2 CartesianCoordToSphericalCoord(float x, float y, float z)
{
    return float2(acos(y), atan2(z, x));
}

float2 CartesianCoordToSphericalCoord(float3 cartesianCoord)
{
    return CartesianCoordToSphericalCoord(cartesianCoord.x, cartesianCoord.y, cartesianCoord.z);
}

//Transform tangent coordinate to N's given space.
float3 TangentCoordToNormalizedWorldCoord(float3 tangentCoord, float3 N)
{
    float3 up = abs(N.y) > 0.9999 ? float3(0, 0, 1) : float3(0, 1, 0);
    float3 tangent = normalize(cross(up, N));
    float3 binormal = normalize(cross(tangent, N));
    return normalize(tangent * tangentCoord.x + normalize(N) * tangentCoord.y + binormal * tangentCoord.z);
}

//Transform tangent coordinate to N's given space.
float3 TangentCoordToWorldCoord(float3 tangentCoord, float3 N)
{
    float3 up = abs(N.y) > 0.9999 ? float3(0, 0, 1) : float3(0, 1, 0);
    float3 tangent = normalize(cross(up, N));
    float3 binormal = normalize(cross(tangent, N));
    return tangent * tangentCoord.x + normalize(N) * tangentCoord.y + binormal * tangentCoord.z;
}

// ----------------------------------------------------------------------------------------------------
// Environment Mapping
// ----------------------------------------------------------------------------------------------------

float3 LatLongUVToCartesian(float2 uv)
{
    float phi = (uv.x - 0.5f) * TWO_PI;
    float theta = uv.y * PI;
    return SphericalCoordToCartesianCoord(theta, phi);
}

float2 CartesianToLatLongUV(float3 cartesianCoord)
{
    cartesianCoord = normalize(cartesianCoord);
    float2 sphericalCoord = CartesianCoordToSphericalCoord(cartesianCoord);
    return float2(sphericalCoord.y * INV_TWO_PI + 0.5, sphericalCoord.x * INV_PI);
}

// ----------------------------------------------------------------------------------------------------
// Inverse transform sampling, From [0, 1] to [-1, 1]
// ----------------------------------------------------------------------------------------------------

float2 InverseSampleCircle(float2 xi)
{
    float r = sqrt(xi.x);
    float theta = TWO_PI * xi.y;
    return r * float2(cos(theta), sin(theta));
}

float4 InverseSampleSphere(float2 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - 2 * xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 N = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    float PDF = INV_FOUR_PI;
    return float4(N, PDF);
}

float4 InverseSampleSphere(float3 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - 2 * xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    float r = pow(xi.z, 1.0 / 3.0);

    float3 N = float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
    float PDF = 3.0 * rcp(FOUR_PI * r * r * r);
    return float4(N, PDF);
}

float4 InverseSampleHemisphere(float2 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 N = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    float PDF = INV_TWO_PI;
    return float4(N, PDF);
}

float4 InverseSampleHemisphere(float3 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = 1 - xi.y;
    float sinTheta = sqrt(1 - cosTheta * cosTheta);
    float r = pow(xi.z, 1.0 / 3.0);

    float3 N = float3(r * sinTheta * cos(phi), r * cosTheta, r * sinTheta * sin(phi));
    float PDF = 3.0 * rcp(TWO_PI * r * r * r);
    return float4(N, PDF);
}

inline float3 FibonacciSpiralHemisphere(float index, float sampleCount)
{
    const float goldenRatio = 0.61803398875;
    float phi = TWO_PI * index * goldenRatio;
    float cosTheta = 1.0 - (2.0 * index + 1.0) / (2.0 * sampleCount);
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    float3 N = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    return N;
}

// ----------------------------------------------------------------------------------------------------
// Cosine-weighted inverse transform sampling
// ----------------------------------------------------------------------------------------------------

float4 CosineSampleHemisphere(float2 xi) // Left-handed Spherical and Cartesian Coordinate
{
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt(1 - xi.y);
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 N = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    float PDF = cosTheta * INV_PI;
    return float4(N, PDF);
}

// ----------------------------------------------------------------------------------------------------
// Importance sampling
// ----------------------------------------------------------------------------------------------------

float4 ImportanceSampleGGX(float2 xi, float roughness) // Left-handed Spherical and Cartesian Coordinate
{
    float a = roughness * roughness;
    float a2 = a * a;
    
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt((1 - xi.y) / (1 + (a2 - 1) * xi.y));
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 H = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));

    float d = (cosTheta * a2 - cosTheta) * cosTheta + 1;
    float D = a2 / (PI * d * d);
    float PDF = D * cosTheta;
    
    return float4(H, PDF);
}

float3 ImportanceSampleGGX(float2 xi, float roughness, float3 N) // Left-handed Spherical and Cartesian Coordinate
{
    float a = roughness * roughness;
    float a2 = a * a;
    
    float phi = PI * (2.0 * xi.x - 1.0);
    float cosTheta = sqrt((1 - xi.y) / (1 + (a2 - 1) * xi.y));
    float sinTheta = sqrt(1 - cosTheta * cosTheta);

    float3 H = float3(sinTheta * cos(phi), cosTheta, sinTheta * sin(phi));
    
    return TangentCoordToNormalizedWorldCoord(H, N);
}

#endif