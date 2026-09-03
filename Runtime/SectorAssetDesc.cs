using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace GaiaTerrain
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SectorAssetDesc
    {
        public int2 splatMapIndices;
        public int4 layerPack0Indices;
        public int4 layerPack1Indices;
        
        public static int GetSize()
        {
            return Marshal.SizeOf(typeof(SectorAssetDesc));
        }
    }
}