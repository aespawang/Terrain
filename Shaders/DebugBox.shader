Shader "Terrain/DebugBox"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DebugBox.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;                 
            };

            struct V2G
            {
                float4 positionHCS : SV_POSITION;
                float3 color : TEXCOORD0;
            };

            struct G2F
            {
                float4 positionHCS : SV_POSITION;
                float3 color : TEXCOORD0;
            };

            StructuredBuffer<DebugBox> _DebugBoxBuffer;

            static float3 DebugLodColors[6] = {
                float3(1,0,0),
                float3(1,1,0),
                float3(0,1,0),
                float3(0,1,1),
                float3(0,0,1),
                float3(1,0,1),
            };
            
            V2G vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                DebugBox debugBox = _DebugBoxBuffer[instanceID];
                uint lod = debugBox.lod;

                float3 minPos = debugBox.minPosition;
                float3 maxPos = debugBox.maxPosition;
                float3 local = IN.positionOS.xyz;
                float3 worldPos = (local + 0.5) * (maxPos - minPos) + minPos;
                
                V2G OUT;
                OUT.positionHCS = TransformObjectToHClip(worldPos);
                OUT.color = DebugLodColors[lod];
                return OUT;
            }

            [maxvertexcount(4)]
            void geom(triangle V2G IN[3], inout LineStream<G2F> outStream)
            {
                G2F OUT;
                OUT.positionHCS = IN[0].positionHCS;
                OUT.color = IN[0].color;
                outStream.Append(OUT);
                OUT.positionHCS = IN[1].positionHCS;
                OUT.color = IN[1].color;
                outStream.Append(OUT);
                OUT.positionHCS = IN[2].positionHCS;
                OUT.color = IN[2].color;
                outStream.Append(OUT);
                OUT.positionHCS = IN[0].positionHCS;
                OUT.color = IN[0].color;
                outStream.Append(OUT);
            }

            float4 frag(G2F IN) : SV_Target
            {
                return float4(IN.color, 1.0);
            }
            
            ENDHLSL
        }
    }
}
