using System.Runtime.InteropServices;
using UnityEngine;

namespace GaiaTerrain
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NodeDebugBox
    {
        /// <summary>
        /// node xz world pos
        /// </summary>
        public Vector2 WorldPos;
        public int Lod;
        public Vector2 MinMaxHeight;
        
        public static int GetSize()
        {
            return Marshal.SizeOf(typeof(NodeDebugBox));
        }
    }
}