Shader "Particles/GPUParticle"
{
    Properties
    {
        _MainTex  ("Sprite", 2D) = "white" {}
        _SpeedTex ("Speed Gradient", 2D) = "white" {}
        _BaseSize ("Base Size", Float) = 0.18
        _LifeMax  ("(auto)", Float) = 20        // just so it serialises
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Particle { float3 pos; float3 vel; float life; float size; };
            StructuredBuffer<Particle> _Particles;

            CBUFFER_START(UnityPerMaterial)
                float  _BaseSize;
                float  _LifeMax;               // supplied from C#
                float4 _SpeedTex_TexelSize;
            CBUFFER_END

            sampler2D _MainTex;
            sampler2D _SpeedTex;

            struct a  { float3 posOS:POSITION; float2 uv:TEXCOORD0; };
            struct v  { float4 posCS:SV_POSITION; float2 uv:TEXCOORD0;
                        float  life:TEXCOORD1;   float speed:TEXCOORD2; };

            v vert (a i, uint id : SV_InstanceID)
            {
                v o;
                Particle p = _Particles[id];

                float3 R = UNITY_MATRIX_IT_MV[0].xyz;
                float3 U = UNITY_MATRIX_IT_MV[1].xyz;
                float  s = _BaseSize * p.size;

                o.posCS = TransformWorldToHClip(p.pos + (R*i.posOS.x + U*i.posOS.y)*s);
                o.uv    = i.uv;

                // fade only in final 15 % of lifetime
                float t = saturate(p.life / _LifeMax);   // 1 → 0 over life
                o.life = saturate((t - 0.15) * (1/0.15)); // 1 … 0

                o.speed = length(p.vel);
                return o;
            }

            half4 frag (v i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);

                if(_SpeedTex_TexelSize.x > 0)
                {
                    half t = saturate(i.speed * 0.04);
                    col.rgb *= tex2D(_SpeedTex, float2(t,0.5)).rgb;
                }

                col.a *= i.life;
                return col;
            }
            ENDHLSL
        }
    }
}
