using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace GaiaTerrain
{
    public class TraverseQuadTree : IDisposable
    {
        private const string SampleNameTraverse = "TraverseQuadTree";
        private const string SampleNameTraverseOneDispatch = "TraverseQuadTreeOneDispatch";
        private static readonly int NameIdTraverseCurrLod = Shader.PropertyToID("_TraverseCurrLod");
        private static readonly int NameIdTraverseDivideFactor = Shader.PropertyToID("_TraverseDivideFactor");
        private static readonly int NameIdConsumeNodeList = Shader.PropertyToID("_ConsumeNodeList");
        private static readonly int NameIdAppendNodeList = Shader.PropertyToID("_AppendNodeList");
        private static readonly int NameIdAppendFinalNodeList = Shader.PropertyToID("_AppendFinalNodeList");
        private static readonly int NameIdMinMaxHeightTexture = Shader.PropertyToID("_MinMaxHeightTexture");
        private static readonly int NameIdHiZMipmap = Shader.PropertyToID("_HiZMipmap");
        private static readonly int NameIdCullingStatBuffer = Shader.PropertyToID("_CullingStatBuffer");
        private static readonly int NameIdAppendDebugBoxBuffer = Shader.PropertyToID("_AppendDebugBoxBuffer");
        private static readonly int NameIdNodeDivisionBuffer = Shader.PropertyToID("_NodeDivisionBuffer");
        private static readonly int NameIdTotalNodeCount = Shader.PropertyToID("_TotalNodeCount");
        private static readonly int NameIdNodeLocBuffer = Shader.PropertyToID("_NodeLocBuffer");
        private static readonly int NameIdVisibilityBuffer = Shader.PropertyToID("_VisibilityBuffer");

        private readonly TerrainConfig _terrainConfig;
        private readonly TerrainFeatures _terrainFeatures;
        private readonly TerrainRuntimeData _terrainRuntimeData;
        
        /// <summary>
        /// 每个node位置用uint2表示（x,y）
        /// </summary>
        private readonly ComputeBuffer _tempNodeListA;
        
        /// <summary>
        /// 每个node位置用uint2表示（x,y）
        /// </summary>
        private readonly ComputeBuffer _tempNodeListB;
        
        /// <summary>
        /// traverse最初生产的数据（即把5x5 max lod的node全部放进来），只设置一次值后面一直不改变
        /// </summary>
        private readonly ComputeBuffer _maxLodNodeList;

        private readonly ComputeBuffer _nodeLocBuffer;
        
        private readonly ComputeBuffer _appendFinalNodeList;

        private readonly ComputeBuffer _visibilityBuffer;

        /// <summary>
        /// dispatch时thread group的形状：uint * 3
        /// </summary>
        private readonly ComputeBuffer _indirectArgsBuffer;
        
        private readonly ComputeBuffer _nodeDivisionBuffer;

        private readonly ComputeShader _computeShader;
        
        private readonly int _kernelIdxTraverseQuadTree;
        
        private readonly int _kernelIdxTraverseQuadTreeOneDispatch;

        private readonly int _maxLodNodeListSize;

        private readonly int _traverseOneDispatchSize;

        public TraverseQuadTree(ComputeShader terrainComputeShader, TerrainConfig terrainConfig,
            TerrainFeatures terrainFeatures, TerrainRuntimeData terrainRuntimeData,
            Texture2D minMaxHeightTexture, ComputeBuffer cullingStatBuffer, ComputeBuffer debugBoxBuffer)
        {
            _terrainConfig = terrainConfig;
            _terrainFeatures = terrainFeatures;
            _terrainRuntimeData = terrainRuntimeData;
            _computeShader = terrainComputeShader;
            _kernelIdxTraverseQuadTree = _computeShader.FindKernel("TraverseQuadTree");
            _kernelIdxTraverseQuadTreeOneDispatch = _computeShader.FindKernel("TraverseQuadTreeOneDispatch");

            _tempNodeListA = new ComputeBuffer(TerrainConfig.TempNodeBufferSize, sizeof(float) * 2, ComputeBufferType.Append);
            _tempNodeListA.name = "[GaiaTerrain]TempNodeListA";
            _tempNodeListB = new ComputeBuffer(TerrainConfig.TempNodeBufferSize, sizeof(float) * 2, ComputeBufferType.Append);
            _tempNodeListB.name = "[GaiaTerrain]TempNodeListB";
            var maxLodNodeCount = terrainConfig.LodNodeCountArray[terrainConfig.MaxLod];
            _maxLodNodeListSize = maxLodNodeCount.x * maxLodNodeCount.y;
            _maxLodNodeList = new ComputeBuffer(_maxLodNodeListSize, sizeof(float) * 2, ComputeBufferType.Append);
            _maxLodNodeList.name = "[GaiaTerrain]MaxLodNodeList";
            InitMaxLodNodeList(maxLodNodeCount);
            
            _nodeLocBuffer = new ComputeBuffer(terrainConfig.TotalNodeCount, sizeof(float) * 3, ComputeBufferType.Structured);
            _nodeLocBuffer.name = "[GaiaTerrain]NodeLocBuffer";
            SetNodeLocBufferData(terrainConfig);
            {
                _computeShader.GetKernelThreadGroupSizes(_kernelIdxTraverseQuadTreeOneDispatch, out var x, out _, out _);
                _traverseOneDispatchSize = (int)(((uint)_terrainConfig.TotalNodeCount + x - 1) / x);
            }
            
            _appendFinalNodeList = new ComputeBuffer(TerrainConfig.MaxNodeBufferSize, sizeof(float) * 3, ComputeBufferType.Append);
            _appendFinalNodeList.name = "[GaiaTerrain]FinalNodeList";
            
            _visibilityBuffer = new ComputeBuffer((terrainConfig.TotalNodeCount + 31) / 32, sizeof(uint), ComputeBufferType.Structured);
            _visibilityBuffer.name = "[GaiaTerrain]VisibilityBuffer";
            
            _indirectArgsBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
            _indirectArgsBuffer.name = "[GaiaTerrain]TraverseIndirectArgsBuffer";
            _indirectArgsBuffer.SetData(new uint[] { 1, 1, 1 });
            
            _nodeDivisionBuffer = new ComputeBuffer(terrainConfig.TotalNodeCount, sizeof(uint), ComputeBufferType.Structured);
            _nodeDivisionBuffer.name = "[GaiaTerrain]NodeDivisionBuffer";
            
            _computeShader.SetInt(NameIdTotalNodeCount, terrainConfig.TotalNodeCount);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTree, NameIdAppendFinalNodeList, _appendFinalNodeList);
            _computeShader.SetTexture(_kernelIdxTraverseQuadTree, NameIdMinMaxHeightTexture, minMaxHeightTexture);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTree, NameIdCullingStatBuffer, cullingStatBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTree, NameIdAppendDebugBoxBuffer, debugBoxBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTree, NameIdNodeDivisionBuffer, _nodeDivisionBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTree, NameIdVisibilityBuffer, _visibilityBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTreeOneDispatch, NameIdAppendFinalNodeList, _appendFinalNodeList);
            _computeShader.SetTexture(_kernelIdxTraverseQuadTreeOneDispatch, NameIdMinMaxHeightTexture, minMaxHeightTexture);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTreeOneDispatch, NameIdCullingStatBuffer, cullingStatBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTreeOneDispatch, NameIdAppendDebugBoxBuffer, debugBoxBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTreeOneDispatch, NameIdNodeDivisionBuffer, _nodeDivisionBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTreeOneDispatch, NameIdNodeLocBuffer, _nodeLocBuffer);
            _computeShader.SetBuffer(_kernelIdxTraverseQuadTreeOneDispatch, NameIdVisibilityBuffer, _visibilityBuffer);
        }

        public void Traverse(CommandBuffer cmd)
        {
            if (_terrainFeatures.enableTraverseOneDispatch)
            {
                TraverseOneDispatch(cmd);
                return;
            }
            
            cmd.BeginSample(SampleNameTraverse);
            cmd.SetComputeFloatParam(_computeShader, NameIdTraverseDivideFactor, _terrainFeatures.traverseDivideFactor);
            cmd.SetComputeTextureParam(_computeShader, _kernelIdxTraverseQuadTree, NameIdHiZMipmap, _terrainRuntimeData.HiZMipmap);
            cmd.SetBufferCounterValue(_maxLodNodeList, (uint)_maxLodNodeListSize); // consume不会修改数据，只会修改栈顶指针位置
            cmd.SetBufferCounterValue(_tempNodeListA,0);
            cmd.SetBufferCounterValue(_tempNodeListB,0);
            cmd.SetBufferCounterValue(_appendFinalNodeList,0);
            cmd.CopyCounterValue(_maxLodNodeList, _indirectArgsBuffer, 0);
            var consumeNodeList = _tempNodeListA;
            var appendNodeList = _tempNodeListB;
            for (var lod = _terrainConfig.MaxLod; lod >= _terrainConfig.MinLod; --lod)
            {
                cmd.SetComputeIntParam(_computeShader, NameIdTraverseCurrLod, lod);
                cmd.SetComputeBufferParam(
                    _computeShader,
                    _kernelIdxTraverseQuadTree,
                    NameIdConsumeNodeList,
                    lod == _terrainConfig.MaxLod ? _maxLodNodeList : consumeNodeList);
                cmd.SetComputeBufferParam(
                    _computeShader,
                    _kernelIdxTraverseQuadTree,
                    NameIdAppendNodeList,
                    appendNodeList);
                cmd.DispatchCompute(
                    _computeShader,
                    _kernelIdxTraverseQuadTree,
                    _indirectArgsBuffer,
                    0);
                cmd.CopyCounterValue(appendNodeList, _indirectArgsBuffer, 0);
                (consumeNodeList, appendNodeList) = (appendNodeList, consumeNodeList);
            }
            cmd.EndSample(SampleNameTraverse);
        }

        private void TraverseOneDispatch(CommandBuffer cmd)
        {
            cmd.BeginSample(SampleNameTraverseOneDispatch);
            cmd.SetComputeFloatParam(_computeShader, NameIdTraverseDivideFactor, _terrainFeatures.traverseDivideFactor);
            cmd.SetComputeTextureParam(_computeShader, _kernelIdxTraverseQuadTreeOneDispatch, NameIdHiZMipmap, _terrainRuntimeData.HiZMipmap);
            cmd.SetBufferCounterValue(_appendFinalNodeList, 0);
            cmd.DispatchCompute(_computeShader, _kernelIdxTraverseQuadTreeOneDispatch, _traverseOneDispatchSize, 1, 1);
            cmd.EndSample(SampleNameTraverseOneDispatch);
        }
        
        public void Dispose()
        {
            _tempNodeListA.Dispose();
            _tempNodeListB.Dispose();
            _maxLodNodeList.Dispose();
            _nodeLocBuffer.Dispose();
            _appendFinalNodeList.Dispose();
            _indirectArgsBuffer.Dispose();
            _nodeDivisionBuffer.Dispose();
            _visibilityBuffer.Dispose();
        }

        public ComputeBuffer GetFinalNodeList()
        {
            return _appendFinalNodeList;
        }
        
        public ComputeBuffer GetNodeDivisionBuffer()
        {
            return _nodeDivisionBuffer;
        }
        
        private void InitMaxLodNodeList(Vector2Int maxLodNodeCount)
        {
            var maxLodNodeList = new uint2[maxLodNodeCount.x * maxLodNodeCount.y];
            var index = 0;
            for (uint y = 0; y < maxLodNodeCount.y; y++)
            {
                for (uint x = 0; x < maxLodNodeCount.x; x++)
                {
                    maxLodNodeList[index++] = new uint2(x, y);
                }
            }
            _maxLodNodeList.SetData(maxLodNodeList);
        }
        
        private void SetNodeLocBufferData(TerrainConfig terrainConfig)
        {
            var nodeLocBufferData = new int[terrainConfig.TotalNodeCount * 3];
            var idx = 0;
            for (var lod = terrainConfig.MinLod; lod <= terrainConfig.MaxLod; ++lod)
            {
                var nodeCount = terrainConfig.LodNodeCountArray[lod];
                for (var y = 0; y < nodeCount.y; ++y)
                {
                    for (var x = 0; x < nodeCount.x; ++x)
                    {
                        nodeLocBufferData[idx * 3 + 0] = x;
                        nodeLocBufferData[idx * 3 + 1] = y;
                        nodeLocBufferData[idx * 3 + 2] = lod;
                        idx++;
                    }
                }
            }
                
            _nodeLocBuffer.SetData(nodeLocBufferData);
        }
    }
}