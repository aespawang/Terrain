using System.Runtime.InteropServices;
using UnityEngine;

namespace GaiaTerrain
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DebugBox
    {
        // public const int DebugBoxNodeType = 0;
        // public const int DebugBoxPatchType = 1;
        
        public Vector3 MinPosition;
        public int Type;
        public Vector3 MaxPosition;
        public int Lod;
    
        public static int GetSize()
        {
            return Marshal.SizeOf(typeof(DebugBox));
        }
    }
}