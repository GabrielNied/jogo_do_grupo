using System;
using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using UnityEditor;
using UnityEngine;
using Via = JogoDoGrupo.Client.Features.Mundo.Editor.Estradas.AplicadorDeEstradas.ViaAplicada;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    public static partial class VisualDaEstrada
    {
        private const float PassoDaRede = 0.3f;
        private const int LimiteDeCelulasDaRede = 600000;

        internal static void ValidarCustoDaRede(IReadOnlyList<Via> vias)
        {
            foreach (var grupo in GruposVisuais(vias))
            {
                if (grupo.Count <= 1)
                    continue;
                if (grupo.Any(v => v.Estrada.Material != null))
                    throw new InvalidOperationException(
                        "Cruzamentos usam o shader nativo para misturar terra e lajotas. Retire o material personalizado das vias conectadas antes de aplicar; nada foi alterado."
                    );
                CelulasDaRede(grupo);
            }
        }

        private static List<List<Via>> GruposVisuais(IReadOnlyList<Via> vias)
        {
            var restantes = new HashSet<Via>(vias);
            var grupos = new List<List<Via>>();
            foreach (var inicio in vias)
            {
                if (!restantes.Remove(inicio))
                    continue;
                var grupo = new List<Via> { inicio };
                for (var i = 0; i < grupo.Count; i++)
                {
                    var atual = grupo[i];
                    foreach (var outra in vias)
                    {
                        if (!restantes.Contains(outra))
                            continue;
                        var raio =
                            PerfilDaEstrada.RaioMaximo(atual.Estrada)
                            + PerfilDaEstrada.RaioMaximo(outra.Estrada)
                            + PerfilDaEstrada.Passo;
                        var corredor = new CorredorDaEstrada(outra.Pontos, raio);
                        if (!atual.Pontos.Any(p => corredor.Distancia(p) <= raio))
                            continue;
                        restantes.Remove(outra);
                        grupo.Add(outra);
                    }
                }
                grupos.Add(grupo);
            }
            return grupos;
        }

        private static HashSet<Vector2Int> CelulasDaRede(IReadOnlyList<Via> vias)
        {
            var celulas = new HashSet<Vector2Int>();
            foreach (var via in vias)
            {
                var estrada = via.Estrada;
                var raio =
                    estrada.Largura * 0.5f * (1f + estrada.VariacaoLateral * 0.5f)
                    + estrada.TransicaoVisual
                    + PassoDaRede;
                // Rasteriza somente as caixas de cada segmento. O vazio entre vias não custa grade.
                for (var i = 1; i < via.Pontos.Count; i++)
                {
                    var a = via.Pontos[i - 1];
                    var b = via.Pontos[i];
                    for (
                        var z = Mathf.FloorToInt((Mathf.Min(a.z, b.z) - raio) / PassoDaRede);
                        z <= Mathf.CeilToInt((Mathf.Max(a.z, b.z) + raio) / PassoDaRede);
                        z++
                    )
                    for (
                        var x = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - raio) / PassoDaRede);
                        x <= Mathf.CeilToInt((Mathf.Max(a.x, b.x) + raio) / PassoDaRede);
                        x++
                    )
                    {
                        celulas.Add(new Vector2Int(x, z));
                        if (celulas.Count > LimiteDeCelulasDaRede)
                            throw new InvalidOperationException(
                                "A rede excede 600.000 células visuais. Divida a autoria em cenas menores; nada foi alterado no Terrain."
                            );
                    }
                }
            }
            return celulas;
        }

        private static float Cobertura(Via via, Vector3 p)
        {
            var distancia = via.Corredor.Distancia(p);
            var estrada = via.Estrada;
            return Mathf.Clamp01(
                (PerfilDaEstrada.MeiaLargura(estrada, p) + estrada.TransicaoVisual - distancia)
                    / estrada.TransicaoVisual
            );
        }

        internal static void GerarRede(IReadOnlyList<Via> vias, Func<Vector3, float> alturaEm)
        {
            foreach (var grupo in GruposVisuais(vias))
                GerarGrupo(grupo, alturaEm);
        }

        private static void GerarGrupo(IReadOnlyList<Via> vias, Func<Vector3, float> alturaEm)
        {
            if (vias.Count == 0)
                return;
            // Vias isoladas continuam com as malhas longitudinais originais, preservando aparência
            // e materiais personalizados. Redes usam coordenadas mundiais e uma só cobertura.
            if (vias.Count == 1)
            {
                var via = vias[0];
                Gerar(
                    via.Estrada,
                    via.Pontos,
                    alturaEm,
                    AplicadorDeEstradas.PastaGerada + "/" + via.Estrada.Id
                );
                return;
            }
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var cores = new List<Color>();
            var triangulos = new List<int>();
            var indices = new Dictionary<Vector2Int, int>();
            var dono = vias[0].Estrada;
            int Vertice(Vector2Int chave)
            {
                if (indices.TryGetValue(chave, out var existente))
                    return existente;
                var p = new Vector3(chave.x * PassoDaRede, 0f, chave.y * PassoDaRede);
                var alpha = 0f;
                var lajotas = 0f;
                foreach (var via in vias)
                {
                    var cobertura = Cobertura(via, p);
                    // União suave de cobertura: somente duas faixas presentes arredondam o canto.
                    // Zero permanece zero, a borda exterior não se espalha para fora da limpeza.
                    alpha = 1f - (1f - alpha) * (1f - cobertura);
                    if (via.Estrada.Tipo == TipoDeEstrada.Lajotas)
                        lajotas = Mathf.Max(lajotas, cobertura);
                }
                var indice = vertices.Count;
                indices.Add(chave, indice);
                // Células transparentes fora do Terrain não precisam consultar sua altura.
                p.y = alpha > 0f ? alturaEm(p) + FolgaVertical : 0f;
                vertices.Add(dono.transform.InverseTransformPoint(p));
                uvs.Add(new Vector2(p.x, p.z));
                cores.Add(new Color(lajotas, 1f, 1f, alpha));
                return indice;
            }
            foreach (var celula in CelulasDaRede(vias).OrderBy(c => c.y).ThenBy(c => c.x))
            {
                var a = Vertice(celula);
                var b = Vertice(celula + Vector2Int.up);
                var c = Vertice(celula + Vector2Int.right);
                var d = Vertice(celula + Vector2Int.one);
                if (cores[a].a + cores[b].a + cores[c].a + cores[d].a <= 0f)
                    continue;
                // Ajusta também vértices transparentes das células de borda para evitar paredes.
                foreach (var indice in new[] { a, b, c, d })
                {
                    if (cores[indice].a > 0f)
                        continue;
                    var p = dono.transform.TransformPoint(vertices[indice]);
                    try
                    {
                        p.y = alturaEm(p) + FolgaVertical;
                    }
                    catch (InvalidOperationException)
                    {
                        p.y = dono.transform.TransformPoint(vertices[a]).y;
                    }
                    vertices[indice] = dono.transform.InverseTransformPoint(p);
                }
                triangulos.Add(a);
                triangulos.Add(b);
                triangulos.Add(c);
                triangulos.Add(c);
                triangulos.Add(b);
                triangulos.Add(d);
            }
            var pasta = AplicadorDeEstradas.PastaGerada + "/" + dono.Id;
            var material = ObterMaterial(dono, pasta + "/Estrada.mat", false);
            material.SetFloat("_Tipo", 3f);
            EditorUtility.SetDirty(material);
            var malha = FinalizarMalha(
                "Rede contínua de estradas",
                vertices,
                uvs,
                cores,
                triangulos
            );
            AtualizarObjeto(
                dono,
                NomeDaSuperficie,
                PersistirMalha(malha, pasta + "/Superficie.asset"),
                material
            );
            foreach (var via in vias)
            {
                var estrada = via.Estrada;
                if (!estrada.Pedregulhos || estrada.DensidadeCascalhos <= 0f)
                    continue;
                var pastaVia = AplicadorDeEstradas.PastaGerada + "/" + estrada.Id;
                var pedras = CriarPedregulhos(
                    estrada,
                    via.Pontos,
                    alturaEm,
                    p =>
                    {
                        var cobertura = Cobertura(via, p);
                        // Prioridade geométrica, depois lajotas, depois identidade estável; sem ordem de aplicação.
                        foreach (var outra in vias)
                        {
                            if (outra == via)
                                continue;
                            var outraCobertura = Cobertura(outra, p);
                            if (outraCobertura > cobertura + 0.00001f)
                                return false;
                            if (
                                Mathf.Abs(outraCobertura - cobertura) <= 0.00001f
                                && (
                                    outra.Estrada.Tipo > estrada.Tipo
                                    || (
                                        outra.Estrada.Tipo == estrada.Tipo
                                        && string.CompareOrdinal(outra.Estrada.Id, estrada.Id) < 0
                                    )
                                )
                            )
                                return false;
                        }
                        return true;
                    }
                );
                AtualizarObjeto(
                    estrada,
                    NomeDosPedregulhos,
                    PersistirMalha(pedras, pastaVia + "/Pedregulhos.asset"),
                    ObterMaterial(estrada, pastaVia + "/Pedregulhos.mat", true)
                );
            }
        }
    }
}
