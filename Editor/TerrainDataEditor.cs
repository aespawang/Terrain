using UnityEditor;
using UnityEngine;

namespace GaiaTerrain.Editor
{
    [CustomEditor(typeof(TerrainData))]
    public class TerrainDataEditor : UnityEditor.Editor
    {
        private GUIStyle _paddingBox;
        private bool _showAlphaTextures = true;
        private bool _showTerrainLayers = true;

        private void OnEnable()
        {
            _paddingBox = new GUIStyle
            {
                padding = new RectOffset(15, 15, 15, 15)
            };
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical(_paddingBox);
            base.OnInspectorGUI();
            var terrainData = (TerrainData)target;
            EditorGUILayout.TextField("Name", terrainData.name);
            EditorGUILayout.Vector3Field("Terrain Size", terrainData.size);
            EditorGUILayout.BoundsField("Terrain Bounds", terrainData.bounds);
            EditorGUILayout.IntField("Base Map Resolution", terrainData.baseMapResolution);

            EditorGUILayout.LabelField("Height Map", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Height Map Resolution", terrainData.heightmapResolution);
            EditorGUILayout.Vector3Field("Height Map Scale", terrainData.heightmapScale);
            EditorGUILayout.ObjectField("Height Map Texture", terrainData.heightmapTexture, typeof(Texture), false);

            EditorGUILayout.LabelField("Holes Map", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Holes Map Resolution", terrainData.holesResolution);
            EditorGUILayout.Toggle("Enable Holes Texture Compression", terrainData.enableHolesTextureCompression);
            EditorGUILayout.ObjectField("Holes Texture", terrainData.holesTexture, typeof(Texture), false);

            EditorGUILayout.LabelField("Alpha Map", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Alpha Map Layers", terrainData.alphamapLayers);
            EditorGUILayout.IntField("Alpha Map Resolution", terrainData.alphamapResolution);
            EditorGUILayout.IntField("Alpha Map Width", terrainData.alphamapWidth);
            EditorGUILayout.IntField("Alpha Map Height", terrainData.alphamapHeight);
            EditorGUILayout.IntField("Alpha Map Texture Count", terrainData.alphamapTextureCount);
            _showAlphaTextures = EditorGUILayout.Foldout(_showAlphaTextures, "Alpha Map Textures");
            if (_showAlphaTextures)
            {
                EditorGUI.indentLevel++;
                for (var i = 0; i < terrainData.alphamapTextures.Length; i++)
                {
                    EditorGUILayout.ObjectField($"[{i}]", terrainData.alphamapTextures[i], typeof(Texture), false);
                }

                EditorGUI.indentLevel--;
            }

            _showTerrainLayers = EditorGUILayout.Foldout(_showTerrainLayers, "Terrain Layers");
            if (_showTerrainLayers)
            {
                EditorGUI.indentLevel++;
                for (var i = 0; i < terrainData.terrainLayers.Length; i++)
                {
                    EditorGUILayout.ObjectField($"Terrain Layer [{i}]", terrainData.terrainLayers[i],
                        typeof(TerrainLayer), false);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }
}