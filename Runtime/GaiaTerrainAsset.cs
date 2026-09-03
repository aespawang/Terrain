using UnityEngine;

namespace GaiaTerrain
{
    [CreateAssetMenu(menuName = "GaiaTerrain/GaiaTerrainAsset")]
    public class GaiaTerrainAsset : ScriptableObject
    {
        // ============================ USER DEFINED ============================
        /// <summary>
        /// A world is divided into (sectorGridSize.x * sectorGridSize.y) sectors
        /// The size of each sector is (sectorSize.x * sectorSize.y * sectorSize.z) m^3
        /// </summary>
        public Vector2Int sectorGridSize = new(5, 5);

        public Vector3 sectorSize = new(2048f, 2048f, 2048f);
        public int sectorHeightMapResolution = 4096;

        public TerrainData[] terrainDataArray;
        public Texture2D[] heightMaps;
        public Texture2D[] minMaxHeightMaps;
        public Texture2D[] splatMaps;
        public TerrainLayer[] terrainLayers;
        public SectorAssetDesc[] sectorAssetDescArray;
        
        public ComputeShader terrainComputeShader;
        public ComputeShader hizMipmapGenComputeShader;
        public Mesh planeMesh;
        public Mesh cubeMesh;
    }
}