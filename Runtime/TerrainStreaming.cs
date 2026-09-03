using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GaiaTerrain
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainLayerDesc
    {
        public Vector2 TilingOffset;
        public Vector2 TilingSize;

        public static int GetSize()
        {
            return Marshal.SizeOf(typeof(TerrainLayerDesc));
        }
    }

    public class TerrainStreaming : IDisposable
    {
        private readonly Texture2D _minMaxHeightTexture;
        private readonly Texture2DArray _heightMapArray;
        private readonly Texture2DArray _splatMapArray;
        private readonly Texture2DArray _albedoMapArray;
        private readonly ComputeBuffer _sectorAssetDescBuffer;
        private readonly ComputeBuffer _terrainLayerDescBuffer;

        public TerrainStreaming(TerrainConfig terrainConfig, Texture2D[] heightMaps, Texture2D[] minMaxHeightMaps,
            Texture2D[] splatMaps, TerrainLayer[] terrainLayers, SectorAssetDesc[] sectorAssetDescArray)
        {
            _minMaxHeightTexture = LoadMinMaxHeightTexture(terrainConfig, minMaxHeightMaps);
            _minMaxHeightTexture.name = "MinMaxHeightMap";
            Debug.Log("Loaded MinMaxHeightMap");
            _heightMapArray = LoadTexture2DArray(heightMaps, true, TextureWrapMode.Clamp, FilterMode.Bilinear);
            _heightMapArray.name = "HeightMapArray";
            Debug.Log("Loaded HeightMapArray");
            _splatMapArray = LoadTexture2DArray(splatMaps, true, TextureWrapMode.Clamp, FilterMode.Bilinear);
            _splatMapArray.name = "SplatMapArray";
            Debug.Log("Loaded SplatMapArray");
            _albedoMapArray = LoadTexture2DArray(terrainLayers.Select(it => it.diffuseTexture).ToArray(), true,
                TextureWrapMode.Repeat, FilterMode.Bilinear, true);
            _albedoMapArray.name = "AlbedoMapArray";
            Debug.Log("Loaded AlbedoMapArray");
            _sectorAssetDescBuffer = CreateSectorAssetDescBuffer(sectorAssetDescArray);
            _sectorAssetDescBuffer.name = "SectorAssetDescBuffer";
            _terrainLayerDescBuffer = CreateTerrainLayerDescBuffer(terrainLayers, terrainConfig.SectorSize);
            _terrainLayerDescBuffer.name = "TerrainLayerDescBuffer";
        }

        public Texture2D GetMinMaxHeightTexture()
        {
            return _minMaxHeightTexture;
        }

        public Texture2DArray GetHeightMapArray()
        {
            return _heightMapArray;
        }

        public Texture2DArray GetSplatMapArray()
        {
            return _splatMapArray;
        }

        public Texture2DArray GetAlbedoMapArray()
        {
            return _albedoMapArray;
        }

        public ComputeBuffer GetSectorAssetDescBuffer()
        {
            return _sectorAssetDescBuffer;
        }

        public ComputeBuffer GetTerrainLayerDescBuffer()
        {
            return _terrainLayerDescBuffer;
        }

        public void Dispose()
        {
            _sectorAssetDescBuffer.Dispose();
            _terrainLayerDescBuffer.Dispose();
        }

        // private static Texture2D GenerateQuadTreeTexture(TerrainConfig terrainConfig)
        // {
        //     var lod0QuadTreeTextureSize = terrainConfig.LodNodeCountArray[0];
        //     var quadTreeTexture = new Texture2D(lod0QuadTreeTextureSize.x, lod0QuadTreeTextureSize.y,
        //         TextureFormat.R16, terrainConfig.LodCount, true);
        //     var size = lod0QuadTreeTextureSize;
        //     var nodeIdx = 0;
        //     for (var lod = terrainConfig.MinLod; lod <= terrainConfig.MaxLod; lod++)
        //     {
        //         for (var y = 0; y < size.y; y++)
        //         {
        //             for (var x = 0; x < size.x; x++)
        //             {
        //                 var color = new Color(nodeIdx * 1.0f / ushort.MaxValue, 0, 0, 0);
        //                 quadTreeTexture.SetPixel(x, y, color, lod);
        //                 nodeIdx++;
        //             }
        //         }
        //
        //         size /= 2;
        //     }
        //
        //     quadTreeTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        //     return quadTreeTexture;
        // }

        // private static ComputeBuffer GenerateNodeDescriptionBuffer(TerrainConfig terrainConfig,
        //     Texture2D[] minMaxHeightMaps)
        // {
        //     var nodeDescriptionBufferSize = terrainConfig.TotalNodeCount;
        //     var nodeDescriptionBuffer = new ComputeBuffer(nodeDescriptionBufferSize, NodeDescription.GetSize());
        //     var nodeDescriptions = new NodeDescription[nodeDescriptionBufferSize];
        //
        //     var idx = 0;
        //     var worldSizeY = terrainConfig.WorldSize.y;
        //     for (var lod = terrainConfig.MinLod; lod <= terrainConfig.MaxLod; lod++)
        //     {
        //         var nodeCount = terrainConfig.LodNodeCountArray[lod];
        //         var minMaxHeightMap = minMaxHeightMaps[lod + 3];
        //         for (var y = 0; y < nodeCount.y; ++y)
        //         {
        //             for (var x = 0; x < nodeCount.x; ++x)
        //             {
        //                 var minHeight = minMaxHeightMap.GetPixel(x, y).r * worldSizeY;
        //                 var maxHeight = minMaxHeightMap.GetPixel(x, y).g * worldSizeY;
        //                 nodeDescriptions[idx++] = new NodeDescription
        //                 {
        //                     MinHeight = minHeight,
        //                     MaxHeight = maxHeight,
        //                     Lod = lod
        //                 };
        //             }
        //         }
        //     }
        //
        //     nodeDescriptionBuffer.SetData(nodeDescriptions);
        //     return nodeDescriptionBuffer;
        // }

        private static Texture2D LoadMinMaxHeightTexture(TerrainConfig terrainConfig, Texture2D[] minMaxHeightMaps)
        {
            var mip0MinMaxHeightMap = minMaxHeightMaps[0];
            var minMaxHeightMap = new Texture2D(mip0MinMaxHeightMap.width, mip0MinMaxHeightMap.height,
                mip0MinMaxHeightMap.format, true, true)
            {
                filterMode = mip0MinMaxHeightMap.filterMode,
                wrapMode = mip0MinMaxHeightMap.wrapMode
            };

            for (var lod = terrainConfig.MinLod; lod <= terrainConfig.MaxLod + TerrainConfig.NumExtraLods; lod++)
            {
                var texture = minMaxHeightMaps[lod];
                Graphics.CopyTexture(texture, 0, 0, minMaxHeightMap, 0, lod);
            }

            minMaxHeightMap.Apply(false, true);
            return minMaxHeightMap;
        }

        private static Texture2DArray LoadTexture2DArray(Texture2D[] textures, bool makeNoLongerReadable,
            TextureWrapMode wrapMode, FilterMode filterMode, bool mipChain = false)
        {
            if (textures == null || textures.Length == 0)
            {
                return null;
            }

            var firstTexture = textures[0];
            var texture2DArray = new Texture2DArray(firstTexture.width, firstTexture.height, textures.Length,
                firstTexture.format, mipChain)
            {
                wrapMode = wrapMode,
                filterMode = filterMode
            };
            for (var i = 0; i < textures.Length; i++)
            {
                if (mipChain)
                {
                    Graphics.CopyTexture(textures[i], 0, texture2DArray, i);
                }
                else
                {
                    Graphics.CopyTexture(textures[i], 0, 0, texture2DArray, i, 0);
                }
            }

            texture2DArray.Apply(updateMipmaps: false, makeNoLongerReadable);
            return texture2DArray;
        }

        private static ComputeBuffer CreateSectorAssetDescBuffer(SectorAssetDesc[] sectorAssetDescArray)
        {
            var sectorAssetDescBuffer = new ComputeBuffer(sectorAssetDescArray.Length, SectorAssetDesc.GetSize());
            sectorAssetDescBuffer.SetData(sectorAssetDescArray);
            return sectorAssetDescBuffer;
        }

        private static ComputeBuffer CreateTerrainLayerDescBuffer(TerrainLayer[] terrainLayers, Vector3 sectorSize)
        {
            var terrainLayerDescArray = new TerrainLayerDesc[terrainLayers.Length];
            for (var i = 0; i < terrainLayers.Length; i++)
            {
                var terrainLayer = terrainLayers[i];
                terrainLayerDescArray[i] = new TerrainLayerDesc
                {
                    TilingOffset = terrainLayer.tileOffset,
                    TilingSize = new Vector2(
                        sectorSize.x / terrainLayer.tileSize.x,
                        sectorSize.z / terrainLayer.tileSize.y
                    )
                };
            }

            var terrainLayerDescBuffer = new ComputeBuffer(terrainLayers.Length, TerrainLayerDesc.GetSize());
            terrainLayerDescBuffer.SetData(terrainLayerDescArray);
            return terrainLayerDescBuffer;
        }
    }
}