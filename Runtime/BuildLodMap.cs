using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GaiaTerrain
{
    public class BuildLodMap : IDisposable
    {
        private static readonly int NameIdLodMapBuffer = Shader.PropertyToID("_LodMapBuffer");
        private static readonly int NameIdLodMapDebugTexture = Shader.PropertyToID("_LodMapDebugTexture");
        private static readonly int NameIdNodeDivisionBuffer = Shader.PropertyToID("_NodeDivisionBuffer");
        private const string SampleName = "BuildLodMap";
        private readonly ComputeShader _computeShader;
        private readonly int _kernelIdxBuildLodMap;
        private readonly ComputeBuffer _lodMapBuffer;
        private readonly int _dispatchX;
        private readonly RenderTexture _lodMapDebugTexture;

        public BuildLodMap(ComputeShader terrainComputeShader, TerrainConfig terrainConfig,
            ComputeBuffer nodeDivisionBuffer)
        {
            _computeShader = terrainComputeShader;
            _kernelIdxBuildLodMap = _computeShader.FindKernel("BuildLodMap");
            var lod0NodeCount = terrainConfig.LodNodeCountArray[0];
            _lodMapBuffer = new ComputeBuffer(lod0NodeCount.x * lod0NodeCount.y, sizeof(uint),
                ComputeBufferType.Structured);
            _lodMapBuffer.name = "[GaiaTerrain]LodMapBuffer";

            _lodMapDebugTexture = new RenderTexture(lod0NodeCount.x, lod0NodeCount.y, 0, RenderTextureFormat.R8)
            {
                enableRandomWrite = true
            };
            _lodMapDebugTexture.Create();
            _lodMapDebugTexture.name = "[GaiaTerrain]LodMapDebugTexture";

            _computeShader.SetBuffer(_kernelIdxBuildLodMap, NameIdLodMapBuffer, _lodMapBuffer);
            _computeShader.SetBuffer(_kernelIdxBuildLodMap, NameIdNodeDivisionBuffer, nodeDivisionBuffer);
            _computeShader.SetTexture(_kernelIdxBuildLodMap, NameIdLodMapDebugTexture, _lodMapDebugTexture);

            {
                _computeShader.GetKernelThreadGroupSizes(_kernelIdxBuildLodMap, out var x, out _, out _);
                _dispatchX = (lod0NodeCount.x * lod0NodeCount.y + (int)x - 1) / (int)x;
            }
        }

        public void Build(CommandBuffer cmd)
        {
            cmd.BeginSample(SampleName);
            cmd.DispatchCompute(_computeShader, _kernelIdxBuildLodMap, _dispatchX, 1, 1);
            cmd.EndSample(SampleName);
        }

        public ComputeBuffer GetLodMapBuffer()
        {
            return _lodMapBuffer;
        }

        public RenderTexture GetLodMapDebugTexture()
        {
            return _lodMapDebugTexture;
        }

        public void Dispose()
        {
            _lodMapBuffer.Release();
            if (_lodMapDebugTexture)
            {
                _lodMapDebugTexture.Release();
            }
        }
    }
}