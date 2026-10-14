Shader "JogoDoGrupo/TerrenoMundo"
{
    Properties
    {
        [MainTexture] _BaseMap("Macro Map (Cores dos Biomas)", 2D) = "white" {}
        _GrassTex("Grama (High-Res 512)", 2D) = "white" {}
        _DirtTex("Terra (High-Res 512)", 2D) = "white" {}
        _SandTex("Areia (High-Res 512)", 2D) = "white" {}
        _RockTex("Rocha (High-Res 512)", 2D) = "white" {}
        _RockBumpMap("Normal Map de Rocha", 2D) = "bump" {}
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

            TEXTURE2D(_RockBumpMap);
            SAMPLER(sampler_RockBumpMap);

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
                // Tint macro do mapa de 3 km (cores base do bioma e iluminação global)
                half4 macroColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // 1. Acentuação da quina no topo da encosta:
                // Mede a normal geométrica exata do triângulo via derivadas de tela.
                // Quando há dobra/quina entre o topo plano e a encosta íngreme,
                // recupera a quebra nítida (quina acentuada) sem curvatura inflada de balão.
                float3 dX = ddx(input.positionWS);
                float3 dY = ddy(input.positionWS);
                float3 faceNormalWS = normalize(cross(dY, dX));
                if (dot(faceNormalWS, input.normalWS) < 0.0)
                {
                    faceNormalWS = -faceNormalWS;
                }

                float diferencaNormal = saturate(1.0 - dot(input.normalWS, faceNormalWS));
                float pesoQuina = smoothstep(0.03, 0.18, diferencaNormal);
                float3 normalAcentuada = normalize(lerp(input.normalWS, faceNormalWS, pesoQuina * 0.75));

                // Dual-scale world tiling: mistura escala micro (4 m) e macro (25 m)
                // para eliminar completamente repetição em xadrez/grelha
                float2 uvMicro = input.positionWS.xz * 0.25;
                float2 uvMacro = input.positionWS.xz * 0.04;

                half4 grass = lerp(SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uvMicro),
                                   SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uvMacro), 0.28);

                half4 dirt  = lerp(SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, uvMicro),
                                   SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, uvMacro), 0.28);

                half4 sand  = lerp(SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvMicro),
                                   SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvMacro), 0.28);

                half4 snow  = lerp(SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, uvMicro),
                                   SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, uvMacro), 0.25);

                // Projeção triplanar contínua para albedo e rugosidade modelada da rocha
                float3 absNorm = saturate(abs(normalAcentuada));
                float3 pesosTriplanar = pow(absNorm, 4.0);
                pesosTriplanar /= (pesosTriplanar.x + pesosTriplanar.y + pesosTriplanar.z + 0.0001);

                float2 uvRockX = input.positionWS.zy * 0.20;
                float2 uvRockZ = input.positionWS.xy * 0.20;
                float2 uvRockY = input.positionWS.xz * 0.20;

                half4 rockSideX = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, uvRockX);
                half4 rockSideZ = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, uvRockZ);
                half4 rockTopY  = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, uvRockY);
                half4 rock = rockSideX * pesosTriplanar.x + rockTopY * pesosTriplanar.y + rockSideZ * pesosTriplanar.z;

                // 2. Rugosidade modelada 3D na pedra (Normal Map triplanar):
                // Captura fisicamente o relevo de estratos, degraus e facetas sob a luz direcional
                half3 nRockX = UnpackNormal(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, uvRockX));
                half3 nRockZ = UnpackNormal(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, uvRockZ));
                half3 nRockY = UnpackNormal(SAMPLE_TEXTURE2D(_RockBumpMap, sampler_RockBumpMap, uvRockY));

                float signX = sign(normalAcentuada.x);
                float signZ = sign(normalAcentuada.z);
                float3 worldNormX = float3(nRockX.z * signX, nRockX.y, nRockX.x * signX);
                float3 worldNormZ = float3(nRockZ.x * signZ, nRockZ.y, nRockZ.z * signZ);
                float3 worldNormY = float3(nRockY.x, nRockY.z, nRockY.y);

                float3 rockNormalWS = normalize(worldNormX * pesosTriplanar.x + worldNormY * pesosTriplanar.y + worldNormZ * pesosTriplanar.z);

                // 3. Transição nítida na quina entre topo plano e encosta de rocha:
                // Inclinações acima de 15 graus começam a virar rocha e em 25 graus já são 100% rocha,
                // cobrindo a encosta inteira do morro até a quina do topo plano.
                float inclinacao = saturate(1.0 - normalAcentuada.y);
                float variacaoRelevo = (SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, input.positionWS.xz * 0.06).r - 0.5) * 0.04;
                float fatorParedao = smoothstep(0.035 + variacaoRelevo, 0.100 + variacaoRelevo, inclinacao);

                // Perturba a normal com o relevo 3D modelado de rocha (ondas serrilhadas)
                float3 normalFinalWS = normalize(lerp(normalAcentuada, rockNormalWS, fatorParedao * 0.90));

                // Identificação orgânica dos materiais pelo mapa macro
                float ehNeve = smoothstep(0.78, 0.90, macroColor.b) * step(0.68, macroColor.r);

                // Anel contínuo de areia em toda a margem de água do pântano e lagoas (Y próximo de -0.5 m)
                float deltaAguaWS = abs(input.positionWS.y - (-0.5));
                float bordaAgua = 1.0 - saturate(deltaAguaWS / 0.85);
                float ehMargemPantano = bordaAgua * smoothstep(0.10, 0.35, macroColor.g + macroColor.r * 0.5);

                float ehAreiaMacro = saturate((macroColor.r - macroColor.b - 0.08) * 4.5);
                float ehAreia = max(ehAreiaMacro, ehMargemPantano) * (1.0 - ehNeve);

                float ehTerra = saturate((macroColor.r - macroColor.g * 0.88) * 3.5) * (1.0 - ehNeve) * (1.0 - ehAreia);
                float ehGrama = saturate((macroColor.g - macroColor.r * 0.82) * 4.0) * (1.0 - ehNeve) * (1.0 - ehAreia);

                half4 chao = grass * ehGrama + dirt * ehTerra + sand * ehAreia + snow * ehNeve;
                float somaPesos = ehGrama + ehTerra + ehAreia + ehNeve;
                chao = somaPesos > 0.01 ? (chao / somaPesos) : grass;

                // Encostas e paredões usam rocha com relevo modelado
                chao = lerp(chao, rock, fatorParedao);

                // Micro-relevo sutil (30% de intensidade) centrado em 0.5
                half4 detail = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, input.positionWS.xz * 0.5);
                half3 microRelevo = lerp(half3(1.0, 1.0, 1.0), detail.rgb * 2.0, 0.30);

                // Tint suave do bioma: preserva a rocha natural pura em paredões e tinge o chão plano
                half3 tintBioma = lerp(half3(1.0, 1.0, 1.0), macroColor.rgb * 1.35, 0.45 * (1.0 - fatorParedao * 0.88));

                // Realce sutil da quina geológica no topo da encosta
                half realceQuina = pesoQuina * smoothstep(0.15, 0.60, fatorParedao) * 0.12;

                half3 albedo = (chao.rgb * tintBioma * microRelevo) + realceQuina;

                // Iluminação URP com sombras e relevo modelado
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light luzPrincipal = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(normalFinalWS, luzPrincipal.direction));
                half3 difusa = luzPrincipal.color * (luzPrincipal.distanceAttenuation * luzPrincipal.shadowAttenuation * NdotL);

                // Luz ambiente harmônica esférica (SH) usando a normal com relevo modelado
                half3 ambiente = SampleSH(normalFinalWS);
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
