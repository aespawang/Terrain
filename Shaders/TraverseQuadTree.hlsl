#ifndef TRAVERSE_QUAD_TREE
#define TRAVERSE_QUAD_TREE

#include "TerrainSharedInput.hlsl"
#include "Common.hlsl"
#include "Culling.hlsl"
#if defined(ENABLE_NODE_BOX_DEBUG)
#include "DebugBox.hlsl"
#endif

float _TraverseDivideFactor;

uint _TraverseCurrLod;
ConsumeStructuredBuffer<uint2> _ConsumeNodeList;
AppendStructuredBuffer<uint2> _AppendNodeList;

float3 CalcNodeCenterPosWithHeight(uint2 nodeXY, uint lod)
{
    float2 pos = CalcNodeCenterPos(nodeXY, lod);
    float2 minMaxHeight = QueryNodeMinMaxHeight(nodeXY, lod);
    return float3(pos.x, (minMaxHeight.x + minMaxHeight.y) / 2, pos.y);
    // return float3(pos.x, _CamPos.y, pos.y);
}

Bounds CalcNodeBounds(uint2 nodeXY, uint lod, float heightOffset)
{
    float2 minMaxHeight = QueryNodeMinMaxHeight(nodeXY, lod);

    // Get Bounds Vertices
    float2 nodePos = CalcNodeCenterPos(nodeXY, lod);
    float2 nodeHalfSize = GetNodeSize(lod) * 0.5;
    Bounds bounds;
    bounds.minPosition.xz = nodePos - nodeHalfSize;
    bounds.minPosition.y = minMaxHeight.x - heightOffset;
    bounds.maxPosition.xz = nodePos + nodeHalfSize;
    bounds.maxPosition.y = minMaxHeight.y + heightOffset;
    return bounds;
}

bool EvaluateNode(uint2 nodeXY, uint lod)
{
    if (lod <= GetMinLod()) return false;
    if (lod > GetMaxLod()) return true;
    float3 nodePos = CalcNodeCenterPosWithHeight(nodeXY, lod);
    float2 nodeSize = GetNodeSize(lod);
    float dist = distance(_CamPos, nodePos);
    float f = dist / (max(nodeSize.x, nodeSize.y) * _TraverseDivideFactor);
    return f < 1.0;
}

void SetVisibility(int index, bool visible)
{
    int word = index >> 5;       // index / 32
    int bit  = index & 31;       // index % 32

    if (visible)
        _VisibilityBuffer[word] |=  (1u << bit);
    else
        _VisibilityBuffer[word] &= ~(1u << bit);
}

bool IsVisible(uint idx)
{
    uint word = idx >> 5;
    uint bit  = idx & 31;
    return (_VisibilityBuffer[word] & (1u << bit)) != 0;
}

bool CheckOrSetNodeVisibility(Bounds bounds, uint nodeIdx)
{
    bool visible = true;
    
    #if defined(ENABLE_TWO_PASS_HIZ) && defined(FIRST_PASS_OF_TWO_PASS_HIZ)
        // first pass of two pass hiz
        visible = IsVisible(nodeIdx) && !isFrustumCulled(bounds, true);
    #else
        visible = !isCulled(bounds, true);
    
        #if defined(ENABLE_TWO_PASS_HIZ) && !defined(FIRST_PASS_OF_TWO_PASS_HIZ)
            SetVisibility(nodeIdx, visible);
        #endif
    #endif

    return visible;
}

[numthreads(1,1,1)]
void TraverseQuadTree()
{
    uint2 nodeXY = _ConsumeNodeList.Consume();
    uint nodeIdx = CalcNodeIdx(nodeXY, _TraverseCurrLod);
    _NodeDivisionBuffer[nodeIdx] = 0;

    #if defined(ENABLE_NODE_CULLING)
        Bounds bounds = CalcNodeBounds(nodeXY, _TraverseCurrLod, _BoundsHeightRedundancy);
        if (!CheckOrSetNodeVisibility(bounds, nodeIdx))
        {
            // note: for other conditions, lod map can be incomplete(only update nodes that are visible)
            // but for 2 pass hiz's first pass , we need to have a full lod map(since we need a seamless depth attachment in first pass)
            #if defined(ENABLE_TWO_PASS_HIZ) && defined(FIRST_PASS_OF_TWO_PASS_HIZ)
            if (EvaluateNode(nodeXY, _TraverseCurrLod))
            {
                _NodeDivisionBuffer[nodeIdx] = 1;
            }
            #endif
            return;
        }
    #endif
    
    if (EvaluateNode(nodeXY, _TraverseCurrLod))
    {
        _NodeDivisionBuffer[nodeIdx] = 1;
        _AppendNodeList.Append(nodeXY * 2);
        _AppendNodeList.Append(nodeXY * 2 + uint2(0, 1));
        _AppendNodeList.Append(nodeXY * 2 + uint2(1, 0));
        _AppendNodeList.Append(nodeXY * 2 + uint2(1, 1));
    }
    else
    {
        _AppendFinalNodeList.Append(float3(nodeXY, _TraverseCurrLod));
        #if defined(ENABLE_NODE_BOX_DEBUG)
            Bounds rawBounds = CalcNodeBounds(nodeXY, _TraverseCurrLod, 0);
            DebugBox debugBox;
            debugBox.type = DEBUG_BOX_NODE_TYPE;
            debugBox.lod = _TraverseCurrLod;
            debugBox.minPosition = rawBounds.minPosition;
            debugBox.maxPosition = rawBounds.maxPosition;
            _AppendDebugBoxBuffer.Append(debugBox);
        #endif
    }
}

StructuredBuffer<uint3> _NodeLocBuffer;
int _TotalNodeCount;

[numthreads(64,1,1)]
void TraverseQuadTreeOneDispatch(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= (uint)_TotalNodeCount) return;

    uint nodeIdx = id.x;
    uint3 nodeLoc = _NodeLocBuffer[id.x];
    uint2 nodeXY = nodeLoc.xy;
    uint nodeLod = nodeLoc.z;

    if (!EvaluateNode(nodeXY / 2, nodeLod + 1))
    {
        _NodeDivisionBuffer[nodeIdx] = 0;
        return;
    }
    
    if (EvaluateNode(nodeXY, nodeLod))
    {
        _NodeDivisionBuffer[nodeIdx] = 1;
        return;
    }

    _NodeDivisionBuffer[nodeIdx] = 0;

    #if defined(ENABLE_NODE_CULLING)
        Bounds bounds = CalcNodeBounds(nodeXY, nodeLod, _BoundsHeightRedundancy);
        if (!CheckOrSetNodeVisibility(bounds, nodeIdx)) return;
    #endif

    _AppendFinalNodeList.Append(float3(nodeXY, nodeLod));
    #if defined(ENABLE_NODE_BOX_DEBUG)
        Bounds rawBounds = CalcNodeBounds(nodeXY, nodeLod, 0);
        DebugBox debugBox;
        debugBox.type = DEBUG_BOX_NODE_TYPE;
        debugBox.lod = nodeLod;
        debugBox.minPosition = rawBounds.minPosition;
        debugBox.maxPosition = rawBounds.maxPosition;
        _AppendDebugBoxBuffer.Append(debugBox);
    #endif
}

#endif