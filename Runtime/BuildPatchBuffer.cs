using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GaiaTerrain
{
    public class BuildPatchBuffer : IDisposable, ISyncStat
    {
        private const string SampleName = "BuildPatchBuffer";
        private static readonly int NameIdFinalNodeList = Shader.PropertyToID("_FinalNodeList");
        private static readonly int NameIdAppendPatchBuffer = Shader.PropertyToID("_AppendPatchBuffer");
        private static readonly int NameIdMinMaxHeightTexture = Shader.PropertyToID("_MinMaxHeightTexture");
        private static readonly int NameIdLodMapBuffer = Shader.PropertyToID("_LodMapBuffer");
        private static readonly int NameIdHiZMipmap = Shader.PropertyToID("_HiZMipmap");
        private static readonly int NameIdCullingStatBuffer = Shader.PropertyToID("_CullingStatBuffer");
        private static readonly int NameIdAppendDebugBoxBuffer = Shader.PropertyToID("_AppendDebugBoxBuffer");
        
        private readonly ComputeShader _computeShader;
        private readonly int _kernelIdxBuildPatchBuffer;
        private readonly TerrainRuntimeData _terrainRuntimeData;
        private readonly ComputeBuffer _finalNodeList;
        private readonly ComputeBuffer _appendPatchBuffer;
        private readonly ComputeBuffer _indirectArgsBuffer;
        private readonly TerrainStatus _terrainStatus;
        private readonly uint[] _statArgs = new uint[3];

        public BuildPatchBuffer(ComputeShader terrainComputeShader, TerrainRuntimeData terrainRuntimeData,
            ComputeBuffer finalNodeList, Texture2D minMaxHeightTexture, ComputeBuffer lodMapBuffer,
            ComputeBuffer cullingStatBuffer, ComputeBuffer debugBoxBuffer, TerrainStatus terrainStatus)
        {
            _computeShader = terrainComputeShader;
            _finalNodeList = finalNodeList;
            _terrainRuntimeData = terrainRuntimeData;
            _kernelIdxBuildPatchBuffer = _computeShader.FindKernel("BuildPatchBuffer");
            _terrainStatus = terrainStatus;
        
            _appendPatchBuffer = new ComputeBuffer(TerrainConfig.MaxNodeBufferSize * TerrainConfig.NumPatchesPerNode,
                Patch.GetSize(), ComputeBufferType.Append);
            _appendPatchBuffer.name = "[GaiaTerrain]PatchBuffer";
            
            _indirectArgsBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
            _indirectArgsBuffer.SetData(new uint[] { 1, 1, 1 });
            
            _computeShader.SetBuffer(_kernelIdxBuildPatchBuffer, NameIdFinalNodeList, finalNodeList);
            _computeShader.SetBuffer(_kernelIdxBuildPatchBuffer, NameIdAppendPatchBuffer, _appendPatchBuffer);
            _computeShader.SetBuffer(_kernelIdxBuildPatchBuffer, NameIdAppendDebugBoxBuffer, debugBoxBuffer);
            _computeShader.SetTexture(_kernelIdxBuildPatchBuffer, NameIdMinMaxHeightTexture, minMaxHeightTexture);
            _computeShader.SetBuffer(_kernelIdxBuildPatchBuffer, NameIdLodMapBuffer, lodMapBuffer);
            _computeShader.SetBuffer(_kernelIdxBuildPatchBuffer, NameIdCullingStatBuffer, cullingStatBuffer);
        }

        public void Build(CommandBuffer cmd)
        {
            cmd.BeginSample(SampleName);
            cmd.SetComputeTextureParam(_computeShader, _kernelIdxBuildPatchBuffer, NameIdHiZMipmap, _terrainRuntimeData.HiZMipmap);
            cmd.SetBufferCounterValue(_appendPatchBuffer, 0);
            cmd.CopyCounterValue(_finalNodeList, _indirectArgsBuffer, 0);
            cmd.DispatchCompute(_computeShader, _kernelIdxBuildPatchBuffer, _indirectArgsBuffer, 0);
            cmd.EndSample(SampleName);
        }

        public ComputeBuffer GetPatchBuffer()
        {
            return _appendPatchBuffer;
        }

        public void Dispose()
        {
            _appendPatchBuffer.Dispose();
            _indirectArgsBuffer.Dispose();
        }

        public void SyncStat()
        {
            _indirectArgsBuffer.GetData(_statArgs);
            var count = _statArgs[0];
            _terrainStatus.numNodes = count;
            _terrainStatus.maxNumNodes = Math.Max(_terrainStatus.maxNumNodes, count);
        }
    }
}