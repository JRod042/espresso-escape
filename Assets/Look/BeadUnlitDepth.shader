Shader "Escape/BeadUnlitDepth"
{
    // k740+: depth-tested dash/seed beads — ZTest LEqual Geometry (NOT Overlay Always / ZTest Always)
    Properties
    {
        _Color ("Color", Color) = (0.28,0.16,0.06,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            ZTest LEqual
            ZWrite On
            Cull Off
            Offset -1, -1
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
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
    FallBack Off
}
