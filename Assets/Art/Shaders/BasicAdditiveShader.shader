Shader "Custom/BasicAdditiveShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        // Additive blending: finalColor = SourceColor + DestinationColor
        // SourceColor = (texture * vertexColor * _Color)
        // DestinationColor = whatever was already on screen
        Blend One One

        // Because this is an additive shader, writing to the depth buffer is usually turned off
        ZWrite Off

        // Usually no culling for 2D-like effects
        Cull Off

        // No lighting pass, purely unlit
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            // Appdata for the vertex shader
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv     : TEXCOORD0;
            };

            // Varyings passed to fragment
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Color;

            // Vertex shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Multiply vertex color by the user-set _Color
                // We'll handle the texture in the fragment function
                o.color = v.color * _Color;

                o.uv = v.uv;
                return o;
            }

            // Fragment shader
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture at the given UVs
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // Multiply by vertex+material color
                fixed4 finalColor = texColor * i.color;

                // finalColor now has its RGBA multiplied by both the material color and the vertex color
                // Under additive blending, the alpha won't affect how it adds to the destination,
                // but if you need alpha for some reason, finalColor.a is already multiplied by _Color.a.

                return finalColor;
            }
            ENDCG
        }
    }
}
