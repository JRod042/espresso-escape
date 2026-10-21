Shader "Escape/SeamFelt"
{
    // Depth-tested opaque seam floor (FeltFiber family). Cull Off + ZWrite On + Offset.
    // NOT BeadUnlit Overlay Always / ZTest Always sticker.
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BumpScale ("Fuzz", Range(0,3)) = 0.40
        _Sheen ("Sheen", Range(0,1)) = 0.04
        _Smoothness ("Smoothness", Range(0,1)) = 0.02
        _Wrap ("Wrap", Range(0,1)) = 0.95
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "LightMode"="ForwardBase" }
        LOD 200
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZWrite On
            ZTest LEqual
            Offset -2, -2
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            sampler2D _BaseMap; float4 _BaseMap_ST;
            fixed4 _BaseColor; float _BumpScale; float _Sheen; float _Smoothness; float _Wrap;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 worldN:TEXCOORD1; float3 viewDir:TEXCOORD2; float3 worldPos:TEXCOORD3; };
            float hash(float3 p){ return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            v2f vert(appdata v){
                v2f o;
                float3 wn = UnityObjectToWorldNormal(v.normal);
                float n = hash(v.vertex.xyz*18.0)-0.5;
                float soft = n * 0.0006;
                v.vertex.xyz += v.normal * soft * _BumpScale;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv,_BaseMap);
                o.worldN = wn;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i):SV_Target{
                fixed4 c = tex2D(_BaseMap,i.uv) * _BaseColor;
                float2 u = i.uv * 97.0 + float2(13.7, 9.1);
                float fiber = hash(float3(u, u.x+u.y));
                float felt = fiber * 0.55 + 0.45;
                c.rgb *= lerp(0.55, 1.15, felt);
                float3 N = normalize(i.worldN);
                float3 L = normalize(UnityWorldSpaceLightDir(i.worldPos));
                float NdotL = dot(N, L);
                float wrap = _Wrap;
                float diffuse = saturate((NdotL + wrap) / (1.0 + wrap));
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * 0.85;
                float3 lit = ambient + _LightColor0.rgb * diffuse;
                lit = max(lit, float3(0.045, 0.035, 0.028)); // k632 darker seam trough floor
                lit = min(lit, float3(0.55, 0.48, 0.42)); // k632 seam cap
                c.rgb *= lit;
                float ndv = saturate(dot(N, normalize(i.viewDir)));
                c.rgb += pow(1.0-ndv, 2.0) * _Sheen * float3(0.20,0.16,0.12);
                return c;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
