#ifndef TERRAIN_SHARED_INPUT
#define TERRAIN_SHARED_INPUT

#include "Patch.hlsl"

#define PATCH_GRID_SIZE 8
#define MAX_LOD_COUNT 6

// Terrain Settings
float3 _WorldSize;
float4 _LodNodeSizeArray[MAX_LOD_COUNT];
int4 _LodNodeCountAndOffsetArray[MAX_LOD_COUNT]; // x:node count x, y: node count y, z:node count offset
float4 _LodInfo; // x:lod count, y:min lod, z: max load, w:0

// Camera Settings
float3 _CamPos;
float4x4 _VFCProjectionViewMatrix;
float4x4 _HiZProjectionViewMatrix;
float2 _ScreenSize;

// Buffers
AppendStructuredBuffer<uint3> _AppendFinalNodeList;
AppendStructuredBuffer<uint3> _AppendCulledNodeList;
StructuredBuffer<uint3> _FinalNodeList;
AppendStructuredBuffer<Patch> _AppendPatchBuffer;
RWStructuredBuffer<uint> _NodeDivisionBuffer;
RWStructuredBuffer<uint> _VisibilityBuffer;

#if defined(ENABLE_NODE_BOX_DEBUG) || defined(ENABLE_PATCH_BOX_DEBUG)
#include "DebugBox.hlsl"
AppendStructuredBuffer<DebugBox> _AppendDebugBoxBuffer;
#endif

// Textures
Texture2D<float2> _MinMaxHeightTexture;

// Other
float _BoundsHeightRedundancy;

#endif