// Agua do pantano e dos rios: transparente, com cor pela profundidade (gravada no canal R da
// cor de vertice pelo ConstrutorDoTerreno) e normal em movimento (duas ondas somadas, sem textura).
Shader "JogoDoGrupo/AguaMundo"
{
    Properties
    {
        _RasaColor("Cor na margem", Color) = (0.30, 0.42, 0.34, 1)
        _FundaColor("Cor no fundo", Color) = (0.04, 0.12, 0.10, 1)
        _OpacidadeRasa("Opacidade na margem", Range(0, 1)) = 0.35
        _OpacidadeFunda("Opacidade no fundo", Range(0, 1)) = 0.92
        _EscalaDasOndas("Escala das ondas (rep/m)", Float) = 0.35
        _VelocidadeDasOndas("Velocidade das ondas", Float) = 0.35
        _ForcaDasOndas("Forca das ondas", Range(0, 1)) = 0.35
        _Brilho("Brilho do sol", Range(8, 256)) = 90
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float  profundidade : TEXCOORD1;
                float  fogCoord   : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _RasaColor;
                half4 _FundaColor;
                half _OpacidadeRasa;
                half _OpacidadeFunda;
                float _EscalaDasOndas;
                float _VelocidadeDasOndas;
                half _ForcaDasOndas;
                half _Brilho;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs v = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = v.positionCS;
                output.positionWS = v.positionWS;
                output.profundidade = input.color.r;
                output.fogCoord = ComputeFogFactor(v.positionCS.z);
                return output;
            }

            // Inclinacao da superficie: duas ondas cruzadas que andam em direcoes diferentes.
            float2 Ondas(float2 p, float t)
            {
                float2 a = p * _EscalaDasOndas;
                float2 d1 = float2(cos(a.x * 3.1 + a.y * 1.7 + t * 1.3), cos(a.y * 2.9 - a.x * 1.3 + t * 1.1));
                float2 d2 = float2(cos(a.x * 7.3 - a.y * 4.1 - t * 1.9), cos(a.y * 6.1 + a.x * 5.2 + t * 1.6)) * 0.5;
                return (d1 + d2) * 0.5;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _VelocidadeDasOndas * 3.0;
                float2 inclinacao = Ondas(input.positionWS.xz, t) * _ForcaDasOndas;
                half3 n = normalize(half3(-inclinacao.x, 1.0, -inclinacao.y));

                half prof = saturate(input.profundidade);
                half3 cor = lerp(_RasaColor.rgb, _FundaColor.rgb, sqrt(prof));
                half opacidade = lerp(_OpacidadeRasa, _OpacidadeFunda, sqrt(prof));

                Light luz = GetMainLight();
                half3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 h = normalize(luz.direction + v);
                half espelho = pow(saturate(dot(n, h)), _Brilho) * 0.8;
                half difusa = saturate(dot(n, luz.direction)) * 0.6 + 0.4;

                // Reflexo do ceu mais forte de raspao (Fresnel simples).
                half fresnel = pow(1.0 - saturate(dot(n, v)), 4.0);
                half3 ceu = SampleSH(n) + half3(0.15, 0.2, 0.25);

                // Espuma fina na margem: onde a profundidade e quase zero.
                half espuma = (1.0 - smoothstep(0.0, 0.08, prof)) * (0.6 + 0.4 * sin(t * 2.0 + input.positionWS.x * 0.9));
                cor = cor * difusa * luz.color + ceu * fresnel * 0.5 + luz.color * espelho + espuma * 0.25;
                opacidade = saturate(opacidade + fresnel * 0.25 + espelho + espuma * 0.2);

                cor = MixFog(cor, input.fogCoord);
                return half4(cor, opacidade);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
