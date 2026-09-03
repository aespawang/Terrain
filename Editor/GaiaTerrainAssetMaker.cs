using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Directory = UnityEngine.Windows.Directory;
using File = UnityEngine.Windows.File;

namespace GaiaTerrain.Editor
{
    public class GaiaTerrainAssetMaker : EditorWindow
    {
        private GUIStyle _paddingBox;
        private Vector2 _scroll;

        private string _exportPath = "Assets/GaiaTerrainAsset/";
        private TerrainData[] _terrainDataArray;
        private Vector2Int _sectorGridSize = new(4, 4);
        private Vector3 _sectorSize = new(1000, 800, 1000);
        private int _sectorHeightMapResolution = 2048;
        private GaiaTerrainAsset _terrainAsset;

        [MenuItem("GaiaTerrain/GaiaTerrainAssetMaker")]
        private static void CreateWindow()
        {
            GetWindow<GaiaTerrainAssetMaker>(nameof(GaiaTerrainAssetMaker));
        }

        public void OnEnable()
        {
            _paddingBox = new GUIStyle
            {
                padding = new RectOffset(15, 15, 15, 15)
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(_paddingBox);

            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                _exportPath = EditorGUILayout.TextField("Export Path", _exportPath);
                _sectorGridSize = EditorGUILayout.Vector2IntField("Sector Grid Size", _sectorGridSize);
                _sectorSize = EditorGUILayout.Vector3Field("Sector Size", _sectorSize);
                _sectorHeightMapResolution =
                    EditorGUILayout.IntField("Sector Height MapResolution", _sectorHeightMapResolution);
                EditorGUILayout.EndVertical();
            }

            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Selected Terrain Data"))
                {
                    _terrainDataArray = Selection.GetFiltered<TerrainData>(SelectionMode.Assets);
                }

                if (GUILayout.Button("Sort By Name"))
                {
                    _terrainDataArray = _terrainDataArray.OrderBy(it => it.name).ToArray();
                }

                if (GUILayout.Button("Remove End With 1"))
                {
                    _terrainDataArray = _terrainDataArray.Where(it => !it.name.EndsWith(" 1")).ToArray();
                }

                if (GUILayout.Button("Transpose"))
                {
                    var tmpTerrainDataArray = new TerrainData[_terrainDataArray.Length];
                    for (var y = 0; y < _sectorGridSize.y; y++)
                    {
                        for (var x = 0; x < _sectorGridSize.x; x++)
                        {
                            tmpTerrainDataArray[y * _sectorGridSize.x + x] =
                                _terrainDataArray[x * _sectorGridSize.y + y];
                        }
                    }

                    _terrainDataArray = tmpTerrainDataArray;
                }

                if (GUILayout.Button("Remove ALL"))
                {
                    _terrainDataArray = Array.Empty<TerrainData>();
                }

                EditorGUILayout.EndHorizontal();
            }

            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Make TerrainAsset"))
                {
                    MakeTerrainAsset();
                    ApplyImportSettings();
                }

                _terrainAsset =
                    (GaiaTerrainAsset)EditorGUILayout.ObjectField(_terrainAsset, typeof(GaiaTerrainAsset), false);
                EditorGUILayout.EndHorizontal();
            }

            if (_terrainDataArray != null && _terrainDataArray.Length > 0)
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                for (var i = 0; i < _terrainDataArray.Length; ++i)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.ObjectField($"TerrainData[{i}]", _terrainDataArray[i], typeof(TerrainData), false);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private bool Check()
        {
            if (string.IsNullOrEmpty(_exportPath))
            {
                Debug.LogWarning("_exportPath is null or empty");
                return false;
            }

            if (!TerrainConfig.CheckValidate(_sectorGridSize, _sectorSize, _sectorHeightMapResolution))
            {
                return false;
            }

            if (_terrainDataArray == null || _terrainDataArray.Length != _sectorGridSize.x * _sectorGridSize.y)
            {
                Debug.LogWarning("_terrainDataArray.Length != _sectorGridSize.x * _sectorGridSize.y");
                return false;
            }

            return true;
        }

