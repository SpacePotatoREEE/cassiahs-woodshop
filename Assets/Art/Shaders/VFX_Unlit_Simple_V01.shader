Shader "VFX/VFX_Unlit_Simple_V01"
{
    /* ------------------------------------------------------------------
       PROPERTIES
    ------------------------------------------------------------------ */
    Properties
    {
        _BaseMap  ("Texture", 2D) = "white" {}   // sRGB-sampled texture
        [HDR] _Tint ("Tint", Color) = (1,1,1,1)  // HDR colour multiplier
    }

    /* ------------------------------------------------------------------
       SUBSHADER
    ------------------------------------------------------------------ */
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
        }

        LOD 100
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha   // pure alpha blending

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            /* ----- URP include ----- */
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            /* ----- Structures ----- */
            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            /* ----- Per-material constants (SRP Batcher friendly) ----- */
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float4 _BaseMap_ST;
            CBUFFER_END

            /* ----- Texture sampler ----- */
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            /* ----- Vertex shader ----- */
            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap); 
                return OUT;
            }

            /* ----- Fragment shader ----- */
            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                return tex * _Tint;
            }
            ENDHLSL
        }
    }
}
