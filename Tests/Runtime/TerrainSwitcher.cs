using UnityEngine;

namespace GaiaTerrain.Tests
{
    public class TerrainSwitcher : MonoBehaviour
    {
        [SerializeField] private bool selectGaiaTerrain = true;
        [SerializeField] private GameObject gaiaTerrain;
        [SerializeField] private GameObject unityTerrain;
        private bool _lastSelectGaiaTerrain;

        private void Update()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (Input.GetKeyDown(KeyCode.C))
            {
                selectGaiaTerrain = !selectGaiaTerrain;
            }
#endif
            
            if (_lastSelectGaiaTerrain == selectGaiaTerrain) return;

            SetGaiaTerrain(selectGaiaTerrain);
            SetUnityTerrain(!selectGaiaTerrain);
            _lastSelectGaiaTerrain = selectGaiaTerrain;
        }
        
        public void VirtualSwitchInput(bool virtualSwitchState)
        {
            selectGaiaTerrain = !selectGaiaTerrain;
        }

        private void SetGaiaTerrain(bool selected)
        {
            if (!gaiaTerrain) return;
            var terrain = gaiaTerrain.GetComponent<GaiaTerrain>();
            if (!terrain) return;
            
            terrain.enabled = selected;
        }

        private void SetUnityTerrain(bool selected)
        {
            if (!unityTerrain) return;
            var terrains = unityTerrain.GetComponentsInChildren<Terrain>();
            if (terrains == null || terrains.Length == 0) return;
            
            foreach (var terrain in terrains)
            {
                terrain.enabled = selected;
            }
        }
    }
}