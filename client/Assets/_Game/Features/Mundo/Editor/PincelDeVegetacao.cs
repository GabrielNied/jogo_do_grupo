using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Ferramenta de autoria com controles nativos do editor, sem UI de runtime.</summary>
    public sealed class PincelDeVegetacao : EditorWindow
    {
        internal static PincelDeVegetacao Instancia { get; private set; }

        [SerializeField]
        private ConjuntoDeVegetacao _conjunto;

        [SerializeField]
        private Terrain _terreno;
        private SerializedObject _edicao;
        private Vector2 _rolagem;
        private bool _ativo;
        private int _grupoUndo = -1;
        private int _controle;
        private Vector3 _ultimaPintura;
        private Terrain _terrenoDoGesto;
        private readonly System.Random _sorteio = new System.Random();
        private string _erro;

        [MenuItem("Jogo do Grupo/Mundo/Pincel de vegetação")]
        public static void Abrir() => GetWindow<PincelDeVegetacao>("Pincel de vegetação");

        private void OnEnable()
        {
            Instancia = this;
            minSize = new Vector2(360f, 480f);
            ToolManager.activeToolChanged += TrocarFerramenta;
            Undo.undoRedoPerformed += AtualizarCena;
            SelecionarTerrain();
        }

        private void OnDisable()
        {
            FinalizarGesto();
            ToolManager.activeToolChanged -= TrocarFerramenta;
            if (ToolManager.activeToolType == typeof(FerramentaDoPincelVegetacao))
                ToolManager.RestorePreviousTool();
            if (Instancia == this)
                Instancia = null;
            Undo.undoRedoPerformed -= AtualizarCena;
        }

        private void OnSelectionChange()
        {
            if (_grupoUndo < 0)
                SelecionarTerrain();
            Repaint();
        }

        private void TrocarFerramenta()
        {
            _ativo = ToolManager.activeToolType == typeof(FerramentaDoPincelVegetacao);
            if (!_ativo)
                FinalizarGesto();
            Repaint();
        }

        private void SelecionarTerrain()
        {
            var selecionado = Selection.activeGameObject;
            if (selecionado != null && selecionado.GetComponent<Terrain>() != null)
                _terreno = selecionado.GetComponent<Terrain>();
        }

        private static void AtualizarCena()
        {
            foreach (var terreno in Terrain.activeTerrains)
                terreno.Flush();
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Mistura manual no Terrain", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Pinte na janela Scene. Shift apaga só as espécies deste conjunto; Ctrl+Z desfaz o gesto inteiro. Alt continua navegando pela cena.",
                MessageType.Info
            );
            using (new EditorGUI.DisabledScope(_grupoUndo >= 0))
            {
                _terreno = (Terrain)
                    EditorGUILayout.ObjectField("Terreno", _terreno, typeof(Terrain), true);
                var conjunto = (ConjuntoDeVegetacao)
                    EditorGUILayout.ObjectField(
                        "Conjunto",
                        _conjunto,
                        typeof(ConjuntoDeVegetacao),
                        false
                    );
                if (conjunto != _conjunto)
                {
                    _conjunto = conjunto;
                    _edicao = null;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Novo conjunto"))
                        SalvarComo(false);
                    using (new EditorGUI.DisabledScope(_conjunto == null))
                    {
                        if (GUILayout.Button("Duplicar"))
                            SalvarComo(true);
                        if (GUILayout.Button("Salvar conjunto"))
                            AssetDatabase.SaveAssetIfDirty(_conjunto);
                    }
                }
            }

            _rolagem = EditorGUILayout.BeginScrollView(_rolagem);
            if (_conjunto != null)
            {
                if (_edicao == null || _edicao.targetObject != _conjunto)
                    _edicao = new SerializedObject(_conjunto);
                _edicao.Update();
                Campo("Raio", "Raio (m)");
                Campo("InclinacaoMaxima", "Inclinação máx. (°)");
                Campo("Semente", "Semente (detalhes)");
                EditorGUILayout.HelpBox(
                    "Cada grupo tem sua densidade e espaçamento. Para grama contínua, use espaçamento 0 e aumente a cobertura. Detalhes são distribuídos pela Unity: a distância é aproximada e limitada pelo modo e resolução do Terrain. Peso 0 desativa uma espécie.",
                    MessageType.None
                );
                EditorGUILayout.LabelField("Árvores", EditorStyles.boldLabel);
                Campo("ArvoresPorCemMetros", "Árvores / 100 m²");
                Campo("DistanciaEntreArvores", "Distância mínima (m)");
                Campo("Arvores", "Espécies de árvores");
                EditorGUILayout.LabelField("Plantas", EditorStyles.boldLabel);
                Percentual("CoberturaDePlantas", "Cobertura (%)");
                Campo("DistanciaEntrePlantas", "Espaçamento aprox. (m)");
                Campo("Plantas", "Espécies de plantas");
                EditorGUILayout.LabelField("Grama", EditorStyles.boldLabel);
                Percentual("CoberturaDeGrama", "Cobertura (%)");
                Campo("DistanciaEntreGramas", "Espaçamento aprox. (m)");
                Campo("Gramas", "Espécies de grama");
                _edicao.ApplyModifiedProperties();
            }
            EditorGUILayout.EndScrollView();
            var erro = ErroAtual();
            if (erro != null)
                EditorGUILayout.HelpBox(erro, MessageType.Warning);
            if (_erro != null)
                EditorGUILayout.HelpBox(_erro, MessageType.Error);
            using (new EditorGUI.DisabledScope(erro != null))
            {
                var ativo = GUILayout.Toggle(
                    _ativo,
                    _ativo ? "Pincel ativo — clique na Scene (Esc para sair)" : "Ativar pincel",
                    "Button"
                );
                if (ativo != _ativo)
                {
                    _ativo = ativo;
                    if (ativo)
                        ToolManager.SetActiveTool<FerramentaDoPincelVegetacao>();
                    else
                        ToolManager.RestorePreviousTool();
                    if (!ativo)
                        FinalizarGesto();
                    SceneView.RepaintAll();
                }
            }
        }

        private void Campo(string propriedade, string texto) =>
            EditorGUILayout.PropertyField(
                _edicao.FindProperty(propriedade),
                new GUIContent(texto),
                true
            );

        private void Percentual(string propriedade, string texto)
        {
            var campo = _edicao.FindProperty(propriedade);
            campo.floatValue =
                EditorGUILayout.Slider(texto, campo.floatValue * 100f, 0f, 100f) / 100f;
        }

        private string ErroAtual()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Saia do Play para pintar e salvar no Terrain de autoria.";
            if (_terreno == null || _terreno.terrainData == null)
                return "Selecione ChaoNativo ou atribua um Terrain.";
            if (!EditorUtility.IsPersistent(_terreno.terrainData))
                return "Este TerrainData é temporário. Abra AutoriaDoMapa e selecione o terreno com o asset de autoria.";
            return PinturaDeVegetacao.Validar(_conjunto);
        }

        private void SalvarComo(bool duplicar)
        {
            var caminho = EditorUtility.SaveFilePanelInProject(
                "Salvar conjunto de vegetação",
                duplicar ? _conjunto.name + " cópia" : "Novo conjunto",
                "asset",
                "Escolha onde guardar a receita do pincel.",
                "Assets/_Game/Features/Mundo/Editor"
            );
            if (string.IsNullOrEmpty(caminho))
                return;
            var novo = duplicar ? Instantiate(_conjunto) : CreateInstance<ConjuntoDeVegetacao>();
            AssetDatabase.CreateAsset(novo, caminho);
            AssetDatabase.SaveAssets();
            _conjunto = novo;
            _edicao = null;
            _erro = null;
        }

        internal void DesenharNaCena(SceneView cena)
        {
            var evento = Event.current;
            if (evento.type == EventType.MouseUp && _grupoUndo >= 0)
            {
                FinalizarGesto();
                evento.Use();
                return;
            }
            if (evento.type == EventType.KeyDown && evento.keyCode == KeyCode.Escape)
            {
                _ativo = false;
                evento.Use();
                ToolManager.RestorePreviousTool();
                FinalizarGesto();
                Repaint();
                cena.Repaint();
                return;
            }
            if (!_ativo || ErroAtual() != null)
            {
                FinalizarGesto();
                return;
            }
            var controle = GUIUtility.GetControlID(FocusType.Passive);
            if (evento.type == EventType.Layout && !evento.alt)
                HandleUtility.AddDefaultControl(controle);
            var colisor = _terreno.GetComponent<TerrainCollider>();
            if (
                colisor == null
                || !colisor.Raycast(
                    HandleUtility.GUIPointToWorldRay(evento.mousePosition),
                    out var hit,
                    float.MaxValue
                )
            )
                return;
            Handles.DrawWireDisc(hit.point, Vector3.up, _conjunto.Raio);
            if (evento.type == EventType.MouseMove)
                cena.Repaint();
            if (
                evento.alt
                || evento.button != 0
                || (evento.type != EventType.MouseDown && evento.type != EventType.MouseDrag)
            )
                return;
            try
            {
                if (evento.type == EventType.MouseDown)
                {
                    Undo.IncrementCurrentGroup();
                    _grupoUndo = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Pintar conjunto de vegetação");
                    Undo.RegisterCompleteObjectUndo(
                        _terreno.terrainData,
                        "Pintar conjunto de vegetação"
                    );
                    _controle = controle;
                    GUIUtility.hotControl = controle;
                    _terrenoDoGesto = _terreno;
                    _ultimaPintura = hit.point;
                    PinturaDeVegetacao.Aplicar(
                        _terreno,
                        _conjunto,
                        hit.point,
                        evento.shift,
                        false,
                        _sorteio
                    );
                }
                else if (_grupoUndo >= 0 && _terrenoDoGesto == _terreno)
                {
                    var passo = Mathf.Max(0.25f, _conjunto.Raio * 0.5f);
                    var direcao = hit.point - _ultimaPintura;
                    var quantidade = Mathf.Min(256, Mathf.FloorToInt(direcao.magnitude / passo));
                    var inicio = _ultimaPintura;
                    for (var i = 1; i <= quantidade; i++)
                    {
                        _ultimaPintura = inicio + direcao.normalized * (i * passo);
                        PinturaDeVegetacao.Aplicar(
                            _terreno,
                            _conjunto,
                            _ultimaPintura,
                            evento.shift,
                            false,
                            _sorteio
                        );
                    }
                }
                _erro = null;
                evento.Use();
                cena.Repaint();
            }
            catch (Exception erro)
            {
                _erro = erro.Message;
                if (_grupoUndo >= 0)
                {
                    Undo.RevertAllDownToGroup(_grupoUndo);
                    _grupoUndo = -1;
                }
                FinalizarGesto();
                _ativo = false;
                ToolManager.RestorePreviousTool();
                Repaint();
            }
        }

        private void FinalizarGesto()
        {
            if (_grupoUndo >= 0)
                Undo.CollapseUndoOperations(_grupoUndo);
            if (_controle != 0 && GUIUtility.hotControl == _controle)
                GUIUtility.hotControl = 0;
            _grupoUndo = -1;
            _controle = 0;
            _terrenoDoGesto = null;
        }
    }

    [EditorTool("Pincel de vegetação")]
    public sealed class FerramentaDoPincelVegetacao : EditorTool
    {
        public override GUIContent toolbarIcon =>
            new GUIContent("Vegetação", "Pintar conjunto de árvores, plantas e grama");

        public override void OnActivated() => PincelDeVegetacao.Abrir();

        public override void OnToolGUI(EditorWindow janela)
        {
            if (janela is SceneView cena && PincelDeVegetacao.Instancia != null)
                PincelDeVegetacao.Instancia.DesenharNaCena(cena);
        }
    }
}
