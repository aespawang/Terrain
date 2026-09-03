using UnityEngine;

namespace GaiaTerrain
{
    public class TerrainConfig
    {
        public readonly Vector2Int SectorGridSize;

        public readonly Vector3 SectorSize;

        public readonly int SectorHeightMapResolution;

        public readonly Vector3 WorldSize;

        public readonly int LodCount;

        public readonly int TotalNodeCount;

        public readonly int MinLod;

        public readonly int MaxLod;

        /// <summary>
        /// 整个world每个lod级别node的个数
        /// </summary>
        public readonly Vector2Int[] LodNodeCountArray;

        /// <summary>
        /// 一个sector中每个lod级别node的个数 lod min -> lod max
        /// </summary>
        public readonly int[] LodNodeCountInSectorArray;

        /// <summary>
        /// 不同lod级别node在世界空间中的大小（米） lod min -> lod max
        /// </summary>
        public readonly Vector2[] LodNodeSizeArray;

        public static bool CheckValidate(Vector2Int sectorGridSize, Vector3 sectorSize, int sectorHeightMapResolution)
        {
            if (sectorGridSize.x <= 0 || sectorGridSize.y <= 0)
            {
                Debug.LogWarning($"SectorGridSize is invalid: {sectorGridSize}");
                return false;
            }

            if (sectorSize.x <= 0 || sectorSize.y <= 0 || sectorSize.z <= 0)
            {
                Debug.LogWarning("SectorSize is invalid");
                return false;
            }

            if (!Mathf.IsPowerOfTwo(sectorHeightMapResolution) || sectorHeightMapResolution < NodeHeightMapResolution)
            {
                Debug.LogWarning($"SectorHeightMapResolution must be the power of 2 and >= {NodeHeightMapResolution}");
                return false;
            }

            return true;
        }

        public static TerrainConfig Create(Vector2Int sectorGridSize, Vector3 sectorSize, int sectorHeightMapResolution)
        {
            return CheckValidate(sectorGridSize, sectorSize, sectorHeightMapResolution)
                ? new TerrainConfig(sectorGridSize, sectorSize, sectorHeightMapResolution)
                : null;
        }

        private TerrainConfig(Vector2Int sectorGridSize, Vector3 sectorSize, int sectorHeightMapResolution)
        {
            SectorGridSize = sectorGridSize;
            SectorSize = sectorSize;
            SectorHeightMapResolution = sectorHeightMapResolution;
            WorldSize = new Vector3(sectorSize.x * sectorGridSize.x, sectorSize.y, sectorSize.z * sectorGridSize.y);
            LodCount = CalcLodCount(sectorHeightMapResolution);
            MinLod = 0;
            MaxLod = LodCount - 1;

            var totalCountInSector = 0;
            var lodNodeCountInSectorArray = new int[LodCount];
            var lodNodeCountArray = new Vector2Int[LodCount];
            var lodNodeSizeArray = new Vector2[LodCount];
            for (var i = 0; i < LodCount; ++i)
            {
                var nodeCount = 1 << (LodCount - i - 1);
                totalCountInSector += nodeCount * nodeCount;
                lodNodeCountInSectorArray[i] = nodeCount;
                lodNodeCountArray[i] = new Vector2Int(
                    nodeCount * sectorGridSize.x,
                    nodeCount * sectorGridSize.y);
                lodNodeSizeArray[i] = new Vector2(
                    sectorSize.x / nodeCount,
                    sectorSize.z / nodeCount);
            }

            TotalNodeCount = totalCountInSector * sectorGridSize.x * sectorGridSize.y;
            LodNodeCountInSectorArray = lodNodeCountInSectorArray;
            LodNodeCountArray = lodNodeCountArray;
            LodNodeSizeArray = lodNodeSizeArray;
        }

        private static int CalcLodCount(int sectorHeightMapResolution)
        {
            return (int)Mathf.Log(sectorHeightMapResolution * 1.0f / NodeHeightMapResolution, 2) + 1;
        }

        /// <summary>
        /// 一个node包含8x8个patch
        /// </summary>
        public const int NumPatchesPerNode = 64;

        public const int PatchHeightMapResolution = 16;

        public const int NodeHeightMapResolution = 128;

        public const int NumExtraLods = 3;

        /// <summary>
        /// ping-pong buffer size
        /// </summary>
        public const int TempNodeBufferSize = 300;

        /// <summary>
        /// 每帧遍历，剔除，渲染的最多node数量
        /// 设置得过小会出现node闪动的情况
        /// </summary>
        public const int MaxNodeBufferSize = 300;

        /// <summary>
        /// node包围盒上下会扩展的长度（米）
        /// </summary>
        public const float BoundsHeightRedundancy = 5.0f;
    }
}