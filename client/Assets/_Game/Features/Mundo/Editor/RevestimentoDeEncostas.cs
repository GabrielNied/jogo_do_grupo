using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    public readonly struct PosicaoDeRocha
    {
        public readonly Terrain Terreno;
        public readonly Vector3 Ponto,
            Normal;
        public readonly float Tamanho,
            Giro;
        public readonly int Modelo;

        public PosicaoDeRocha(
            Terrain terreno,
            Vector3 ponto,
            Vector3 normal,
            float tamanho,
            float giro,
            int modelo
        )
        {
            Terreno = terreno;
            Ponto = ponto;
            Normal = normal;
            Tamanho = tamanho;
            Giro = giro;
            Modelo = modelo;
        }
    }

    public static class RevestimentoDeEncostas
    {
        public const int LimiteMaximo = 2000;
        public const int TentativasMaximas = 250000;

        public static string Validar(ConfigDeRochas rochas)
        {
            if (
                rochas.Modelos == null
                || rochas.Modelos.Length == 0
                || rochas.Modelos.Any(m =>
                    m == null
                    || !EditorUtility.IsPersistent(m)
                    || m.GetComponentsInChildren<Renderer>(true).Length == 0
                )
            )
                return "Arraste ao menos um prefab/modelo persistente com Renderer, sem entradas vazias.";
            if (
                !float.IsFinite(rochas.Espacamento)
                || rochas.Espacamento < 0.5f
                || !float.IsFinite(rochas.Tamanho.x)
                || !float.IsFinite(rochas.Tamanho.y)
                || rochas.Tamanho.x <= 0
                || rochas.Tamanho.y < rochas.Tamanho.x
                || !float.IsFinite(rochas.Densidade)
                || rochas.Densidade < 0
                || rochas.Densidade > 1
                || !float.IsFinite(rochas.Enterramento)
                || rochas.Enterramento < 0
                || rochas.Enterramento > 1
                || !float.IsFinite(rochas.AcompanharNormal)
                || rochas.AcompanharNormal < 0
                || rochas.AcompanharNormal > 1
                || !float.IsFinite(rochas.InclinacaoMinima)
                || rochas.InclinacaoMinima < 0
                || rochas.InclinacaoMinima > 90
                || rochas.Limite < 1
                || rochas.Limite > LimiteMaximo
            )
                return "Use tamanho/espacamento positivos, variacoes entre 0 e 1 e limite de 1 a 2000 rochas.";
            return null;
        }

        public static List<PosicaoDeRocha> Planejar(
            Terrain[] terrenos,
            ConfigDeEncostas pintura,
            ConfigDeRochas rochas,
            Func<float, bool> cancelar = null
        )
        {
            var resultado = new List<PosicaoDeRocha>();
            if (rochas.Densidade <= 0f)
                return resultado;
            var ocupados = new Dictionary<Vector3Int, List<Vector3>>();
            var distanciaMinima = rochas.Espacamento * 0.7f;
            var tentativas = 0;
            foreach (
                var terreno in terrenos
                    .OrderBy(t => t.transform.position.x)
                    .ThenBy(t => t.transform.position.z)
            )
            {
                var dados = terreno.terrainData;
                var origem = terreno.transform.position;
                var minimo = new Vector2(origem.x, origem.z);
                var maximo = minimo + new Vector2(dados.size.x, dados.size.z);
                if (pintura.LimitarRegiao)
                {
                    minimo = Vector2.Max(minimo, pintura.Centro - Vector2.one * pintura.Raio);
                    maximo = Vector2.Min(maximo, pintura.Centro + Vector2.one * pintura.Raio);
                }
                for (
                    var cx = Mathf.FloorToInt(minimo.x / rochas.Espacamento);
                    cx <= Mathf.FloorToInt(maximo.x / rochas.Espacamento);
                    cx++
                )
                for (
                    var cz = Mathf.FloorToInt(minimo.y / rochas.Espacamento);
                    cz <= Mathf.FloorToInt(maximo.y / rochas.Espacamento);
                    cz++
                )
                {
                    var uc = Mathf.Clamp01(
                        ((cx + 0.5f) * rochas.Espacamento - origem.x) / dados.size.x
                    );
                    var vc = Mathf.Clamp01(
                        ((cz + 0.5f) * rochas.Espacamento - origem.z) / dados.size.z
                    );
                    var normalCentral = dados.GetInterpolatedNormal(uc, vc);
                    // A grade XZ sozinha separa pedras verticalmente em paredes fortes.
                    // Subdivide no eixo dominante do declive para aproximar espacamento na superficie.
                    var divisoes = Mathf.Clamp(
                        Mathf.CeilToInt(1f / Mathf.Max(0.125f, normalCentral.y)),
                        1,
                        8
                    );
                    var divisoesX =
                        Mathf.Abs(normalCentral.x) >= Mathf.Abs(normalCentral.z) ? divisoes : 1;
                    var divisoesZ = divisoesX == 1 ? divisoes : 1;
                    for (var sx = 0; sx < divisoesX; sx++)
                    for (var sz = 0; sz < divisoesZ; sz++)
                    {
                        if (++tentativas > TentativasMaximas)
                            throw new InvalidOperationException(
                                "Regiao muito grande: aumente o espacamento ou limite a regiao antes de gerar."
                            );
                        if (
                            tentativas % 256 == 0
                            && cancelar?.Invoke((float)resultado.Count / rochas.Limite) == true
                        )
                            throw new OperationCanceledException();
                        var random = new System.Random(
                            unchecked(
                                cx * 73856093
                                ^ cz * 19349663
                                ^ sx * 83492791
                                ^ sz * 15485863
                                ^ rochas.Semente
                            )
                        );
                        float Sorteio() => (float)random.NextDouble();
                        // Grade mundial com pequeno desvio: pecas formam faixas coesas na encosta,
                        // em vez de scatter livre sobre o mapa todo. O campo de ruido cria bolsao e fratura.
                        var x =
                            (cx + (sx + 0.5f + (Sorteio() - 0.5f) * 0.3f) / divisoesX)
                            * rochas.Espacamento;
                        var z =
                            (cz + (sz + 0.5f + (Sorteio() - 0.5f) * 0.3f) / divisoesZ)
                            * rochas.Espacamento;
                        var u = (x - origem.x) / dados.size.x;
                        var v = (z - origem.z) / dados.size.z;
                        if (
                            u < 0
                            || u >= 1
                            || v < 0
                            || v >= 1
                            || PinturaDeEncostas.Buraco(dados, u, v)
                        )
                            continue;
                        var angulo = dados.GetSteepness(u, v);
                        var ponto = new Vector3(x, origem.y + dados.GetInterpolatedHeight(u, v), z);
                        var peso = pintura.Mascara(ponto, angulo);
                        var coesao = Mathf.Lerp(
                            0.35f,
                            1f,
                            Mathf.Clamp01(pintura.Irregularidade(x, z) + 0.5f)
                        );
                        if (
                            angulo < rochas.InclinacaoMinima
                            || Sorteio() > peso * rochas.Densidade * coesao
                        )
                            continue;
                        var chave = Vector3Int.FloorToInt(ponto / distanciaMinima);
                        var perto = false;
                        for (var dx = -1; dx <= 1; dx++)
                        for (var dy = -1; dy <= 1; dy++)
                        for (var dz = -1; dz <= 1; dz++)
                            if (
                                ocupados.TryGetValue(
                                    chave + new Vector3Int(dx, dy, dz),
                                    out var vizinhos
                                )
                            )
                                foreach (var vizinho in vizinhos)
                                    if (
                                        (vizinho - ponto).sqrMagnitude
                                        < distanciaMinima * distanciaMinima
                                    )
                                        perto = true;
                        if (perto)
                            continue;
                        if (!ocupados.TryGetValue(chave, out var celula))
                            ocupados.Add(chave, celula = new List<Vector3>());
                        celula.Add(ponto);
                        resultado.Add(
                            new PosicaoDeRocha(
                                terreno,
                                ponto,
                                dados.GetInterpolatedNormal(u, v),
                                Mathf.Lerp(rochas.Tamanho.x, rochas.Tamanho.y, Sorteio()),
                                Mathf.Lerp(-25f, 25f, Sorteio()),
                                random.Next(rochas.Modelos.Length)
                            )
                        );
                        if (resultado.Count == rochas.Limite)
                            return resultado;
                    }
                }
            }
            return resultado;
        }

        public static int Aplicar(
            Terrain[] terrenos,
            ConfigDeEncostas pintura,
            ConfigDeRochas rochas,
            Func<float, bool> cancelar = null
        )
        {
            var erro = Validar(rochas);
            if (erro != null)
                throw new ArgumentException(erro);
            // Planeja antes de remover o resultado anterior; cancelamento nao perde trabalho.
            var posicoes = Planejar(terrenos, pintura, rochas, cancelar);
            Undo.IncrementCurrentGroup();
            var grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Revestir encostas com rochas");
            try
            {
                Remover(terrenos, pintura);
                var pais = new Dictionary<Terrain, RochasGeradasDeEncosta>();
                var criadas = 0;
                foreach (var posicao in posicoes)
                {
                    if (cancelar?.Invoke((float)criadas / Math.Max(1, posicoes.Count)) == true)
                        throw new OperationCanceledException();
                    if (!pais.TryGetValue(posicao.Terreno, out var grupoDeRochas))
                    {
                        grupoDeRochas = posicao
                            .Terreno.GetComponentsInChildren<RochasGeradasDeEncosta>(true)
                            .FirstOrDefault(m => m.transform.parent == posicao.Terreno.transform);
                        if (grupoDeRochas == null)
                        {
                            var raiz = new GameObject("Rochas das encostas (decoracao)");
                            raiz.transform.SetParent(posicao.Terreno.transform, false);
                            grupoDeRochas = raiz.AddComponent<RochasGeradasDeEncosta>();
                            Undo.RegisterCreatedObjectUndo(raiz, "Criar revestimento");
                        }
                        Undo.RegisterCompleteObjectUndo(grupoDeRochas, "Atualizar grupo de rochas");
                        pais.Add(posicao.Terreno, grupoDeRochas);
                    }
                    var modelo = rochas.Modelos[posicao.Modelo];
                    // Copia decorativa independente: preserva referencias de mesh/material,
                    // sem overrides de prefab que possam voltar ao asset ao reabrir a cena.
                    var instancia = Object.Instantiate(modelo, grupoDeRochas.transform);
                    Undo.RegisterCreatedObjectUndo(instancia, "Criar rocha decorativa");
                    // So decoracao: nenhuma plataforma, corpo fisico ou script de gameplay local.
                    foreach (
                        var componente in instancia.GetComponentsInChildren<MonoBehaviour>(true)
                    )
                        Undo.DestroyObjectImmediate(componente);
                    foreach (var componente in instancia.GetComponentsInChildren<Joint>(true))
                        Undo.DestroyObjectImmediate(componente);
                    foreach (var componente in instancia.GetComponentsInChildren<Rigidbody>(true))
                        Undo.DestroyObjectImmediate(componente);
                    foreach (var componente in instancia.GetComponentsInChildren<Collider>(true))
                        Undo.DestroyObjectImmediate(componente);
                    Undo.RecordObject(instancia.transform, "Ajustar rocha na encosta");
                    instancia.transform.rotation =
                        Quaternion.Slerp(
                            Quaternion.identity,
                            Quaternion.FromToRotation(Vector3.up, posicao.Normal),
                            rochas.AcompanharNormal
                        ) * Quaternion.Euler(0f, posicao.Giro, 0f);
                    var bounds = Limites(instancia);
                    var maior = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    if (maior <= 0.0001f)
                        throw new InvalidOperationException(
                            modelo.name + ": modelo sem volume visivel."
                        );
                    instancia.transform.localScale *= posicao.Tamanho / maior;
                    bounds = Limites(instancia);
                    instancia.transform.position +=
                        posicao.Ponto
                        - bounds.center
                        - posicao.Normal * posicao.Tamanho * rochas.Enterramento;
                    grupoDeRochas.Pecas.Add(
                        new PecaDeEncostaGerada
                        {
                            Objeto = instancia,
                            Ancora = new Vector2(posicao.Ponto.x, posicao.Ponto.z),
                        }
                    );
                    EditorUtility.SetDirty(grupoDeRochas);
                    criadas++;
                }
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(grupo);
                return posicoes.Count;
            }
            catch
            {
                Undo.RevertAllDownToGroup(grupo);
                throw;
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }

        public static void Remover(Terrain[] terrenos, ConfigDeEncostas pintura = null)
        {
            foreach (var terreno in terrenos)
            foreach (var marcador in terreno.GetComponentsInChildren<RochasGeradasDeEncosta>(true))
                if (marcador.transform.parent == terreno.transform)
                {
                    if (pintura == null || !pintura.LimitarRegiao)
                        Undo.DestroyObjectImmediate(marcador.gameObject);
                    else
                    {
                        Undo.RegisterCompleteObjectUndo(marcador, "Remover rochas da regiao");
                        for (var i = marcador.Pecas.Count - 1; i >= 0; i--)
                        {
                            var peca = marcador.Pecas[i];
                            if (
                                peca.Objeto != null
                                && pintura.PesoRegiao(peca.Ancora.x, peca.Ancora.y) <= 0f
                            )
                                continue;
                            if (peca.Objeto != null)
                                Undo.DestroyObjectImmediate(peca.Objeto);
                            marcador.Pecas.RemoveAt(i);
                        }
                        EditorUtility.SetDirty(marcador);
                        if (marcador.Pecas.Count == 0)
                            Undo.DestroyObjectImmediate(marcador.gameObject);
                    }
                }
        }

        private static Bounds Limites(GameObject instancia)
        {
            var renderers = instancia.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
