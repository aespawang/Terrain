using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace GaiaTerrain
{
    public class DrawPatches : IDisposable, ISyncStat
    {
        private const string SampleName = "DrawPatches";
        private static readonly int NameIdWorldSize = Shader.PropertyToID("_WorldSize");
        private static readonly int NameIdHeightMapArray = Shader.PropertyToID("_HeightMapArray");
        private static readonly int NameIdSplatMapArray = Shader.PropertyToID("_SplatMapArray");
        private static readonly int NameIdAlbedoMapArray = Shader.PropertyToID("_AlbedoMapArray");
        private static readonly int NameIdSectorAssetDescBuffer = Shader.PropertyToID("_SectorAssetDescBuffer");
        private static readonly int NameIdTerrainLayerDescBuffer = Shader.PropertyToID("_TerrainLayerDescBuffer");
        private static readonly int NameIdPatchBuffer = Shader.PropertyToID("_PatchBuffer");
        private static readonly int NameIdSectorInfo = Shader.PropertyToID("_SectorInfo");
        private static readonly int NameIdMaxLod = Shader.PropertyToID("_MaxLod");

        private readonly TerrainFeatures _terrainFeatures;
        private readonly Mesh _planeMesh;
        private readonly Material _material;
        private readonly int _passIdxForwardLit;
        private readonly int _passIdxDepthOnly;
        private readonly DebugSettings _debugSettings;
        private readonly ComputeBuffer _indirectArgsBuffer;
        private readonly ComputeBuffer _patchBuffer;
        private readonly TerrainStatus _terrainStatus;
        private readonly uint[] _statArgs = new uint[5];
        private readonly LocalKeyword _keywordEnableLodSeamless;
        private readonly LocalKeyword _keywordEnablePatchDebug;
        private readonly LocalKeyword _keywordEnableNodeDebug;
        private readonly LocalKeyword _keywordEnableLodDebug;
        private readonly LocalKeyword _keywordEnableLodTransDebug;
        private readonly LocalKeyword _keywordEnableHeightDebug;
        private readonly LocalKeyword _keywordEnableCheckerDebug;

        public DrawPatches(TerrainConfig terrainConfig, TerrainFeatures terrainFeatures, Mesh planeMesh,
            Texture2DArray heightMapArray, Texture2DArray splatMapArray, Texture2DArray albedoMapArray,
            ComputeBuffer sectorAssetDescBuffer, ComputeBuffer terrainLayerDescBuffer,
            ComputeBuffer patchBuffer, DebugSettings debugSettings, TerrainStatus terrainStatus)
        {
            _terrainFeatures = terrainFeatures;
            _patchBuffer = patchBuffer;
            _debugSettings = debugSettings;
            _terrainStatus = terrainStatus;
            _planeMesh = planeMesh;
            
            var shader = Shader.Find("Terrain/Terrain");
            _material = new Material(shader);
            _passIdxForwardLit = _material.FindPass("ForwardLit");
            _passIdxDepthOnly = _material.FindPass("DepthOnly");
            
            var worldSize = terrainConfig.WorldSize;

            _indirectArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            SetInstanceCount(0);
            
            _material.SetTexture(NameIdHeightMapArray, heightMapArray);
            _material.SetTexture(NameIdSplatMapArray, splatMapArray);
            _material.SetTexture(NameIdAlbedoMapArray, albedoMapArray);
            _material.SetBuffer(NameIdSectorAssetDescBuffer, sectorAssetDescBuffer);
            _material.SetBuffer(NameIdTerrainLayerDescBuffer, terrainLayerDescBuffer);
            _material.SetBuffer(NameIdPatchBuffer, _patchBuffer);
            _material.SetVector(NameIdWorldSize, new Vector4(worldSize.x, worldSize.y, worldSize.z, 0));
            _material.SetVector(NameIdSectorInfo, new Vector4(
                terrainConfig.SectorGridSize.x,
                terrainConfig.SectorGridSize.y,
                terrainConfig.SectorSize.x / terrainConfig.SectorHeightMapResolution,
                terrainConfig.SectorSize.z / terrainConfig.SectorHeightMapResolution));
            _material.SetInt(NameIdMaxLod, terrainConfig.MaxLod);
                
            _keywordEnableLodSeamless = new LocalKeyword(shader, "ENABLE_LOD_SEAMLESS");
            _keywordEnablePatchDebug = new LocalKeyword(shader, "ENABLE_PATCH_DEBUG");
            _keywordEnableNodeDebug = new LocalKeyword(shader, "ENABLE_NODE_DEBUG");
            _keywordEnableLodDebug = new LocalKeyword(shader, "ENABLE_LOD_DEBUG");
            _keywordEnableLodTransDebug = new LocalKeyword(shader, "ENABLE_LOD_TRANS_DEBUG");
            _keywordEnableHeightDebug = new LocalKeyword(shader, "ENABLE_HEIGHT_DEBUG");
            _keywordEnableCheckerDebug = new LocalKeyword(shader, "ENABLE_CHECKER_DEBUG");
        }

        public void Render(CommandBuffer cmd, bool isDepthOnly = false)
        {
            SetShaderKeywords();
            cmd.BeginSample(SampleName);
            cmd.CopyCounterValue(_patchBuffer, _indirectArgsBuffer, sizeof(uint));
            cmd.DrawMeshInstancedIndirect(_planeMesh, 0, _material,
                isDepthOnly ? _passIdxDepthOnly : _passIdxForwardLit, _indirectArgsBuffer);
            cmd.EndSample(SampleName);
        }

        public void Dispose()
        {
            _indirectArgsBuffer.Dispose();
        }

        private void SetInstanceCount(uint instanceCount)
        {
            var args = new uint[] { 0, 0, 0, 0, 0 };
            args[0] = _planeMesh.GetIndexCount(0);
            args[1] = instanceCount;
            args[2] = _planeMesh.GetIndexStart(0);
            args[3] = _planeMesh.GetBaseVertex(0);
            _indirectArgsBuffer.SetData(args);
        }

        private void SetShaderKeywords()
        {
            if (_terrainFeatures.enableLodSeamless)
            {
                _material.EnableKeyword(_keywordEnableLodSeamless);
            }
            else
            {
                _material.DisableKeyword(_keywordEnableLodSeamless);
            }
            
            if (_debugSettings.enablePatchDebug)
            {
                _material.EnableKeyword(_keywordEnablePatchDebug);
            }
            else
            {
                _material.DisableKeyword(_keywordEnablePatchDebug);
            }

            if (_debugSettings.enableNodeDebug)
            {
                _material.EnableKeyword(_keywordEnableNodeDebug);
            }
            else
            {
                _material.DisableKeyword(_keywordEnableNodeDebug);
            }

            if (_debugSettings.enableLodDebug)
            {
                _material.EnableKeyword(_keywordEnableLodDebug);
            }
            else
            {
                _material.DisableKeyword(_keywordEnableLodDebug);
            }

            if (_debugSettings.enableLodTransDebug)
            {
                _material.EnableKeyword(_keywordEnableLodTransDebug);
            }
            else
            {
                _material.DisableKeyword(_keywordEnableLodTransDebug);
            }

            if (_debugSettings.enableHeightDebug)
            {
                _material.EnableKeyword(_keywordEnableHeightDebug);
            }
            else
            {
                _material.DisableKeyword(_keywordEnableHeightDebug);
            }

            if (_debugSettings.enableCheckerDebug)
            {
                _material.EnableKeyword(_keywordEnableCheckerDebug);
            }
            else
            {
                _material.DisableKeyword(_keywordEnableCheckerDebug);
            }
        }

        public void SyncStat()
        {
            _indirectArgsBuffer.GetData(_statArgs);
            _terrainStatus.numPatches = _statArgs[1];
            _terrainStatus.maxNumPatches = Math.Max(_terrainStatus.maxNumPatches, _statArgs[1]);
        }
    }
}