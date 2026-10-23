using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    public sealed class FerramentaDeEncostas : EditorWindow
    {
        [SerializeField]
        private ReceitaDeEncostas _receita;
        private ReceitaDeEncostas _temporaria;
        private PreviaDeEncostas _previa;
        private Vector2 _rolagem;
        private string _resultado;
        private bool _mostrarRochas,
            _mostrarMaterial;
        private ReceitaDeEncostas Atual => _receita != null ? _receita : _temporaria;

        [MenuItem("Jogo do Grupo/Mundo/Texturas do terreno/Pintar encostas")]
        public static void Abrir() => GetWindow<FerramentaDeEncostas>("Pintar encostas");

        private void OnEnable()
        {
            _temporaria = CreateInstance<ReceitaDeEncostas>();
            // HideAndDontSave inclui NotEditable e bloqueia os PropertyFields do rascunho.
            _temporaria.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _previa = new PreviaDeEncostas();
            EditorApplication.playModeStateChanged += AoMudarPlay;
            Undo.undoRedoPerformed += AoDesfazer;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= AoMudarPlay;
            Undo.undoRedoPerformed -= AoDesfazer;
            _previa?.Dispose();
            if (_temporaria != null)
                DestroyImmediate(_temporaria);
        }

        private void AoMudarPlay(PlayModeStateChange estado) => _previa?.Limpar();

        private void AoDesfazer()
        {
            _previa?.Limpar();
            Repaint();
        }

        private void OnSelectionChange()
        {
            _previa?.Limpar();
            Repaint();
        }

        public static Terrain[] Selecionados() =>
            Selection
                .gameObjects.SelectMany(o => o.GetComponentsInChildren<Terrain>())
                .Distinct()
                .OrderBy(t => t.transform.position.x)
                .ThenBy(t => t.transform.position.z)
                .ToArray();

        private void OnGUI()
        {
            if (Atual == null)
                return;
            _rolagem = EditorGUILayout.BeginScrollView(_rolagem);
            var terrenos = Selecionados();
            EditorGUILayout.HelpBox(
                "Selecione Terrain(s) ou o grupo de blocos na Hierarchy. "
                    + "Previa e uma sobreposicao laranja na Scene; aplicar modifica as alphamaps nativas. "
                    + "Configure os campos abaixo; salvar uma receita e opcional. Nenhuma operacao e automatica.",
                MessageType.Info
            );
            EditorGUILayout.LabelField("Terrenos selecionados", terrenos.Length.ToString());
            var receita = (ReceitaDeEncostas)
                EditorGUILayout.ObjectField(
                    "Receita (opcional)",
                    _receita,
                    typeof(ReceitaDeEncostas),
                    false
                );
            if (receita != _receita)
            {
                _receita = receita;
                _previa.Limpar();
            }
            if (GUILayout.Button("Salvar copia da receita"))
                SalvarReceita();
            var serializada = new SerializedObject(Atual);
            serializada.Update();
            var pintura = serializada.FindProperty("Pintura");
            Campo(pintura, "Camada", "Camada de rocha (qualquer textura)");
            Campo(pintura, "UsarInclinacao", "Usar inclinacao");
            if (pintura.FindPropertyRelative("UsarInclinacao").boolValue)
            {
                Campo(pintura, "Inicio", "Comecar em (graus)");
                Campo(pintura, "Plena", "Cobertura plena (graus)");
                Campo(pintura, "Cobertura", "Cobertura por inclinacao");
            }
            Campo(pintura, "UsarAltitude", "Usar altitude mundial");
            if (pintura.FindPropertyRelative("UsarAltitude").boolValue)
            {
                Campo(pintura, "AltitudeInicial", "Comecar em (metros mundiais)");
                Campo(pintura, "AltitudePlena", "Plena em (metros mundiais)");
                Campo(pintura, "CoberturaAltitude", "Cobertura por altitude");
            }
            EditorGUILayout.LabelField("Transicoes irregulares", EditorStyles.boldLabel);
            Campo(pintura, "EscalaIrregularidade", "Escala das manchas (m)");
            Campo(pintura, "VariacaoAltitude", "Variacao da altura (+/- m)");
            Campo(pintura, "VariacaoAngulo", "Variacao da inclinacao (+/- graus)");
            Campo(pintura, "Semente", "Semente das manchas");
            Campo(pintura, "LimitarRegiao", "Limitar a regiao circular");
            if (pintura.FindPropertyRelative("LimitarRegiao").boolValue)
            {
                Campo(pintura, "Centro", "Centro mundial (X, Z)");
                Campo(pintura, "Raio", "Raio (m)");
                Campo(pintura, "Borda", "Suavidade da borda (m)");
            }
            _mostrarRochas = EditorGUILayout.Foldout(
                _mostrarRochas,
                "Revestimento com rochas 3D (opcional)",
                true
            );
            if (_mostrarRochas)
            {
                EditorGUILayout.HelpBox(
                    "Decoracao sem colisores ou scripts de gameplay. Pecas acompanham "
                        + "a mascara acima e a inclinacao minima abaixo. Cada aplicacao substitui somente o grupo gerado "
                        + "na regiao escolhida; fora dela, preserva as pecas anteriores. Use modelos com a mesma linguagem visual.",
                    MessageType.Info
                );
                EditorGUILayout.PropertyField(
                    serializada.FindProperty("Rochas"),
                    new GUIContent("Configuracao de rochas"),
                    true
                );
            }
            if (serializada.ApplyModifiedProperties())
                _previa.Limpar();
            var erro = Validar(terrenos, Atual.Pintura);
            if (erro != null)
                EditorGUILayout.HelpBox(erro, MessageType.Warning);
            using (new EditorGUI.DisabledScope(erro != null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Atualizar previa"))
                    Executar(
                        () =>
                            _previa.Atualizar(
                                terrenos,
                                Atual.Pintura,
                                _mostrarRochas ? Atual.Rochas : null
                            ),
                        false
                    );
                if (GUILayout.Button("Limpar previa"))
                    _previa.Limpar();
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Aplicar / reaplicar pintura"))
                    Executar(() =>
                    {
                        PinturaDeEncostas.AplicarLote(terrenos, Atual.Pintura, false, Cancelar);
                        _resultado =
                            "Pintura aplicada a partir da base preservada. Ctrl+Z desfaz o lote; salve o projeto.";
                    });
                if (GUILayout.Button("Restaurar pintura de base"))
                    Executar(() =>
                    {
                        PinturaDeEncostas.AplicarLote(terrenos, Atual.Pintura, true, Cancelar);
                        _resultado =
                            "Base restaurada. Receita e rochas continuam disponiveis; Ctrl+Z desfaz.";
                    });
                if (_mostrarRochas)
                {
                    var erroRochas = RevestimentoDeEncostas.Validar(Atual.Rochas);
                    if (erroRochas != null)
                        EditorGUILayout.HelpBox(erroRochas, MessageType.Warning);
                    using (new EditorGUI.DisabledScope(erroRochas != null))
                        if (GUILayout.Button("Aplicar / substituir rochas geradas"))
                            Executar(() =>
                            {
                                var total = RevestimentoDeEncostas.Aplicar(
                                    terrenos,
                                    Atual.Pintura,
                                    Atual.Rochas,
                                    Cancelar
                                );
                                _resultado =
                                    total
                                    + " rochas decorativas geradas. Salve e publique o mapa pelo fluxo habitual.";
                            });
                    if (GUILayout.Button("Remover somente rochas geradas"))
                        Executar(() =>
                        {
                            Undo.IncrementCurrentGroup();
                            Undo.SetCurrentGroupName("Remover revestimento de encostas");
                            RevestimentoDeEncostas.Remover(terrenos, Atual.Pintura);
                            _resultado = "Grupos gerados removidos. Ctrl+Z restaura.";
                        });
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "A primeira aplicacao guarda somente as alphamaps originais compactadas em "
                    + "Mundo/Editor/BasesDeEncostas. Reaplicar usa essa base, permitindo reduzir os valores. Retoques "
                    + "manuais bloqueiam a reaplicacao: assuma explicitamente a pintura atual como nova base para preserva-los. "
                    + "Isso incorpora tambem a rocha ja aplicada. Desative Auto Repaint de outras ferramentas.",
                MessageType.Info
            );
            using (new EditorGUI.DisabledScope(erro != null))
                if (GUILayout.Button("Assumir pintura atual como nova base..."))
                    if (
                        EditorUtility.DisplayDialog(
                            "Substituir base preservada?",
                            "A pintura atual passa a ser a base. "
                                + "Retoques e rocha ja aplicada serao preservados em novas aplicacoes. Ctrl+Z desfaz esta troca.",
                            "Assumir base atual",
                            "Cancelar"
                        )
                    )
                        Executar(() => AssumirBase(terrenos));
            _mostrarMaterial = EditorGUILayout.Foldout(
                _mostrarMaterial,
                "Material triplanar (opcional)",
                true
            );
            if (_mostrarMaterial)
                MostrarMaterial(terrenos, erro);
            if (!string.IsNullOrEmpty(_resultado))
                EditorGUILayout.HelpBox(_resultado, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void MostrarMaterial(Terrain[] terrenos, string erro)
        {
            EditorGUILayout.HelpBox(
                "Cria um material novo para os terrenos selecionados, sem mudar os materiais "
                    + "originais. A camada escolhida usa projecao triplanar; o basemap distante permanece nativo. "
                    + "Ctrl+Z restaura a atribuicao anterior.",
                MessageType.Info
            );
            var valido =
                erro == null && MaterialDeEncostas.Validar(terrenos, Atual.Pintura.Camada, out _);
            if (
                erro == null
                && !MaterialDeEncostas.Validar(terrenos, Atual.Pintura.Camada, out var erroMaterial)
            )
                EditorGUILayout.HelpBox(erroMaterial, MessageType.Warning);
            using (new EditorGUI.DisabledScope(!valido))
                if (GUILayout.Button("Criar e aplicar material triplanar..."))
                {
                    var caminho = EditorUtility.SaveFilePanelInProject(
                        "Novo material de encostas",
                        "MaterialDeEncostas",
                        "mat",
                        "Escolha onde salvar o material novo."
                    );
                    if (!string.IsNullOrEmpty(caminho))
                        Executar(() =>
                        {
                            if (AssetDatabase.LoadMainAssetAtPath(caminho) != null)
                                throw new InvalidOperationException(
                                    "Escolha um caminho novo para preservar o asset existente."
                                );
                            MaterialDeEncostas.CriarEAplicar(
                                caminho,
                                terrenos,
                                Atual.Pintura.Camada
                            );
                            _resultado =
                                "Material novo aplicado. Ctrl+Z restaura as atribuicoes; salve o projeto.";
                        });
                }
        }

        private static void Campo(SerializedProperty pai, string nome, string texto) =>
            EditorGUILayout.PropertyField(pai.FindPropertyRelative(nome), new GUIContent(texto));

        public static string Validar(Terrain[] terrenos, ConfigDeEncostas pintura)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Saia do Play para editar.";
            if (terrenos.Length == 0)
                return "Selecione um ou mais objetos com Terrain.";
            if (terrenos.Length > 36)
                return "Selecione no maximo 36 terrenos por lote.";
            if (
                terrenos.Any(t =>
                    t.terrainData == null || !EditorUtility.IsPersistent(t.terrainData)
                )
            )
                return "Cada TerrainData precisa estar salvo como asset.";
            if (terrenos.Select(t => t.terrainData).Distinct().Count() != terrenos.Length)
                return "Dois objetos compartilham TerrainData. Selecione apenas um de cada base.";
            if (
                terrenos.Any(t =>
                    t.transform.lossyScale != Vector3.one
                    || Quaternion.Angle(t.transform.rotation, Quaternion.identity) > 0.01f
                )
            )
                return "Use Terrain com escala 1 e rotacao 0.";
            if (
                terrenos.Any(t =>
                    t.terrainData.terrainLayers.Length == 0
                    || t.terrainData.terrainLayers.Any(c => c == null)
                )
            )
                return "Configure as camadas de solo, sem referencias vazias.";
            if (pintura.Camada != null && pintura.Camada.diffuseTexture == null)
                return "A camada precisa de textura de cor.";
            return pintura.Validar();
        }

        private void SalvarReceita()
        {
            var caminho = EditorUtility.SaveFilePanelInProject(
                "Salvar receita",
                "ReceitaDeEncostas",
                "asset",
                "A receita guarda parametros, sem alterar o mapa."
            );
            if (string.IsNullOrEmpty(caminho))
                return;
            if (AssetDatabase.LoadMainAssetAtPath(caminho) != null)
            {
                _resultado = "Escolha um caminho novo para preservar a receita existente.";
                return;
            }
            _receita = Instantiate(Atual);
            _receita.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(_receita, caminho);
        }

        private static void AssumirBase(Terrain[] terrenos)
        {
            Undo.IncrementCurrentGroup();
            var grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Assumir pintura atual como base");
            try
            {
                foreach (var terreno in terrenos)
                {
                    var registro = BaseDeEncostas.Obter(terreno.terrainData, true);
                    Undo.RegisterCompleteObjectUndo(registro, "Substituir base de encostas");
                    registro.Capturar(terreno.terrainData);
                    EditorUtility.SetDirty(registro);
                }
                Undo.CollapseUndoOperations(grupo);
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

        private bool Cancelar(float progresso) =>
            EditorUtility.DisplayCancelableProgressBar(
                "Encostas",
                "Preparando o lote; cancelar restaura a operacao.",
                progresso
            );

        private void Executar(Action acao, bool limparPrevia = true)
        {
            try
            {
                acao();
                if (limparPrevia)
                    _previa.Limpar();
            }
            catch (Exception erro)
            {
                _resultado =
                    erro is OperationCanceledException
                        ? "Operacao cancelada; o lote foi restaurado."
                        : erro.Message;
                if (
                    !(
                        erro is OperationCanceledException
                        || erro is ArgumentException
                        || erro is InvalidOperationException
                    )
                )
                    Debug.LogException(erro);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                SceneView.RepaintAll();
            }
        }
    }
}