        private void MakeTerrainAsset()
        {
            if (_terrainDataArray == null || _terrainDataArray.Length == 0)
            {
                _terrainDataArray = _terrainAsset.terrainDataArray;
            }

            if (!Check()) return;

            if (!Directory.Exists(_exportPath))
            {
                Directory.CreateDirectory(_exportPath);
            }

            if (_terrainAsset == null)
            {
                _terrainAsset = CreateInstance<GaiaTerrainAsset>();
                AssetDatabase.CreateAsset(_terrainAsset, Path.Combine(_exportPath, "GaiaTerrainAsset.asset"));
            }

            _terrainAsset.sectorGridSize = _sectorGridSize;
            _terrainAsset.sectorSize = _sectorSize;
            _terrainAsset.sectorHeightMapResolution = _sectorHeightMapResolution;

            _terrainAsset.terrainDataArray = _terrainDataArray;
            AttachExtraAssets();

            ExportHeightMaps();
            _terrainAsset.minMaxHeightMaps = MinMaxHeightMapGenerator.Generate(_sectorGridSize, _sectorSize,
                _sectorHeightMapResolution,
                _terrainDataArray, _exportPath);

            ExportSectorAssetDescArray();

            EditorUtility.SetDirty(_terrainAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private void AttachExtraAssets()
        {
            const string packageDir = "Packages/com.gaia.gaia-terrain";
            _terrainAsset.planeMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(packageDir, "Meshes/Plane16.mesh"));
            _terrainAsset.cubeMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(packageDir, "Meshes/Cube.mesh"));
            _terrainAsset.terrainComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(packageDir, "Shaders/Terrain.compute"));
            _terrainAsset.hizMipmapGenComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(packageDir, "Shaders/HiZMipmapGen.compute"));
        }

        private void ExportHeightMaps()
        {
            var exportPath = Path.Combine(_exportPath, "HeightMaps");
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            var heightMaps = new Texture2D[_terrainDataArray.Length];

            var resolution = _sectorHeightMapResolution + 1;
            for (var i = 0; i < _terrainDataArray.Length; i++)
            {
                var terrainData = _terrainDataArray[i];
                var tex2d = new Texture2D(resolution, resolution, TextureFormat.RFloat, false);
                var heights = terrainData.GetHeights(0, 0, resolution, resolution);
                for (var y = 0; y < resolution; y++)
                {
                    for (var x = 0; x < resolution; x++)
                    {
                        tex2d.SetPixel(x, y, new Color(heights[y, x], 0, 0, 0));
                    }
                }

                tex2d.Apply(false, false);

                var bytes = tex2d.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
                var path = Path.Combine(exportPath, $"HeightMap_{i / _sectorGridSize.x}_{i % _sectorGridSize.x}.exr");
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
                heightMaps[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                Debug.Log($"[{i + 1} / {_terrainDataArray.Length}] Export HeightMap: {path}");
            }

            _terrainAsset.heightMaps = heightMaps;
        }

        private void ExportSectorAssetDescArray()
        {
            var splatMaps = new List<Texture2D>();
            var terrainLayers = new List<TerrainLayer>();
            var terrainLayerToIdx = new Dictionary<TerrainLayer, int>();
            var sectorAssetDescArray = new List<SectorAssetDesc>();
            foreach (var terrainData in _terrainDataArray)
            {
                var sectorAssetDesc = new SectorAssetDesc
                {
                    splatMapIndices = new int2(-1, -1),
                    layerPack0Indices = new int4(-1, -1, -1, -1),
                    layerPack1Indices = new int4(-1, -1, -1, -1)
                };
                for (var i = 0; i < Mathf.Min(terrainData.alphamapTextures.Length, 2); i++)
                {
                    sectorAssetDesc.splatMapIndices[i] = splatMaps.Count;
                    splatMaps.Add(terrainData.alphamapTextures[i]);
                }

                for (var i = 0; i < Mathf.Min(terrainData.terrainLayers.Length, 8); i++)
                {
                    var terrainLayer = terrainData.terrainLayers[i];
                    if (!terrainLayerToIdx.ContainsKey(terrainLayer))
                    {
                        terrainLayerToIdx.Add(terrainLayer, terrainLayers.Count);
                        terrainLayers.Add(terrainLayer);
                    }

                    var idx = terrainLayerToIdx[terrainLayer];
                    if (i < 4)
                    {
                        sectorAssetDesc.layerPack0Indices[i % 4] = idx;
                    }
                    else
                    {
                        sectorAssetDesc.layerPack1Indices[i % 4] = idx;
                    }
                }

                sectorAssetDescArray.Add(sectorAssetDesc);
            }

            _terrainAsset.splatMaps = splatMaps.ToArray();
            _terrainAsset.terrainLayers = terrainLayers.ToArray();
            _terrainAsset.sectorAssetDescArray = sectorAssetDescArray.ToArray();
        }

        private void ApplyImportSettings()
        {
            ApplyHeightMapImportSettings();
            ApplyMinMaxHeightMapImportSettings();
            AssetDatabase.Refresh();
        }

        private void ApplyHeightMapImportSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Path.Combine(_exportPath, "HeightMaps") });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (!importer)
                {
                    continue;
                }

                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.sRGBTexture = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.isReadable = false;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Point;

                var defaultSettings = importer.GetPlatformTextureSettings("DefaultTexturePlatform");
                defaultSettings.overridden = true;
                defaultSettings.maxTextureSize = 4096;
                defaultSettings.format = TextureImporterFormat.RFloat;
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(defaultSettings);

                importer.SaveAndReimport();
            }
        }

        private void ApplyMinMaxHeightMapImportSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Path.Combine(_exportPath, "MinMaxHeightMaps") });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (!importer)
                {
                    continue;
                }

                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.sRGBTexture = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.isReadable = false;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;

                var defaultSettings = importer.GetPlatformTextureSettings("DefaultTexturePlatform");
                defaultSettings.overridden = true;
                defaultSettings.maxTextureSize = 2048;
                defaultSettings.format = TextureImporterFormat.RGFloat;
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(defaultSettings);

                importer.SaveAndReimport();
            }
        }
    }
}