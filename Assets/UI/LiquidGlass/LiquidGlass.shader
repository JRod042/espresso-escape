// Liquid Glass (frosted, translucent, rounded) panel for uGUI in URP.
// - LIQUIDGLASS_BLUR on : samples URP's _CameraOpaqueTexture (Opaque Texture must be enabled on the
//   URP asset or camera, and the Canvas must be Screen Space - Camera or World Space) and blurs it.
// - LIQUIDGLASS_BLUR off: tinted translucent fallback (works everywhere, incl. Screen Space - Overlay).
// The LiquidGlassPanel component picks the variant and feeds per-panel size/radius/blur via UV1/UV2.
Shader "UI/LiquidGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (optional mask)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TintStrength ("Tint Strength (blur mode)", Range(0,1)) = 0.25
        _FallbackOpacity ("Fallback Opacity (no blur source)", Range(0,1)) = 0.55
        _Saturation ("Backdrop Saturation", Range(0,2)) = 1.25
        _Brightness ("Backdrop Brightness", Range(0.5,1.5)) = 1.05
        _RimColor ("Rim Highlight", Color) = (1,1,1,0.7)
        _RimWidth ("Rim Width", Range(0,8)) = 1.5
        _GradientTop ("Inner Gradient Top", Color) = (1,1,1,0.18)
        _GradientBottom ("Inner Gradient Bottom", Color) = (1,1,1,0.0)
        _EdgeSoftness ("Edge Anti-alias", Range(0.5,3)) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "LiquidGlass"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ LIQUIDGLASS_BLUR

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 uv1      : TEXCOORD1; // xy = normalized pos in rect, zw = rect size
                float4 uv2      : TEXCOORD2; // x = corner radius, y = blur radius (px)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 rect          : TEXCOORD2;
                float4 params        : TEXCOORD3;
                float4 screenPos     : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ClipRect;
            float _TintStrength, _FallbackOpacity, _Saturation, _Brightness, _RimWidth, _EdgeSoftness;
            fixed4 _RimColor, _GradientTop, _GradientBottom;

            #if defined(LIQUIDGLASS_BLUR)
            sampler2D _CameraOpaqueTexture;
            float4 _CameraOpaqueTexture_TexelSize;

            // 16-tap golden-angle spiral: cheap, smooth, no extra passes (mobile/Metal friendly).
            float3 SampleBackdropBlur(float2 uv, float radiusPx)
            {
                float2 texel = _CameraOpaqueTexture_TexelSize.xy;
                float3 acc = tex2D(_CameraOpaqueTexture, uv).rgb;
                [unroll]
                for (int k = 1; k < 16; k++)
                {
                    float r = sqrt(k / 15.0) * radiusPx;
                    float a = k * 2.39996323;
                    acc += tex2D(_CameraOpaqueTexture, uv + float2(cos(a), sin(a)) * r * texel).rgb;
                }
                return acc / 16.0;
            }
            #endif

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                o.rect = v.uv1;
                o.params = v.uv2;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Without the LiquidGlassPanel component UV1 is empty: treat as a plain square panel.
                bool hasRect = i.rect.z > 0.0;
                float2 n = hasRect ? i.rect.xy : i.texcoord;
                float2 size = hasRect ? i.rect.zw : float2(100.0, 100.0);
                float radius = hasRect ? min(i.params.x, 0.5 * min(size.x, size.y)) : 0.0;

                // Rounded-rect signed distance (negative inside), in rect units.
                float2 p = (n - 0.5) * size;
                float2 q = abs(p) - (0.5 * size - radius);
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                float aa = max(fwidth(d), 1e-4) * _EdgeSoftness;
                float shape = saturate(0.5 - d / aa);

                float4 tint = i.color; // rgb = tint, a = opacity
                float3 col;
                float alpha;

                #if defined(LIQUIDGLASS_BLUR)
                    float2 suv = i.screenPos.xy / max(i.screenPos.w, 1e-5);
                    float3 bg = SampleBackdropBlur(suv, max(i.params.y, 0.0));
                    float lum = dot(bg, float3(0.299, 0.587, 0.114));
                    bg = lerp(lum.xxx, bg, _Saturation) * _Brightness;
                    col = lerp(bg, tint.rgb, _TintStrength);
                    alpha = tint.a;
                #else
                    col = tint.rgb;
                    alpha = tint.a * _FallbackOpacity;
                #endif

                // Light inner gradient (brighter at the top).
                float4 grad = lerp(_GradientBottom, _GradientTop, saturate(n.y));
                col += grad.rgb * grad.a;

                // Subtle white rim, strongest along the top edge.
                float rimW = max(_RimWidth, 1e-3);
                float rim = saturate(1.0 - abs(d + rimW * 0.5) / rimW) * _RimColor.a;
                rim *= lerp(0.45, 1.0, saturate(n.y));
                col = lerp(col, _RimColor.rgb, rim);
                alpha = max(alpha, rim);

                alpha *= shape * tex2D(_MainTex, i.texcoord).a;

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(col * alpha, alpha); // premultiplied
            }
        ENDCG
        }
    }
    Fallback "UI/Default"
}