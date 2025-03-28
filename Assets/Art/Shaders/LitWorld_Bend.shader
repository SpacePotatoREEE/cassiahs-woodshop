Shader "Universal Render Pipeline/LitWorld_Bend"
{
    Properties
    {
        // Standard URP Lit properties
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        // ... (other URP Lit properties if needed) ...

        // --- CUSTOM BEND PROPERTIES ---
        _BendStrength ("Bend Strength", Range(0,0.01)) = 0.001
        _BendExponent ("Bend Exponent", Range(1,4)) = 2.0
        _PlayerPos    ("Player Position (XZ)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Lit"
            "RenderType"="Opaque"
        }

        // --------------------------------------------------------
        // 1) Forward Pass: Apply bending in the vertex function
        // --------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   MyLitPassVertex
            #pragma fragment LitPassFragment // standard URP frag function
            #pragma target 2.0

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            // Custom Bend Properties
            float  _BendStrength;
            float  _BendExponent;
            float4 _PlayerPos;

            // 1A) Custom Vertex function overrides "LitPassVertex"
            Varyings MyLitPassVertex(Attributes input)
            {
                // 1) First, call the standard URP vertex logic
                Varyings output = LitPassVertex(input);

                // 2) Bend the world-space position
                float3 worldPos = output.positionWS;
                float2 distVec  = worldPos.xz - _PlayerPos.xz;
                float  distance = length(distVec);
                float  bendValue = pow(distance, _BendExponent) * _BendStrength;
                worldPos.y -= bendValue;

                // 3) Update the position
                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);

                return output;
            }

            ENDHLSL
        }

        // --------------------------------------------------------
        // 2) ShadowCaster Pass: replicate snippet, add bending
        // --------------------------------------------------------
       Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }
    ZWrite On
    ColorMask 0
    Cull [_Cull]

    HLSLPROGRAM
    #pragma vertex   MyShadowPassVertex
    #pragma fragment ShadowPassFragment
    #pragma multi_compile_shadowcaster
    #pragma shader_feature_local _ALPHATEST_ON

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

    // Your custom parameters
    float  _BendStrength;
    float  _BendExponent;
    float4 _PlayerPos;

    // This overrides the standard vertex function, but calls it internally
    Varyings MyShadowPassVertex(Attributes input)
    {
        // 1) Call the built-in function from ShadowCasterPass.hlsl
        Varyings output = ShadowPassVertex(input);

        // 2) We do NOT have direct world-space position in "output" (the built-in pass
        //    clamps and transforms it). If you need to do your own world transform,
        //    see Option B below. If you just want to do a final displacement in clip space,
        //    you can manipulate output.positionCS directly (less physically correct).
        //
        // For a purely clip-space tweak (not recommended for typical "bending"):
        // float bendAmount = ...some function of output.positionCS.xz?
        // output.positionCS.y -= bendAmount;

        return output;
    }

    ENDHLSL
}


        // (Optional) DepthOnly, DepthNormals, GBuffer, etc. if you want them bent.

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
