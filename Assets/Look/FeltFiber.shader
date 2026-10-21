Shader "Escape/FeltFiber"
{
    // URP. Imagine handmade felt: clean matte wool, soft wrap light, fuzzy rim, real soft shadows,
    // tangent-space fuzz normal. No vertex displacement, no baked flecks.
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Fuzz normal", 2D) = "bump" {}
        _BumpScale ("Fuzz", Range(0,3)) = 1.0
        _Sheen ("Sheen", Range(0,1)) = 0.12
        _Smoothness ("Smoothness", Range(0,1)) = 0.05
        _Wrap ("Wrap", Range(0,1)) = 0.60
        _FiberAmt ("Fiber grain", Range(0,0.3)) = 0.07
        _SeamAmt ("Seam groove", Range(0,1)) = 0
        _SeamWidth ("Seam width (object x)", Range(0.005,0.1)) = 0.022
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BumpMap_ST;
            half4 _BaseColor;
            half _BumpScale, _Sheen, _Smoothness, _Wrap, _FiberAmt, _SeamAmt, _SeamWidth;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float hash(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nrm.normalWS;
                real sign = v.tangentOS.w * GetOddNegativeScale();
                o.tangentWS = float4(nrm.tangentWS, sign);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionOS = v.positionOS.xyz;
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                // fine wool grain: three scrambled scales, mean-preserving, gentle
                float2 u1 = i.uv * 97.0 + 13.7;
                float2 u2 = i.uv.yx * 211.0 + 4.2;
                float2 u3 = i.uv * 353.0 + 7.1;
                float g = hash(float3(u1, u1.x + u1.y)) * 0.45
                        + hash(float3(u2, u2.x * 0.7)) * 0.35
                        + hash(float3(u3, u3.y * 0.3)) * 0.20;
                c.rgb *= 1.0 + (g - 0.5) * 2.0 * _FiberAmt;

                // optional seam groove down object-space x = 0 on the front (+z) hemisphere
                if (_SeamAmt > 0.001)
                {
                    float w = max(0.004, _SeamWidth);
                    float gx = exp(-0.5 * (i.positionOS.x * i.positionOS.x) / (w * w));
                    float front = saturate(i.positionOS.z * 6.0);
                    c.rgb *= 1.0 - gx * front * _SeamAmt * 0.28;
                }

                // tangent-space fuzz normal
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv * _BumpMap_ST.xy + _BumpMap_ST.zw), _BumpScale);
                float3 bitangent = i.tangentWS.w * cross(i.normalWS, i.tangentWS.xyz);
                float3 N = normalize(TransformTangentToWorld(nTS, half3x3(i.tangentWS.xyz, bitangent, i.normalWS)));
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half shadow = lerp(1.0, mainLight.shadowAttenuation, 0.85); // felt never goes fully black in shadow

                float ndl = dot(N, mainLight.direction);
                float wrap = _Wrap;
                float diffuse = saturate((ndl + wrap) / (1.0 + wrap));
                diffuse *= diffuse * (3.0 - 2.0 * diffuse); // smoothstep: soft wool falloff
                half3 ambient = SampleSH(N);
                half3 lit = ambient + mainLight.color * diffuse * shadow;

                // point lights (Bean Soft Key / Fill, values HELD) — wrapped like the key, no specular.
                // The Built-in version of this shader had no ForwardAdd pass, so those intensities were never
                // applied to felt; in URP forward they are, so the term is scaled to a soft fill, not a hot spot.
                #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; li++)
                {
                    Light l = GetAdditionalLight(li, i.positionWS, half4(1,1,1,1));
                    float nl = saturate((dot(N, l.direction) + wrap * 0.5) / (1.0 + wrap * 0.5));
                    lit += l.color * min(l.distanceAttenuation, 1.0) * l.shadowAttenuation * nl * 0.18;
                }
                #endif
                lit = max(lit, half3(0.22, 0.20, 0.18));
                c.rgb *= lit;

                // fuzzy edge: felt catches light at grazing angles, no plastic highlight
                float ndv = saturate(dot(N, V));
                float rim = pow(1.0 - ndv, 2.2) * (_Sheen * 0.9 + 0.04);
                c.rgb += rim * half3(0.95, 0.90, 0.82) * (0.5 + 0.5 * shadow);
                // tiny matte sheen toward the key
                float3 H = normalize(mainLight.direction + V);
                float spec = pow(saturate(dot(N, H)), 6.0) * _Smoothness * 0.25;
                c.rgb += spec * mainLight.color * shadow;
                c.rgb = MixFog(c.rgb, i.fogFactor);
                c.a = 1.0;
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vr { float4 positionCS : SV_POSITION; };

            Vr ShadowVert(A v)
            {
                Vr o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                return o;
            }
            half4 ShadowFrag(Vr i) : SV_Target { return 0; }
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
