using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.LaboratorioVisual.Editor
{
    /// <summary>Somente dados fictícios e interações locais. Não acessa Core, rede ou features.</summary>
    public sealed class TelaLaboratorio
    {
        public static readonly string[] Secoes =
        {
            "Componentes",
            "Grupo e chat",
            "Inventário",
            "Combate",
            "Cidade",
        };
        public VisualElement Raiz { get; }
        public int SecaoAtual { get; private set; }
        public int TamanhoTexto { get; private set; } = 16;
        public bool Comparando { get; private set; }
        private readonly RecursosLaboratorio _recursos;
        private readonly VisualElement _conteudo;
        private readonly VisualElement _tela;
        private readonly VisualElement _modal;
        private readonly Image _referencia;
        private readonly List<Button> _abas = new List<Button>();
        private readonly Label _aviso;

        public TelaLaboratorio(RecursosLaboratorio recursos = null)
        {
            _recursos = recursos;
            Raiz = Elemento("lumora-lab");
            Raiz.name = "laboratorio-visual";
            if (recursos != null)
            {
                Raiz.styleSheets.Add(recursos.Estilo);
                Raiz.style.unityFontDefinition = FontDefinition.FromFont(recursos.Leitura);
                Raiz.style.backgroundImage = new StyleBackground(recursos.Fundo);
            }
            var topo = Elemento("lab-topo");
            Raiz.Add(topo);
            if (recursos != null)
                topo.Add(
                    new Image
                    {
                        image = recursos.Logo,
                        scaleMode = ScaleMode.ScaleToFit,
                        name = "marca",
                        style = { width = 210, height = 72 },
                    }
                );
            var apresentacao = Elemento("lab-apresentacao");
            apresentacao.Add(Texto("Ateliê de interfaces", "lab-titulo"));
            apresentacao.Add(Texto("Protótipo visual • dados de demonstração", "lab-suave"));
            topo.Add(apresentacao);
            var ferramentas = Elemento("lab-ferramentas");
            topo.Add(ferramentas);
            var tamanho = new SliderInt("Texto", 16, 24)
            {
                value = 16,
                showInputField = true,
                name = "tamanho-texto",
            };
            tamanho.RegisterValueChangedCallback(e => DefinirTamanhoTexto(e.newValue));
            ferramentas.Add(tamanho);
            var comparar = new Toggle("Comparar com arte") { name = "comparar-arte" };
            comparar.RegisterValueChangedCallback(e => Comparar(e.newValue));
            ferramentas.Add(comparar);
            var corpo = Elemento("lab-corpo");
            Raiz.Add(corpo);
            var menu = new ScrollView { name = "menu-exemplos" };
            menu.AddToClassList("lab-menu");
            corpo.Add(menu);
            menu.Add(Texto("EXPERIMENTAR", "lab-legenda"));
            for (var i = 0; i < Secoes.Length; i++)
            {
                var indice = i;
                var botao = Botao(Secoes[i], () => Navegar(indice), "lab-aba");
                botao.name = "abrir-" + i;
                _abas.Add(botao);
                menu.Add(botao);
            }
            menu.Add(
                Texto("Tudo aqui é local.\nOs exemplos não definem regras do jogo.", "lab-nota")
            );
            var area = Elemento("lab-area");
            corpo.Add(area);
            _tela = Elemento("lab-tela");
            area.Add(_tela);
            var scroll = new ScrollView { name = "rolagem-exemplo" };
            scroll.AddToClassList("lab-rolagem");
            _tela.Add(scroll);
            _conteudo = Elemento("lab-conteudo");
            scroll.Add(_conteudo);
            _referencia = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                name = "prancha-referencia",
            };
            _referencia.AddToClassList("lab-referencia");
            area.Add(_referencia);
            _modal = Elemento("lab-veu");
            _modal.name = "modal";
            _modal.style.display = DisplayStyle.None;
            Raiz.Add(_modal);
            _aviso = Texto(
                "Escolha um exemplo. Use Tab para testar o foco e Enter para acionar botões.",
                "lab-status"
            );
            _aviso.name = "status";
            Raiz.Add(_aviso);
            Raiz.RegisterCallback<KeyDownEvent>(
                e =>
                {
                    if (_modal.style.display.value != DisplayStyle.Flex)
                        return;
                    if (e.keyCode == KeyCode.Escape)
                    {
                        FecharModal();
                        e.StopPropagation();
                    }
                    else if (e.keyCode == KeyCode.Tab)
                    {
                        _modal.Q<Button>("fechar-modal")?.Focus();
                        e.PreventDefault();
                        e.StopPropagation();
                    }
                },
                TrickleDown.TrickleDown
            );
            Navegar(0);
            Comparar(false);
        }

        public void Navegar(int indice)
        {
            if (indice < 0 || indice >= Secoes.Length)
                throw new ArgumentOutOfRangeException(nameof(indice));
            SecaoAtual = indice;
            FecharModal();
            _conteudo.Clear();
            for (var i = 0; i < _abas.Count; i++)
                _abas[i].EnableInClassList("lab-selecionado", i == indice);
            _conteudo.Add(Texto(Secoes[indice], "lab-titulo"));
            _conteudo.Add(
                Texto(
                    "Explore os controles e compare acabamento, leitura e proporções.",
                    "lab-suave"
                )
            );
            switch (indice)
            {
                case 0:
                    Componentes();
                    break;
                case 1:
                    Grupo();
                    break;
                case 2:
                    Inventario();
                    break;
                case 3:
                    Combate();
                    break;
                case 4:
                    Cidade();
                    break;
            }
            if (_recursos != null)
                _referencia.image = _recursos.Pranchas[indice];
            DefinirTamanhoTexto(TamanhoTexto);
        }

        public void DefinirTamanhoTexto(int tamanho)
        {
            TamanhoTexto = Mathf.Clamp(tamanho, 16, 24);
            Raiz.style.fontSize = TamanhoTexto;
            Raiz.Query<TextElement>()
                .ForEach(e =>
                {
                    e.style.fontSize =
                        e.ClassListContains("lab-titulo") ? 28
                        : e.ClassListContains("lab-subtitulo") ? 22
                        : TamanhoTexto;
                    if (
                        _recursos != null
                        && !e.ClassListContains("lab-titulo")
                        && !e.ClassListContains("lab-subtitulo")
                        && !(e is Button)
                    )
                        e.style.unityFontDefinition = FontDefinition.FromFont(_recursos.Leitura);
                });
            Raiz.Query<Button>().ForEach(b => b.style.fontSize = Mathf.Max(18, TamanhoTexto));
        }

        public void Comparar(bool mostrar)
        {
            Comparando = mostrar;
            _referencia.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
            _tela.EnableInClassList("lab-metade", mostrar);
        }

        private void Componentes()
        {
            var painel = Painel("Uma aventura começa em boa companhia", true);
            painel.Add(Texto("Acentuação: ações, essência, coração. 0123456789", "lab-leitura"));
            var linha = Elemento("lab-linha");
            linha.Add(
                Botao(
                    "Ação principal",
                    () => Avisar("Ação demonstrada com sucesso."),
                    "lab-primario"
                )
            );
            linha.Add(
                Botao(
                    "Abrir convite",
                    () =>
                        AbrirModal(
                            "Vamos explorar?",
                            "Convite fictício para testar o painel de confirmação."
                        )
                )
            );
            var indisponivel = Botao("Indisponível", () => Avisar("Não deve executar."));
            indisponivel.SetEnabled(false);
            indisponivel.name = "botao-desabilitado";
            linha.Add(indisponivel);
            painel.Add(linha);
            var campo = new TextField("Nome da expedição") { name = "nome-expedicao" };
            painel.Add(campo);
            painel.Add(
                Botao(
                    "Validar campo",
                    () =>
                    {
                        var vazio = string.IsNullOrWhiteSpace(campo.value);
                        campo.EnableInClassList("lab-erro", vazio);
                        Avisar(
                            vazio ? "Preencha o nome da expedição." : "Nome aceito: " + campo.value
                        );
                    }
                )
            );
            painel.Add(new Toggle("Receber convites de amigos") { value = true });
            painel.Add(new Slider("Volume de efeitos", 0, 100) { value = 70 });
            painel.Add(Barra("Essência • 72 / 100", 72));
            painel.Add(Texto("Informação • Sucesso • Atenção • Erro", "lab-leitura"));
            var cores = Elemento("lab-linha");
            foreach (var hex in new[] { "#173B50", "#39B9AE", "#F5BE63", "#F4E6CB", "#688A48" })
            {
                var amostra = Elemento("lab-amostra");
                ColorUtility.TryParseHtmlString(hex, out var cor);
                amostra.style.backgroundColor = cor;
                cores.Add(amostra);
                amostra.tooltip = hex;
            }
            painel.Add(cores);
        }

        private void Grupo()
        {
            var painel = Painel("Expedição entre amigos", true);
            painel.Add(Texto("Reúna sua companhia e prepare a próxima aventura.", "lab-leitura"));
            var membros = Elemento("lab-cartoes");
            foreach (var nome in new[] { "Kael", "Seraphina", "Brin", "Lyria" })
            {
                var membro = Elemento("lab-cartao");
                membro.Add(Texto(nome.Substring(0, 1), "lab-retrato"));
                membro.Add(Texto(nome, "lab-subtitulo"));
                membro.Add(Texto("Pronto para partir", "lab-leitura"));
                membro.Add(Barra("Vida", 85));
                membros.Add(membro);
            }
            painel.Add(membros);
            painel.Add(
                Botao(
                    "Convidar jogador",
                    () =>
                        AbrirModal(
                            "Convite de expedição",
                            "Este convite existe apenas no laboratório."
                        ),
                    "lab-primario"
                )
            );
            var chat = Painel("Conversa do grupo", false);
            var mensagens = new ScrollView { name = "mensagens-chat", style = { height = 160 } };
            mensagens.Add(Texto("Seraphina: Vamos seguir pela ponte!", "lab-leitura"));
            mensagens.Add(Texto("Brin: Já levei os suprimentos.", "lab-leitura"));
            chat.Add(mensagens);
            var campo = new TextField("Mensagem") { name = "texto-chat", maxLength = 240 };
            chat.Add(campo);
            chat.Add(
                Botao(
                    "Enviar",
                    () =>
                    {
                        if (string.IsNullOrWhiteSpace(campo.value))
                        {
                            Avisar("Digite uma mensagem antes de enviar.");
                            return;
                        }
                        if (mensagens.childCount >= 30)
                            mensagens.RemoveAt(0);
                        var mensagem = Texto("Você: " + campo.value.Trim(), "lab-leitura");
                        mensagens.Add(mensagem);
                        mensagens.ScrollTo(mensagem);
                        campo.value = "";
                        DefinirTamanhoTexto(TamanhoTexto);
                    }
                )
            );
        }

        private void Inventario()
        {
            var painel = Painel("Mochila de exploração", false);
            var busca = new TextField("Buscar item") { name = "buscar-item" };
            painel.Add(busca);
            var grade = Elemento("lab-grade");
            grade.name = "grade-itens";
            painel.Add(grade);
            foreach (
                var nome in new[]
                {
                    "Espada de viagem",
                    "Essência azul",
                    "Poção de vigor",
                    "Botas de couro",
                    "Minério bruto",
                    "Madeira antiga",
                    "Cristal turquesa",
                    "Mapa do vale",
                }
            )
            {
                var item = Botao(
                    nome,
                    () =>
                        AbrirModal(
                            nome,
                            "Item demonstrativo. Atributos e valores não representam balanceamento."
                        ),
                    "lab-item"
                );
                item.name = nome;
                item.tooltip = "Inspecionar " + nome;
                grade.Add(item);
            }
            var vazio = Texto("Nenhum item encontrado.", "lab-leitura");
            vazio.name = "busca-vazia";
            vazio.style.display = DisplayStyle.None;
            painel.Add(vazio);
            busca.RegisterValueChangedCallback(e =>
            {
                var encontrados = 0;
                foreach (var item in grade.Children())
                {
                    var visivel =
                        item.name.IndexOf(e.newValue ?? "", StringComparison.OrdinalIgnoreCase)
                        >= 0;
                    item.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
                    if (visivel)
                        encontrados++;
                }
                vazio.style.display = encontrados == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            });
            painel.Add(
                Texto(
                    "Clique num item para testar a inspeção. Nomes longos devem quebrar sem cortar.",
                    "lab-leitura"
                )
            );
        }

        private void Combate()
        {
            var painel = Painel("Em meio à aventura", false);
            painel.Add(
                Texto("Decoração reduzida para manter a leitura durante o combate.", "lab-leitura")
            );
            var vidaPainel = Barra("Vida • demonstração", 85);
            var vida = vidaPainel.Q<ProgressBar>();
            painel.Add(vidaPainel);
            painel.Add(Barra("Essência • demonstração", 64));
            painel.Add(
                Botao(
                    "Simular dano",
                    () =>
                    {
                        vida.value = Mathf.Max(0, vida.value - 15);
                        Avisar("Dano visual simulado. Nenhum personagem foi alterado.");
                    }
                )
            );
            painel.Add(Botao("Restaurar vida", () => vida.value = 85));
            var atalhos = Elemento("lab-cartoes");
            for (var i = 1; i <= 6; i++)
            {
                var numero = i;
                var habilidade = Botao(
                    numero + "\nEssência",
                    () => Avisar("Prévia da habilidade " + numero),
                    "lab-atalho"
                );
                habilidade.tooltip = "Habilidade fictícia " + numero;
                atalhos.Add(habilidade);
            }
            painel.Add(atalhos);
            painel.Add(
                Botao(
                    "Mostrar aviso de aliado",
                    () =>
                        AbrirModal(
                            "Um aliado precisa de ajuda",
                            "Teste de leitura e hierarquia do aviso sobre o HUD."
                        )
                )
            );
        }

        private void Cidade()
        {
            var painel = Painel("O lugar que construímos juntos", true);
            painel.Add(
                Texto(
                    "Proposta visual de cidade e contribuições. Sistemas futuros, sem implementação de gameplay.",
                    "lab-leitura"
                )
            );
            painel.Add(Barra("Contribuição da comunidade", 62));
            var mural = Elemento("lab-pergaminho");
            if (_recursos != null)
                mural.style.backgroundImage = new StyleBackground(_recursos.Pergaminho);
            mural.Add(Texto("Mural dos fundadores", "lab-subtitulo"));
            mural.Add(
                Texto(
                    "Uma ponte, um jardim, um lugar para encontrar os amigos. Cada contribuição pode deixar uma história.",
                    "lab-leitura"
                )
            );
            painel.Add(mural);
            painel.Add(
                Botao(
                    "Registrar contribuição",
                    () =>
                        AbrirModal(
                            "Uma marca no mundo",
                            "Contribuição de demonstração. Nada será salvo no servidor."
                        ),
                    "lab-primario"
                )
            );
        }

        private VisualElement Painel(string titulo, bool ornamentado)
        {
            var painel = Elemento("lab-painel");
            _conteudo.Add(painel);
            painel.Add(Texto(titulo, "lab-subtitulo"));
            if (ornamentado && _recursos != null)
            {
                painel.AddToClassList("lab-ornamentado");
                var moldura = Elemento("lab-moldura");
                moldura.pickingMode = PickingMode.Ignore;
                moldura.style.backgroundImage = new StyleBackground(_recursos.Moldura);
                painel.Add(moldura);
            }
            return painel;
        }

        private VisualElement Barra(string titulo, float valor)
        {
            var grupo = Elemento("lab-recurso");
            grupo.Add(Texto(titulo, "lab-leitura"));
            var barra = new ProgressBar { title = "", value = valor };
            barra.AddToClassList("lab-barra");
            grupo.Add(barra);
            return grupo;
        }

        public void AbrirModal(string titulo, string mensagem)
        {
            _modal.Clear();
            var painel = Elemento("lab-dialogo");
            painel.Add(Texto(titulo, "lab-subtitulo"));
            painel.Add(Texto(mensagem, "lab-leitura"));
            var fechar = Botao("Fechar", FecharModal, "lab-primario");
            fechar.name = "fechar-modal";
            painel.Add(fechar);
            _modal.Add(painel);
            _modal.style.display = DisplayStyle.Flex;
            DefinirTamanhoTexto(TamanhoTexto);
            fechar.Focus();
        }

        public void FecharModal()
        {
            _modal.style.display = DisplayStyle.None;
            _modal.Clear();
        }

        private void Avisar(string mensagem)
        {
            _aviso.text = mensagem;
        }

        private static VisualElement Elemento(string classe)
        {
            var e = new VisualElement();
            e.AddToClassList(classe);
            return e;
        }

        private Label Texto(string texto, string classe)
        {
            var label = new Label(texto) { enableRichText = false };
            label.AddToClassList(classe);
            if (_recursos != null && (classe == "lab-titulo" || classe == "lab-subtitulo"))
                label.style.unityFontDefinition = FontDefinition.FromFont(_recursos.Titulo);
            return label;
        }

        private Button Botao(string texto, Action acao, string classe = "lab-secundario")
        {
            var botao = new Button(acao) { text = texto };
            botao.AddToClassList(classe);
            if (_recursos != null)
                botao.style.unityFontDefinition = FontDefinition.FromFont(_recursos.Controle);
            return botao;
        }
    }
}
