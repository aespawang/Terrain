using UnityEngine;

namespace GaiaTerrain
{
    public class GaiaTerrain : MonoBehaviour
    {
        [SerializeField] private GaiaTerrainAsset terrainAsset;
        [SerializeField] private TerrainStatus terrainStatus;

        private void OnEnable()
        {
            terrainStatus.Reset();
        }

        public GaiaTerrainAsset GetTerrainAsset()
        {
            return terrainAsset;
        }

        public TerrainStatus GetTerrainStatus()
        {
            return terrainStatus;
        }
    }
}