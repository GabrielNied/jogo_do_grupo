#ifndef JOGO_ENCOSTAS_TERRAIN_DEPTH_NORMALS_INCLUDED
#define JOGO_ENCOSTAS_TERRAIN_DEPTH_NORMALS_INCLUDED

void EncostasDepthNormalsFragment(Varyings IN, out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
#ifdef _ALPHATEST_ON
    ClipHoles(IN.uvMainAndLM.xy);
#endif
    EncostasPreparar(IN);
    float2 splatUV = (IN.uvMainAndLM.xy * (_Control_TexelSize.zw - 1) + 0.5) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
    half3 normalTS = half3(0, 0, 1);
    NormalMapMix(IN.uvSplat01, IN.uvSplat23, splatControl, normalTS);
    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);
#ifdef _GBUFFER_NORMALS_OCT
    float2 octNormalWS = PackNormalOctQuadEncode(inputData.normalWS);
    outNormalWS = half4(PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5)), 0);
#else
    outNormalWS = half4(inputData.normalWS, 0);
#endif
#ifdef _WRITE_SMOOTHNESS
    half4 hasMask = half4(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3);
    half4 masks[4];
    ComputeMasks(masks, hasMask, IN.uvSplat01, IN.uvSplat23);
    half weight;
    half4 diffuse;
    half4 defaultSmoothness;
    SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, splatControl, weight, diffuse, defaultSmoothness, normalTS);
    half4 smoothness = lerp(defaultSmoothness, half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a), hasMask);
    outNormalWS.a = dot(splatControl, smoothness);
#endif
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
