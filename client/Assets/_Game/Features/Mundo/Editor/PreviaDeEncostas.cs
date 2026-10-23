using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Overlay descartavel: nunca troca TerrainData, material ou conteudo da cena.</summary>
    public sealed class PreviaDeEncostas : IDisposable
    {
        private readonly List<Mesh> _malhas = new List<Mesh>();
        private List<PosicaoDeRocha> _rochas;
        private Material _material;
        private const int Resolucao = 32;

        public PreviaDeEncostas() => SceneView.duringSceneGui += Desenhar;

        public void Atualizar(Terrain[] terrenos, ConfigDeEncostas pintura, ConfigDeRochas rochas)
        {
            Limpar();
            try
            {
                _material = new Material(Shader.Find("Hidden/Internal-Colored"))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _material.SetInt("_Cull", (int)CullMode.Off);
                _material.SetInt("_ZWrite", 0);
                _material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                foreach (var terreno in terrenos)
                {
                    var dados = terreno.terrainData;
                    var vertices = new Vector3[(Resolucao + 1) * (Resolucao + 1)];
                    var cores = new Color[vertices.Length];
                    var triangulos = new List<int>();
                    for (var z = 0; z <= Resolucao; z++)
                    for (var x = 0; x <= Resolucao; x++)
                    {
                        var u = (float)x / Resolucao;
                        var v = (float)z / Resolucao;
                        var indice = z * (Resolucao + 1) + x;
                        var mundo =
                            terreno.transform.position
                            + new Vector3(
                                u * dados.size.x,
                                dados.GetInterpolatedHeight(u, v),
                                v * dados.size.z
                            );
                        vertices[indice] = mundo + dados.GetInterpolatedNormal(u, v) * 0.12f;
                        var peso = pintura.Mascara(mundo, dados.GetSteepness(u, v));
                        cores[indice] = new Color(1f, 0.55f, 0.08f, peso * 0.7f);
                        if (
                            x == Resolucao
                            || z == Resolucao
                            || PinturaDeEncostas.Buraco(
                                dados,
                                (x + 0.5f) / Resolucao,
                                (z + 0.5f) / Resolucao
                            )
                        )
                            continue;
                        triangulos.AddRange(
                            new[]
                            {
                                indice,
                                indice + Resolucao + 1,
                                indice + 1,
                                indice + 1,
                                indice + Resolucao + 1,
                                indice + Resolucao + 2,
                            }
                        );
                    }
                    var malha = new Mesh
                    {
                        name = "Previa temporaria de encostas",
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    _malhas.Add(malha);
                    malha.vertices = vertices;
                    malha.colors = cores;
                    malha.SetTriangles(triangulos, 0);
                    malha.RecalculateBounds();
                }
                if (rochas != null && RevestimentoDeEncostas.Validar(rochas) == null)
                    _rochas = RevestimentoDeEncostas.Planejar(terrenos, pintura, rochas);
                SceneView.RepaintAll();
            }
            catch
            {
                Limpar();
                throw;
            }
        }

        private void Desenhar(SceneView scene)
        {
            if (
                Event.current.type != EventType.Repaint
                || EditorApplication.isPlayingOrWillChangePlaymode
            )
                return;
            if (_material != null && _material.SetPass(0))
                foreach (var malha in _malhas)
                    Graphics.DrawMeshNow(malha, Matrix4x4.identity);
            if (_rochas == null)
                return;
            using (new Handles.DrawingScope(Color.cyan))
                foreach (var rocha in _rochas)
                    Handles.DrawWireDisc(
                        rocha.Ponto + rocha.Normal * 0.2f,
                        rocha.Normal,
                        rocha.Tamanho * 0.5f
                    );
        }

        public void Limpar()
        {
            foreach (var malha in _malhas)
                if (malha != null)
                    Object.DestroyImmediate(malha);
            _malhas.Clear();
            if (_material != null)
                Object.DestroyImmediate(_material);
            _material = null;
            _rochas = null;
            SceneView.RepaintAll();
        }

        public void Dispose()
        {
            SceneView.duringSceneGui -= Desenhar;
            Limpar();
        }
    }
}
