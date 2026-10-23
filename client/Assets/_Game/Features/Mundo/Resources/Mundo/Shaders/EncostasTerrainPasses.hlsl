#ifndef JOGO_ENCOSTAS_TERRAIN_PASSES_INCLUDED
#define JOGO_ENCOSTAS_TERRAIN_PASSES_INCLUDED

// Extensao de amostragem do TerrainLit da URP 17.6. A mistura de camadas,
// iluminacao PBR, decals, GI e sombras continuam nos passes do pacote.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

float4 _EncostasCamadas;
float4 _EncostasEscala;
float _EncostasNitidez;
float _EncostasDistanciaBase;
static float3 encostasPositionWS;
static half3 encostasNormalWS;
static half3 encostasTangentWS;
static half3 encostasBitangentWS;
static half3 encostasPesos;
static half encostasForca;

half4 EncostasCor(TEXTURE2D_PARAM(textura, amostrador), float2 uv, half selecionada)
{
    if (selecionada < 0.5h || encostasForca < 0.001h)
        return SAMPLE_TEXTURE2D(textura, amostrador, uv);
    float2 uvX = encostasPositionWS.zy * _EncostasEscala.xy + _EncostasEscala.zw;
    float2 uvY = encostasPositionWS.xz * _EncostasEscala.xy + _EncostasEscala.zw;
    float2 uvZ = encostasPositionWS.xy * _EncostasEscala.xy + _EncostasEscala.zw;
    half4 triplanar = SAMPLE_TEXTURE2D(textura, amostrador, uvX) * encostasPesos.x
        + SAMPLE_TEXTURE2D(textura, amostrador, uvY) * encostasPesos.y
        + SAMPLE_TEXTURE2D(textura, amostrador, uvZ) * encostasPesos.z;
    if (encostasForca > 0.999h)
        return triplanar;
    return lerp(SAMPLE_TEXTURE2D(textura, amostrador, uv), triplanar, encostasForca);
}

half4 EncostasNormal(TEXTURE2D_PARAM(textura, amostrador), float2 uv, half selecionada, half escala)
{
    if (selecionada < 0.5h || encostasForca < 0.001h)
        return half4(UnpackNormalScale(SAMPLE_TEXTURE2D(textura, amostrador, uv), escala), 1);
    float2 uvX = encostasPositionWS.zy * _EncostasEscala.xy + _EncostasEscala.zw;
    float2 uvY = encostasPositionWS.xz * _EncostasEscala.xy + _EncostasEscala.zw;
    float2 uvZ = encostasPositionWS.xy * _EncostasEscala.xy + _EncostasEscala.zw;
    half3 normalX = UnpackNormalScale(SAMPLE_TEXTURE2D(textura, amostrador, uvX), escala);
    half3 normalY = UnpackNormalScale(SAMPLE_TEXTURE2D(textura, amostrador, uvY), escala);
    half3 normalZ = UnpackNormalScale(SAMPLE_TEXTURE2D(textura, amostrador, uvZ), escala);

    // Whiteout triplanar: um normal map plano devolve exatamente a normal do
    // terreno. Misturar diretamente as tres normais tangentes daria luz errada.
    normalX = half3(normalX.xy + encostasNormalWS.zy, abs(normalX.z) * encostasNormalWS.x);
    normalY = half3(normalY.xy + encostasNormalWS.xz, abs(normalY.z) * encostasNormalWS.y);
    normalZ = half3(normalZ.xy + encostasNormalWS.xy, abs(normalZ.z) * encostasNormalWS.z);
    half3 normalWS = normalize(normalX.zyx * encostasPesos.x
        + normalY.xzy * encostasPesos.y + normalZ.xyz * encostasPesos.z);
    // Base identica a InitializeInputData, incluindo a inversao do tangent do
    // Terrain e seus comprimentos na variante instanciada por pixel.
    half3 normalTS = half3(
        dot(normalWS, encostasTangentWS) / max(dot(encostasTangentWS, encostasTangentWS), 1e-5h),
        dot(normalWS, encostasBitangentWS) / max(dot(encostasBitangentWS, encostasBitangentWS), 1e-5h),
        dot(normalWS, encostasNormalWS));
    normalTS = normalize(normalTS);
    if (encostasForca < 0.999h)
    {
        half3 nativo = UnpackNormalScale(SAMPLE_TEXTURE2D(textura, amostrador, uv), escala);
        normalTS = normalize(lerp(nativo, normalTS, encostasForca));
    }
    return half4(normalTS, 1);
}

