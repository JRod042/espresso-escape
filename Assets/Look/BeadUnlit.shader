Shader "Escape/BeadUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (0.02,0.02,0.02,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        LOD 100
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            ZTest Always
            ZWrite Off
            Cull Off
            Offset -8, -8
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // tiny NDC push toward camera so beads win depth vs FeltFiber nap
                o.pos.z -= 0.002 * o.pos.w;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
    FallBack Off
}
