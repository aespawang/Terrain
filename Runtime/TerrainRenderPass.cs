using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GaiaTerrain
{
    public class TerrainRenderPass : ScriptableRenderPass
    {
        private readonly TerrainConfig _terrainConfig;
        private readonly TerrainFeatures _terrainFeatures;
        private readonly TerrainRuntimeData _terrainRuntimeData;
        private readonly DebugSettings _debugSettings;
        private readonly TerrainStatus _terrainStatus;
        private readonly ComputeShader _computeShader;
        private readonly TerrainStreaming _terrainStreaming;
        private readonly TraverseQuadTree _traverseQuadTree;
        private readonly BuildLodMap _buildLodMap;
        private readonly BuildPatchBuffer _buildPatchBuffer;
        private readonly DrawDebugBoxes _drawDebugBoxes;
        private readonly DrawPatches _drawPatches;
        private readonly SyncStatManager _syncStatManager;
        private readonly ComputeBuffer _cullingStatBuffer;
        private readonly uint[] _clearCullingStat = new uint[4];
        private readonly uint[] _currCullingStat = new uint[4];
        private readonly LocalKeyword _keywordEnableFrustumCulling;
        private readonly LocalKeyword _keywordEnableHiZCulling;
        private readonly LocalKeyword _keywordEnableTwoPassHiZ;
        private readonly LocalKeyword _keywordFirstPassOfTwoPassHiz;
        private readonly LocalKeyword _keywordEnableNodeCulling;
        private readonly LocalKeyword _keywordEnablePatchCulling;
        private readonly LocalKeyword _keywordEnableCullingStat;
        private readonly LocalKeyword _keywordEnableNodeBoxDebug;
        private readonly LocalKeyword _keywordEnablePatchBoxDebug;

        private const string SampleNameFirstPass = "FirstPass";
        private const string SampleNameHiZGenerationPass = "HiZGenerationPass";
        private const string SampleNameSecondPass = "SecondPass";
        private static readonly int NameIdLodNodeSizeArray = Shader.PropertyToID("_LodNodeSizeArray");
        private static readonly int NameIdLodNodeCountAndOffsetArray = Shader.PropertyToID("_LodNodeCountAndOffsetArray");
        private static readonly int NameIdLodInfo = Shader.PropertyToID("_LodInfo");
        private static readonly int NameIdCamPos = Shader.PropertyToID("_CamPos");
        private static readonly int NameIdVFCProjectionViewMatrix = Shader.PropertyToID("_VFCProjectionViewMatrix");
        private static readonly int NameIdHiZProjectionViewMatrix = Shader.PropertyToID("_HiZProjectionViewMatrix");
        private static readonly int NameIdScreenSize = Shader.PropertyToID("_ScreenSize");
        private static readonly int NameIdBoundsHeightRedundancy = Shader.PropertyToID("_BoundsHeightRedundancy");
        private static readonly int NameIdWorldSize = Shader.PropertyToID("_WorldSize");
        
        public TerrainRenderPass(TerrainConfig terrainConfig,
            ComputeShader terrainComputeShader, Mesh planeMesh, Mesh cubeMesh,
            TerrainStreaming terrainStreaming, TerrainFeatures terrainFeatures,
            TerrainRuntimeData terrainRuntimeData, DebugSettings debugSettings, TerrainStatus terrainStatus)
        {
            _terrainConfig = terrainConfig;
            _terrainStreaming = terrainStreaming;
            _terrainFeatures = terrainFeatures;
            _debugSettings = debugSettings;
            _terrainStatus = terrainStatus;
            _computeShader = terrainComputeShader;
            _terrainRuntimeData = terrainRuntimeData;

            _cullingStatBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.Structured);
            _cullingStatBuffer.name = "[GaiaTerrain]CullingStatBuffer";

            _drawDebugBoxes = new DrawDebugBoxes(cubeMesh);
            _traverseQuadTree = new TraverseQuadTree(terrainComputeShader, terrainConfig,
                terrainFeatures, terrainRuntimeData,
                _terrainStreaming.GetMinMaxHeightTexture(), _cullingStatBuffer, _drawDebugBoxes.GetDebugBoxBuffer()
            );
            _buildLodMap = new BuildLodMap(terrainComputeShader, terrainConfig, _traverseQuadTree.GetNodeDivisionBuffer());
            _buildPatchBuffer = new BuildPatchBuffer(terrainComputeShader, terrainRuntimeData,
                _traverseQuadTree.GetFinalNodeList(),
                _terrainStreaming.GetMinMaxHeightTexture(), _buildLodMap.GetLodMapBuffer(),
                _cullingStatBuffer, _drawDebugBoxes.GetDebugBoxBuffer(),
                terrainStatus
            );
            _drawPatches = new DrawPatches(terrainConfig, terrainFeatures,
                planeMesh,
                _terrainStreaming.GetHeightMapArray(),
                _terrainStreaming.GetSplatMapArray(),
                _terrainStreaming.GetAlbedoMapArray(),
                _terrainStreaming.GetSectorAssetDescBuffer(),
                _terrainStreaming.GetTerrainLayerDescBuffer(),
                _buildPatchBuffer.GetPatchBuffer(), debugSettings, terrainStatus);

            _syncStatManager = new SyncStatManager();
            _syncStatManager.Register(_buildPatchBuffer);
            _syncStatManager.Register(_drawPatches);

            SetConstants();

            _keywordEnableFrustumCulling = new LocalKeyword(_computeShader, "ENABLE_VFC");
            _keywordEnableHiZCulling = new LocalKeyword(_computeShader, "ENABLE_HIZ");
            _keywordEnableTwoPassHiZ = new LocalKeyword(_computeShader, "ENABLE_TWO_PASS_HIZ");
            _keywordFirstPassOfTwoPassHiz = new LocalKeyword(_computeShader, "FIRST_PASS_OF_TWO_PASS_HIZ");
            _keywordEnableNodeCulling = new LocalKeyword(_computeShader, "ENABLE_NODE_CULLING");
            _keywordEnablePatchCulling = new LocalKeyword(_computeShader, "ENABLE_PATCH_CULLING");
            _keywordEnableCullingStat = new LocalKeyword(_computeShader, "ENABLE_CULLING_STAT");
            _keywordEnableNodeBoxDebug = new LocalKeyword(_computeShader, "ENABLE_NODE_BOX_DEBUG");
            _keywordEnablePatchBoxDebug = new LocalKeyword(_computeShader, "ENABLE_PATCH_BOX_DEBUG");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = Camera.main;
            if (!camera) return;

            SetShaderKeywords();

            var cmd = CommandBufferPool.Get("Terrain Render Pass");

            cmd.SetComputeVectorParam(_computeShader, NameIdCamPos, camera.transform.position);
            cmd.SetComputeVectorParam(_computeShader, NameIdScreenSize,
                new Vector2(camera.pixelWidth, camera.pixelHeight));
            cmd.SetComputeMatrixParam(_computeShader, NameIdVFCProjectionViewMatrix,
                camera.projectionMatrix * camera.worldToCameraMatrix);
            cmd.SetComputeMatrixParam(_computeShader, NameIdHiZProjectionViewMatrix,
                camera.projectionMatrix * camera.worldToCameraMatrix);

            if (renderingData.cameraData.isSceneViewCamera)
            {
                _drawPatches.Render(cmd);
            }
            else
            {
                if (_terrainStatus.enableStat)
                {
                    // reset
                    cmd.SetBufferData(_cullingStatBuffer, _clearCullingStat);
                }
                
                if (_debugSettings.enableNodeBoxDebug || _debugSettings.enablePatchBoxDebug)
                {
                    _drawDebugBoxes.ResetDebugBoxBufferCounter(cmd);
                }
                
                if (!_terrainFeatures.enableTwoPassHiZCulling)
                {
                    ExecutePass2(cmd);

                }
                else
                {
                    _computeShader.EnableKeyword(_keywordFirstPassOfTwoPassHiz);
                    ExecutePass1(cmd);
                
                    ExecuteHiZGenerationPass(cmd, ref renderingData);
                
                    _computeShader.DisableKeyword(_keywordFirstPassOfTwoPassHiz);
                    ExecutePass2(cmd);
                }
            }
            
            if (_debugSettings.enableNodeBoxDebug || _debugSettings.enablePatchBoxDebug)
            {
                _drawDebugBoxes.Render(cmd);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
            Stat();
        }

        public void Dispose()
        {
            _terrainStreaming.Dispose();
            _traverseQuadTree.Dispose();
            _buildLodMap.Dispose();
            _buildPatchBuffer.Dispose();
            _drawDebugBoxes.Dispose();
            _drawPatches.Dispose();
            _syncStatManager.Clear();
            _cullingStatBuffer.Dispose();
        }

        private void SetConstants()
        {
            // constant buffer needs 16 bytes aligned
            var lodNodeSizeArray = new float[_terrainConfig.LodNodeSizeArray.Length * 4];
            for (var i = 0; i < _terrainConfig.LodNodeSizeArray.Length; i++)
            {
                var lodNodeSize = _terrainConfig.LodNodeSizeArray[i];
                lodNodeSizeArray[i * 4] = lodNodeSize.x;
                lodNodeSizeArray[i * 4 + 1] = lodNodeSize.y;
            }

            _computeShader.SetFloats(NameIdLodNodeSizeArray, lodNodeSizeArray);

            var lodNodeCountAndOffsetArray = new int[_terrainConfig.LodNodeCountArray.Length * 4];
            var nodeCountOffset = 0;
            for (var i = 0; i < _terrainConfig.LodNodeCountArray.Length; i++)
            {
                var lodNodeCount = _terrainConfig.LodNodeCountArray[i];
                lodNodeCountAndOffsetArray[i * 4] = lodNodeCount.x;
                lodNodeCountAndOffsetArray[i * 4 + 1] = lodNodeCount.y;
                lodNodeCountAndOffsetArray[i * 4 + 2] = nodeCountOffset;
                nodeCountOffset += lodNodeCount.x * lodNodeCount.y;
            }

            _computeShader.SetInts(NameIdLodNodeCountAndOffsetArray, lodNodeCountAndOffsetArray);
            _computeShader.SetVector(NameIdLodInfo,
                new Vector4(_terrainConfig.LodCount, _terrainConfig.MinLod, _terrainConfig.MaxLod, 0));

            _computeShader.SetFloat(NameIdBoundsHeightRedundancy, TerrainConfig.BoundsHeightRedundancy);
            _computeShader.SetVector(NameIdWorldSize, new Vector4(
                _terrainConfig.WorldSize.x, _terrainConfig.WorldSize.y, _terrainConfig.WorldSize.z, 0));
        }

        private void SetShaderKeywords()
        {
            if (_terrainFeatures.enableFrustumCulling || _terrainFeatures.enableTwoPassHiZCulling)
            {
                _computeShader.EnableKeyword(_keywordEnableFrustumCulling);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnableFrustumCulling);
            }

            if (_terrainFeatures.enableHiZCulling || _terrainFeatures.enableTwoPassHiZCulling)
            {
                _computeShader.EnableKeyword(_keywordEnableHiZCulling);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnableHiZCulling);
            }
            
            if (_terrainFeatures.enableTwoPassHiZCulling)
            {
                _computeShader.EnableKeyword(_keywordEnableTwoPassHiZ);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnableTwoPassHiZ);
            }

            if (_terrainFeatures.enableNodeCulling)
            {
                _computeShader.EnableKeyword(_keywordEnableNodeCulling);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnableNodeCulling);
            }

            if (_terrainFeatures.enablePatchCulling)
            {
                _computeShader.EnableKeyword(_keywordEnablePatchCulling);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnablePatchCulling);
            }

            if (_terrainStatus.enableStat)
            {
                _computeShader.EnableKeyword(_keywordEnableCullingStat);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnableCullingStat);
            }

            if (_debugSettings.enableNodeBoxDebug)
            {
                _computeShader.EnableKeyword(_keywordEnableNodeBoxDebug);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnableNodeBoxDebug);
            }

            if (_debugSettings.enablePatchBoxDebug)
            {
                _computeShader.EnableKeyword(_keywordEnablePatchBoxDebug);
            }
            else
            {
                _computeShader.DisableKeyword(_keywordEnablePatchBoxDebug);
            }
        }

        // PASS 1 -- Prepass depth
        private void ExecutePass1(CommandBuffer cmd)
        {
            cmd.BeginSample(SampleNameFirstPass);
            _traverseQuadTree.Traverse(cmd);
            _buildLodMap.Build(cmd);
            _buildPatchBuffer.Build(cmd);
            _drawPatches.Render(cmd, true);
            cmd.EndSample(SampleNameFirstPass);
        }
        
        // HiZ Generation Pass
        private void ExecuteHiZGenerationPass(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.BeginSample(SampleNameHiZGenerationPass);
            var hizMipmap = HiZMipmapUtility.GetOrCreateHiZMipmap(renderingData.cameraData.camera, ref _terrainRuntimeData.HiZMipmap);
            HiZMipmapUtility.GenerateHiZMipmapPassCommand(cmd, renderingData, hizMipmap, _terrainFeatures.enableReduceHiZMultiPass);
            cmd.SetRenderTarget(
                renderingData.cameraData.renderer.cameraColorTargetHandle, 
                renderingData.cameraData.renderer.cameraDepthTargetHandle
            );
            
            cmd.EndSample(SampleNameHiZGenerationPass);
        }
        
        // PASS 2 -- Real render with HiZ Culling
        private void ExecutePass2(CommandBuffer cmd)
        {
            cmd.BeginSample(SampleNameSecondPass);
            _traverseQuadTree.Traverse(cmd);
            if(!_terrainFeatures.enableTwoPassHiZCulling)
                _buildLodMap.Build(cmd);
            _buildPatchBuffer.Build(cmd);
            _drawPatches.Render(cmd);
            cmd.EndSample(SampleNameSecondPass);
        }

        private void Stat()
        {
            if (!_terrainStatus.enableStat) return;
            _syncStatManager.SyncStatAll();
            _cullingStatBuffer.GetData(_currCullingStat);
            _terrainStatus.numNodeCulledByFrustum = _currCullingStat[0];
            _terrainStatus.numNodeCulledByHiZ = _currCullingStat[1];
            _terrainStatus.numPatchCulledByFrustum = _currCullingStat[2];
            _terrainStatus.numPatchCulledByHiZ = _currCullingStat[3];
            _terrainStatus.UpdateRates();
        }
    }
}