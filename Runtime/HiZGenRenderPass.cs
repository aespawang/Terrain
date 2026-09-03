using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GaiaTerrain
{
    public class HiZGenRenderPass : ScriptableRenderPass
    {
        private readonly TerrainRuntimeData _terrainRuntimeData;
        private readonly TerrainFeatures _terrainFeatures;
        
        public HiZGenRenderPass(TerrainRuntimeData terrainRuntimeData, TerrainFeatures terrainFeatures)
        {
            _terrainRuntimeData = terrainRuntimeData;
            _terrainFeatures = terrainFeatures;
        }
            
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var hizMipmap = HiZMipmapUtility.GetOrCreateHiZMipmap(renderingData.cameraData.camera, ref _terrainRuntimeData.HiZMipmap);
            var cmd = CommandBufferPool.Get("HiZ Mipmap Generate Pass");
            cmd.Clear();
            
            HiZMipmapUtility.GenerateHiZMipmapPassCommand(cmd, renderingData, hizMipmap, _terrainFeatures.enableReduceHiZMultiPass);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}