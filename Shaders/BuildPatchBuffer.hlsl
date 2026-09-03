#ifndef BUILD_PATCH_BUFFER
#define BUILD_PATCH_BUFFER

#include "TerrainSharedInput.hlsl"
#include "Common.hlsl"
#include "Culling.hlsl"
#include "DebugBox.hlsl"

Bounds CalcPatchBounds(uint2 nodeXY, uint2 patchOffset, Patch patch, float heightOffset)
{
    uint2 patchXY = nodeXY * PATCH_GRID_SIZE + patchOffset;
    float2 minMaxHeight = QueryPatchMinMaxHeight(patchXY, patch.lod);
    float2 nodeSize = GetNodeSize(patch.lod);
    float2 patchHalfSize = nodeSize * 0.5 / PATCH_GRID_SIZE;
    Bounds bounds;
    bounds.minPosition.xz = patch.position - patchHalfSize;
    bounds.minPosition.y = minMaxHeight.x - heightOffset;
    bounds.maxPosition.xz = patch.position + patchHalfSize;
    bounds.maxPosition.y = minMaxHeight.y + heightOffset;
    return bounds;
}

uint QueryLodMap(uint2 lod0NodeXY)
{
    uint2 lod0NodeCount = GetNodeCount(0);
    if(lod0NodeXY.x < 0 || lod0NodeXY.y < 0 || lod0NodeXY.x >= lod0NodeCount.x || lod0NodeXY.y >= lod0NodeCount.y)
    {
        return 0;
    }
    
    return _LodMapBuffer[lod0NodeXY.y * lod0NodeCount.x + lod0NodeXY.x];
}

uint4 CalcNodeRect(uint3 nodeLoc)
{
    uint lod0ChildNodeCount = 1 << nodeLoc.z;
    uint2 leftBottom = nodeLoc.xy * lod0ChildNodeCount;
    return uint4(leftBottom, leftBottom + lod0ChildNodeCount - 1);
}

uint4 CalcLodTransitions(uint3 nodeLoc, uint2 patchOffset)
{
    uint lod = nodeLoc.z;
    uint4 nodeRect = CalcNodeRect(nodeLoc);
    uint4 lodTransitions = uint4(0, 0, 0, 0);

    // left
    if (patchOffset.x == 0)
    {
        int lodTrans = QueryLodMap(nodeRect.xy + int2(-1, 0)) - lod;
        lodTransitions.x = max(lodTrans, 0);
    }

    // bottom
    if(patchOffset.y == 0)
    {
        int lodTrans = QueryLodMap(nodeRect.xy + int2(0, -1)) - lod;
        lodTransitions.y = max(lodTrans, 0);
    }

    // right
    if(patchOffset.x == PATCH_GRID_SIZE - 1)
    {
        int lodTrans = QueryLodMap(nodeRect.zw + int2(1, 0)) - lod;
        lodTransitions.z = max(lodTrans, 0);
    }
    
    // up
    if(patchOffset.y == PATCH_GRID_SIZE - 1)
    {
        int lodTrans = QueryLodMap(nodeRect.zw + int2(0, 1)) - lod;
        lodTransitions.w = max(lodTrans, 0);
    }

    return lodTransitions;
}

[numthreads(PATCH_GRID_SIZE,PATCH_GRID_SIZE,1)] // 每个thread group处理一个node
void BuildPatchBuffer(uint3 groupThreadId : SV_GroupThreadID, uint3 groupId : SV_GroupID)
{
    uint2 patchOffset = groupThreadId.xy;
    uint3 nodeLoc = _FinalNodeList[groupId.x];
    uint lod = nodeLoc.z;
    float2 nodePos = CalcNodeCenterPos(nodeLoc.xy, lod);
    float2 nodeSize = GetNodeSize(lod);
    Patch patch;
    patch.position = nodePos + (patchOffset - (PATCH_GRID_SIZE - 1) * 0.5) * nodeSize / PATCH_GRID_SIZE;
    patch.lod = lod;
    patch.lodTransitions = CalcLodTransitions(nodeLoc, patchOffset);

    // Patch Culling
    #if defined(ENABLE_PATCH_CULLING)
        Bounds bounds = CalcPatchBounds(nodeLoc.xy, patchOffset, patch, _BoundsHeightRedundancy);
        if (isCulled(bounds, false)) return;
    #endif
    
    _AppendPatchBuffer.Append(patch);

    #if defined(ENABLE_PATCH_BOX_DEBUG)
        Bounds rawBounds = CalcPatchBounds(nodeLoc.xy, patchOffset, patch, 0);
        DebugBox debugBox;
        debugBox.type = DEBUG_BOX_PATCH_TYPE;
        debugBox.lod = lod;
        debugBox.maxPosition = rawBounds.maxPosition;
        debugBox.minPosition = rawBounds.minPosition;
        _AppendDebugBoxBuffer.Append(debugBox);
    #endif
}

#endif