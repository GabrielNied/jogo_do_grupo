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
    public static partial class AplicadorDeEstradas
    {
        internal sealed class ViaAplicada
        {
            public EstradaNativa Estrada;
            public RegistroDaEstrada Registro;
            public List<Vector3> Pontos;
            public CorredorDaEstrada Corredor;
            public string Configuracao;
            public List<AlteracaoDeTerreno> Legadas;
        }

        private static void AplicarConfiguracao(string json, EstradaNativa estrada)
        {
            var id = estrada.Id;
            var registro = estrada.Registro;
            EditorJsonUtility.FromJsonOverwrite(json, estrada);
            estrada.Id = id;
            estrada.Registro = registro;
        }

        // O registro antigo não guardava rebaixo/transições separadamente. Seu perfil esparso
        // permanece exato até o autor reaplicar essa via; inferimos apenas a aparência já aplicada.
        private static void ConfiguracaoLegada(EstradaNativa estrada, RegistroDaEstrada registro)
        {
            var so = new SerializedObject(estrada);
            so.FindProperty("_largura").floatValue = registro.Largura;
            so.FindProperty("_transicao").floatValue = registro.Transicao;
            so.FindProperty("_margemVegetacao").floatValue = registro.Margem;
            so.FindProperty("_variacaoLateral").floatValue = registro.Variacao;
            so.FindProperty("_semente").intValue = registro.Semente;
            var superficie = estrada.transform.Find("SuperficieEstrada");
            if (superficie != null)
            {
                var mesh = superficie.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null && mesh.uv.Length > 0)
                {
                    var uvs = mesh.uv;
                    var extensao = uvs.TakeWhile(uv => Mathf.Approximately(uv.y, uvs[0].y))
                        .Max(uv => Mathf.Abs(uv.x));
                    var p = registro.Caminho[0];
                    var ruido = Mathf.PerlinNoise(p.x * 0.17f + registro.Semente, p.z * 0.17f);
                    var meiaLargura =
                        registro.Largura * 0.5f * (1f + (ruido - 0.5f) * registro.Variacao);
                    so.FindProperty("_transicaoVisual").floatValue = Mathf.Clamp(
                        extensao - meiaLargura,
                        0.2f,
                        registro.Transicao
                    );
                }
                var material = superficie.GetComponent<Renderer>()?.sharedMaterial;
                if (material != null)
                {
                    var caminho = AssetDatabase.GetAssetPath(material);
                    so.FindProperty("_material").objectReferenceValue =
                        (
                            caminho.StartsWith(PastaGerada + "/", StringComparison.Ordinal)
                            && caminho.EndsWith("/Estrada.mat", StringComparison.Ordinal)
                        )
                            ? null
                            : material;
                    if (material.HasProperty("_Tipo"))
                        so.FindProperty("_tipo").enumValueIndex =
                            material.GetFloat("_Tipo") >= 0.5f ? 1 : 0;
                    if (material.HasProperty("_Cascos"))
                        so.FindProperty("_intensidadeCascos").floatValue = material.GetFloat(
                            "_Cascos"
                        );
                }
            }
            var pedras = estrada.transform.Find("PedregulhosEstrada");
            so.FindProperty("_pedregulhos").boolValue = pedras != null;
            if (pedras != null)
            {
                var material = pedras.GetComponent<Renderer>()?.sharedMaterial;
                if (material != null && material.HasProperty("_CorPedra"))
                    so.FindProperty("_corCascalhos").colorValue = material.GetColor("_CorPedra");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Recompor(EstradaNativa alvo, bool aplicar)
        {
            ValidarEdicao(alvo, aplicar);
            ValidarIdentidade(alvo);
            var registroAlvo = alvo.Registro as RegistroDaEstrada;
            if (!aplicar && (registroAlvo == null || !registroAlvo.Aplicada))
                return;
            // Uma rede por cena. Inclui faixas antigas, novas e células de grama compartilhadas,
            // mesmo quando a resolução do Terrain aproxima corredores visualmente separados.
            var todas = Object
                .FindObjectsByType<EstradaNativa>(FindObjectsInactive.Include)
                .Where(e => e.gameObject.scene == alvo.gameObject.scene)
                .ToArray();
            foreach (var estrada in todas)
                if (estrada.PossuiRegistro && estrada.Registro == null)
                    throw new InvalidOperationException(
                        "Falta o backup de uma estrada da cena. Recupere seu Registro original antes de recompor a rede."
                    );
            var estradas = todas
                .Where(e => e.gameObject.scene == alvo.gameObject.scene)
                .Where(e => e == alvo || (e.Registro is RegistroDaEstrada r && r.Aplicada))
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .ToArray();
            foreach (var estrada in estradas)
            {
                ValidarEdicao(estrada, estrada == alvo && aplicar);
                ValidarIdentidade(estrada);
            }
            var terrenos = estradas.SelectMany(TerrenosDa).Distinct().ToArray();
            ValidarTerrenos(terrenos);
            var anteriores = estradas
                .Select(e => e.Registro as RegistroDaEstrada)
                .Where(r => r != null && r.Aplicada)
                .ToArray();
            foreach (var r in anteriores)
            {
                if (r.EmAplicacao || (r.Rede != null && r.Rede.EmAplicacao))
                    throw new InvalidOperationException(
                        "Aplicação da rede interrompida. Use Restaurar rede interrompida no menu Estradas antes de continuar."
                    );
                ValidarIntegridade(r, terrenos);
            }
            var configuracoesAtuais = estradas.ToDictionary(
                e => e,
                e => EditorJsonUtility.ToJson(e)
            );
            var registrosAnteriores = estradas
                .Where(e => e.Registro != null)
                .ToDictionary(
                    e => (RegistroDaEstrada)e.Registro,
                    e => EditorJsonUtility.ToJson(e.Registro)
                );
            var baseOriginal = Mesclar(anteriores.SelectMany(r => r.Alteracoes).Distinct());
            var baseTemporaria = ScriptableObject.CreateInstance<RegistroDaEstrada>();
            baseTemporaria.Aplicada = true;
            baseTemporaria.Alteracoes = baseOriginal;
            var vias = new List<ViaAplicada>();
            RegistroDaEstrada rede = anteriores.Select(r => r.Rede).FirstOrDefault(r => r != null);
            string redeAnterior = rede == null ? null : EditorJsonUtility.ToJson(rede);
            var qualidadesAnteriores = terrenos.ToDictionary(t => t, t => t.heightmapPixelError);
            var houveEscrita = false;
            var registrosCriados = new List<RegistroDaEstrada>();
            try
            {
                foreach (var estrada in estradas)
                {
                    if (estrada == alvo && !aplicar)
                        continue;
                    var registro = estrada.Registro as RegistroDaEstrada;
                    var nova = estrada == alvo;
                    var legadas =
                        !nova
                        && registro != null
                        && registro.PerfilLegado != null
                        && registro.PerfilLegado.Count > 0
                            ? registro.PerfilLegado
                            : (
                                !nova
                                && registro != null
                                && registro.Rede == null
                                && string.IsNullOrEmpty(registro.ConfiguracaoAplicada)
                                    ? registro.Alteracoes
                                    : null
                            );
                    if (legadas != null && string.IsNullOrEmpty(registro.ConfiguracaoAplicada))
                        ConfiguracaoLegada(estrada, registro);
                    var pontos = nova ? PerfilDaEstrada.Amostrar(estrada) : registro.Caminho;
                    if (!nova && !string.IsNullOrEmpty(registro.ConfiguracaoAplicada))
                        AplicarConfiguracao(registro.ConfiguracaoAplicada, estrada);
                    ValidarEdicao(estrada);
                    ValidarCobertura(estrada, pontos, TerrenosDa(estrada));
                    var via = new ViaAplicada
                    {
                        Estrada = estrada,
                        Registro = registro,
                        Pontos = pontos,
                        Corredor = new CorredorDaEstrada(
                            pontos,
                            PerfilDaEstrada.RaioMaximo(estrada)
                        ),
                        Configuracao = EditorJsonUtility.ToJson(estrada),
                        Legadas = legadas,
                    };
                    vias.Add(via);
                }
                // Tudo é calculado usando o backup original, antes de qualquer escrita no Terrain.
                var novas = Mesclar(
                    vias.SelectMany(v =>
                        v.Legadas != null
                            ? (IEnumerable<AlteracaoDeTerreno>)v.Legadas
                            : TerrenosDa(v.Estrada)
                                .Where(t => Intersecta(v.Corredor.Limites, t))
                                .Select(t => Preparar(v.Estrada, t, v.Corredor, baseTemporaria))
                    )
                );
                foreach (var a in novas)
                {
                    var terreno = terrenos.First(t => t.terrainData == a.Dados);
                    var anterior = baseOriginal.FirstOrDefault(v => v.Dados == a.Dados);
                    a.ErroPixelOriginal =
                        anterior != null
                        && Mathf.Approximately(
                            terreno.heightmapPixelError,
                            anterior.ErroPixelAplicado
                        )
                            ? anterior.ErroPixelOriginal
                            : terreno.heightmapPixelError;
                    a.ErroPixelAplicado = Mathf.Min(
                        terreno.heightmapPixelError,
                        ErroPixelDaEstrada
                    );
                }
                VisualDaEstrada.ValidarCustoDaRede(vias);
                GarantirPasta(PastaGerada);
                foreach (var via in vias)
                {
                    if (string.IsNullOrEmpty(via.Estrada.Id))
                        via.Estrada.Id = Guid.NewGuid().ToString("N");
                    var pasta = PastaGerada + "/" + via.Estrada.Id;
                    GarantirPasta(pasta);
                    if (via.Registro == null)
                    {
                        via.Registro = AssetDatabase.LoadAssetAtPath<RegistroDaEstrada>(
                            pasta + "/Registro.asset"
                        );
                        if (via.Registro == null)
                        {
                            via.Registro = ScriptableObject.CreateInstance<RegistroDaEstrada>();
                            AssetDatabase.CreateAsset(via.Registro, pasta + "/Registro.asset");
                        }
                        registrosCriados.Add(via.Registro);
                    }
                }
                if (rede == null)
                {
                    rede = ScriptableObject.CreateInstance<RegistroDaEstrada>();
                    var dono = vias.Count > 0 ? vias[0].Estrada : alvo;
                    var pasta = PastaGerada + "/" + dono.Id;
                    GarantirPasta(pasta);
                    AssetDatabase.CreateAsset(
                        rede,
                        AssetDatabase.GenerateUniqueAssetPath(pasta + "/Rede.asset")
                    );
                }
                var grupo = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Recompor rede de estradas");
                Undo.RegisterCompleteObjectUndo(rede, "Backup da rede");
                foreach (var estrada in estradas)
                {
                    Undo.RecordObject(estrada, "Rede de estradas");
                    if (estrada.Registro != null)
                        Undo.RegisterCompleteObjectUndo(estrada.Registro, "Registro da estrada");
                }
                foreach (var terreno in terrenos)
                {
                    Undo.RegisterCompleteObjectUndo(terreno.terrainData, "Terreno da rede");
                    Undo.RecordObject(terreno, "Precisão do terreno");
                }
                // Diário persistido ANTES da escrita: guarda também a antiga faixa abandonada.
                rede.Alteracoes = Diario(baseOriginal, novas);
                rede.EmAplicacao = true;
                foreach (var via in vias)
                {
                    var r = via.Registro;
                    r.Rede = rede;
                    r.Caminho = via.Pontos;
                    r.Raio = PerfilDaEstrada.RaioMaximo(via.Estrada);
                    r.Largura = via.Estrada.Largura;
                    r.Transicao = via.Estrada.TransicaoDoCorredor;
                    r.Margem = via.Estrada.MargemVegetacao;
                    r.Variacao = via.Estrada.VariacaoLateral;
                    r.Semente = via.Estrada.Semente;
                    r.ConfiguracaoAplicada = via.Configuracao;
                    r.PerfilLegado = via.Legadas;
                    r.Alteracoes = new List<AlteracaoDeTerreno>();
                    r.Aplicada = true;
                    r.EmAplicacao = true;
                    via.Estrada.Registro = r;
                    EditorUtility.SetDirty(via.Estrada);
                    EditorUtility.SetDirty(r);
                }
                if (!aplicar)
                {
                    registroAlvo.EmAplicacao = true;
                    registroAlvo.Rede = rede;
                    EditorUtility.SetDirty(registroAlvo);
                }
                EditorUtility.SetDirty(rede);
                AssetDatabase.SaveAssets();
                // Salva os ajustes pendentes reais no diário, mesmo se o editor interromper.
                foreach (var par in configuracoesAtuais)
                    AplicarConfiguracao(par.Value, par.Key);
                EditorSceneManager.SaveScene(alvo.gameObject.scene);
                foreach (var via in vias)
                    AplicarConfiguracao(via.Configuracao, via.Estrada);
                houveEscrita = true;
                Escrever(baseOriginal, false);
                Escrever(novas, true);
                foreach (var a in novas)
                {
                    LerAlturas(
                        a,
                        (i, valor) =>
                        {
                            a.AlturasDepois[i] = valor;
                            return valor;
                        },
                        false
                    );
                    var terreno = terrenos.First(t => t.terrainData == a.Dados);
                    terreno.heightmapPixelError = Mathf.Min(
                        terreno.heightmapPixelError,
                        ErroPixelDaEstrada
                    );
                    a.ErroPixelAplicado = terreno.heightmapPixelError;
                    EditorUtility.SetDirty(terreno);
                }
                foreach (var estrada in estradas)
                    VisualDaEstrada.Remover(estrada);
                VisualDaEstrada.GerarRede(vias, p => AlturaEm(terrenos, p));
                // Não deixe de fora células antigas até a superfície ser concluída.
                rede.Alteracoes = novas;
                rede.EmAplicacao = false;
                foreach (var via in vias)
                    via.Registro.EmAplicacao = false;
                if (!aplicar)
                {
                    registroAlvo.Aplicada = false;
                    registroAlvo.EmAplicacao = false;
                    registroAlvo.Rede = null;
                    registroAlvo.PerfilLegado = null;
                    registroAlvo.Alteracoes = new List<AlteracaoDeTerreno>();
                    EditorUtility.SetDirty(registroAlvo);
                }
                RestaurarQualidade(alvo, baseOriginal, terrenos);
                Undo.CollapseUndoOperations(grupo);
            }
            catch
            {
                if (houveEscrita)
                {
                    Escrever(rede.Alteracoes, false);
                    Escrever(baseOriginal, true);
                }
                if (redeAnterior != null)
                    EditorJsonUtility.FromJsonOverwrite(redeAnterior, rede);
                foreach (var par in registrosAnteriores)
                {
                    EditorJsonUtility.FromJsonOverwrite(par.Value, par.Key);
                    EditorUtility.SetDirty(par.Key);
                }
                foreach (var r in registrosCriados)
                {
                    r.Aplicada = false;
                    r.EmAplicacao = false;
                    r.Rede = null;
                    r.Alteracoes = new List<AlteracaoDeTerreno>();
                    EditorUtility.SetDirty(r);
                }
                foreach (var via in vias)
                    if (registrosCriados.Contains(via.Registro))
                        via.Estrada.Registro = null;
                foreach (var par in qualidadesAnteriores)
                {
                    par.Key.heightmapPixelError = par.Value;
                    EditorUtility.SetDirty(par.Key);
                }
                if (redeAnterior == null && rede != null)
                {
                    rede.EmAplicacao = false;
                    rede.Alteracoes = new List<AlteracaoDeTerreno>();
                }
                if (houveEscrita)
                {
                    // Restaura também o visual anterior se a falha ocorreu depois de removê-lo.
                    var antigas = new List<ViaAplicada>();
                    foreach (var estrada in estradas)
                    {
                        var r = estrada.Registro as RegistroDaEstrada;
                        VisualDaEstrada.Remover(estrada);
                        if (r == null || !r.Aplicada)
                            continue;
                        if (!string.IsNullOrEmpty(r.ConfiguracaoAplicada))
                            AplicarConfiguracao(r.ConfiguracaoAplicada, estrada);
                        antigas.Add(
                            new ViaAplicada
                            {
                                Estrada = estrada,
                                Registro = r,
                                Pontos = r.Caminho,
                                Corredor = new CorredorDaEstrada(r.Caminho, r.Raio),
                            }
                        );
                    }
                    VisualDaEstrada.GerarRede(antigas, p => AlturaEm(terrenos, p));
                }
                throw;
            }
            finally
            {
                // Restaura ajustes ainda não aplicados no Inspector de outras vias.
                foreach (var par in configuracoesAtuais)
                {
                    var id = par.Key.Id;
                    var registro = par.Key.Registro;
                    EditorJsonUtility.FromJsonOverwrite(par.Value, par.Key);
                    par.Key.Id = id;
                    par.Key.Registro = registro;
                    EditorUtility.SetDirty(par.Key);
                }
                foreach (var via in vias)
                    if (via.Registro != null)
                        EditorUtility.SetDirty(via.Registro);
                Object.DestroyImmediate(baseTemporaria);
                if (rede != null)
                    EditorUtility.SetDirty(rede);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(alvo.gameObject.scene);
                EditorSceneManager.SaveScene(alvo.gameObject.scene);
            }
            Debug.Log(
                "[Estradas] Rede recomposta e salva sobre o terreno original; publicação independente.",
                alvo
            );
        }

        // Mescla esparsa determinística: mínimo de altura equivale ao maior rebaixo.
        private static List<AlteracaoDeTerreno> Mesclar(IEnumerable<AlteracaoDeTerreno> origem)
        {
            var resultado = new List<AlteracaoDeTerreno>();
            foreach (var grupo in origem.GroupBy(a => a.Dados))
            {
                var lista = grupo.ToArray();
                var a = JsonUtility.FromJson<AlteracaoDeTerreno>(JsonUtility.ToJson(lista[0]));
                var alturas = lista
                    .SelectMany(v =>
                        v.IndicesAltura.Select(
                            (indice, i) =>
                                (indice, antes: v.AlturasAntes[i], depois: v.AlturasDepois[i])
                        )
                    )
                    .GroupBy(v => v.indice)
                    .OrderBy(g => g.Key)
                    .ToArray();
                a.IndicesAltura = alturas.Select(g => g.Key).ToArray();
                a.AlturasAntes = alturas.Select(g => g.First().antes).ToArray();
                a.AlturasDepois = alturas.Select(g => g.Min(v => v.depois)).ToArray();
                a.Detalhes = lista
                    .SelectMany(v => v.Detalhes)
                    .GroupBy(c => c.Camada)
                    .Select(g =>
                    {
                        var celulas = g.SelectMany(c =>
                                c.Indices.Select((indice, i) => (indice, valor: c.Antes[i]))
                            )
                            .GroupBy(v => v.indice)
                            .OrderBy(c => c.Key)
                            .ToArray();
                        return new AlteracaoDeDetalhe
                        {
                            Camada = g.Key,
                            Indices = celulas.Select(c => c.Key).ToArray(),
                            Antes = celulas.Select(c => c.First().valor).ToArray(),
                        };
                    })
                    .ToList();
                // Conserva multiplicidade de árvores coincidentes, sem duplicar entre corredores.
                a.ArvoresRemovidas = lista
                    .SelectMany(v => v.ArvoresRemovidas)
                    .Distinct()
                    .SelectMany(arvore =>
                        Enumerable.Repeat(
                            arvore,
                            lista.Max(v => v.ArvoresRemovidas.Count(t => t.Equals(arvore)))
                        )
                    )
                    .ToArray();
                resultado.Add(a);
            }
            return resultado;
        }

        private static List<AlteracaoDeTerreno> Diario(
            List<AlteracaoDeTerreno> antigas,
            List<AlteracaoDeTerreno> novas
        )
        {
            var diario = Mesclar(antigas.Concat(novas));
            foreach (var a in diario)
            {
                var nova = novas.FirstOrDefault(n => n.Dados == a.Dados);
                if (nova != null)
                {
                    a.ErroPixelOriginal = nova.ErroPixelOriginal;
                    a.ErroPixelAplicado = nova.ErroPixelAplicado;
                }
                var valores =
                    nova == null
                        ? new Dictionary<int, float>()
                        : nova
                            .IndicesAltura.Select(
                                (indice, i) => (indice, valor: nova.AlturasDepois[i])
                            )
                            .ToDictionary(v => v.indice, v => v.valor);
                var antiga = antigas.FirstOrDefault(n => n.Dados == a.Dados);
                var anteriores =
                    antiga == null
                        ? new Dictionary<int, float>()
                        : antiga
                            .IndicesAltura.Select(
                                (indice, i) => (indice, valor: antiga.AlturasDepois[i])
                            )
                            .ToDictionary(v => v.indice, v => v.valor);
                a.AlturasIntermediarias = new float[a.IndicesAltura.Length];
                for (var i = 0; i < a.IndicesAltura.Length; i++)
                {
                    a.AlturasDepois[i] = valores.TryGetValue(a.IndicesAltura[i], out var valor)
                        ? valor
                        : a.AlturasAntes[i];
                    a.AlturasIntermediarias[i] = anteriores.TryGetValue(
                        a.IndicesAltura[i],
                        out var anterior
                    )
                        ? anterior
                        : a.AlturasAntes[i];
                }
            }
            return diario;
        }

        [MenuItem("Jogo do Grupo/Mundo/Estradas/Restaurar rede interrompida")]
        public static void RestaurarRedeInterrompida()
        {
            var todas = Object
                .FindObjectsByType<EstradaNativa>(FindObjectsInactive.Include)
                .Where(e =>
                    e.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                )
                .ToArray();
            foreach (var estrada in todas)
                if (estrada.PossuiRegistro && estrada.Registro == null)
                    throw new InvalidOperationException(
                        "Falta o backup de uma estrada da cena. Recupere seu Registro original antes de recompor a rede."
                    );
            var estradas = todas;
            var redes = estradas
                .Select(e => e.Registro as RegistroDaEstrada)
                .Where(r => r != null && r.Rede != null && (r.EmAplicacao || r.Rede.EmAplicacao))
                .Select(r => r.Rede)
                .Distinct()
                .ToArray();
            foreach (var rede in redes)
            {
                // A tolerância só aceita células ainda na base ou já no resultado do diário.
                var membros = estradas
                    .Where(e => (e.Registro as RegistroDaEstrada)?.Rede == rede)
                    .ToArray();
                foreach (var membro in membros)
                    ValidarIntegridade(
                        (RegistroDaEstrada)membro.Registro,
                        membros.SelectMany(TerrenosDa).Distinct().ToArray(),
                        true
                    );
                Escrever(rede.Alteracoes, false);
                rede.EmAplicacao = false;
                foreach (var membro in membros)
                {
                    var r = (RegistroDaEstrada)membro.Registro;
                    r.Aplicada = false;
                    r.EmAplicacao = false;
                    VisualDaEstrada.Remover(membro);
                    EditorUtility.SetDirty(r);
                    EditorSceneManager.MarkSceneDirty(membro.gameObject.scene);
                }
                RestaurarQualidade(
                    membros[0],
                    rede.Alteracoes,
                    membros.SelectMany(TerrenosDa).Distinct().ToArray()
                );
                EditorUtility.SetDirty(rede);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
        }
    }
}
