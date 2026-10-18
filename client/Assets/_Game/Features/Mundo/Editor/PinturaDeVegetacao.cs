using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Escreve somente nas instâncias e camadas nativas de TerrainData.</summary>
    public static class PinturaDeVegetacao
    {
        private const int CoberturaNativaMaxima = 16;
        private const int TentativasPorArvore = 12;

        public static string Validar(ConjuntoDeVegetacao conjunto)
        {
            if (conjunto == null)
                return "Selecione ou crie um conjunto.";
            if (
                !NoIntervalo(conjunto.Raio, 0.5f, 50f)
                || !NoIntervalo(conjunto.ArvoresPorCemMetros, 0f, 25f)
                || !NoIntervalo(conjunto.CoberturaDePlantas, 0f, 1f)
                || !NoIntervalo(conjunto.CoberturaDeGrama, 0f, 1f)
                || !NoIntervalo(conjunto.InclinacaoMaxima, 0f, 45f)
                || !NoIntervalo(conjunto.DistanciaEntreArvores, 0.1f, 20f)
            )
                return "Há valores fora dos limites do pincel.";
            var erro = ValidarGrupo(conjunto.Arvores, true, "Árvores");
            if (erro != null)
                return erro;
            erro = ValidarGrupo(conjunto.Plantas, false, "Plantas");
            if (erro != null)
                return erro;
            erro = ValidarGrupo(conjunto.Gramas, false, "Grama");
            if (erro != null)
                return erro;
            if (conjunto.Arvores.Count + conjunto.Plantas.Count + conjunto.Gramas.Count == 0)
                return "Adicione espécies às listas de árvores, plantas ou grama para começar.";
            foreach (var planta in conjunto.Plantas)
            foreach (var grama in conjunto.Gramas)
                if (
                    planta.Peso > 0f
                    && grama.Peso > 0f
                    && planta.Modelo == grama.Modelo
                    && planta.Textura == grama.Textura
                )
                    return "A mesma espécie deve pertencer a plantas OU grama, para manter densidades independentes.";
            return null;
        }

        private static bool NoIntervalo(float valor, float minimo, float maximo) =>
            !float.IsNaN(valor) && !float.IsInfinity(valor) && valor >= minimo && valor <= maximo;

        private static string ValidarGrupo(List<EspecieDoPincel> grupo, bool arvore, string nome)
        {
            if (grupo == null)
                return nome + ": lista inválida.";
            var usados = new HashSet<UnityEngine.Object>();
            foreach (var especie in grupo)
            {
                if (especie == null || !NoIntervalo(especie.Peso, 0f, 1000000f))
                    return nome + ": peso inválido.";
                if (especie.Peso == 0f)
                    continue;
                if (
                    !NoIntervalo(especie.Tamanho.x, 0.05f, 20f)
                    || !NoIntervalo(especie.Tamanho.y, especie.Tamanho.x, 20f)
                )
                    return nome + ": tamanho mínimo/máximo inválido.";
                if ((especie.Modelo == null) == (especie.Textura == null))
                    return nome + ": escolha um modelo OU uma textura em cada espécie ativa.";
                UnityEngine.Object asset =
                    especie.Modelo != null ? (UnityEngine.Object)especie.Modelo : especie.Textura;
                if (!usados.Add(asset))
                    return nome + ": a mesma espécie aparece duas vezes.";
                if (!EditorUtility.IsPersistent(asset))
                    return nome
                        + ": use assets da janela Project, não objetos temporários da cena.";
                if (
                    arvore
                    && (especie.Modelo == null || especie.Modelo.GetComponent<LODGroup>() == null)
                )
                    return nome + ": use prefabs com LOD Group na raiz para permitir rotação.";
                if (!arvore && especie.Modelo != null)
                {
                    var renderer = especie.Modelo.GetComponent<MeshRenderer>();
                    if (
                        renderer == null
                        || especie.Modelo.GetComponent<MeshFilter>() == null
                        || renderer.sharedMaterials.Length != 1
                    )
                        return nome
                            + ": detalhes nativos precisam de uma malha e um material na raiz. Use outro prefab.";
                }
            }
            return null;
        }

        public static void Aplicar(
            Terrain terreno,
            ConjuntoDeVegetacao conjunto,
            Vector3 centro,
            bool apagar,
            bool registrarUndo = true,
            System.Random sorteio = null
        )
        {
            var erro = Validar(conjunto);
            if (erro != null)
                throw new ArgumentException(erro);
            if (terreno == null || terreno.terrainData == null)
                throw new ArgumentException("Selecione um Terrain válido.");
            if (
                terreno.transform.lossyScale != Vector3.one
                || terreno.transform.rotation != Quaternion.identity
            )
                throw new ArgumentException("O Terrain precisa de escala 1 e rotação 0.");
            var dados = terreno.terrainData;
            if (
                dados.detailResolution == 0
                && !apagar
                && (
                    (conjunto.CoberturaDePlantas > 0f && Soma(conjunto.Plantas) > 0f)
                    || (conjunto.CoberturaDeGrama > 0f && Soma(conjunto.Gramas) > 0f)
                )
            )
                throw new ArgumentException(
                    "Configure a resolução de detalhes nas configurações do Terrain antes de pintar grama ou plantas."
                );
            if (registrarUndo)
                Undo.RegisterCompleteObjectUndo(
                    dados,
                    apagar ? "Apagar conjunto de vegetação" : "Pintar conjunto de vegetação"
                );
            var local = centro - terreno.transform.position;
            var random = sorteio ?? new System.Random();
            PintarArvores(dados, conjunto, local, apagar, random);
            PintarDetalhes(
                dados,
                conjunto,
                conjunto.Plantas,
                conjunto.CoberturaDePlantas,
                local,
                apagar
            );
            PintarDetalhes(
                dados,
                conjunto,
                conjunto.Gramas,
                conjunto.CoberturaDeGrama,
                local,
                apagar
            );
            EditorUtility.SetDirty(dados);
            terreno.Flush();
        }

        private static float Soma(List<EspecieDoPincel> especies)
        {
            var soma = 0f;
            foreach (var especie in especies)
                if (especie != null && especie.Peso > 0f)
                    soma += especie.Peso;
            return soma;
        }

        public static int SortearEspecie(List<EspecieDoPincel> especies, System.Random random)
        {
            var soma = Soma(especies);
            if (soma <= 0f)
                return -1;
            var valor = random.NextDouble() * soma;
            var ultimo = -1;
            for (var i = 0; i < especies.Count; i++)
            {
                if (especies[i].Peso <= 0f)
                    continue;
                ultimo = i;
                valor -= especies[i].Peso;
                if (valor < 0d)
                    return i;
            }
            return ultimo;
        }

        private static bool Dentro(Vector3 posicao, Vector3 centro, float raio) =>
            new Vector2(posicao.x - centro.x, posicao.z - centro.z).sqrMagnitude <= raio * raio;

        private static Vector3 Metros(TreeInstance arvore, TerrainData dados) =>
            Vector3.Scale(arvore.position, dados.size);

        private static void PintarArvores(
            TerrainData dados,
            ConjuntoDeVegetacao conjunto,
            Vector3 centro,
            bool apagar,
            System.Random random
        )
        {
            var prototipos = new List<TreePrototype>(dados.treePrototypes);
            var indices = new Dictionary<int, EspecieDoPincel>();
            foreach (var especie in conjunto.Arvores)
            {
                if (especie.Modelo == null || (!apagar && especie.Peso <= 0f))
                    continue;
                var indice = prototipos.FindIndex(p => p.prefab == especie.Modelo);
                if (indice < 0 && !apagar && conjunto.ArvoresPorCemMetros > 0f)
                {
                    indice = prototipos.Count;
                    prototipos.Add(new TreePrototype { prefab = especie.Modelo });
                }
                if (indice >= 0)
                    indices[indice] = especie;
            }
            var arvores = new List<TreeInstance>(dados.treeInstances);
            if (apagar)
            {
                arvores.RemoveAll(a =>
                    indices.ContainsKey(a.prototypeIndex)
                    && Dentro(Metros(a, dados), centro, conjunto.Raio)
                );
                dados.treeInstances = arvores.ToArray();
                return;
            }
            if (conjunto.ArvoresPorCemMetros == 0f || Soma(conjunto.Arvores) == 0f)
                return;
            dados.treePrototypes = prototipos.ToArray();
            var alvo = Mathf.Max(
                1,
                (int)
                    Math.Floor(
                        Math.PI
                            * conjunto.Raio
                            * conjunto.Raio
                            * conjunto.ArvoresPorCemMetros
                            / 100d
                    )
            );
            var existentes = 0;
            var grade = new Dictionary<Vector2Int, List<Vector2>>();
            var passo = conjunto.DistanciaEntreArvores;
            foreach (var arvore in arvores)
            {
                var posicao = Metros(arvore, dados);
                if (Dentro(posicao, centro, conjunto.Raio))
                    existentes++;
                if (Dentro(posicao, centro, conjunto.Raio + passo))
                    RegistrarNaGrade(grade, new Vector2(posicao.x, posicao.z), passo);
            }
            var faltam = Mathf.Max(0, alvo - existentes);
            for (
                var tentativa = 0;
                tentativa < faltam * TentativasPorArvore && existentes < alvo;
                tentativa++
            )
            {
                var angulo = random.NextDouble() * Math.PI * 2d;
                var distancia = Math.Sqrt(random.NextDouble()) * conjunto.Raio;
                var x = centro.x + (float)(Math.Cos(angulo) * distancia);
                var z = centro.z + (float)(Math.Sin(angulo) * distancia);
                var nx = x / dados.size.x;
                var nz = z / dados.size.z;
                if (
                    nx < 0f
                    || nx > 1f
                    || nz < 0f
                    || nz > 1f
                    || !PodePintar(dados, nx, nz, conjunto.InclinacaoMaxima)
                )
                    continue;
                var ponto = new Vector2(x, z);
                if (!CabeNaGrade(grade, ponto, passo))
                    continue;
                var especie = conjunto.Arvores[SortearEspecie(conjunto.Arvores, random)];
                var indice = prototipos.FindIndex(p => p.prefab == especie.Modelo);
                var tamanho = Mathf.Lerp(
                    especie.Tamanho.x,
                    especie.Tamanho.y,
                    (float)random.NextDouble()
                );
                arvores.Add(
                    new TreeInstance
                    {
                        position = new Vector3(
                            nx,
                            dados.GetInterpolatedHeight(nx, nz) / dados.size.y,
                            nz
                        ),
                        prototypeIndex = indice,
                        widthScale = tamanho,
                        heightScale = tamanho,
                        rotation = (float)(random.NextDouble() * Math.PI * 2d),
                        color = Color.white,
                        lightmapColor = Color.white,
                    }
                );
                RegistrarNaGrade(grade, ponto, passo);
                existentes++;
            }
            dados.treeInstances = arvores.ToArray();
        }

        private static Vector2Int Celula(Vector2 ponto, float passo) =>
            new Vector2Int(Mathf.FloorToInt(ponto.x / passo), Mathf.FloorToInt(ponto.y / passo));

        private static void RegistrarNaGrade(
            Dictionary<Vector2Int, List<Vector2>> grade,
            Vector2 ponto,
            float passo
        )
        {
            var celula = Celula(ponto, passo);
            if (!grade.TryGetValue(celula, out var lista))
                grade[celula] = lista = new List<Vector2>();
            lista.Add(ponto);
        }

        private static bool CabeNaGrade(
            Dictionary<Vector2Int, List<Vector2>> grade,
            Vector2 ponto,
            float passo
        )
        {
            var celula = Celula(ponto, passo);
            for (var z = -1; z <= 1; z++)
            for (var x = -1; x <= 1; x++)
                if (grade.TryGetValue(celula + new Vector2Int(x, z), out var lista))
                    foreach (var outro in lista)
                        if ((outro - ponto).sqrMagnitude < passo * passo)
                            return false;
            return true;
        }

        private static void PintarDetalhes(
            TerrainData dados,
            ConjuntoDeVegetacao conjunto,
            List<EspecieDoPincel> especies,
            float cobertura,
            Vector3 centro,
            bool apagar
        )
        {
            var soma = Soma(especies);
            if (dados.detailResolution == 0 || (!apagar && (soma <= 0f || cobertura == 0f)))
                return;
            var resolucao = dados.detailResolution;
            var x0 = Mathf.Clamp(
                Mathf.FloorToInt((centro.x - conjunto.Raio) / dados.size.x * resolucao),
                0,
                resolucao - 1
            );
            var z0 = Mathf.Clamp(
                Mathf.FloorToInt((centro.z - conjunto.Raio) / dados.size.z * resolucao),
                0,
                resolucao - 1
            );
            var x1 = Mathf.Clamp(
                Mathf.CeilToInt((centro.x + conjunto.Raio) / dados.size.x * resolucao),
                0,
                resolucao - 1
            );
            var z1 = Mathf.Clamp(
                Mathf.CeilToInt((centro.z + conjunto.Raio) / dados.size.z * resolucao),
                0,
                resolucao - 1
            );
            foreach (var especie in especies)
            {
                if (
                    (especie.Modelo == null && especie.Textura == null)
                    || (!apagar && especie.Peso <= 0f)
                )
                    continue;
                var prototipos = new List<DetailPrototype>(dados.detailPrototypes);
                var indice = prototipos.FindIndex(p =>
                    p.prototype == especie.Modelo && p.prototypeTexture == especie.Textura
                );
                if (indice < 0)
                {
                    if (apagar)
                        continue;
                    indice = prototipos.Count;
                    prototipos.Add(
                        new DetailPrototype
                        {
                            prototype = especie.Modelo,
                            prototypeTexture = especie.Textura,
                            usePrototypeMesh = especie.Modelo != null,
                            useInstancing = especie.Modelo != null,
                            renderMode =
                                especie.Modelo != null
                                    ? DetailRenderMode.VertexLit
                                    : DetailRenderMode.GrassBillboard,
                            minWidth = especie.Tamanho.x,
                            maxWidth = especie.Tamanho.y,
                            minHeight = especie.Tamanho.x,
                            maxHeight = especie.Tamanho.y,
                            healthyColor = Color.white,
                            dryColor = Color.white,
                        }
                    );
                    dados.detailPrototypes = prototipos.ToArray();
                }
                var camada = dados.GetDetailLayer(x0, z0, x1 - x0 + 1, z1 - z0 + 1, indice);
                for (var z = z0; z <= z1; z++)
                for (var x = x0; x <= x1; x++)
                {
                    var nx = (x + 0.5f) / resolucao;
                    var nz = (z + 0.5f) / resolucao;
                    if (
                        !Dentro(
                            new Vector3(nx * dados.size.x, 0f, nz * dados.size.z),
                            centro,
                            conjunto.Raio
                        )
                    )
                        continue;
                    if (apagar)
                    {
                        camada[z - z0, x - x0] = 0;
                        continue;
                    }
                    if (!PodePintar(dados, nx, nz, conjunto.InclinacaoMaxima))
                        continue;
                    var esperado = cobertura * CoberturaNativaMaxima * especie.Peso / soma;
                    var inteiro = Mathf.FloorToInt(esperado);
                    var hash = unchecked(
                        (uint)(x * 73856093 ^ z * 19349663 ^ indice * 83492791 ^ conjunto.Semente)
                    );
                    hash ^= hash >> 16;
                    hash *= 0x7feb352d;
                    hash ^= hash >> 15;
                    var ruido = (hash & 0xffff) / 65536f;
                    var alvo = inteiro + (ruido < esperado - inteiro ? 1 : 0);
                    camada[z - z0, x - x0] = Mathf.Max(camada[z - z0, x - x0], alvo);
                }
                dados.SetDetailLayer(x0, z0, indice, camada);
            }
        }

        private static bool PodePintar(TerrainData dados, float x, float z, float inclinacao)
        {
            var hx = Mathf.Clamp((int)(x * dados.holesResolution), 0, dados.holesResolution - 1);
            var hz = Mathf.Clamp((int)(z * dados.holesResolution), 0, dados.holesResolution - 1);
            return !dados.IsHole(hx, hz) && dados.GetSteepness(x, z) <= inclinacao;
        }
    }
}
