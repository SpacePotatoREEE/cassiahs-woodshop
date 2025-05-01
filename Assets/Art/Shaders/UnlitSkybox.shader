// UnlitSkyboxSphere.shader
// ───────────────────────
// • Pure-unlit: no lights, shadows, or GI.
// • Renders in the Background queue; depth writes off.
// • Front-face culling draws the inside of a normal, outward-facing sphere.
// • **Texture now rotates with the sphere** by sampling in *object space*.
// • Unity 6 (URP 2024+) compatible.

Shader "Custom/URP/UnlitSkyboxSphere"
{
    Properties
    {
        _MainTex ("Skybox Cubemap", Cube) = "_Skybox" {}
        _Exposure ("Exposure", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "Queue"           = "Background"
            "IgnoreProjector" = "True"
            "RenderPipeline"  = "UniversalRenderPipeline"
        }

        Cull   Front         // show inside faces
        ZWrite Off
        ZTest  LEqual
        Lighting Off         // stay unlit

        Pass
        {
            Name "UnlitSkybox"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _Exposure;
            CBUFFER_END

            /* ───────────── STRUCTS ───────────── */
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirOS      : TEXCOORD0;   // object-space view dir
            };

            /* ───────────── VERTEX ───────────── */
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // World position & clip-space output
                float3 positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.positionCS    = TransformWorldToHClip(positionWS);

                // World-space vector from camera to this vertex
                float3 dirWS = normalize(positionWS - _WorldSpaceCameraPos);

                // Convert to object space so sphere rotation affects lookup
                float3 dirOS = mul((float3x3)unity_WorldToObject, dirWS);
                OUT.dirOS    = normalize(dirOS);

                return OUT;
            }

            /* ───────────── FRAGMENT ───────────── */
            half4 frag (Varyings IN) : SV_Target
            {
                float3 col = SAMPLE_TEXTURECUBE(_MainTex, sampler_MainTex, IN.dirOS).rgb;
                return half4(col * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
