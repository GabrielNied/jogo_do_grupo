using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    public static class PerfilDaEstrada
    {
        public const float Passo = 0.5f;

        public static List<Vector3> Amostrar(EstradaNativa estrada)
        {
            var container = estrada.Spline;
            if (container == null || container.Splines.Count != 1 || container.Spline.Count < 2)
                throw new InvalidOperationException(
                    "Use uma spline com pelo menos dois pontos por estrada."
                );
            if (container.Spline.Closed)
                throw new InvalidOperationException("Nesta versão, use uma spline aberta.");
            var comprimento = container.CalculateLength();
            if (!float.IsFinite(comprimento) || comprimento < 1f || comprimento > 20000f)
                throw new InvalidOperationException(
                    "O comprimento precisa estar entre 1 e 20.000 metros."
                );
            var quantidade = Mathf.CeilToInt(comprimento / Passo);
            var pontos = new List<Vector3>(quantidade + 1);
            for (var i = 0; i <= quantidade; i++)
                pontos.Add((Vector3)container.EvaluatePosition(i / (float)quantidade));
            return pontos;
        }

        public static float MeiaLargura(EstradaNativa estrada, Vector3 ponto)
        {
            var ruido = Mathf.PerlinNoise(ponto.x * 0.17f + estrada.Semente, ponto.z * 0.17f);
            return estrada.Largura * 0.5f * (1f + (ruido - 0.5f) * estrada.VariacaoLateral);
        }

        public static float Distancia(Vector3 ponto, IReadOnlyList<Vector3> pontos)
        {
            var menor = float.PositiveInfinity;
            for (var i = 1; i < pontos.Count; i++)
            {
                var a = new Vector2(pontos[i - 1].x, pontos[i - 1].z);
                var b = new Vector2(pontos[i].x, pontos[i].z);
                var p = new Vector2(ponto.x, ponto.z);
                var vetor = b - a;
                var t = Mathf.Clamp01(
                    Vector2.Dot(p - a, vetor) / Mathf.Max(vetor.sqrMagnitude, 0.000001f)
                );
                menor = Mathf.Min(menor, Vector2.Distance(p, a + vetor * t));
            }
            return menor;
        }

        public static float Depressao(EstradaNativa estrada, Vector3 ponto, float distancia)
        {
            var raio = MeiaLargura(estrada, ponto) + estrada.Transicao;
            if (distancia >= raio)
                return 0f;
            var t = Mathf.Clamp01(distancia / raio);
            return estrada.Rebaixo * (1f - t * t * (3f - 2f * t));
        }

        public static float RaioMaximo(EstradaNativa estrada)
        {
            return estrada.Largura * 0.5f * (1f + estrada.VariacaoLateral * 0.5f)
                + estrada.TransicaoDoCorredor
                + estrada.MargemVegetacao;
        }
    }
}
