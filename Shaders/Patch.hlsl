#ifndef PATCH
#define PATCH

struct Patch
{
    float2 position;
    uint lod;
    uint4 lodTransitions;
};

#endif