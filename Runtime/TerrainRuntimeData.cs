using UnityEngine;

namespace GaiaTerrain
{
    public class TerrainRuntimeData
    {
        public RenderTexture HiZMipmap;

        public void InitDummyHiZ()
        {
            if (HiZMipmap != null) HiZMipmap.Release();
            HiZMipmap = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false
            };
            HiZMipmap.Create();

            RenderTexture.active = HiZMipmap;
            GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            RenderTexture.active = null;
        }
    }
}