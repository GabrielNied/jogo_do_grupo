using System;
using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Core.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    public sealed class JanelaCharacterPlayground : EditorWindow
    {
        [SerializeField]
        int _indice;

        [SerializeField]
        int _lado;

        [SerializeField]
        float _zoom = 1f;

        [SerializeField]
        bool _sockets = true;
        string _erro;
        readonly Dictionary<FontAsset, FontAsset> _fontes = new Dictionary<FontAsset, FontAsset>();
        readonly List<UnityEngine.Object> _recursosDasFontes = new List<UnityEngine.Object>();

        [MenuItem("Jogo do Grupo/Personagem/Character Playground/Abrir ferramenta")]
        public static void Abrir()
        {
            var janela = GetWindow<JanelaCharacterPlayground>();
            janela.titleContent = new GUIContent("Character Playground");
            janela.minSize = new Vector2(360, 480);
            janela.Show();
        }

        void OnEnable() => SceneView.duringSceneGui += DesenharReferencias;

        void OnDisable()
        {
            SceneView.duringSceneGui -= DesenharReferencias;
            foreach (var recurso in _recursosDasFontes)
                DestroyImmediate(recurso);
            _recursosDasFontes.Clear();
            _fontes.Clear();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            Montar(rootVisualElement);
        }

        // Mesma arvore usada pela janela e pelas capturas reais do UI Toolkit na validacao.
        public void Montar(VisualElement raiz)
        {
            EstiloVisual.Aplicar(raiz);
            var painel = ComponentesVisuais.PainelRolavel();
            raiz.Add(painel);
            painel.style.flexGrow = 1;
            painel.Add(ComponentesVisuais.Texto("Character Playground", titulo: true));
            painel.Add(ComponentesVisuais.Texto("Desenvolvimento • Locomoção v3"));
            painel.Add(
                ComponentesVisuais.Texto("Abrir Playground → Escolher personagem → Spawn → Play")
            );
            var mensagem = ComponentesVisuais.Texto("");
            painel.Add(mensagem);
            var abrir = ComponentesVisuais.Botao(
                "Abrir Playground",
                () => Acao(CharacterPlayground.Abrir)
            );
            painel.Add(abrir);
            painel.Add(ComponentesVisuais.Texto("Character", subtitulo: true));
            var lista = CharacterPlayground.Lista;
            var nomes =
                lista == null
                    ? new System.Collections.Generic.List<string>()
                    : lista
                        .Personagens.Select((p, i) => p == null ? "Entrada vazia " + i : p.Nome)
                        .ToList();
            _indice = Mathf.Clamp(_indice, 0, Math.Max(0, nomes.Count - 1));
            var selecao = new DropdownField("Personagem", nomes, nomes.Count > 0 ? _indice : -1);
            selecao.AddToClassList("campo");
            selecao.RegisterValueChangedCallback(_ =>
            {
                _indice = selecao.index;
                RuntimeCharacterPlayground.Indice = _indice;
            });
            painel.Add(selecao);
            var catalogo = ComponentesVisuais.Botao(
                "Editar catálogo",
                () => Selection.activeObject = CharacterPlayground.Lista
            );
            painel.Add(catalogo);
            painel.Add(ComponentesVisuais.Texto("Actions", subtitulo: true));
            var acoes = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var spawn = ComponentesVisuais.Botao(
                "Spawn",
                () => Acao(() => CharacterPlayground.Spawn(_indice)),
                principal: true
            );
            var reset = ComponentesVisuais.Botao(
                "Reset",
                () => Acao(() => CharacterPlayground.Reset(_indice))
            );
            spawn.style.flexGrow = 1;
            reset.style.flexGrow = 1;
            acoes.Add(spawn);
            acoes.Add(reset);
            painel.Add(acoes);
            painel.Add(ComponentesVisuais.Texto("Inspeção", subtitulo: true));
            var ferramentas = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap },
            };
            foreach (
                var nome in new[] { "Frente", "Lateral direita", "Costas", "Lateral esquerda" }
            )
            {
                var lado = ferramentas.childCount;
                var botao = ComponentesVisuais.Botao(
                    nome,
                    () =>
                        Acao(() =>
                        {
                            _lado = lado;
                            CharacterPlayground.Inspecionar(_lado, _zoom);
                        })
                );
                botao.style.flexBasis = new Length(48, LengthUnit.Percent);
                botao.style.flexGrow = 1;
                ferramentas.Add(botao);
            }
            var corpoInteiro = ComponentesVisuais.Botao(
                "Corpo inteiro",
                () =>
                    Acao(() =>
                    {
                        _zoom = 1f;
                        CharacterPlayground.Inspecionar(_lado, _zoom);
                    })
            );
            corpoInteiro.style.flexBasis = new Length(100, LengthUnit.Percent);
            ferramentas.Add(corpoInteiro);
            painel.Add(ferramentas);
            var zoom = new Slider("Aproximação", 0.5f, 2f) { value = _zoom, showInputField = true };
            zoom.style.flexBasis = new Length(100, LengthUnit.Percent);
            zoom.RegisterValueChangedCallback(e =>
            {
                _zoom = e.newValue;
                CharacterPlayground.Inspecionar(_lado, _zoom);
            });
            ferramentas.Add(zoom);
            var sockets = new Toggle("Sockets na Scene View ao selecionar") { value = _sockets };
            sockets.labelElement.style.whiteSpace = WhiteSpace.Normal;
            sockets.labelElement.style.flexShrink = 1;
            sockets.RegisterValueChangedCallback(e =>
            {
                _sockets = e.newValue;
                SceneView.RepaintAll();
            });
            painel.Add(sockets);
            painel.Add(ComponentesVisuais.Texto("Diagnóstico", subtitulo: true));
            var diagnostico = ComponentesVisuais.Texto("");
            painel.Add(diagnostico);
            painel.Add(ComponentesVisuais.Texto("Runtime", subtitulo: true));
            var runtime = ComponentesVisuais.Texto("");
            painel.Add(runtime);
            painel.Add(
                ComponentesVisuais.Texto(
                    "Grid: 1 m • Régua: 2 m • Barra: 1 m\nTarget Test Area: área dourada ao fundo.\nNavegação livre: Scene View do Unity.\nSalvar a cena mantém a instância e a câmera."
                )
            );
            void Atualizar()
            {
                var pode = CharacterPlayground.PodeOperar;
                var valido =
                    lista != null
                    && _indice >= 0
                    && _indice < lista.Personagens.Count
                    && lista.Personagens[_indice]?.Prefab != null;
                abrir.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                selecao.SetEnabled(pode && nomes.Count > 0);
                catalogo.SetEnabled(
                    !EditorApplication.isPlayingOrWillChangePlaymode && lista != null
                );
                spawn.SetEnabled(pode && valido);
                reset.SetEnabled(pode && valido);
                ferramentas.SetEnabled(
                    CharacterPlayground.PodeEditar && CharacterPlayground.Atual != null
                );
                mensagem.text =
                    _erro
                    ?? (
                        Application.isPlaying
                            ? "Contexto local: clique no Game View para controlar."
                        : !pode ? "Abra a cena pelo botão acima."
                        : !valido ? "Catálogo vazio ou prefab ausente. Edite o catálogo."
                        : "Cena de desenvolvimento • Spawn e Play para testar locomoção."
                    );
                diagnostico.text = Diagnosticar();
                runtime.text = RuntimeCharacterPlayground.Diagnosticar();
                zoom.SetValueWithoutNotify(_zoom);
            }
            void Acao(Action acao)
            {
                try
                {
                    _erro = null;
                    acao();
                }
                catch (Exception erro)
                {
                    _erro = erro.Message;
                }
            }
            Atualizar();
            IsolarFontes(raiz);
            painel.schedule.Execute(Atualizar).Every(1000);
        }

        void IsolarFontes(VisualElement raiz)
        {
            // Fontes SDF dinamicas acumulam glifos durante o render. Copias temporarias
            // evitam que salvar a cena grave esse cache nos assets compartilhados de Core.
            var leitura = raiz.style.unityFontDefinition.value.fontAsset;
            FontAsset Copiar(FontAsset original)
            {
                if (original == null)
                    return null;
                if (_fontes.TryGetValue(original, out var copia))
                    return copia;
                copia = Instantiate(original);
                copia.hideFlags = HideFlags.HideAndDontSave;
                _recursosDasFontes.Add(copia);
                if (original.material != null)
                {
                    copia.material = Instantiate(original.material);
                    copia.material.hideFlags = HideFlags.HideAndDontSave;
                    _recursosDasFontes.Add(copia.material);
                }
                for (var i = 0; i < copia.atlasTextures.Length; i++)
                {
                    if (copia.atlasTextures[i] == null)
                        continue;
                    copia.atlasTextures[i] = Instantiate(copia.atlasTextures[i]);
                    copia.atlasTextures[i].hideFlags = HideFlags.HideAndDontSave;
                    _recursosDasFontes.Add(copia.atlasTextures[i]);
                }
                if (copia.material != null && copia.atlasTextures.Length > 0)
                    copia.material.mainTexture = copia.atlasTextures[0];
                _fontes.Add(original, copia);
                return copia;
            }
            void Aplicar(VisualElement elemento)
            {
                var original = elemento.style.unityFontDefinition.value.fontAsset ?? leitura;
                if (original != null)
                    elemento.style.unityFontDefinition = FontDefinition.FromSDFFont(
                        Copiar(original)
                    );
            }
            Aplicar(raiz);
            raiz.Query<TextElement>().ForEach(Aplicar);
        }

        public static string Diagnosticar()
        {
            var objeto = CharacterPlayground.Atual;
            if (objeto == null)
                return "Personagem atual: nenhum.";
            var animator = objeto.GetComponentInChildren<Animator>();
            var bounds = CharacterPlayground.Medir(objeto);
            var bones = objeto
                .GetComponentsInChildren<SkinnedMeshRenderer>()
                .SelectMany(r => r.bones)
                .Where(t => t != null)
                .Distinct()
                .Count();
            return $"Atual: {objeto.name}\nAnimator: {(animator != null ? "sim" : "não")}\nAvatar válido: {(animator != null && animator.avatar != null && animator.avatar.isValid ? "sim" : "não")}\nHumanoid: {(animator != null && animator.isHuman ? "sim" : "não")}\nAltura da malha: {bounds.size.y:F6} m\nBones da skin: {bones}\nBase da malha: {bounds.min.y:F6} m\nSockets:\n"
                + string.Join(
                    "\n",
                    CharacterPlayground.Sockets.Select(nome =>
                        $"{nome}: {(CharacterPlayground.Socket(objeto, nome) != null ? "encontrado" : "ausente")}"
                    )
                );
        }

        void DesenharReferencias(SceneView vista)
        {
            if (!CharacterPlayground.PodeEditar)
                return;
            var anterior = Handles.color;
            try
            {
                Handles.color = Color.white;
                Handles.Label(new Vector3(-2, 2.1f, 0), "2 m");
                Handles.Label(new Vector3(2, 0.65f, 0), "1 m — referência de comprimento");
                Handles.Label(new Vector3(0, 0.05f, 4), "Target Test Area — reservado");
                var atual = CharacterPlayground.Atual;
                if (
                    !_sockets
                    || atual == null
                    || Selection.activeTransform == null
                    || !Selection.activeTransform.IsChildOf(atual.transform)
                )
                    return;
                Handles.color = Color.cyan;
                foreach (var nome in CharacterPlayground.Sockets)
                {
                    var socket = CharacterPlayground.Socket(atual, nome);
                    if (socket == null)
                        continue;
                    Handles.DrawWireDisc(socket.position, vista.camera.transform.forward, 0.035f);
                    Handles.Label(
                        socket.position
                            + Vector3.up
                                * (Array.IndexOf(CharacterPlayground.Sockets, nome) * 0.06f),
                        nome
                    );
                }
            }
            finally
            {
                Handles.color = anterior;
            }
        }
    }
}
