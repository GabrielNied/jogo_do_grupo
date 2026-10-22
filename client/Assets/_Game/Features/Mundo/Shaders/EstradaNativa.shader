Shader "JogoDoGrupo/Mundo/EstradaNativa"
{
    Properties
    {
        [MainTexture] _BaseMap ("Terra CC0 (opcional)", 2D) = "white" {}
        _UsarTextura ("Usar textura de terra", Range(0, 1)) = 0
        _CorTerra ("Tom da terra", Color) = (0.48, 0.36, 0.23, 1)
        _CorPedra ("Tom das lajotas e pedras", Color) = (0.42, 0.43, 0.39, 1)
        [Enum(Terra,0,Lajotas,1,Pedregulhos,2)] _Tipo ("Superfície", Float) = 0
        _Cascos ("Vestígios de cascos", Range(0, 1)) = 0.25
        _Semente ("Semente", Float) = 17
        _Smoothness ("Suavidade", Range(0, 1)) = 0.12
        [HideInInspector] _ZWrite ("Gravar profundidade", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-20" }

        // O Terrain fornece a profundidade; a superfície mistura somente as margens.
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _CorTerra;
                half4 _CorPedra;
                half _UsarTextura;
                half _Tipo;
                half _Cascos;
                float _Semente;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : TEXCOORD3;
                half4 fogAndVertexLight : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p)
            {
                float3 q = frac(float3(p.x, p.y, p.x) * 0.1031 + _Semente * 0.0137);
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            float Ruido(float2 p)
            {
                float2 celula = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(celula), Hash(celula + float2(1, 0)), f.x),
                    lerp(Hash(celula + float2(0, 1)), Hash(celula + float2(1, 1)), f.x), f.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs posicao = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posicao.positionCS;
                output.positionWS = posicao.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color;
                output.fogAndVertexLight = half4(ComputeFogFactor(posicao.positionCS.z),
                    VertexLighting(posicao.positionWS, output.normalWS));
                return output;
            }

            float Cascos(float2 uv)
            {
                // Uma impressão esparsa por célula, com posição e direção independentes.
                float2 celula = floor(uv / float2(1.7, 2.3));
                float escolha = Hash(celula + 41.0);
                float2 centro = (celula + float2(Hash(celula + 7.0), Hash(celula + 19.0)))
                    * float2(1.7, 2.3);
                float2 q = uv - centro;
                float angulo = (Hash(celula + 63.0) - 0.5) * 0.8
                    + step(0.5, Hash(celula + 83.0)) * 3.14159265;
                q = float2(q.x * cos(angulo) - q.y * sin(angulo),
                    q.x * sin(angulo) + q.y * cos(angulo));
                float arco = abs(length(q / float2(0.058, 0.075)) - 0.82);
                float marca = 1.0 - smoothstep(0.1, 0.26, arco);
                marca *= smoothstep(-0.045, -0.025, q.y);
                float detalhe = 1.0 - smoothstep(0.025, 0.12,
                    max(length(ddx(uv)), length(ddy(uv))));
                return marca * step(escolha, 0.22) * detalhe * _Cascos;
            }

            half3 NormalDoRelevo(float3 posicao, half3 normal, float altura)
            {
                float3 dx = ddx(posicao);
                float3 dy = ddy(posicao);
                float3 r1 = cross(dy, normal);
                float3 r2 = cross(normal, dx);
                float determinante = dot(dx, r1);
                float3 gradiente = sign(determinante) * (ddx(altura) * r1 + ddy(altura) * r2)
                    / max(abs(determinante), 0.00001);
                return normalize(normal - gradiente);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.uv;
                float metrosPorPixel = max(length(ddx(uv)), length(ddy(uv)));
                float macro = Ruido(input.positionWS.xz * 0.16);
                float grao = lerp(0.5, Ruido(uv * 23.0),
                    1.0 - smoothstep(0.015, 0.06, metrosPorPixel));
                float micro = lerp(0.5, Ruido(uv * 71.0),
                    1.0 - smoothstep(0.005, 0.018, metrosPorPixel));
                half3 terraProcedural = _CorTerra.rgb * lerp(0.78, 1.2, grao);
                half3 textura = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.positionWS.xz * 0.32).rgb;
                half3 texturaMacro = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.positionWS.xz * 0.073).rgb;
                half3 terra = lerp(terraProcedural,
                    lerp(textura, texturaMacro, 0.22) * _CorTerra.rgb * 2.1, _UsarTextura);
                terra *= lerp(0.82, 1.1, macro);
                float marcas = Cascos(uv);
                terra *= 1.0 - marcas * 0.28;
                float altura = grao * 0.005 + micro * 0.0015 - marcas * 0.003;
                half3 albedo = terra;
                half oclusao = 1.0 - marcas * 0.16;

                if ((_Tipo > 0.5 && _Tipo < 1.5) || _Tipo > 2.5)
                {
                    // Lajotas desencontradas em escala métrica, com juntas irregulares.
                    float2 grade = uv * float2(3.3, 2.35);
                    grade += float2(Ruido(uv * 8.0), Ruido(uv * 8.0 + 9.0)) * 0.065;
                    grade.x += floor(grade.y) * 0.5;
                    float2 celula = floor(grade);
                    float2 dentro = frac(grade);
                    float2 borda = min(dentro, 1.0 - dentro) / float2(3.3, 2.35);
                    float distanciaDaJunta = min(borda.x, borda.y);
                    float filtroDaJunta = max(fwidth(distanciaDaJunta), 0.001);
                    float junta = smoothstep(0.012 - filtroDaJunta,
                        0.02 + filtroDaJunta, distanciaDaJunta);
                    float pedra = Hash(celula + 131.0);
                    half3 lajota = _CorPedra.rgb * lerp(0.76, 1.24, pedra)
                        * lerp(0.91, 1.07, grao);
                    albedo = lerp(terra * 0.7, lajota, junta);
                    altura = lerp(-0.006, 0.008 + pedra * 0.007, junta) + micro * 0.001;
                    oclusao = lerp(0.65, 1.0, junta);
                    float detalheDaLajota = 1.0 - smoothstep(0.1, 0.32, metrosPorPixel);
                    albedo = lerp(_CorPedra.rgb * 0.86, albedo, detalheDaLajota);
                    altura *= detalheDaLajota;
                    if (_Tipo > 2.5)
                    {
                        float pesoLajotas = smoothstep(0.0, 1.0, input.color.r);
                        albedo = lerp(terra, albedo, pesoLajotas);
                        altura = lerp(grao * 0.005 + micro * 0.0015 - marcas * 0.003,
                            altura, pesoLajotas);
                        oclusao = lerp(1.0 - marcas * 0.16, oclusao, pesoLajotas);
                    }
                }
                else if (_Tipo > 1.5)
                {
                    albedo = _CorPedra.rgb * input.color.rgb * lerp(0.86, 1.12, grao);
                    altura = micro * 0.002;
                }

                float margem = saturate(input.color.a);
                // Ruído só na faixa de mistura mantém o caminho legível e quebra o recorte.
                float ruidoDaMargem = lerp(Ruido(input.positionWS.xz * 2.8),
                    Ruido(input.positionWS.xz * 8.0), 0.25);
                float alpha = smoothstep(0.0, 1.0, saturate(margem + (ruidoDaMargem - 0.5)
                    * margem * (1.0 - margem) * 2.2));
                clip(alpha - 0.003);
                half3 normal = NormalDoRelevo(input.positionWS, normalize(input.normalWS), altura);
                InputData dados = (InputData)0;
                dados.positionWS = input.positionWS;
                dados.positionCS = input.positionCS;
                dados.normalWS = normal;
                dados.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                dados.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                dados.fogCoord = input.fogAndVertexLight.x;
                dados.vertexLighting = input.fogAndVertexLight.yzw;
                dados.bakedGI = SampleSH(normal);
                dados.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                dados.shadowMask = half4(1, 1, 1, 1);

                SurfaceData superficie = (SurfaceData)0;
                superficie.albedo = albedo;
                superficie.metallic = 0;
                superficie.specular = half3(0.04, 0.04, 0.04);
                superficie.smoothness = _Smoothness;
                superficie.normalTS = half3(0, 0, 1);
                superficie.occlusion = oclusao;
                superficie.alpha = alpha;
                half4 cor = UniversalFragmentPBR(dados, superficie);
                cor.rgb = MixFog(cor.rgb, dados.fogCoord);
                return half4(cor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
