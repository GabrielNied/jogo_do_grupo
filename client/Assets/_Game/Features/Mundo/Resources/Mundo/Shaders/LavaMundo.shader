// Lava: emissiva, com crosta escura que corre sobre o magma (ruido de valor deslocado no tempo,
// sem textura). O brilho nao depende da luz do sol: a lava ilumina a si mesma, e a bruma a cobre.
Shader "JogoDoGrupo/LavaMundo"
{
    Properties
    {
        _CrostaColor("Cor da crosta", Color) = (0.10, 0.04, 0.03, 1)
        _MagmaColor("Cor do magma", Color) = (1.0, 0.32, 0.04, 1)
        _QuenteColor("Cor do magma quente", Color) = (1.0, 0.62, 0.12, 1)
        _EscalaDoFluxo("Escala do fluxo (rep/m)", Float) = 0.12
        _VelocidadeDoFluxo("Velocidade do fluxo (m/s)", Float) = 0.6
        _Emissao("Forca da emissao", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float  fogCoord   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CrostaColor;
                half4 _MagmaColor;
                half4 _QuenteColor;
                float _EscalaDoFluxo;
                float _VelocidadeDoFluxo;
                float _Emissao;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs v = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = v.positionCS;
                output.positionWS = v.positionWS;
                output.fogCoord = ComputeFogFactor(v.positionCS.z);
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * Ruido(p);
                    p = p * 2.03 + 17.0;
                    a *= 0.5;
                }
                return v;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz * _EscalaDoFluxo;
                float t = _Time.y * _VelocidadeDoFluxo * _EscalaDoFluxo;

                // Duas camadas que correm em direcoes diferentes; a crosta e onde o ruido sobe.
                float fluxoA = Fbm(p + float2(t, t * 0.6));
                float fluxoB = Fbm(p * 1.7 + float2(-t * 0.8, t * 1.1) + fluxoA * 1.5);
                float crosta = smoothstep(0.42, 0.62, fluxoB);
                float quente = smoothstep(0.42, 0.18, fluxoB);

                half3 magma = lerp(_MagmaColor.rgb, _QuenteColor.rgb, quente);
                half3 cor = lerp(magma * _Emissao, _CrostaColor.rgb + magma * 0.15, crosta);

                cor = MixFog(cor, input.fogCoord);
                return half4(cor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
