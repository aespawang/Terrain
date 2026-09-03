using System;

namespace GaiaTerrain
{
    [Serializable]
    public class TerrainStatus
    {
        public bool enableStat;
        
        public uint numNodes;
        public uint maxNumNodes;
        public uint numPatches;
        public uint maxNumPatches;
        
        public uint  numNodeCulledByFrustum;
        public uint  numNodeCulledByHiZ;
        public float nodeFrustumCullingRate;
        public float nodeHiZCullingRate;
        
        public uint  numPatchCulledByFrustum;
        public uint  numPatchCulledByHiZ;
        public float patchFrustumCullingRate;
        public float patchHiZCullingRate;
        
        public void Reset()
        {
            numNodes = 0;
            maxNumNodes = 0;
            numPatches = 0;
            maxNumPatches = 0;
            numNodeCulledByFrustum = 0;
            numNodeCulledByHiZ = 0;
            nodeFrustumCullingRate = 0.0f;
            nodeHiZCullingRate = 0.0f;
            numPatchCulledByFrustum = 0;
            numPatchCulledByHiZ = 0;
            patchFrustumCullingRate = 0.0f;
            patchHiZCullingRate = 0.0f;
        }
        
        public void UpdateRates()
        {
            var total = numNodes + numNodeCulledByFrustum + numNodeCulledByHiZ;
            if (total > 0)
            {
                nodeFrustumCullingRate = (float)numNodeCulledByFrustum / total;
                nodeHiZCullingRate = (float)numNodeCulledByHiZ / total;
            }
            else
            {
                nodeFrustumCullingRate = 0.0f;
                nodeHiZCullingRate = 0.0f;
            }
            
            total = numPatches + numPatchCulledByFrustum + numPatchCulledByHiZ;
            if (total > 0)
            {
                patchFrustumCullingRate = (float)numPatchCulledByFrustum / total;
                patchHiZCullingRate = (float)numPatchCulledByHiZ / total;
            }
            else
            {
                patchFrustumCullingRate = 0.0f;
                patchHiZCullingRate = 0.0f;
            }
        }
    }
}