Shader "Terrain/Terrain"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Patch.hlsl"

            #pragma shader_feature ENABLE_PATCH_DEBUG
            #pragma shader_feature ENABLE_NODE_DEBUG
            #pragma shader_feature ENABLE_LOD_DEBUG
            #pragma shader_feature ENABLE_LOD_TRANS_DEBUG
            #pragma shader_feature ENABLE_LOD_SEAMLESS
            #pragma shader_feature ENABLE_HEIGHT_DEBUG
            #pragma shader_feature ENABLE_CHECKER_DEBUG

            TEXTURE2D_ARRAY(_HeightMapArray);
            SAMPLER(sampler_HeightMapArray);

            float4 _SectorInfo; // xy: sector grid size, zw: sector size / resolution
            float4 _WorldSize; // xyz
            int _MaxLod;
            StructuredBuffer<Patch> _PatchBuffer;

            float2 CalcNodeCenter(Patch patch)
            {
                float2 nodeCount = _SectorInfo.xy * pow(2, _MaxLod - patch.lod);
                float nodeSize = _WorldSize.x / nodeCount.x; 
                uint2 nodeLoc = floor((patch.position + _WorldSize.xz * 0.5) / nodeSize);
                return -_WorldSize.xz * 0.5 + (nodeLoc + 0.5) * nodeSize;
            }

            void StitchPatch(inout float3 vertex, inout float2 uv, uint4 lodTransitions)
            {
                // Plane16 Settings
                #define PATCH_MESH_SIZE 8
                #define PATCH_MESH_GRID_SIZE 0.5
                #define PATCH_MESH_GRID_COUNT 16
                
                uint2 vertexLoc = floor((vertex.xz + PATCH_MESH_SIZE * 0.5 + 0.01) / PATCH_MESH_GRID_SIZE);
                float uvGridStrip = 1.0 / PATCH_MESH_GRID_COUNT;

                uint lodTrans = lodTransitions.x;
                if (lodTrans > 0 && vertexLoc.x == 0)
                {
                    uint gridStripCount = pow(2, lodTrans);
                    uint modIndex = vertexLoc.y % gridStripCount;
                    if(modIndex != 0) { vertex.z -= PATCH_MESH_GRID_SIZE * modIndex; uv.y -= uvGridStrip * modIndex; return; }
                }

                lodTrans = lodTransitions.y;
                if (lodTrans > 0 && vertexLoc.y == 0)
                {
                    uint gridStripCount = pow(2, lodTrans);
                    uint modIndex = vertexLoc.x % gridStripCount;
                    if(modIndex != 0) { vertex.x -= PATCH_MESH_GRID_SIZE * modIndex; uv.x -= uvGridStrip * modIndex; return; }
                }

                lodTrans = lodTransitions.z;
                if (lodTrans > 0 && vertexLoc.x == PATCH_MESH_GRID_COUNT)
                {
                    uint gridStripCount = pow(2, lodTrans);
                    uint modIndex = vertexLoc.y % gridStripCount;
                    if(modIndex != 0) { vertex.z += PATCH_MESH_GRID_SIZE * (gridStripCount - modIndex); uv.y += uvGridStrip * (gridStripCount - modIndex); return; }
                }

                lodTrans = lodTransitions.w;
                if (lodTrans > 0 && vertexLoc.y == PATCH_MESH_GRID_COUNT)
                {
                    uint gridStripCount = pow(2, lodTrans);
                    uint modIndex = vertexLoc.x % gridStripCount;
                    if(modIndex != 0) { vertex.x += PATCH_MESH_GRID_SIZE * (gridStripCount - modIndex); uv.x += uvGridStrip * (gridStripCount - modIndex); return; }
                }
            }

            void ApplyTerrainVertexModification(inout float3 positionOS, inout float2 uv, uint instanceID, out float height, out float2 uvInWorld, out Patch patch)
            {
                patch = _PatchBuffer[instanceID];

                #if defined(ENABLE_LOD_SEAMLESS)
                StitchPatch(positionOS, uv, patch.lodTransitions);
                #endif
                
                uint lod = patch.lod;
                float2 scale = _SectorInfo.zw * pow(2, lod) * 2.0;
                positionOS.xz *= scale;
                
                #if defined(ENABLE_PATCH_DEBUG)
                positionOS.xz *= 0.98;
                #endif
                
                positionOS.xz += patch.position;
                
                #if defined(ENABLE_NODE_DEBUG)
                float2 nodePos = CalcNodeCenter(patch);
                positionOS.xz = nodePos + (positionOS.xz - nodePos) * 0.995;
                #endif

                uvInWorld = (positionOS.xz + _WorldSize.xz * 0.5) / _WorldSize.xz;
                uvInWorld = clamp(uvInWorld, 0.0, 0.999999);
                
                float2 uvTmp = uvInWorld * _SectorInfo.xy;
                int2 sectorLoc = trunc(uvTmp);
                int sectorIdx = sectorLoc.y * _SectorInfo.x + sectorLoc.x;
                float2 uvInSector = frac(uvTmp);
                
                height = SAMPLE_TEXTURE2D_ARRAY_LOD(_HeightMapArray, sampler_HeightMapArray, uvInSector, sectorIdx, 0).r;
                positionOS.y = height * _WorldSize.y;
            }

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uvInWorld : TEXCOORD0;
                float4 shadowCoords : TEXCOORD1;
                #if defined(ENABLE_LOD_DEBUG)
                uint lod : TEXCOORD2;
                #endif
                #if defined(ENABLE_HEIGHT_DEBUG)
                float height : TEXCOORD3;
                #endif
                #if defined(ENABLE_CHECKER_DEBUG)
                float2 positionWS : TEXCOORD4;
                #endif
                #if defined(ENABLE_LOD_TRANS_DEBUG)
                uint4 lodTrans : TEXCOORD5;
                #endif
            };

            TEXTURE2D_ARRAY(_SplatMapArray);
            SAMPLER(sampler_SplatMapArray);
            TEXTURE2D_ARRAY(_AlbedoMapArray);
            SAMPLER(sampler_AlbedoMapArray);
            
            struct SectorAssetDesc
            {
                int2 splatMapIndices;
                int4 layerPack0Indices;
                int4 layerPack1Indices;
            };
            StructuredBuffer<SectorAssetDesc> _SectorAssetDescBuffer;

            struct TerrainLayerDesc
            {
                float2 tilingOffset;
                float2 tilingSize;
            };
            StructuredBuffer<TerrainLayerDesc> _TerrainLayerDescBuffer;

            #if defined(ENABLE_LOD_DEBUG)
            static float3 DebugLodColors[6] = { float3(1,0,0), float3(1,1,0), float3(0,1,0), float3(0,1,1), float3(0,0,1), float3(1,0,1) };
            #endif

            float4 SampleAlbedo(float2 uv, int layerIdx)
            {
                if (layerIdx == -1) return float4(0, 0, 0, 1);
                float4 color = SAMPLE_TEXTURE2D_ARRAY(_AlbedoMapArray, sampler_AlbedoMapArray, uv, layerIdx).rgba;
                return float4(color.rgb, 1);
            }

            float2 TransformTex(float2 uv, int layerIdx)
            {
                TerrainLayerDesc terrainLayerDesc = _TerrainLayerDescBuffer[layerIdx];
                return terrainLayerDesc.tilingSize * uv + terrainLayerDesc.tilingOffset;
            }
            
            Varyings vert (Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
                float height;
                float2 uvInWorld;
                Patch patch;

                ApplyTerrainVertexModification(IN.positionOS, IN.uv, instanceID, height, uvInWorld, patch);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uvInWorld = uvInWorld;
                
                // Shadow Coords
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.shadowCoords = GetShadowCoord(positions);

                // Debug Data Passing
                #if defined(ENABLE_LOD_DEBUG)
                OUT.lod = patch.lod;
                #endif
                #if defined(ENABLE_LOD_TRANS_DEBUG)
                OUT.lodTrans = patch.lodTransitions;
                #endif
                #if defined(ENABLE_HEIGHT_DEBUG)
                OUT.height = height;
                #endif
                #if defined(ENABLE_CHECKER_DEBUG)
                OUT.positionWS = IN.positionOS.xz;
                #endif
                
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uvTmp = IN.uvInWorld * _SectorInfo.xy;
                float2 uvInSector = frac(uvTmp);
                int2 sectorLoc = trunc(uvTmp);
                int sectorIdx = sectorLoc.y * _SectorInfo.x + sectorLoc.x;

                SectorAssetDesc sectorAssetDesc = _SectorAssetDescBuffer[sectorIdx];
                int2 splatMapIndices = sectorAssetDesc.splatMapIndices;
                int4 layerPack0 = sectorAssetDesc.layerPack0Indices;
                int4 layerPack1 = sectorAssetDesc.layerPack1Indices;
                
                float4 uvSplat01 = float4(TransformTex(uvInSector, layerPack0.x), TransformTex(uvInSector, layerPack0.y));
                float4 uvSplat23 = float4(TransformTex(uvInSector, layerPack0.z), TransformTex(uvInSector, layerPack0.w));
                float4 uvSplat45 = float4(TransformTex(uvInSector, layerPack1.x), TransformTex(uvInSector, layerPack1.y));
                float4 uvSplat67 = float4(TransformTex(uvInSector, layerPack1.z), TransformTex(uvInSector, layerPack1.w));
                
                float4 splat0 = float4(0, 0, 0, 0);
                if (splatMapIndices.x != -1) splat0 = SAMPLE_TEXTURE2D_ARRAY_LOD(_SplatMapArray, sampler_SplatMapArray, uvInSector, splatMapIndices.x, 0).rgba;

                float4 albedo0 = SampleAlbedo(uvSplat01.xy, layerPack0.x);
                float4 albedo1 = SampleAlbedo(uvSplat01.zw, layerPack0.y);
                float4 albedo2 = SampleAlbedo(uvSplat23.xy, layerPack0.z);
                float4 albedo3 = SampleAlbedo(uvSplat23.zw, layerPack0.w);

                float4 splat1 = float4(0, 0, 0, 0);
                if (splatMapIndices.y != -1) splat1 = SAMPLE_TEXTURE2D_ARRAY_LOD(_SplatMapArray, sampler_SplatMapArray, uvInSector, splatMapIndices.y, 0).rgba;

                float4 albedo4 = SampleAlbedo(uvSplat45.xy, layerPack1.x);
                float4 albedo5 = SampleAlbedo(uvSplat45.zw, layerPack1.y);
                float4 albedo6 = SampleAlbedo(uvSplat67.xy, layerPack1.z);
                float4 albedo7 = SampleAlbedo(uvSplat67.zw, layerPack1.w);
                
                float4 color = splat0.x * albedo0 + splat0.y * albedo1 + splat0.z * albedo2 + splat0.w * albedo3
                             + splat1.x * albedo4 + splat1.y * albedo5 + splat1.z * albedo6 + splat1.w * albedo7;
                
                half shadowAmount = MainLightRealtimeShadow(IN.shadowCoords);
                Light light = GetMainLight();
                color = color * float4(light.color, 1.0) * shadowAmount;

                #if defined(ENABLE_HEIGHT_DEBUG)
                color = float4(lerp(float3(0, 0, 1), float3(1, 0, 0), IN.height), 1.0);
                #endif

                #if defined(ENABLE_CHECKER_DEBUG)
                bool checker = ((int)IN.positionWS.x + (int)IN.positionWS.y) % 2 == 0;
                color.rgb *= checker ? float3(1, 1, 1) : float3(0.5, 0.5, 0.5);
                #endif
                
                #if defined(ENABLE_LOD_DEBUG)
                color *= float4(DebugLodColors[IN.lod], 1.0);
                #endif

                #if defined(ENABLE_LOD_TRANS_DEBUG)
                uint lodTrans = IN.lodTrans.x + IN.lodTrans.y + IN.lodTrans.z + IN.lodTrans.w;
                if (lodTrans > 0) color *= 10;
                #endif
                
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
                float height;
                float2 uvInWorld;
                Patch patch;

                ApplyTerrainVertexModification(IN.positionOS, IN.uv, instanceID, height, uvInWorld, patch);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}