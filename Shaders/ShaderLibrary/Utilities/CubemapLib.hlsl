#ifndef YPIPELINE_CUBEMAP_UTILS_LIB_INCLUDED
#define YPIPELINE_CUBEMAP_UTILS_LIB_INCLUDED

static const float3 k_CubeMapFaceDir[6] =
{
    float3(1.0, 0.0, 0.0),
    float3(-1.0, 0.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 0.0, 1.0),
    float3(0.0, 0.0, -1.0)
};

int GetCubeMapFaceID(float3 dir)
{
    int faceID;
    float3 a = abs(dir);

    if (a.z >= a.x && a.z >= a.y)
    {
        faceID = (dir.z < 0.0) ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z;
    }
    else if (a.y >= a.x)
    {
        faceID = (dir.y < 0.0) ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y;
    }
    else
    {
        faceID = (dir.x < 0.0) ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X;
    }

    return faceID;
}

int GetCubeMapFaceIDFast(float3 dir)
{
    float3 a = abs(dir);
    int isZ = a.z >= a.x && a.z >= a.y;
    int isY = !isZ && a.y >= a.x;
    
    return isZ ? (dir.z < 0 ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z) : 
           isY ? (dir.y < 0 ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y) : 
                 (dir.x < 0 ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X);
}

float2 CubemapDirToFaceUVFast(float3 dir, out int faceID)
{
    float3 a = abs(dir);
    int isZ = a.z >= a.x && a.z >= a.y;
    int isY = !isZ && a.y >= a.x;
    
    faceID = isZ ? (dir.z < 0 ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z) : 
             isY ? (dir.y < 0 ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y) : 
                   (dir.x < 0 ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X);
    
    float maxComp = max(max(a.x, a.y), a.z);
    float3 nd = dir * rcp(maxComp);
    
    static const float3 U[6] = {
        float3( 0.0, 0.0,-1.0), float3( 0.0, 0.0, 1.0),  // +X, -X
        float3( 1.0, 0.0, 0.0), float3( 1.0, 0.0, 0.0),  // +Y, -Y
        float3( 1.0, 0.0, 0.0), float3(-1.0, 0.0, 0.0)   // +Z, -Z
    };
    
    static const float3 V[6] = {
        float3( 0.0, 1.0, 0.0), float3( 0.0, 1.0, 0.0),  // +X, -X
        float3( 0.0, 0.0,-1.0), float3( 0.0, 0.0, 1.0),  // +Y, -Y
        float3( 0.0, 1.0, 0.0), float3( 0.0, 1.0, 0.0)   // +Z, -Z
    };
    
    float2 uv = float2(dot(U[faceID], nd), dot(V[faceID], nd));
    return uv * 0.5 + 0.5;
}

float3 CubemapFaceUVToDir(int faceID, float2 uv)
{
    float3 dir = 0;
    switch (faceID)
    {
        case 0: //+X
            dir.x = 1.0;
            dir.y = uv.y * 2.0 - 1.0;
            dir.z = uv.x * -2.0 + 1.0;
            break;
            
        case 1: //-X
            dir.x = -1.0;
            dir.yz = uv.yx * 2.0 - 1.0;
            break;
            
        case 2: //+Y
            dir.x = uv.x * 2.0 - 1.0;
            dir.z = uv.y * -2.0 + 1.0;
            dir.y = 1.0;
            break;
                
        case 3: //-Y
            dir.xz = uv.xy * 2.0 - 1.0;
            dir.y = -1.0;
            break;
            
        case 4: //+Z
            dir.xy = uv.xy * 2.0 - 1.0;
            dir.z = 1.0;
            break;
            
        case 5: //-Z
            dir.x = uv.x * -2.0 + 1.0;
            dir.y = uv.y * 2.0 - 1.0;
            dir.z = -1.0;
            break;
    }
    return dir;
}

float3 CubemapFaceUVToDirFast(int faceID, float2 uv)
{
    static const float3 N[6] = {
        float3( 1.0, 0.0, 0.0), float3(-1.0, 0.0, 0.0),
        float3( 0.0, 1.0, 0.0), float3( 0.0,-1.0, 0.0),
        float3( 0.0, 0.0, 1.0), float3( 0.0, 0.0,-1.0)
    };
    static const float3 T[6] = {
        float3( 0.0, 0.0,-1.0), float3( 0.0, 0.0, 1.0),
        float3( 1.0, 0.0, 0.0), float3( 1.0, 0.0, 0.0),
        float3( 1.0, 0.0, 0.0), float3(-1.0, 0.0, 0.0)
    };
    static const float3 B[6] = {
        float3( 0.0, 1.0, 0.0), float3( 0.0, 1.0, 0.0),
        float3( 0.0, 0.0,-1.0), float3( 0.0, 0.0, 1.0),
        float3( 0.0, 1.0, 0.0), float3( 0.0, 1.0, 0.0)
    };

    uv = uv * 2.0 - 1.0;
    return N[faceID] + uv.x * T[faceID] + uv.y * B[faceID];
}

#endif