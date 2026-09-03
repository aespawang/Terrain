using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace GaiaTerrain
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Patch
    {
        public float2 Position;
        public uint Lod;
        public uint4 LodTransitions;

        public static int GetSize()
        {
            return Marshal.SizeOf(typeof(Patch));
        }
    }
}