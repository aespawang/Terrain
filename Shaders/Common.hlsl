#ifndef COMMON
#define COMMON

#include "TerrainSharedInput.hlsl"

uint GetLodCount()
{
    return (uint)_LodInfo.x;
}

uint GetMinLod()
{
    return (uint)_LodInfo.y;
}

uint GetMaxLod()
{
    return (uint)_LodInfo.z;
}

uint CalcNodeIdx(uint2 nodeXY, uint lod)
{
    return _LodNodeCountAndOffsetArray[lod].z + nodeXY.y * _LodNodeCountAndOffsetArray[lod].x + nodeXY.x;
}

float2 GetNodeSize(uint lod)
{
    return _LodNodeSizeArray[lod].xy;
}

int2 GetNodeCount(uint lod)
{
    return _LodNodeCountAndOffsetArray[lod].xy;
}

int GetNodeCountOffset(uint lod)
{
    return _LodNodeCountAndOffsetArray[lod].z;
}

float2 CalcNodeCenterPos(uint2 nodeXY, uint lod)
{
    float2 nodeSize = GetNodeSize(lod);
    int2 nodeCount = GetNodeCount(lod);
    float2 nodeCenterPos = ((float2)nodeXY - (nodeCount - 1) * 0.5) * nodeSize;
    return nodeCenterPos;
}

float2 QueryNodeMinMaxHeight(uint2 nodeXY, uint lod)
{
    return _MinMaxHeightTexture.Load(int3(nodeXY, lod + 3)).xy * _WorldSize.y;
}

float2 QueryPatchMinMaxHeight(uint2 patchXY, uint lod)
{
    return _MinMaxHeightTexture.Load(int3(patchXY, lod)).xy * _WorldSize.y;
}

#endif