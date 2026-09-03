#ifndef CULLING
#define CULLING

#include "TerrainSharedInput.hlsl"
#include "Bounds.hlsl"

static const float4 _FrustumPlanes[6] = {
    float4( 1,  0,  0,  1),   //  Left   :  x + w < 0
    float4(-1,  0,  0,  1),   //  Right  : -x + w < 0
    float4( 0,  1,  0,  1),   //  Bottom :  y + w < 0
    float4( 0, -1,  0,  1),   //  Top    : -y + w < 0
    float4( 0,  0,  1,  1),   //  Near   :  z + w < 0
    float4( 0,  0, -1,  1)    //  Far    : -z + w < 0
};
static const float _FloatMax = 3.4028235e+38f;
static const float _Epsilon = -0.0001f;

// [0]: node fc count
// [1]: node hiz count
// [2]: patch fc count
// [3]: patch hiz count
#if defined(ENABLE_CULLING_STAT)
RWStructuredBuffer<uint> _CullingStatBuffer;
#endif

#if defined(ENABLE_VFC) || defined(ENABLE_TWO_PASS_HIZ)
bool isFrustumCulled(Bounds bounds, bool isNode)
{
    float4 boundsCorners[8];
    boundsCorners[0] = mul(_VFCProjectionViewMatrix, float4(bounds.minPosition.x, bounds.minPosition.y, bounds.minPosition.z, 1.0f)); // lbn
    boundsCorners[1] = mul(_VFCProjectionViewMatrix, float4(bounds.minPosition.x, bounds.minPosition.y, bounds.maxPosition.z, 1.0f)); // lbf
    boundsCorners[2] = mul(_VFCProjectionViewMatrix, float4(bounds.minPosition.x, bounds.maxPosition.y, bounds.minPosition.z, 1.0f)); // ltn
    boundsCorners[3] = mul(_VFCProjectionViewMatrix, float4(bounds.minPosition.x, bounds.maxPosition.y, bounds.maxPosition.z, 1.0f)); // ltf
    boundsCorners[4] = mul(_VFCProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.minPosition.y, bounds.minPosition.z, 1.0f)); // rbn
    boundsCorners[5] = mul(_VFCProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.minPosition.y, bounds.maxPosition.z, 1.0f)); // rbf
    boundsCorners[6] = mul(_VFCProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.maxPosition.y, bounds.minPosition.z, 1.0f)); // rtn
    boundsCorners[7] = mul(_VFCProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.maxPosition.y, bounds.maxPosition.z, 1.0f)); // rtf

    bool isOutside = false;
    for (int p = 0; p < 6; ++ p)
    {
        bool outsidePlane = true;
        for (int v = 0; v < 8; ++ v) 
        {
            if (dot(_FrustumPlanes[p], boundsCorners[v]) > _Epsilon) 
            {
                outsidePlane = false;
                break;
            }
        }
        if (outsidePlane) 
        {
            isOutside = true;
            break;
        }
    }
    if (isOutside)
    {
        #if defined(ENABLE_CULLING_STAT)
            InterlockedAdd(_CullingStatBuffer[isNode ? 0 : 2], 1);
        #endif
        return true;
    }
    return false;
}
#endif

#if defined(ENABLE_HIZ) || defined(ENABLE_TWO_PASS_HIZ)
Texture2D<float> _HiZMipmap;

