using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GaiaTerrain
{
    public class GaiaTerrainRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private TerrainFeatures terrainFeatures;
        [SerializeField] private DebugSettings debugSettings;

        private readonly TerrainRuntimeData _terrainRuntimeData = new();
        private GaiaTerrain _terrain;
        private GaiaTerrainAsset _terrainAsset;
        private TerrainRenderPass _terrainRenderPass;
        private HiZGenRenderPass _hiZGenRenderPass;

        public override void Create()
        {
            if (!Application.isPlaying) return;

            _terrain = FindAnyObjectByType<GaiaTerrain>();
            if (!_terrain || !_terrain.enabled)
            {
                return;
            }

            var terrainAsset = _terrain.GetTerrainAsset();

            if (!terrainAsset)
            {
                Debug.LogWarning("GaiaTerrainAsset is null!");
                return;
            }

            var terrainConfig = TerrainConfig.Create(
                terrainAsset.sectorGridSize,
                terrainAsset.sectorSize,
                terrainAsset.sectorHeightMapResolution);

            if (terrainConfig == null)
            {
                return;
            }

            if (!ValidateTerrainAsset(terrainAsset, terrainConfig))
            {
                return;
            }

            _terrainRuntimeData.InitDummyHiZ();
            HiZMipmapUtility.Initialize(terrainAsset.hizMipmapGenComputeShader);

            if (_terrainRenderPass == null)
            {
                var terrainStreaming = new TerrainStreaming(terrainConfig,
                    terrainAsset.heightMaps,
                    terrainAsset.minMaxHeightMaps,
                    terrainAsset.splatMaps,
                    terrainAsset.terrainLayers,
                    terrainAsset.sectorAssetDescArray);

                _terrainRenderPass = new TerrainRenderPass(terrainConfig,
                    terrainAsset.terrainComputeShader, terrainAsset.planeMesh, terrainAsset.cubeMesh, terrainStreaming,
                    terrainFeatures, _terrainRuntimeData, debugSettings, _terrain.GetTerrainStatus())
                {
                    // renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
                    renderPassEvent = RenderPassEvent.AfterRenderingOpaques
                };
            }

            _hiZGenRenderPass ??= new HiZGenRenderPass(_terrainRuntimeData, terrainFeatures)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!Application.isPlaying) return;
            if (!_terrain || !_terrain.enabled) return;

            var isMainCamera = renderingData.cameraData.camera == Camera.main;
            if (!isMainCamera && !renderingData.cameraData.isSceneViewCamera) return;

            if (_terrainRenderPass != null) renderer.EnqueuePass(_terrainRenderPass);

            if (!terrainFeatures.enableHiZCulling || terrainFeatures.enableTwoPassHiZCulling || !isMainCamera) return;
            if (_hiZGenRenderPass != null) renderer.EnqueuePass(_hiZGenRenderPass);
        }

        protected override void Dispose(bool disposing)
        {
            _terrainRenderPass?.Dispose();
        }

        private static bool ValidateTerrainAsset(GaiaTerrainAsset terrainAsset, TerrainConfig terrainConfig)
        {
            if (!terrainAsset.terrainComputeShader)
            {
                Debug.LogWarning("terrain compute shader is null!");
                return false;
            }

            if (!terrainAsset.hizMipmapGenComputeShader)
            {
                Debug.LogWarning("naive hiz compute shader is null!");
                return false;
            }

            if (!terrainAsset.planeMesh)
            {
                Debug.LogWarning("plane mesh is null!");
                return false;
            }

            if (!terrainAsset.cubeMesh)
            {
                Debug.LogWarning("cube mesh is null!");
                return false;
            }

            if (terrainAsset.heightMaps == null || terrainAsset.heightMaps.Length !=
                terrainConfig.SectorGridSize.x * terrainConfig.SectorGridSize.y)
            {
                Debug.LogWarning(
                    $"terrainAsset.heightMaps.Length != {terrainConfig.SectorGridSize.x * terrainConfig.SectorGridSize.y}");
                return false;
            }

            foreach (var heightMap in terrainAsset.heightMaps)
            {
                if (heightMap.width != terrainConfig.SectorHeightMapResolution + 1 ||
                    heightMap.height != terrainConfig.SectorHeightMapResolution + 1)
                {
                    Debug.LogWarning(
                        $"The resolution of {heightMap.name} != {terrainConfig.SectorHeightMapResolution + 1}");
                    return false;
                }
            }

            if (terrainAsset.splatMaps == null || terrainAsset.splatMaps.Length == 0)
            {
                Debug.LogWarning("terrainAsset.splatMaps is null or empty!");
                return false;
            }

            if (terrainAsset.terrainLayers == null || terrainAsset.terrainLayers.Length == 0)
            {
                Debug.LogWarning("terrainAsset.terrainLayers is null or empty");
                return false;
            }

            if (terrainAsset.sectorAssetDescArray == null || terrainAsset.sectorAssetDescArray.Length !=
                terrainConfig.SectorGridSize.x * terrainConfig.SectorGridSize.y)
            {
                Debug.LogWarning(
                    $"terrainAsset.sectorAssetDescArray.Length != {terrainConfig.SectorGridSize.x * terrainConfig.SectorGridSize.y}");
                return false;
            }

            return true;
        }
    }
}