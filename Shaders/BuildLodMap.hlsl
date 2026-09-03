#ifndef BUILD_LOD_MAP
#define BUILD_LOD_MAP

#include "Common.hlsl"

RWStructuredBuffer<uint> _LodMapBuffer;
RWTexture2D<float> _LodMapDebugTexture;

[numthreads(64,1,1)]
void BuildLodMap(uint3 id : SV_DispatchThreadID)
{
    uint lodMapIdx = id.x;
    int2 mip0NodeCount = GetNodeCount(0);
    if (lodMapIdx >= (uint)(mip0NodeCount.x * mip0NodeCount.y))
    {
        return;
    }

    int2 lodMapXY = int2(lodMapIdx % mip0NodeCount.x, lodMapIdx / mip0NodeCount.x);
    uint minLod = GetMinLod();
    uint maxLod = GetMaxLod();
    for (uint lod = maxLod; lod >= minLod; --lod)
    {
        uint2 nodeXY = lodMapXY >> lod;
        uint nodeIdx = CalcNodeIdx(nodeXY, lod);
        if (_NodeDivisionBuffer[nodeIdx] == 0)
        {
            _LodMapBuffer[lodMapIdx] = lod;
            _LodMapDebugTexture[lodMapXY] = lod * 1.0 / maxLod;
            return;
        }
    }
}


#endif