// O despacho fica limitado a TerrainLitPasses; os includes de iluminacao acima
// usam as macros originais. Cada camada nao selecionada mantem a amostra nativa.
#define ENCOSTAS_AMOSTRAS(i, componente) \
half4 EncostasSample_Splat##i(float2 uv) { return EncostasCor(TEXTURE2D_ARGS(_Splat##i, sampler_Splat0), uv, _EncostasCamadas.componente); } \
half4 EncostasSample_Mask##i(float2 uv) { return EncostasCor(TEXTURE2D_ARGS(_Mask##i, sampler_Mask0), uv, _EncostasCamadas.componente); } \
half4 EncostasSample_Normal##i(float2 uv) { return EncostasNormal(TEXTURE2D_ARGS(_Normal##i, sampler_Normal0), uv, _EncostasCamadas.componente, _NormalScale##i); }
ENCOSTAS_AMOSTRAS(0, x)
ENCOSTAS_AMOSTRAS(1, y)
ENCOSTAS_AMOSTRAS(2, z)
ENCOSTAS_AMOSTRAS(3, w)
#undef ENCOSTAS_AMOSTRAS

half4 EncostasSample_Control(float2 uv) { return SAMPLE_TEXTURE2D(_Control, sampler_Control, uv); }
half4 EncostasSample_MainTex(float2 uv) { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv); }
half4 EncostasSample_MetallicTex(float2 uv) { return SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, uv); }
#ifdef UNITY_INSTANCING_ENABLED
half4 EncostasSample_TerrainNormalmapTexture(float2 uv) { return SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, uv); }
#endif

#undef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) EncostasSample##textureName(coord2)
// Os helpers de normal ja desempacotaram os mapas na representacao tangente.
#define UnpackNormalScale(packedNormal, scale) ((packedNormal).xyz)
#define SplatmapFragment EncostasFragmentNativo
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
#undef SplatmapFragment
#undef UnpackNormalScale
#undef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) PLATFORM_SAMPLE_TEXTURE2D(textureName, samplerName, coord2)

void EncostasPreparar(Varyings IN)
{
    encostasPositionWS = GetAbsolutePositionWS(IN.positionWS);
    // O basemap distante nativo usa UV planar. Voltar gradualmente a essa
    // projecao antes da troca evita um salto visivel na textura da montanha.
    float distanciaBase = max(_EncostasDistanciaBase, 1.0);
    encostasForca = 1 - smoothstep(distanciaBase * 0.65, distanciaBase * 0.9,
        distance(IN.positionWS, GetCameraPositionWS()));
#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    float2 sampleCoords = (IN.uvMainAndLM.xy / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
    encostasNormalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
    half3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, encostasNormalWS);
    encostasTangentWS = -tangentWS;
    encostasBitangentWS = cross(encostasNormalWS, tangentWS);
#elif defined(_NORMALMAP)
    encostasNormalWS = normalize(IN.normal.xyz);
    encostasTangentWS = -IN.tangent.xyz;
    encostasBitangentWS = IN.bitangent.xyz;
#else
    encostasNormalWS = normalize(IN.normal.xyz);
    encostasTangentWS = half3(1, 0, 0);
    encostasBitangentWS = half3(0, 0, 1);
#endif
    encostasPesos = pow(abs(encostasNormalWS), clamp(_EncostasNitidez, 1, 8));
    encostasPesos /= max(dot(encostasPesos, 1.0h), 1e-5h);
}

#ifdef TERRAIN_GBUFFER
GBufferFragOutput SplatmapFragment(Varyings IN)
{
    EncostasPreparar(IN);
    return EncostasFragmentNativo(IN);
}
#else
void SplatmapFragment(Varyings IN, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    EncostasPreparar(IN);
    EncostasFragmentNativo(IN, outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}
#endif

#endif
