using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GaiaTerrain
{
    public class DrawDebugBoxes : IDisposable
    {
        private const string SampleName = "DrawDebugBoxes";
        private static readonly int NameIdDebugBoxBuffer = Shader.PropertyToID("_DebugBoxBuffer");
        
        private readonly ComputeBuffer _debugBoxBuffer;
        private readonly ComputeBuffer _indirectArgsBuffer;
        private readonly Material _material;
        private readonly Mesh _cubeMesh;

        public DrawDebugBoxes(Mesh cubeMesh)
        {
            _cubeMesh = cubeMesh;

            _debugBoxBuffer = new ComputeBuffer(TerrainConfig.MaxNodeBufferSize * (TerrainConfig.NumPatchesPerNode + 1),
                DebugBox.GetSize(), ComputeBufferType.Append);
            
            _indirectArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            SetInstanceCount(0);
            
            _material = new Material(Shader.Find("Terrain/DebugBox"));
            _material.SetBuffer(NameIdDebugBoxBuffer, _debugBoxBuffer);
        }

        public ComputeBuffer GetDebugBoxBuffer()
        {
            return _debugBoxBuffer;
        }

        public void ResetDebugBoxBufferCounter(CommandBuffer cmd)
        {
            cmd.SetBufferCounterValue(_debugBoxBuffer, 0);
        }
        
        public void Render(CommandBuffer cmd)
        {
            cmd.BeginSample(SampleName);
            cmd.CopyCounterValue(_debugBoxBuffer, _indirectArgsBuffer, sizeof(uint));
            cmd.DrawMeshInstancedIndirect(_cubeMesh, 0, _material, -1, _indirectArgsBuffer);
            cmd.EndSample(SampleName);
        }
        
        public void Dispose()
        {
            _debugBoxBuffer.Dispose();
            _indirectArgsBuffer.Release();
        }
        
        private void SetInstanceCount(uint instanceCount)
        {
            var args = new uint[] { 0, 0, 0, 0, 0 };
            args[0] = _cubeMesh.GetIndexCount(0);
            args[1] = instanceCount;
            args[2] = _cubeMesh.GetIndexStart(0);
            args[3] = _cubeMesh.GetBaseVertex(0);
            _indirectArgsBuffer.SetData(args);
        }
    }
}