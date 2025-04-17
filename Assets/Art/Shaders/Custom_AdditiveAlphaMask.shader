Shader "Custom/AdditiveAlphaMask"
{
    Properties
    {
        _MainTex ("Texture (RGBA)", 2D) = "white" {}
        _Tint    ("Tint Color",     Color) = (1,1,1,1)
    }

    SubShader
    {
        // — Transparent queue so it draws after the opaque objects
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        // — Additive blending:  (SrcColor * 1) + (DstColor * 1)
        Blend One One

        // — We don’t need depth writes for transparent overlays
        ZWrite Off
        Cull Off          // (optional) draw both faces if you like

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _Tint;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);   // RGBA sample

                // Multiply RGB by alpha so only the opaque part is added.
                fixed3 rgb = tex.rgb * tex.a;

                // Optional tint (alpha of _Tint is also used as a multiplier)
                rgb  *= _Tint.rgb;
                float aOut = tex.a * _Tint.a;        // <‑ mainly useful for sorting

                return fixed4(rgb, aOut);
            }
            ENDCG
        }
    }

    // No lighting, no shadow casting
    Fallback Off
}
