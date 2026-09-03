#ifndef DEBUG_BOX
#define DEBUG_BOX

struct DebugBox
{
    float3 minPosition;
    int type;
    float3 maxPosition;
    int lod;
};

#define DEBUG_BOX_NODE_TYPE 0
#define DEBUG_BOX_PATCH_TYPE 1

#endif