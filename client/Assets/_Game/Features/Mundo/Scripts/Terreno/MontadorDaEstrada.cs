using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>Superficie visual da estrada autoral, apoiada no relevo oficial.</summary>
    public static class MontadorDaEstrada
    {
        private const float PassoLongitudinal = 2f;
        private const int FaixasTransversais = 10;
        private const float FolgaVertical = 0.06f;
        private const float MetrosPorRepeticao = 4f;
        private const float TransicaoLateral = 1.5f;
        private const string CaminhoTextura = "Mundo/Estrada/dirt_diff_2k";

        public static void Criar(Transform pai)
        {
            var tracado = EstradaDoPasso.Tracado;
            var malha = CriarMalha(tracado);
            var objeto = new GameObject("EstradaDoPasso");
            objeto.transform.SetParent(pai, false);
            objeto.AddComponent<MeshFilter>().sharedMesh = malha;

            objeto.AddComponent<MeshRenderer>().sharedMaterial = CriarMaterial();
        }

        public static Material CriarMaterial()
        {
            var shader = Resources.Load<Shader>("EstradaMundo");
            if (shader == null)
            {
                throw new InvalidOperationException("Shader da estrada não encontrado.");
            }

            var textura = Resources.Load<Texture2D>(CaminhoTextura);
            if (textura == null)
            {
                throw new InvalidOperationException("Textura da estrada não encontrada.");
            }

            return new Material(shader) { name = "EstradaDoPasso_Terra", mainTexture = textura };
        }

        public static Mesh CriarMalha(TracadoDoMundo tracado)
        {
            return CriarMalha(tracado, MapaDoMundo.AlturaEm);
        }

        public static Mesh CriarMalha(TracadoDoMundo tracado, Func<float, float, float> alturaEm)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cores = new List<Color>();
            var triangulos = new List<int>();
            var metrosPercorridos = 0f;
            var colunas = FaixasTransversais + 1;
            for (var trecho = 0; trecho + 1 < tracado.Pontos.Length; trecho++)
            {
                var a = tracado.Pontos[trecho];
                var b = tracado.Pontos[trecho + 1];
                var dx = b.X - a.X;
                var dz = b.Z - a.Z;
                var comprimento = Mathf.Sqrt(dx * dx + dz * dz);
                if (comprimento < 0.01f)
                {
                    continue;
                }

                var etapas = Mathf.Max(1, Mathf.CeilToInt(comprimento / PassoLongitudinal));
                var normalX = -dz / comprimento;
                var normalZ = dx / comprimento;
                for (var passo = 0; passo <= etapas; passo++)
                {
                    var t = (float)passo / etapas;
                    var centroX = Mathf.Lerp(a.X, b.X, t);
                    var centroZ = Mathf.Lerp(a.Z, b.Z, t);
                    for (var coluna = 0; coluna < colunas; coluna++)
                    {
                        var meiaLarguraVisual = tracado.Largura * 0.5f + TransicaoLateral;
                        var lateral =
                            (coluna / (float)FaixasTransversais - 0.5f) * meiaLarguraVisual * 2f;
                        var x = centroX + normalX * lateral;
                        var z = centroZ + normalZ * lateral;
                        vertices.Add(new Vector3(x, alturaEm(x, z) + FolgaVertical, z));
                        var opacidade = Mathf.SmoothStep(
                            0f,
                            1f,
                            (meiaLarguraVisual - Mathf.Abs(lateral)) / TransicaoLateral
                        );
                        cores.Add(new Color(1f, 1f, 1f, opacidade));
                        uvs.Add(
                            new Vector2(
                                (metrosPercorridos + comprimento * t) / MetrosPorRepeticao,
                                lateral / MetrosPorRepeticao
                            )
                        );
                    }

                    if (passo == 0)
                    {
                        continue;
                    }

                    var anterior = vertices.Count - 2 * colunas;
                    for (var coluna = 0; coluna < FaixasTransversais; coluna++)
                    {
                        var v = anterior + coluna;
                        triangulos.Add(v);
                        triangulos.Add(v + 1);
                        triangulos.Add(v + colunas);
                        triangulos.Add(v + 1);
                        triangulos.Add(v + colunas + 1);
                        triangulos.Add(v + colunas);
                    }
                }

                metrosPercorridos += comprimento;
            }

            var malha = new Mesh { name = "EstradaDoPasso", indexFormat = IndexFormat.UInt32 };
            malha.SetVertices(vertices);
            malha.SetUVs(0, uvs);
            malha.SetColors(cores);
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            return malha;
        }
    }
}
