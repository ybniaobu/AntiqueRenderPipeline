#ifndef YPIPELINE_INTERSECTION_TEST_LIBRARY_INCLUDED
#define YPIPELINE_INTERSECTION_TEST_LIBRARY_INCLUDED

struct AABB 
{
    float3 center;
    float3 extent; // half-sizes along x, y, z
};

struct AABBMinMax
{
    float3 min;
    float3 max;
};

struct OBB 
{
    float3 center;
    float3 axes[3];
    float3 extent; // half-sizes along local axes
};

inline AABBMinMax BuildAABBMinMax(float3 center, float3 extent)
{
    AABBMinMax aabb;
    aabb.min = center - extent;
    aabb.max = center + extent;
    return aabb;
}

inline bool IsValidAABB_MinMax(AABBMinMax aabb)
{
    return all(aabb.min <= aabb.max);
}

inline bool IsValidAABB(AABB aabb)
{
    float3 aabbMin = aabb.center - aabb.extent;
    float3 aabbMax = aabb.center + aabb.extent;
    return all(aabbMin <= aabbMax);
}

// ----------------------------------------------------------------------------------------------------
// Build AABB / OBB
// ----------------------------------------------------------------------------------------------------

void BuildViewAABBFromWorldAABB(AABB worldAABB, out AABB viewAABB)
{
    float3 centerVS = TransformWorldToView(worldAABB.center);
    centerVS.z = -centerVS.z;
    float3 extentVS = abs(TransformWorldToViewDir(worldAABB.extent));
    viewAABB.center = centerVS;
    viewAABB.extent = extentVS;
}

void BuildViewAABBFromWorldAABB(AABB worldAABB, out AABBMinMax viewAABB)
{
    float3 centerVS = TransformWorldToView(worldAABB.center);
    centerVS.z = -centerVS.z;
    float3 extentVS = abs(TransformWorldToViewDir(worldAABB.extent));
    viewAABB.min = centerVS - extentVS;
    viewAABB.max = centerVS + extentVS;
}

void BuildViewOBBFromWorldAABB(AABB worldAABB, out OBB viewOBB) 
{
    float3 centerVS = TransformWorldToView(worldAABB.center);
    centerVS.z = -centerVS.z;
    viewOBB.center = centerVS;
    viewOBB.axes[0] = TransformWorldToViewDir(float3(1, 0, 0));
    viewOBB.axes[0].z = -viewOBB.axes[0].z;
    viewOBB.axes[1] = TransformWorldToViewDir(float3(0, 1, 0));
    viewOBB.axes[1].z = -viewOBB.axes[1].z;
    viewOBB.axes[2] = TransformWorldToViewDir(float3(0, 0, 1));
    viewOBB.axes[2].z = -viewOBB.axes[2].z;
    viewOBB.extent = worldAABB.extent; // half-sizes unchanged (rotation preserves length)
}

// ----------------------------------------------------------------------------------------------------
// AABB Intersection Test
// ----------------------------------------------------------------------------------------------------

bool AABB_AABB_Intersect(AABBMinMax aabb1, AABBMinMax aabb2)
{
    return all(aabb1.min <= aabb2.max) && all(aabb1.max >= aabb2.min);
}

bool AABB_Sphere_Intersect(AABBMinMax aabb, float3 sphereCenter, float radius)
{
    float3 closestPoint = clamp(sphereCenter, aabb.min, aabb.max);
    float3 diff = closestPoint - sphereCenter;
    float sqrDiff = dot(diff, diff);
    float sqrRadius = radius * radius;
    return sqrDiff <= sqrRadius;
}

bool AABB_Point_Intersect(AABBMinMax aabb, float3 p)
{
    return all(p >= aabb.min) && all(p <= aabb.max);
}

// ----------------------------------------------------------------------------------------------------
// OBB Intersection Test
// ----------------------------------------------------------------------------------------------------

float2 ProjectAABB(AABB aabb, float3 axis)
{
    float radius = dot(abs(axis), aabb.extent);
    float center = dot(axis, aabb.center);
    return float2(center - radius, center + radius);
}

float2 ProjectOBB(OBB obb, float3 axis)
{
    float radius = abs(dot(axis, obb.axes[0])) * obb.extent.x +
                   abs(dot(axis, obb.axes[1])) * obb.extent.y +
                   abs(dot(axis, obb.axes[2])) * obb.extent.z;
    float center = dot(axis, obb.center);
    return float2(center - radius, center + radius);
}

bool AABB_OBB_Intersect_3Axis(AABB aabb, OBB obb) 
{
    for (int i = 0; i < 3; i++) 
    {
        float3 axis = obb.axes[i];
        float2 projA = ProjectAABB(aabb, axis);
        float2 projB = ProjectOBB(obb, axis);
        if (projA.x > projB.y || projB.x > projA.y) return false;
    }
    return true;
}

#endif