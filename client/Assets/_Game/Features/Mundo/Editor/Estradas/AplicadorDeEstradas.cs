using System;
using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Features.Mundo.Estradas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Mundo.Editor.Estradas
{
    public static class AplicadorDeEstradas
    {
        public const string PastaGerada = "Assets/_Game/Features/Mundo/EstradasGeradas";
        private const float ToleranciaAltura = 0.0000001f;
        private const float ErroPixelDaEstrada = 0.25f;

        public static Terrain[] TerrenosDa(EstradaNativa estrada)
        {
            return (
                estrada.Terrenos != null && estrada.Terrenos.Length > 0
                    ? estrada.Terrenos
                    : Terrain.activeTerrains
            )
                .Where(t => t != null && t.gameObject.scene == estrada.gameObject.scene)
                .Distinct()
                .ToArray();
        }

        public static void Aplicar(EstradaNativa estrada)
        {
            ValidarEdicao(estrada);
            ValidarIdentidade(estrada);
            var pontos = PerfilDaEstrada.Amostrar(estrada);
            var terrenos = TerrenosDa(estrada);
            ValidarTerrenos(terrenos);
            var raio = PerfilDaEstrada.RaioMaximo(estrada);
            var corredor = new CorredorDaEstrada(pontos, raio);
            ValidarCobertura(estrada, pontos, terrenos);
            ValidarOutrasEstradas(estrada, pontos, raio);
            var registro = estrada.Registro as RegistroDaEstrada;
            if (registro != null && registro.EmAplicacao)
                throw new InvalidOperationException(
                    "Restaure a aplicação interrompida antes de aplicar novamente."
                );
            if (registro != null && registro.Aplicada)
                ValidarIntegridade(registro, terrenos);

            // Planeja tudo antes de tocar o Terrain: reaplicar lê os valores anteriores do backup.
            var novas = terrenos
                .Where(t => Intersecta(corredor.Limites, t))
                .Select(t => Preparar(estrada, t, corredor, registro))
                .ToList();
            ValidarCelulasCompartilhadas(estrada, novas);
            GarantirPasta(PastaGerada);
            if (string.IsNullOrEmpty(estrada.Id))
                estrada.Id = Guid.NewGuid().ToString("N");
            var pasta = PastaGerada + "/" + estrada.Id;
            GarantirPasta(pasta);
            var grupoUndo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Aplicar estrada nativa");
            Undo.RecordObject(estrada, "Aplicar estrada nativa");
            if (registro == null)
            {
                registro = ScriptableObject.CreateInstance<RegistroDaEstrada>();
                AssetDatabase.CreateAsset(registro, pasta + "/Registro.asset");
                estrada.Registro = registro;
            }
            Undo.RegisterCompleteObjectUndo(registro, "Backup da estrada");
            var anteriores = registro.Alteracoes;
            foreach (
                var dado in novas
                    .Select(a => a.Dados)
                    .Concat(registro.Alteracoes.Select(a => a.Dados))
                    .Distinct()
            )
                Undo.RegisterCompleteObjectUndo(dado, "Terreno da estrada");
            if (registro.Aplicada)
                Escrever(registro.Alteracoes, false);
            registro.Alteracoes = novas;
            registro.Caminho = pontos;
            registro.Raio = raio;
            registro.Largura = estrada.Largura;
            // O registro guarda a faixa efetivamente protegida, inclusive em backups antigos.
            registro.Transicao = estrada.TransicaoDoCorredor;
            registro.Margem = estrada.MargemVegetacao;
            registro.Variacao = estrada.VariacaoLateral;
            registro.Semente = estrada.Semente;
            registro.Aplicada = true;
            registro.EmAplicacao = true;
            foreach (var alteracao in novas)
            {
                var terreno = terrenos.First(t => t.terrainData == alteracao.Dados);
                Undo.RecordObject(terreno, "Precisão do terreno da estrada");
                terreno.heightmapPixelError = Mathf.Min(
                    terreno.heightmapPixelError,
                    ErroPixelDaEstrada
                );
                alteracao.ErroPixelAplicado = terreno.heightmapPixelError;
                EditorUtility.SetDirty(terreno);
            }
            RestaurarQualidade(estrada, anteriores, terrenos);
            EditorUtility.SetDirty(registro);
            EditorUtility.SetDirty(estrada);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(estrada.gameObject.scene);
            EditorSceneManager.SaveScene(estrada.gameObject.scene);
            try
            {
                Escrever(novas, true);
                foreach (var alteracao in novas)
                    LerAlturas(
                        alteracao,
                        (i, valor) =>
                        {
                            alteracao.AlturasDepois[i] = valor;
                            return valor;
                        },
                        false
                    );
                VisualDaEstrada.Gerar(estrada, pontos, p => AlturaEm(terrenos, p), pasta);
                registro.EmAplicacao = false;
            }
            catch
            {
                Escrever(novas, false);
                registro.Aplicada = false;
                registro.EmAplicacao = false;
                VisualDaEstrada.Remover(estrada);
                RestaurarQualidade(estrada, novas.Concat(anteriores), terrenos);
                throw;
            }
            finally
            {
                EditorUtility.SetDirty(registro);
                EditorSceneManager.MarkSceneDirty(estrada.gameObject.scene);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(estrada.gameObject.scene);
                Undo.CollapseUndoOperations(grupoUndo);
            }
            Debug.Log(
                "[Estradas] Estrada aplicada e salva. O mapa publicado continua independente.",
                estrada
            );
        }

        public static void Restaurar(EstradaNativa estrada)
        {
            ValidarEdicao(estrada, false);
            ValidarIdentidade(estrada);
            var registro = estrada.Registro as RegistroDaEstrada;
            if (registro == null || !registro.Aplicada)
                return;
            ValidarIntegridade(registro, TerrenosDa(estrada));
            Undo.SetCurrentGroupName("Restaurar estrada nativa");
            Undo.RegisterCompleteObjectUndo(registro, "Restaurar estrada nativa");
            foreach (var alteracao in registro.Alteracoes)
                Undo.RegisterCompleteObjectUndo(alteracao.Dados, "Restaurar terreno da estrada");
            Escrever(registro.Alteracoes, false);
            registro.Aplicada = false;
            registro.EmAplicacao = false;
            VisualDaEstrada.Remover(estrada);
            RestaurarQualidade(estrada, registro.Alteracoes, TerrenosDa(estrada));
            EditorUtility.SetDirty(registro);
            EditorSceneManager.MarkSceneDirty(estrada.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(estrada.gameObject.scene);
        }

        public static float AlturaEm(IReadOnlyList<Terrain> terrenos, Vector3 ponto)
        {
            foreach (var terreno in terrenos)
                if (Contem(terreno, ponto))
                    return terreno.SampleHeight(ponto) + terreno.transform.position.y;
            throw new InvalidOperationException(
                "Há um ponto da estrada fora dos Terrain selecionados."
            );
        }

        public static void GarantirPasta(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var pos = caminho.LastIndexOf('/');
            GarantirPasta(caminho.Substring(0, pos));
            AssetDatabase.CreateFolder(caminho.Substring(0, pos), caminho.Substring(pos + 1));
        }

        private static void ValidarEdicao(EstradaNativa estrada, bool validarConfiguracao = true)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException(
                    "Aplique estradas fora do Play e após a compilação."
                );
            if (
                !estrada.gameObject.scene.IsValid()
                || string.IsNullOrEmpty(estrada.gameObject.scene.path)
            )
                throw new InvalidOperationException(
                    "Salve a cena de autoria antes de aplicar a estrada."
                );
            if (PrefabUtility.IsPartOfPrefabAsset(estrada))
                throw new InvalidOperationException("Edite uma instância na cena de autoria.");
            if (estrada.PossuiRegistro && estrada.Registro == null)
                throw new InvalidOperationException(
                    "O asset de backup desta estrada está ausente. Recupere o Registro original antes de reaplicar/restaurar."
                );
            if (!validarConfiguracao)
                return;
            if (
                !float.IsFinite(estrada.Largura)
                || !float.IsFinite(estrada.Transicao)
                || !float.IsFinite(estrada.TransicaoVisual)
                || !float.IsFinite(estrada.DensidadeCascalhos)
                || !float.IsFinite(estrada.CorCascalhos.r)
                || !float.IsFinite(estrada.CorCascalhos.g)
                || !float.IsFinite(estrada.CorCascalhos.b)
                || !float.IsFinite(estrada.Rebaixo)
                || !float.IsFinite(estrada.MargemVegetacao)
                || !float.IsFinite(estrada.VariacaoLateral)
                || estrada.Largura < 0.5f
                || estrada.Largura > 100f
                || estrada.Transicao < 0.2f
                || estrada.Transicao > 50f
                || estrada.TransicaoVisual < 0.2f
                || estrada.TransicaoVisual > 50f
                || estrada.DensidadeCascalhos < 0f
                || estrada.DensidadeCascalhos > 8f
                || estrada.Rebaixo < 0f
                || estrada.Rebaixo > 0.35f
                || estrada.MargemVegetacao < 0f
                || estrada.MargemVegetacao > 50f
            )
                throw new InvalidOperationException(
                    "Largura, transições, rebaixo, margem ou cascalhos estão fora dos limites."
                );
        }

        private static void ValidarIdentidade(EstradaNativa estrada)
        {
            foreach (
                var outra in Object.FindObjectsByType<EstradaNativa>(FindObjectsInactive.Include)
            )
            {
                if (outra == estrada)
                    continue;
                if (
                    (!string.IsNullOrEmpty(estrada.Id) && outra.Id == estrada.Id)
                    || (estrada.Registro != null && outra.Registro == estrada.Registro)
                )
                    throw new InvalidOperationException(
                        "A estrada foi duplicada junto com o backup. Nada foi alterado. Remova a cópia e crie outra pelo menu para ter um registro independente."
                    );
            }
        }

        private static void ValidarTerrenos(Terrain[] terrenos)
        {
            if (terrenos.Length == 0)
                throw new InvalidOperationException("Não há Terrain ativo nesta cena.");
            var dados = new HashSet<TerrainData>();
            foreach (var terreno in terrenos)
            {
                if (terreno.terrainData == null || !AssetDatabase.Contains(terreno.terrainData))
                    throw new InvalidOperationException(
                        "Salve cada TerrainData como asset antes de aplicar estradas."
                    );
                if (
                    terreno.transform.rotation != Quaternion.identity
                    || terreno.transform.lossyScale != Vector3.one
                )
                    throw new InvalidOperationException(
                        "Terrain precisa ter rotação zero e escala 1."
                    );
                if (!dados.Add(terreno.terrainData))
                    throw new InvalidOperationException(
                        "Dois Terrain compartilham TerrainData. Dê um asset próprio a cada bloco."
                    );
            }
        }

        private static void ValidarCobertura(
            EstradaNativa estrada,
            List<Vector3> pontos,
            Terrain[] terrenos
        )
        {
            for (var i = 0; i < pontos.Count; i++)
            {
                var ponto = pontos[i];
                var direcao =
                    pontos[Mathf.Min(i + 1, pontos.Count - 1)] - pontos[Mathf.Max(0, i - 1)];
                var lado = new Vector3(-direcao.z, 0f, direcao.x).normalized;
                var raio =
                    PerfilDaEstrada.MeiaLargura(estrada, ponto) + estrada.TransicaoDoCorredor;
                var passos = Mathf.Max(2, Mathf.CeilToInt(raio * 4f));
                for (var lateral = 0; lateral <= passos; lateral++)
                {
                    var p = ponto + lado * Mathf.Lerp(-raio, raio, lateral / (float)passos);
                    var terreno = terrenos.FirstOrDefault(t => Contem(t, p));
                    if (terreno == null)
                        throw new InvalidOperationException(
                            "A estrada e suas bordas precisam caber nos Terrain selecionados. Ajuste os pontos ou selecione os blocos vizinhos."
                        );
                    var dados = terreno.terrainData;
                    var local = p - terreno.transform.position;
                    var x = Mathf.Clamp(
                        Mathf.FloorToInt(local.x / dados.size.x * dados.holesResolution),
                        0,
                        dados.holesResolution - 1
                    );
                    var z = Mathf.Clamp(
                        Mathf.FloorToInt(local.z / dados.size.z * dados.holesResolution),
                        0,
                        dados.holesResolution - 1
                    );
                    if (dados.IsHole(x, z))
                        throw new InvalidOperationException(
                            "A estrada cruza um buraco do Terrain. Ajuste o traçado; pontes precisam de autoria própria."
                        );
                }
            }
        }

        private static void ValidarOutrasEstradas(
            EstradaNativa estrada,
            List<Vector3> pontos,
            float raio
        )
        {
            foreach (
                var outra in Object.FindObjectsByType<EstradaNativa>(FindObjectsInactive.Include)
            )
            {
                if (outra == estrada || outra.gameObject.scene != estrada.gameObject.scene)
                    continue;
                var registro = outra.Registro as RegistroDaEstrada;
                if (registro == null || !registro.Aplicada)
                    continue;
                var corredor = new CorredorDaEstrada(
                    registro.Caminho,
                    raio + registro.Raio + PerfilDaEstrada.Passo
                );
                if (
                    pontos.Any(p =>
                        corredor.Distancia(p) <= raio + registro.Raio + PerfilDaEstrada.Passo
                    )
                )
                    throw new InvalidOperationException(
                        "Esta faixa encosta na estrada '"
                            + outra.name
                            + "'. A versão inicial mantém corredores separados para preservar os backups. Restaure a outra ou afaste os traçados."
                    );
            }
        }

        private static AlteracaoDeTerreno Preparar(
            EstradaNativa estrada,
            Terrain terreno,
            CorredorDaEstrada corredor,
            RegistroDaEstrada registro
        )
        {
            var dados = terreno.terrainData;
            var anterior =
                registro != null && registro.Aplicada
                    ? registro.Alteracoes.FirstOrDefault(a => a.Dados == dados)
                    : null;
            var alteracao = new AlteracaoDeTerreno
            {
                Dados = dados,
                Origem = terreno.transform.position,
                Tamanho = dados.size,
                ResolucaoAltura = dados.heightmapResolution,
                ResolucaoDetalhe = dados.detailResolution,
                ErroPixelOriginal = ErroOriginal(estrada, terreno, anterior),
                PrototiposArvores = dados
                    .treePrototypes.Select(p => p.prefab == null ? null : p.prefab)
                    .ToArray(),
                PrototiposDetalhes = dados
                    .detailPrototypes.Select(p => p.prototype == null ? null : p.prototype)
                    .ToArray(),
                TexturasDetalhes = dados
                    .detailPrototypes.Select(p =>
                        p.prototypeTexture == null ? null : p.prototypeTexture
                    )
                    .ToArray(),
            };
            var indices = new List<int>();
            var antes = new List<float>();
            var depois = new List<float>();
            var resolucao = dados.heightmapResolution;
            var rect = Recorte(terreno, corredor.Limites, resolucao, true);
            ValidarBuracos(estrada, terreno, corredor);
            var alturas = dados.GetHeights(rect.x, rect.y, rect.width, rect.height);
            var originais =
                anterior == null
                    ? new Dictionary<int, float>()
                    : anterior
                        .IndicesAltura.Select((v, i) => (v, i))
                        .ToDictionary(p => p.v, p => anterior.AlturasAntes[p.i]);
            for (var z = rect.y; z < rect.yMax; z++)
            for (var x = rect.x; x < rect.xMax; x++)
            {
                var ponto = Posicao(alteracao, x, z, resolucao, true);
                var depressao = PerfilDaEstrada.Depressao(
                    estrada,
                    ponto,
                    corredor.Distancia(ponto)
                );
                if (depressao <= 0f)
                    continue;
                var indice = z * resolucao + x;
                var altura = originais.TryGetValue(indice, out var original)
                    ? original
                    : alturas[z - rect.y, x - rect.x];
                if (altura * dados.size.y < depressao)
                    throw new InvalidOperationException(
                        "O Terrain não tem altura suficiente para o rebaixo; eleve sua base antes de aplicar."
                    );
                indices.Add(indice);
                antes.Add(altura);
                depois.Add(altura - depressao / dados.size.y);
            }
            alteracao.IndicesAltura = indices.ToArray();
            alteracao.AlturasAntes = antes.ToArray();
            alteracao.AlturasDepois = depois.ToArray();
            PrepararDetalhes(estrada, terreno, corredor, anterior, alteracao);
            var arvores = dados.treeInstances.AsEnumerable();
            if (anterior != null)
                arvores = arvores.Concat(anterior.ArvoresRemovidas.Select(a => a.Instancia()));
            alteracao.ArvoresRemovidas = arvores
                .Where(a =>
                    NaFaixa(
                        estrada,
                        corredor,
                        alteracao.Origem + Vector3.Scale(a.position, dados.size)
                    )
                )
                .Select(ArvoreDaEstrada.De)
                .ToArray();
            return alteracao;
        }

        private static float ErroOriginal(
            EstradaNativa estrada,
            Terrain terreno,
            AlteracaoDeTerreno anterior
        )
        {
            if (
                anterior != null
                && Mathf.Approximately(terreno.heightmapPixelError, anterior.ErroPixelAplicado)
            )
                return anterior.ErroPixelOriginal;
            foreach (
                var outra in Object.FindObjectsByType<EstradaNativa>(FindObjectsInactive.Include)
            )
            {
                if (outra == estrada)
                    continue;
                var registro = outra.Registro as RegistroDaEstrada;
                if (registro == null || !registro.Aplicada)
                    continue;
                var outraAlteracao = registro.Alteracoes.FirstOrDefault(a =>
                    a.Dados == terreno.terrainData
                );
                if (
                    outraAlteracao != null
                    && Mathf.Approximately(
                        terreno.heightmapPixelError,
                        outraAlteracao.ErroPixelAplicado
                    )
                )
                    return outraAlteracao.ErroPixelOriginal;
            }
            return terreno.heightmapPixelError;
        }

        private static void RestaurarQualidade(
            EstradaNativa estrada,
            IEnumerable<AlteracaoDeTerreno> alteracoes,
            Terrain[] terrenos
        )
        {
            var aplicadas = Object
                .FindObjectsByType<EstradaNativa>(FindObjectsInactive.Include)
                .Select(e => e.Registro as RegistroDaEstrada)
                .Where(r => r != null && r.Aplicada)
                .ToArray();
            foreach (var alteracao in alteracoes)
            {
                if (aplicadas.Any(r => r.Alteracoes.Any(a => a.Dados == alteracao.Dados)))
                    continue;
                var terreno = terrenos.FirstOrDefault(t => t.terrainData == alteracao.Dados);
                if (
                    terreno == null
                    || !Mathf.Approximately(
                        terreno.heightmapPixelError,
                        alteracao.ErroPixelAplicado
                    )
                )
                    continue;
                Undo.RecordObject(terreno, "Restaurar precisão do terreno");
                terreno.heightmapPixelError = alteracao.ErroPixelOriginal;
                EditorUtility.SetDirty(terreno);
            }
        }

        private static void ValidarBuracos(
            EstradaNativa estrada,
            Terrain terreno,
            CorredorDaEstrada corredor
        )
        {
            var dados = terreno.terrainData;
            var resolucao = dados.holesResolution;
            if (resolucao <= 0)
                return;
            var rect = Recorte(terreno, corredor.Limites, resolucao, false);
            var buracos = dados.GetHoles(rect.x, rect.y, rect.width, rect.height);
            var diagonal = new Vector2(dados.size.x, dados.size.z).magnitude / resolucao * 0.5f;
            for (var z = rect.y; z < rect.yMax; z++)
            for (var x = rect.x; x < rect.xMax; x++)
            {
                // GetHoles usa true para superfície existente e false para um buraco.
                if (buracos[z - rect.y, x - rect.x])
                    continue;
                var ponto =
                    terreno.transform.position
                    + new Vector3(
                        (x + 0.5f) / resolucao * dados.size.x,
                        0f,
                        (z + 0.5f) / resolucao * dados.size.z
                    );
                if (
                    corredor.Distancia(ponto)
                    <= PerfilDaEstrada.MeiaLargura(estrada, ponto)
                        + estrada.TransicaoDoCorredor
                        + diagonal
                )
                    throw new InvalidOperationException(
                        "A estrada cruza um buraco do Terrain. Ajuste o traçado; pontes precisam de autoria própria."
                    );
            }
        }

        private static void ValidarCelulasCompartilhadas(
            EstradaNativa estrada,
            List<AlteracaoDeTerreno> novas
        )
        {
            foreach (
                var outra in Object.FindObjectsByType<EstradaNativa>(FindObjectsInactive.Include)
            )
            {
                if (outra == estrada)
                    continue;
                var registro = outra.Registro as RegistroDaEstrada;
                if (registro == null || !registro.Aplicada)
                    continue;
                foreach (var nova in novas)
                {
                    var antiga = registro.Alteracoes.FirstOrDefault(a => a.Dados == nova.Dados);
                    if (antiga == null)
                        continue;
                    var alturas = new HashSet<int>(antiga.IndicesAltura);
                    if (nova.IndicesAltura.Any(alturas.Contains))
                        FalhaSobreposicao(outra);
                    foreach (var camada in nova.Detalhes)
                    {
                        var outraCamada = antiga.Detalhes.FirstOrDefault(c =>
                            c.Camada == camada.Camada
                        );
                        if (outraCamada == null)
                            continue;
                        var celulas = new HashSet<int>(outraCamada.Indices);
                        if (camada.Indices.Any(celulas.Contains))
                            FalhaSobreposicao(outra);
                    }
                }
            }
        }

        private static void FalhaSobreposicao(EstradaNativa outra)
        {
            throw new InvalidOperationException(
                "A resolução do Terrain faz esta faixa compartilhar células com '"
                    + outra.name
                    + "'. Afaste os corredores ou aumente a resolução de detalhes antes de aplicar."
            );
        }

        private static void PrepararDetalhes(
            EstradaNativa estrada,
            Terrain terreno,
            CorredorDaEstrada corredor,
            AlteracaoDeTerreno anterior,
            AlteracaoDeTerreno alteracao
        )
        {
            var dados = terreno.terrainData;
            var resolucao = dados.detailResolution;
            if (resolucao == 0)
                return;
            var rect = Recorte(terreno, corredor.Limites, resolucao, false);
            for (var camada = 0; camada < dados.detailPrototypes.Length; camada++)
            {
                var valores = dados.GetDetailLayer(rect.x, rect.y, rect.width, rect.height, camada);
                var antiga = anterior?.Detalhes.FirstOrDefault(c => c.Camada == camada);
                var originais =
                    antiga == null
                        ? new Dictionary<int, int>()
                        : antiga
                            .Indices.Select((v, i) => (v, i))
                            .ToDictionary(p => p.v, p => antiga.Antes[p.i]);
                var indices = new List<int>();
                var antes = new List<int>();
                for (var z = rect.y; z < rect.yMax; z++)
                for (var x = rect.x; x < rect.xMax; x++)
                {
                    var ponto = Posicao(alteracao, x, z, resolucao, false);
                    // Células inteiras de grama também precisam desaparecer na borda do corredor.
                    var meiaDiagonal =
                        new Vector2(dados.size.x, dados.size.z).magnitude / resolucao * 0.5f;
                    if (!NaFaixa(estrada, corredor, ponto, meiaDiagonal))
                        continue;
                    var indice = z * resolucao + x;
                    indices.Add(indice);
                    antes.Add(
                        originais.TryGetValue(indice, out var valor)
                            ? valor
                            : valores[z - rect.y, x - rect.x]
                    );
                }
                alteracao.Detalhes.Add(
                    new AlteracaoDeDetalhe
                    {
                        Camada = camada,
                        Indices = indices.ToArray(),
                        Antes = antes.ToArray(),
                    }
                );
            }
        }

        private static bool NaFaixa(
            EstradaNativa estrada,
            CorredorDaEstrada corredor,
            Vector3 ponto,
            float extra = 0f
        )
        {
            return corredor.Distancia(ponto)
                <= PerfilDaEstrada.MeiaLargura(estrada, ponto)
                    + estrada.TransicaoDoCorredor
                    + estrada.MargemVegetacao
                    + extra;
        }

        private static void ValidarIntegridade(RegistroDaEstrada registro, Terrain[] terrenos)
        {
            var corredor = new CorredorDaEstrada(registro.Caminho, registro.Raio);
            foreach (var a in registro.Alteracoes)
            {
                var dados = a.Dados;
                var terreno = terrenos.FirstOrDefault(t => t.terrainData == dados);
                if (
                    dados == null
                    || terreno == null
                    || terreno.transform.position != a.Origem
                    || dados.size != a.Tamanho
                    || dados.heightmapResolution != a.ResolucaoAltura
                    || dados.detailResolution != a.ResolucaoDetalhe
                )
                    throw new InvalidOperationException(
                        "O Terrain original foi removido, movido ou redimensionado. Recupere sua configuração antes de restaurar."
                    );
                ValidarReferencias(
                    "árvores",
                    dados.treePrototypes.Select(p => p.prefab).ToArray(),
                    a.PrototiposArvores
                );
                ValidarReferencias(
                    "modelos de detalhes",
                    dados.detailPrototypes.Select(p => p.prototype).ToArray(),
                    a.PrototiposDetalhes
                );
                ValidarReferencias(
                    "texturas de detalhes",
                    dados.detailPrototypes.Select(p => p.prototypeTexture).ToArray(),
                    a.TexturasDetalhes
                );
                LerAlturas(
                    a,
                    (i, valor) =>
                    {
                        var tolerancia = registro.EmAplicacao ? 2f / 65535f : ToleranciaAltura;
                        if (
                            Mathf.Abs(valor - a.AlturasDepois[i]) > tolerancia
                            && (
                                !registro.EmAplicacao
                                || Mathf.Abs(valor - a.AlturasAntes[i]) > tolerancia
                            )
                        )
                            Conflito();
                        return valor;
                    },
                    false
                );
                foreach (var camada in a.Detalhes)
                    LerDetalhes(
                        a,
                        camada,
                        (i, valor) =>
                        {
                            if (valor != 0 && (!registro.EmAplicacao || valor != camada.Antes[i]))
                                Conflito();
                            return valor;
                        },
                        false
                    );
                if (
                    !registro.EmAplicacao
                    && dados.treeInstances.Any(arvore =>
                    {
                        var p = a.Origem + Vector3.Scale(arvore.position, dados.size);
                        var ruido = Mathf.PerlinNoise(p.x * 0.17f + registro.Semente, p.z * 0.17f);
                        var raio =
                            registro.Largura * 0.5f * (1f + (ruido - 0.5f) * registro.Variacao)
                            + registro.Transicao
                            + registro.Margem;
                        return corredor.Distancia(p) <= raio;
                    })
                )
                    Conflito();
            }
        }

        private static void ValidarReferencias<T>(string grupo, T[] atuais, T[] anteriores)
            where T : Object
        {
            if (anteriores == null || atuais.Length != anteriores.Length)
                throw new InvalidOperationException(
                    "As camadas de "
                        + grupo
                        + " mudaram: quantidade atual "
                        + atuais.Length
                        + ", original "
                        + (anteriores == null ? "ausente" : anteriores.Length.ToString())
                        + ". Reponha os protótipos originais antes de restaurar."
                );
            for (var i = 0; i < atuais.Length; i++)
            {
                // O getter nativo pode devolver fake-null; o backup recarrega null real.
                if ((Object)atuais[i] == (Object)anteriores[i])
                    continue;
                throw new InvalidOperationException(
                    "As camadas de "
                        + grupo
                        + " mudaram no índice "
                        + i
                        + ": atual "
                        + Referencia(atuais[i])
                        + "; original "
                        + Referencia(anteriores[i])
                        + ". Reponha os protótipos originais antes de restaurar."
                );
            }
        }

        private static string Referencia(Object objeto)
        {
            if (objeto == null)
                return "(nenhum)";
            var caminho = AssetDatabase.GetAssetPath(objeto);
            return objeto.name + " [" + AssetDatabase.AssetPathToGUID(caminho) + "]";
        }

        private static void Conflito()
        {
            throw new InvalidOperationException(
                "Há edição manual dentro da faixa aplicada. Nada foi alterado. Desfaça essa edição antes de reaplicar/restaurar; pintura e vegetação fora da faixa são preservadas."
            );
        }

        private static void Escrever(List<AlteracaoDeTerreno> alteracoes, bool aplicar)
        {
            foreach (var a in alteracoes)
            {
                LerAlturas(a, (i, _) => aplicar ? a.AlturasDepois[i] : a.AlturasAntes[i], true);
                foreach (var camada in a.Detalhes)
                    LerDetalhes(a, camada, (i, _) => aplicar ? 0 : camada.Antes[i], true);
                var arvores = a.Dados.treeInstances.ToList();
                var ocorrencias = new Dictionary<TreeInstance, int>();
                foreach (var salva in a.ArvoresRemovidas)
                {
                    var removida = salva.Instancia();
                    var indice = arvores.FindIndex(atual => MesmaArvore(atual, removida));
                    if (aplicar && indice >= 0)
                        arvores.RemoveAt(indice);
                    if (!aplicar)
                    {
                        ocorrencias.TryGetValue(removida, out var quantidade);
                        quantidade++;
                        ocorrencias[removida] = quantidade;
                        if (arvores.Count(atual => MesmaArvore(atual, removida)) < quantidade)
                            arvores.Add(removida);
                    }
                }
                a.Dados.SetTreeInstances(arvores.ToArray(), false);
                a.Dados.SyncHeightmap();
                EditorUtility.SetDirty(a.Dados);
            }
        }

        private static bool MesmaArvore(TreeInstance a, TreeInstance b)
        {
            return a.position == b.position
                && a.prototypeIndex == b.prototypeIndex
                && a.rotation == b.rotation
                && a.widthScale == b.widthScale
                && a.heightScale == b.heightScale
                && a.color.Equals(b.color)
                && a.lightmapColor.Equals(b.lightmapColor);
        }

        private static void LerAlturas(
            AlteracaoDeTerreno a,
            Func<int, float, float> funcao,
            bool escrever
        )
        {
            var indices = a.IndicesAltura;
            var resolucao = a.ResolucaoAltura;
            for (var inicio = 0; inicio < indices.Length; )
            {
                var fim = inicio + 1;
                var linha = indices[inicio] / resolucao;
                while (fim < indices.Length && indices[fim] / resolucao == linha)
                    fim++;
                var x = indices[inicio] % resolucao;
                var largura = indices[fim - 1] % resolucao - x + 1;
                var valores = a.Dados.GetHeights(x, linha, largura, 1);
                for (var i = inicio; i < fim; i++)
                    valores[0, indices[i] % resolucao - x] = funcao(
                        i,
                        valores[0, indices[i] % resolucao - x]
                    );
                if (escrever)
                    a.Dados.SetHeightsDelayLOD(x, linha, valores);
                inicio = fim;
            }
        }

        private static void LerDetalhes(
            AlteracaoDeTerreno a,
            AlteracaoDeDetalhe camada,
            Func<int, int, int> funcao,
            bool escrever
        )
        {
            var indices = camada.Indices;
            var resolucao = a.ResolucaoDetalhe;
            for (var inicio = 0; inicio < indices.Length; )
            {
                var fim = inicio + 1;
                var linha = indices[inicio] / resolucao;
                while (fim < indices.Length && indices[fim] / resolucao == linha)
                    fim++;
                var x = indices[inicio] % resolucao;
                var largura = indices[fim - 1] % resolucao - x + 1;
                var valores = a.Dados.GetDetailLayer(x, linha, largura, 1, camada.Camada);
                for (var i = inicio; i < fim; i++)
                    valores[0, indices[i] % resolucao - x] = funcao(
                        i,
                        valores[0, indices[i] % resolucao - x]
                    );
                if (escrever)
                    a.Dados.SetDetailLayer(x, linha, camada.Camada, valores);
                inicio = fim;
            }
        }

        private static Vector3 Posicao(
            AlteracaoDeTerreno a,
            int x,
            int z,
            int resolucao,
            bool altura
        )
        {
            var divisor = altura ? resolucao - 1f : resolucao;
            var deslocamento = altura ? 0f : 0.5f;
            return a.Origem
                + new Vector3(
                    (x + deslocamento) / divisor * a.Tamanho.x,
                    0f,
                    (z + deslocamento) / divisor * a.Tamanho.z
                );
        }

        private static RectInt Recorte(Terrain terreno, Bounds limites, int resolucao, bool altura)
        {
            var divisor = altura ? resolucao - 1f : resolucao;
            var origem = terreno.transform.position;
            var tamanho = terreno.terrainData.size;
            var x0 = Mathf.Clamp(
                Mathf.FloorToInt((limites.min.x - origem.x) / tamanho.x * divisor) - 1,
                0,
                resolucao - 1
            );
            var z0 = Mathf.Clamp(
                Mathf.FloorToInt((limites.min.z - origem.z) / tamanho.z * divisor) - 1,
                0,
                resolucao - 1
            );
            var x1 = Mathf.Clamp(
                Mathf.CeilToInt((limites.max.x - origem.x) / tamanho.x * divisor) + 1,
                0,
                resolucao - 1
            );
            var z1 = Mathf.Clamp(
                Mathf.CeilToInt((limites.max.z - origem.z) / tamanho.z * divisor) + 1,
                0,
                resolucao - 1
            );
            return new RectInt(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
        }

        private static bool Contem(Terrain terreno, Vector3 ponto)
        {
            var p = ponto - terreno.transform.position;
            var tamanho = terreno.terrainData.size;
            return p.x >= -0.001f
                && p.z >= -0.001f
                && p.x <= tamanho.x + 0.001f
                && p.z <= tamanho.z + 0.001f;
        }

        private static bool Intersecta(Bounds limites, Terrain terreno)
        {
            var origem = terreno.transform.position;
            var tamanho = terreno.terrainData.size;
            return limites.max.x >= origem.x
                && limites.min.x <= origem.x + tamanho.x
                && limites.max.z >= origem.z
                && limites.min.z <= origem.z + tamanho.z;
        }
    }
}
