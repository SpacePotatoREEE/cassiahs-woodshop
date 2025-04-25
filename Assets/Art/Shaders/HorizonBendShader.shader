Shader "Custom/HorizonBendLit"
{
    Properties
    {
        _BaseMap      ("Base Map",    2D)    = "white" {}
        _BaseColor    ("Base Color",  Color) = (1,1,1,1)

        // ── horizon-bend ──
        _BendRadius    ("Bend Radius",    Float) = 50
        _BendStrength  ("Bend Strength",  Float) = 1
        _PlayerPos     ("Player Pos",     Vector) = (0,0,0,0)

        // ── final shadow-acne safety-net ──
        _ReceiverBias  ("Shadow Receiver Bias (m)", Range(0,0.01)) = 0.0005
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline"  "RenderType"="Opaque" }
        LOD 200

        // ────────────────────────────────────────────────
        // 1)  FORWARD pass — RECEIVES shadows
        // ────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex   vert
            #pragma fragment frag

            // shadow keywords
            #pragma multi_compile_fragment _  _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _  _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _  _SHADOWS_SOFT
            #pragma multi_compile_fragment _  _SCREEN_SPACE_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _BendRadius;
                float  _BendStrength;
                float4 _PlayerPos;
                float  _ReceiverBias;   // metres
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            // ---------- bend helper ----------
            float3 Bend(float3 pWS)
            {
                float2 d = pWS.xz - _PlayerPos.xz;
                pWS.y   -= _BendStrength * dot(d,d) / _BendRadius;
                return pWS;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posWS = Bend(TransformObjectToWorld(IN.positionOS.xyz));

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.positionWS = posWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 1) Manually add receiver bias (along light direction)
                Light  l0          = GetMainLight();                // no shadows yet
                float3 biasedWS    = IN.positionWS + l0.direction * _ReceiverBias;

                // 2) Convert to shadow-space and sample shadow map
                float4 sc          = TransformWorldToShadowCoord(biasedWS);
                Light  mainLight   = GetMainLight(sc);

                // 3) Simple Lambert
                half3 albedo       = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb
                                     * _BaseColor.rgb;
                half  NdotL        = saturate(dot(IN.normalWS, mainLight.direction));
                half3 finalColor   = albedo * mainLight.color * NdotL * mainLight.shadowAttenuation;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // ────────────────────────────────────────────────
        // 2)  SHADOW-CASTER pass — unchanged
        // ────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex   vSC
            #pragma fragment fSC

            #pragma multi_compile_vertex _  _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_vertex _  _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_vertex _  _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float  _BendRadius;
                float  _BendStrength;
                float4 _PlayerPos;
            CBUFFER_END

            float3 Bend(float3 pWS)
            {
                float2 d = pWS.xz - _PlayerPos.xz;
                pWS.y   -= _BendStrength * dot(d,d) / _BendRadius;
                return pWS;
            }

            V vSC (A IN)
            {
                V OUT;
                float3 pWS = Bend(TransformObjectToWorld(IN.positionOS.xyz));
                half3  nWS = TransformObjectToWorldNormal(IN.normalOS);
                half3  lDir= GetMainLight().direction;

                pWS = ApplyShadowBias(pWS, nWS, lDir);
                OUT.positionCS = TransformWorldToHClip(pWS);
                return OUT;
            }
            half4 fSC(V IN):SV_Target{ return 0; }
            ENDHLSL
        }
    }
}
