using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaiaTerrain.Editor
{
    public static class MinMaxHeightMapGenerator
    {
        public static Texture2D[] Generate(Vector2Int sectorGridSize, Vector3 sectorSize, int sectorHeightMapResolution, TerrainData[] terrainDataArray, string exportPath)
        {
            var terrainConfig = TerrainConfig.Create(sectorGridSize, sectorSize, sectorHeightMapResolution);
            if (terrainConfig == null)
            {
                return null;
            }
            
            var heightDataArray = LoadHeightData(terrainDataArray);
            if (heightDataArray == null)
            {
                return null;
            }

            var minMaxHeightTexture = CalcMip0MinMaxHeight(heightDataArray, terrainConfig);
            for (var lod = terrainConfig.MinLod + 1; lod <= terrainConfig.MaxLod + TerrainConfig.NumExtraLods; ++lod)
            {
                CalcMinMaxHeight(minMaxHeightTexture, lod);
            }

            minMaxHeightTexture.Apply(false, false);
            return SaveMinMaxHeightMaps(minMaxHeightTexture, terrainConfig, exportPath);
        }

        private static float[][,] LoadHeightData(TerrainData[] terrainDataArray)
        {
            if (terrainDataArray == null || terrainDataArray.Length == 0)
            {
                return null;
            }
            
            var width = terrainDataArray[0].heightmapTexture.width;
            var height = terrainDataArray[0].heightmapTexture.height;
            var numTextures = terrainDataArray.Length;
            var heightDataArray = new float[numTextures][,];
            for (var i = 0; i < numTextures; i++)
            {
                var terrainData = terrainDataArray[i];
                if (terrainData.heightmapTexture.width != width || terrainData.heightmapTexture.height != height)
                {
                    return null;
                }
                
                var heights = terrainData.GetHeights(0, 0, width, height);
                heightDataArray[i] = heights;
            }
            
            return heightDataArray;
        }

        private static Texture2D CalcMip0MinMaxHeight(float[][,] heightDataArray, TerrainConfig terrainConfig)
        {
            var sectorGridSize = terrainConfig.SectorGridSize;
            const int patchHeightMapResolution = TerrainConfig.PatchHeightMapResolution;
            var patchCountInSector = terrainConfig.SectorHeightMapResolution / TerrainConfig.PatchHeightMapResolution;
            var minMaxHeightTexture = new Texture2D(
                patchCountInSector * terrainConfig.SectorGridSize.x,
                patchCountInSector * terrainConfig.SectorGridSize.y,
                TextureFormat.RGFloat, true, true);

            for (var patchY = 0; patchY < minMaxHeightTexture.height; patchY++)
            {
                for (var patchX = 0; patchX < minMaxHeightTexture.width; patchX++)
                {
                    var sectorY = patchY / patchCountInSector;
                    var sectorX = patchX / patchCountInSector;
                    var heightData = heightDataArray[sectorY * sectorGridSize.x + sectorX];
                    var startY = (patchY - sectorY * patchCountInSector) * patchHeightMapResolution;
                    var startX = (patchX - sectorX * patchCountInSector) * patchHeightMapResolution;
                    var minHeight = heightData[startY, startX];
                    var maxHeight = minHeight;
                    for (var y = 0; y <= patchHeightMapResolution; y++)
                    {
                        for (var x = 0; x <= patchHeightMapResolution; x++)
                        {
                            var height = heightData[startY + y, startX + x];
                            minHeight = Mathf.Min(minHeight, height);
                            maxHeight = Mathf.Max(maxHeight, height);
                        }
                    }

                    minMaxHeightTexture.SetPixel(patchX, patchY, new Color(minHeight, maxHeight, 0f, 0f), 0);
                }
            }

            return minMaxHeightTexture;
        }

        private static void CalcMinMaxHeight(Texture2D minMaxHeightTexture, int lod)
        {
            var width = minMaxHeightTexture.width >> lod;
            var height = minMaxHeightTexture.height >> lod;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var h0 = minMaxHeightTexture.GetPixel(x * 2 + 0, y * 2 + 0, lod - 1);
                    var h1 = minMaxHeightTexture.GetPixel(x * 2 + 0, y * 2 + 1, lod - 1);
                    var h2 = minMaxHeightTexture.GetPixel(x * 2 + 1, y * 2 + 0, lod - 1);
                    var h3 = minMaxHeightTexture.GetPixel(x * 2 + 1, y * 2 + 1, lod - 1);
                    minMaxHeightTexture.SetPixel(x, y, new Color(
                        Mathf.Min(h0.r, Mathf.Min(h1.r, Mathf.Min(h2.r, h3.r))),
                        Mathf.Max(h0.g, Mathf.Max(h1.g, Mathf.Max(h2.g, h3.g))),
                        0f,
                        0f
                    ), lod);
                }
            }
        }

        private static Texture2D[] SaveMinMaxHeightMaps(Texture2D minMaxHeightTexture, TerrainConfig terrainConfig, string exportPath)
        {
            const string texturesDir = "MinMaxHeightMaps";
            var dirPath = Path.Combine(exportPath, texturesDir);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            
            var width = minMaxHeightTexture.width;
            var height = minMaxHeightTexture.height;
            var minMaxHeightMaps = new List<Texture2D>();
            var lodCount = terrainConfig.MaxLod + TerrainConfig.NumExtraLods - terrainConfig.MinLod + 1;
            for (var lod = terrainConfig.MinLod; lod <= terrainConfig.MaxLod + TerrainConfig.NumExtraLods; ++lod)
            {
                var tmpTexture = new Texture2D(width >> lod, height >> lod, TextureFormat.RGFloat, false, true);
                tmpTexture.SetPixels(minMaxHeightTexture.GetPixels(lod));
                tmpTexture.Apply();
                var bytes = tmpTexture.EncodeToEXR();
                var path = Path.Combine(dirPath, $"MinMaxHeightMap_lod{lod}.exr");
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                minMaxHeightMaps.Add(texture);
                Debug.Log($"[{lod + 1} / {lodCount}] Export MinMaxHeightMap: {path}");
            }

            return minMaxHeightMaps.ToArray();
        }
    }
}