bool isHiZCulled(Bounds bounds, bool isNode)
{
    float4 boundsCorners[8];
    boundsCorners[0] = mul(_HiZProjectionViewMatrix, float4(bounds.minPosition.x, bounds.minPosition.y, bounds.minPosition.z, 1.0f)); // lbn
    boundsCorners[1] = mul(_HiZProjectionViewMatrix, float4(bounds.minPosition.x, bounds.minPosition.y, bounds.maxPosition.z, 1.0f)); // lbf
    boundsCorners[2] = mul(_HiZProjectionViewMatrix, float4(bounds.minPosition.x, bounds.maxPosition.y, bounds.minPosition.z, 1.0f)); // ltn
    boundsCorners[3] = mul(_HiZProjectionViewMatrix, float4(bounds.minPosition.x, bounds.maxPosition.y, bounds.maxPosition.z, 1.0f)); // ltf
    boundsCorners[4] = mul(_HiZProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.minPosition.y, bounds.minPosition.z, 1.0f)); // rbn
    boundsCorners[5] = mul(_HiZProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.minPosition.y, bounds.maxPosition.z, 1.0f)); // rbf
    boundsCorners[6] = mul(_HiZProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.maxPosition.y, bounds.minPosition.z, 1.0f)); // rtn
    boundsCorners[7] = mul(_HiZProjectionViewMatrix, float4(bounds.maxPosition.x, bounds.maxPosition.y, bounds.maxPosition.z, 1.0f)); // rtf

    float cornerMaxDepth = 0.0f;
    float2 screenBoundsMinPos = float2(1, 1) * _FloatMax;
    float2 screenBoundsMaxPos = float2(-1, -1) * _FloatMax;
    [unroll]
    for (int idx = 0; idx < 8; ++idx)
    {
        boundsCorners[idx] /= boundsCorners[idx].w;
        boundsCorners[idx].xy = (boundsCorners[idx].xy + 1.0f) * 0.5f * _ScreenSize;
        boundsCorners[idx].z = (1.0f - boundsCorners[idx].z) * 0.5f;
        cornerMaxDepth = max(cornerMaxDepth, boundsCorners[idx].z);
        screenBoundsMinPos = min(screenBoundsMinPos, boundsCorners[idx].xy);
        screenBoundsMaxPos = max(screenBoundsMaxPos, boundsCorners[idx].xy);
    }

    uint2 screenBoundsMinUintPos = (uint2)clamp(floor(screenBoundsMinPos), 0.0f, _ScreenSize.xy);
    uint2 screenBoundsMaxUintPos = (uint2)clamp(ceil(screenBoundsMaxPos), 0.0f, _ScreenSize.xy);

    uint2 minBoundsRemappedtoHiz0 = screenBoundsMinUintPos / 2;
    uint2 maxBoundsRemappedtoHiz0 = screenBoundsMaxUintPos / 2;

    // note: In fact, more precise pruning reduces the final shading overhead;
    // moreover, since extremely elongated bound cases do not occur, we use min instead of max.
    int mipmapLod = firstbithigh(min(maxBoundsRemappedtoHiz0.x - minBoundsRemappedtoHiz0.x, maxBoundsRemappedtoHiz0.y - minBoundsRemappedtoHiz0.y));
    uint2 mipBoundMinPos = minBoundsRemappedtoHiz0 >> mipmapLod;
    uint2 mipBoundMaxPos = maxBoundsRemappedtoHiz0 >> mipmapLod;

    float screenBoundsMinDepth = 1.0f;
    for (uint i = mipBoundMinPos.x; i <= mipBoundMaxPos.x; i++)
    {
        for (uint j = mipBoundMinPos.y; j <= mipBoundMaxPos.y; j++)
        {
            screenBoundsMinDepth = min(screenBoundsMinDepth, _HiZMipmap.Load(int3(i, j, mipmapLod)));
        }
    }
    
    if (screenBoundsMinDepth > cornerMaxDepth)
    {
        #if defined(ENABLE_CULLING_STAT)
            InterlockedAdd(_CullingStatBuffer[isNode ? 1 : 3], 1);
        #endif
        return true;
    }
    return false;
}
#endif

bool isCulled(Bounds bounds, bool isNode)
{
    #if (defined(ENABLE_VFC) && defined(ENABLE_HIZ)) || defined(ENABLE_TWO_PASS_HIZ)
        return isFrustumCulled(bounds, isNode) || isHiZCulled(bounds, isNode);
    #elif defined(ENABLE_VFC)
        return isFrustumCulled(bounds, isNode);
    #elif defined(ENABLE_HIZ)
        return isHiZCulled(bounds, isNode);
    #else
        return false;
    #endif
}

#endif

