using System;
using UnityEngine;

namespace GaiaTerrain
{
    /// <summary>
    /// 地形渲染开启/关闭功能设置
    /// </summary>
    [Serializable]
    public class TerrainFeatures
    {
        [Tooltip("值越大所需Node越多，当画面发生闪烁时表示NodeBuffer不够用了")]
        public float traverseDivideFactor = 1.0f;
        public bool enableFrustumCulling;
        public bool enableHiZCulling;
        public bool enableTwoPassHiZCulling;
        public bool enableNodeCulling;
        public bool enablePatchCulling;
        public bool enableLodSeamless = true;
        public bool enableTraverseOneDispatch;
        public bool enableReduceHiZMultiPass; 
    }
}