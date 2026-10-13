Shader "JogoDoGrupo/TerrenoMundo"
{
    Properties
    {
        [MainTexture] _BaseMap("Macro Map (Cores dos Biomas)", 2D) = "white" {}
        _GrassTex("Grama (High-Res 512)", 2D) = "white" {}
        _DirtTex("Terra (High-Res 512)", 2D) = "white" {}
        _SandTex("Areia (High-Res 512)", 2D) = "white" {}
        _RockTex("Rocha (High-Res 512)", 2D) = "white" {}
        _SnowTex("Neve (High-Res 512)", 2D) = "white" {}
        _DetailAlbedoMap("Detalhes (1024)", 2D) = "gray" {}
        _DetailTiling("World Tiling (rep/m)", Float) = 0.2
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD3;
                float fogCoord      : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_GrassTex);
            SAMPLER(sampler_GrassTex);

            TEXTURE2D(_DirtTex);
            SAMPLER(sampler_DirtTex);

            TEXTURE2D(_SandTex);
            SAMPLER(sampler_SandTex);

            TEXTURE2D(_RockTex);
            SAMPLER(sampler_RockTex);

            TEXTURE2D(_SnowTex);
            SAMPLER(sampler_SnowTex);

            TEXTURE2D(_DetailAlbedoMap);
            SAMPLER(sampler_DetailAlbedoMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _DetailTiling;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Tint macro do mapa de 3 km (cores base do bioma)
                half4 macroColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // Coordenadas mundiais para repetição de alta frequência física
                float escala = _DetailTiling > 0.001 ? _DetailTiling : 0.2;
                float2 worldUV = input.positionWS.xz * escala;

                // Amostras de alta definição dos materiais
                half4 grass = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, worldUV);
                half4 dirt  = SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, worldUV);
                half4 sand  = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, worldUV);
                half4 snow  = SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, worldUV);
                half4 detail = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, worldUV * 1.5);

                // Projeção triplanar para a rocha dos paredões e penhascos (elimina distorção vertical)
                float3 pesosTriplanar = saturate(abs(input.normalWS));
                pesosTriplanar = pow(pesosTriplanar, 4.0);
                pesosTriplanar /= (pesosTriplanar.x + pesosTriplanar.y + pesosTriplanar.z + 0.0001);

                half4 rochaLadoX = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, input.positionWS.zy * (escala * 0.75));
                half4 rochaLadoZ = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, input.positionWS.xy * (escala * 0.75));
                half4 rochaTopoY = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, worldUV * 0.75);
                half4 rock = rochaLadoX * pesosTriplanar.x + rochaTopoY * pesosTriplanar.y + rochaLadoZ * pesosTriplanar.z;

                // Fator de penhasco / paredão rochoso pela inclinação da normal
                float inclinacao = saturate(1.0 - input.normalWS.y);
                float fatorParedao = smoothstep(0.35, 0.65, inclinacao);

                // Detecção do tipo de chão dominante pela cor macro
                float lum = (macroColor.r + macroColor.g + macroColor.b) * 0.3333;
                float ehNeve = smoothstep(0.82, 0.94, lum);
                float ehAreia = smoothstep(0.12, 0.35, macroColor.r - macroColor.g * 0.75) * (1.0 - ehNeve);

                // Mistura orgânica de grama e terra
                float variacaoSolo = sin(input.positionWS.x * 0.35) * cos(input.positionWS.z * 0.35);
                half4 chao = lerp(grass, dirt, saturate(variacaoSolo * 0.45 + 0.15));

                // Mescla com areia nas praias e dunas
                chao = lerp(chao, sand, ehAreia);

                // Mescla com neve nos picos e cumes
                chao = lerp(chao, snow, ehNeve);

                // Mescla com rocha triplanar nos paredões íngremes
                chao = lerp(chao, rock, fatorParedao);

                // Modulação: cor do bioma combinada com textura física e micro-relevo
                half3 albedo = macroColor.rgb * chao.rgb * (detail.rgb * 2.0);

                // Iluminação URP (luz direcional + sombras)
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light luzPrincipal = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(input.normalWS, luzPrincipal.direction));
                half3 difusa = luzPrincipal.color * (luzPrincipal.distanceAttenuation * luzPrincipal.shadowAttenuation * NdotL);

                // Luz ambiente harmônica esférica (SH)
                half3 ambiente = SampleSH(input.normalWS);
                half3 corFinal = albedo * (difusa + ambiente);

                // Névoa atmosférica
                corFinal = MixFog(corFinal, input.fogCoord);

                return half4(corFinal, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
