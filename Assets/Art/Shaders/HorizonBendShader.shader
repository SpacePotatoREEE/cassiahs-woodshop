Shader "Custom/HorizonBend_URP_Lit_NoInverse"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BendStrength ("Bend Strength", Range(0,0.01)) = 0.001
        _BendExponent ("Bend Exponent", Range(1,4))    = 2.0
        _PlayerPos  ("Player Position (XZ only)", Vector) = (0,0,0,0)
        _Color      ("Base Color Tint", Color)            = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        LOD 100

        // -------------------------------------------------------------
        // 1) Forward Lit Pass
        // -------------------------------------------------------------
        Pass
        {
            Name "HorizonBendLitPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            //===========================================================
            // Pragmas & Includes
            //===========================================================
            #pragma vertex vert
            #pragma fragment frag

            // Enable URP lighting/shadow variants as needed
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            // URP core includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            //===========================================================
            // Properties / Uniforms
            //===========================================================
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float _BendStrength;
            float _BendExponent;
            float4 _PlayerPos; 
            float4 _Color; // Tint color

            // Built-in matrices provided by Unity in URP:
            //   unity_ObjectToWorld : float4x4
            //   unity_WorldToObject : float4x4
            //   UNITY_MATRIX_VP     : float4x4 (View * Projection)

            //===========================================================
            // Vertex Input & Output Structs
            //===========================================================
            struct Attributes
            {
                float3 position : POSITION;
                float3 normal   : NORMAL;
                float2 uv       : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION; // Clip-space position
                float3 normalWS   : TEXCOORD0;   // World-space normal
                float2 uv         : TEXCOORD1;   // UV
                float3 positionWS : TEXCOORD2;   // World-space position
            };

            //===========================================================
            // Helper: Transform a normal from object to world space
            // without calling 'inverse()'.
            //
            // Because:
            //   unity_WorldToObject == (objectToWorld)^(-1)
            // So the inverse-transpose of (objectToWorld)
            // is just the transpose of (unity_WorldToObject).
            //===========================================================
            float3 TransformObjectToWorldNormal(float3 n)
            {
                // Take the top-left 3x3 of unity_WorldToObject
                float3x3 worldToObject3x3 = (float3x3) unity_WorldToObject;

                // The correct normal transform for object->world is
                // (objectToWorld)^(-1) transposed, i.e. worldToObject^T
                float3x3 normalMatrix = transpose(worldToObject3x3);

                // Transform & normalize
                return normalize(mul(normalMatrix, n));
            }

            //===========================================================
            // Vertex Shader
            //===========================================================
            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // (1) Transform position to world space
                float3 worldPos = mul(unity_ObjectToWorld, float4(IN.position, 1.0)).xyz;

                // (2) Apply horizon bend in world space
                float2 distVec    = worldPos.xz - _PlayerPos.xz;
                float  distance   = length(distVec);
                float  bendAmount = pow(distance, _BendExponent) * _BendStrength;
                worldPos.y       -= bendAmount;

                // (3) World->Clip
                OUT.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

                // (4) Transform normal -> world space (no inverse() call)
                OUT.normalWS = TransformObjectToWorldNormal(IN.normal, false);

                // (5) UV & final world pos
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.positionWS = worldPos;

                return OUT;
            }

            //===========================================================
            // Fragment Shader (Simple Lambert)
            //===========================================================
            float4 frag (Varyings IN) : SV_Target
            {
                // Sample main texture
                float4 sampled   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float3 baseColor = sampled.rgb * _Color.rgb;

                // Grab main directional light (URP)
                Light mainLight  = GetMainLight();
                float3 N         = normalize(IN.normalWS);
                float3 L         = normalize(mainLight.direction);
                float  NdotL     = saturate(dot(N, L));

                // Simple diffuse
                float3 diffuse   = NdotL * mainLight.color;

                // Small ambient factor (optional)
                float3 ambient   = 0.05f * baseColor;

                float3 finalColor = baseColor * diffuse + ambient;
                return float4(finalColor, sampled.a);
            }
            ENDHLSL
        }

        // -------------------------------------------------------------
        // 2) Shadow Caster Pass (with horizon bend)
        // -------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            float _BendStrength;
            float _BendExponent;
            float4 _PlayerPos;

            struct Attributes
            {
                float3 position : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // Object->World
                float3 worldPos = mul(unity_ObjectToWorld, float4(IN.position, 1.0)).xyz;

                // Bend
                float2 distVec   = worldPos.xz - _PlayerPos.xz;
                float  distance  = length(distVec);
                float  bendValue = pow(distance, _BendExponent) * _BendStrength;
                worldPos.y      -= bendValue;

                // World->Clip
                OUT.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                return OUT;
            }

            float4 fragShadow() : SV_Target
            {
                // For shadow pass, just return 0 (depth only)
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
