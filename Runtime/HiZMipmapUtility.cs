using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GaiaTerrain
{
    public static class HiZMipmapUtility
    {
        private static LocalKeyword _keywordEnableMSAA;
        private static LocalKeyword _keywordEnableReduceHiZMultiPass;

        private static ComputeShader _computeShader;
        private static int _kernelIdxReduceHiZ;
        private static int _kernelIdxReduceHiZMultiPass;

        private static readonly int NameIdDepthTexMSAA = Shader.PropertyToID("_DepthTexMSAA");
        private static readonly int NameIdSampleCount = Shader.PropertyToID("_SampleCount");
        private static readonly int NameIdInTex = Shader.PropertyToID("_InTex");
        private static readonly int NameIdSrcMipIndex = Shader.PropertyToID("_SrcMipIndex");
        private static readonly int NameIdOutMip1 = Shader.PropertyToID("_OutMip1");
        private static readonly int NameIdOutMip2 = Shader.PropertyToID("_OutMip2");
        private static readonly int NameIdOutMip3 = Shader.PropertyToID("_OutMip3");
        private static readonly int NameIdOutMip4 = Shader.PropertyToID("_OutMip4");
        private static readonly int NameIdSrcDstTexSize = Shader.PropertyToID("_SrcDstTexSize");

        public static void Initialize(ComputeShader shader)
        {
            _computeShader = shader;
            _kernelIdxReduceHiZ = shader.FindKernel("ReduceHiZ");
            _kernelIdxReduceHiZMultiPass =  shader.FindKernel("ReduceHiZMultiPass");

            _keywordEnableMSAA = new LocalKeyword(_computeShader, "ENABLE_MSAA");
            _keywordEnableReduceHiZMultiPass = new LocalKeyword(_computeShader, "ENABLE_REDUCE_HIZ_MULTIPASS");
        }

        public static RenderTexture GetOrCreateHiZMipmap(Camera camera, ref RenderTexture hizMipmap)
        {
            var hizMip0Size = CalcHiZMapSize(camera);

            if (hizMipmap != null && hizMipmap.width == hizMip0Size.x && hizMipmap.height == hizMip0Size.y)
                return hizMipmap;
            if (hizMipmap != null)
            {
                hizMipmap.Release();
                hizMipmap = null;
            }

            var mipCount = (int)Mathf.Log(Mathf.Max(hizMip0Size.x, hizMip0Size.y), 2) + 1;
            var desc = new RenderTextureDescriptor(hizMip0Size.x, hizMip0Size.y, RenderTextureFormat.RFloat, 0,
                mipCount)
            {
                autoGenerateMips = false,
                useMipMap = mipCount > 1,
                enableRandomWrite = true
            };

            hizMipmap = new RenderTexture(desc);
            hizMipmap.Create();

            return hizMipmap;
        }

        public static void GenerateHiZMipmapPassCommand(CommandBuffer cmd, in RenderingData renderingData,
            RenderTexture hizMipmap, bool enableReduceHiZMultiPass)
        {
            var camera = renderingData.cameraData.camera;
            var depthTexture = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            bool useMSAA = renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1;

            var hizWidth = hizMipmap.width;
            var hizHeight = hizMipmap.height;

            var hizMipSizeArray = GenerateMipSizes(hizWidth, hizHeight);
            if (hizMipSizeArray == null)
            {
                Debug.LogError("Failed to generate hizMipSizeArray.");
                return;
            }

            cmd.BeginSample("HiZ Mipmap Generator Pass");
            _computeShader.GetKernelThreadGroupSizes(_kernelIdxReduceHiZ, out var threadX, out var threadY, out _);
            if (!enableReduceHiZMultiPass)
            {
                cmd.DisableKeyword(_computeShader, _keywordEnableReduceHiZMultiPass);
                if (useMSAA)
                {
                    int sampleCount = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
                    cmd.EnableKeyword(_computeShader, _keywordEnableMSAA);
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZ, NameIdDepthTexMSAA, depthTexture);
                    cmd.SetComputeIntParam(_computeShader, NameIdSampleCount, sampleCount);
                }
                else
                {
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZ, NameIdInTex, depthTexture);
                    cmd.SetComputeIntParam(_computeShader, NameIdSrcMipIndex, 0);
                }

                cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZ, NameIdOutMip1, hizMipmap, 0);
                cmd.SetComputeIntParams(_computeShader, NameIdSrcDstTexSize, camera.pixelWidth, camera.pixelHeight,
                    hizMipSizeArray[0].x, hizMipSizeArray[0].y);
                cmd.DispatchCompute(_computeShader, _kernelIdxReduceHiZ,
                    Mathf.CeilToInt(hizMipSizeArray[0].x * 1.0f / threadX),
                    Mathf.CeilToInt(hizMipSizeArray[0].y * 1.0f / threadY), 1);

                cmd.DisableKeyword(_computeShader, _keywordEnableMSAA);

                for (var i = 1; i < hizMipmap.mipmapCount; ++i)
                {
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZ, NameIdInTex, hizMipmap);
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZ, NameIdOutMip1, hizMipmap, i);
                    cmd.SetComputeIntParam(_computeShader, NameIdSrcMipIndex, i - 1);
                    cmd.SetComputeIntParams(_computeShader, NameIdSrcDstTexSize, hizMipSizeArray[i - 1].x,
                        hizMipSizeArray[i - 1].y, hizMipSizeArray[i].x, hizMipSizeArray[i].y);
                    cmd.DispatchCompute(_computeShader, _kernelIdxReduceHiZ,
                        Mathf.CeilToInt(hizMipSizeArray[i].x * 1.0f / threadX),
                        Mathf.CeilToInt(hizMipSizeArray[i].y * 1.0f / threadY), 1);
                }
            }
            else
            {
                cmd.EnableKeyword(_computeShader, _keywordEnableReduceHiZMultiPass);
                if (useMSAA)
                {
                    int sampleCount = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
                    cmd.EnableKeyword(_computeShader, _keywordEnableMSAA);
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdDepthTexMSAA,
                        depthTexture);
                    cmd.SetComputeIntParam(_computeShader, NameIdSampleCount, sampleCount);
                }
                else
                {
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdInTex, depthTexture);
                    cmd.SetComputeIntParam(_computeShader, NameIdSrcMipIndex, 0);
                }

                cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip1, hizMipmap, 0);
                cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip2, hizMipmap, 1);
                cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip3, hizMipmap, 2);
                cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip4, hizMipmap, 3);
                cmd.SetComputeIntParams(_computeShader, NameIdSrcDstTexSize, camera.pixelWidth, camera.pixelHeight,
                    hizWidth, hizHeight);
                cmd.DispatchCompute(_computeShader, _kernelIdxReduceHiZMultiPass,
                    Mathf.CeilToInt(hizWidth * 1.0f / threadX),
                    Mathf.CeilToInt(hizHeight * 1.0f / threadY), 1);
                
                cmd.DisableKeyword(_computeShader, _keywordEnableMSAA);
                
                for (var i = 4; i < hizMipmap.mipmapCount; i += 4)
                {
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdInTex, hizMipmap);
                    cmd.SetComputeIntParam(_computeShader, NameIdSrcMipIndex, i - 1);
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip1, hizMipmap, Math.Min(i, hizMipmap.mipmapCount - 1));
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip2, hizMipmap, Math.Min(i + 1, hizMipmap.mipmapCount - 1));
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip3, hizMipmap, Math.Min(i + 2, hizMipmap.mipmapCount - 1));
                    cmd.SetComputeTextureParam(_computeShader, _kernelIdxReduceHiZMultiPass, NameIdOutMip4, hizMipmap, Math.Min(i + 3, hizMipmap.mipmapCount - 1));
                    cmd.SetComputeIntParams(_computeShader, NameIdSrcDstTexSize, 
                        hizMipSizeArray[i - 1].x, hizMipSizeArray[i - 1].y, 
                        hizMipSizeArray[i].x, hizMipSizeArray[i].y);
                    cmd.DispatchCompute(_computeShader, _kernelIdxReduceHiZMultiPass,
                        Mathf.CeilToInt(hizMipSizeArray[i].x * 1.0f / threadX),
                        Mathf.CeilToInt(hizMipSizeArray[i].y * 1.0f / threadY), 1);
                }
            }
            cmd.EndSample("HiZ Mipmap Generator Pass");
        }

        private static int2 CalcHiZMapSize(Camera camera)
        {
            var textureSizeWidth = Mathf.IsPowerOfTwo(camera.pixelWidth)
                ? camera.pixelWidth
                : Mathf.NextPowerOfTwo(camera.pixelWidth) / 2;
            var textureSizeHeight = Mathf.IsPowerOfTwo(camera.pixelHeight)
                ? camera.pixelHeight
                : Mathf.NextPowerOfTwo(camera.pixelHeight) / 2;
            return new int2(textureSizeWidth, textureSizeHeight);
        }

        private static Vector2Int[] GenerateMipSizes(int width, int height)
        {
            List<Vector2Int> mips = new List<Vector2Int>();
            if (!Mathf.IsPowerOfTwo(width) || !Mathf.IsPowerOfTwo(height))
            {
                Debug.LogError("[HiZGenRenderPass::GenerateMipSizes] Width and height must be power of 2.");
                return null;
            }

            int w = width;
            int h = height;

            mips.Add(new Vector2Int(w, h));

            while (w > 1 || h > 1)
            {
                w = w > 1 ? w / 2 : 1;
                h = h > 1 ? h / 2 : 1;

                mips.Add(new Vector2Int(w, h));
            }

            return mips.ToArray();
        }
    }
}