Shader "Escape/Bead"
{
    // URP. Glossy sewn-on bead (eyes, smile thread knots). Depth-tested, lit, strong highlight.
    Properties
    {
        _Color ("Color", Color) = (0.02,0.015,0.012,1)
        _Gloss ("Gloss", Range(0,1)) = 0.9
        _SpecPower ("Highlight tightness", Range(8,256)) = 96
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _Gloss, _SpecPower;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZTest LEqual
            ZWrite On
            Cull Back
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 N = normalize(i.normalWS);
                Light L = GetMainLight();
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 H = normalize(L.direction + V);
                float ndl = saturate(dot(N, L.direction) * 0.5 + 0.5);
                half3 col = _Color.rgb * (SampleSH(N) + L.color * ndl);
                float spec = pow(saturate(dot(N, H)), _SpecPower) * _Gloss;
                float ndv = saturate(dot(N, V));
                float fres = pow(1.0 - ndv, 3.0) * 0.35 * _Gloss;
                col += (spec + fres) * (L.color + 0.4);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vr { float4 positionCS : SV_POSITION; };
            Vr DepthVert(A v)
            {
                Vr o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            half4 DepthFrag(Vr i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
