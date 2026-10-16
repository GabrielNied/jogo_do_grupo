using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Talentos;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Talentos
{
    /// <summary>
    /// Árvore de atributos (tecla N): os cinco caminhos e os nós híbridos em visão radial,
    /// o painel do nó escolhido (custo, rank, efeito, requisitos e o motivo do bloqueio), a
    /// prévia antes e depois e a compra. O servidor decide a compra; a tela só pede e mostra.
    /// Interface montada em C# com a biblioteca compartilhada Lumora UI.
    /// </summary>
    internal sealed class JanelaArvore : JanelaDoJogo
    {
        public const string Chave = "talentos.arvore";
        public const string Tecla = "N";
        public const int Ordem = 60;

        private static readonly EstadoDoNo[] EstadosDaLegenda =
        {
            EstadoDoNo.Bloqueado,
            EstadoDoNo.Disponivel,
            EstadoDoNo.Comprado,
            EstadoDoNo.NoMaximo,
            EstadoDoNo.CondicionalmenteAtivo,
        };

        private readonly EstadoDaArvore _estado = new EstadoDaArvore();
        private readonly Dictionary<string, VisualElement> _nos =
            new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Label> _marcas = new Dictionary<string, Label>();

        private Label _mensagem;
        private VisualElement _corpo;
        private Label _pontos;
        private Label _atributos;
        private VisualElement _arvore;
        private VisualElement _arestas;
        private VisualElement _detalhes;
        private Button _comprar;
        private Button _redistribuir;
        private Label _textoConfirmarRedistribuir;
        private VisualElement _confirmacaoRedistribuir;
        private VisualElement _painelPerfis;
        private VisualElement _containerSalvarPerfil;
        private TextField _inputNomePerfil;
        private VisualElement _listaPerfis;
        private List<PerfilTalentosDto> _perfis = new List<PerfilTalentosDto>();
        private Label _retorno;
        private bool _montouNos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                TextosTalentos.Titulo,
                Tecla,
                Ordem,
                () => new JanelaArvore()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            _mensagem = ComponentesVisuais.Texto("");
            _mensagem.style.display = DisplayStyle.None;
            conteudo.Add(_mensagem);

            _corpo = new VisualElement();
            _corpo.style.display = DisplayStyle.None;
            conteudo.Add(_corpo);

            _pontos = ComponentesVisuais.Texto("", subtitulo: true);
            _corpo.Add(_pontos);
            _atributos = ComponentesVisuais.Texto("");
            _atributos.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);
            _corpo.Add(_atributos);

            _arvore = new VisualElement { name = "talentos.arvore" };
            _arvore.style.width = Length.Percent(100);
            _arvore.style.maxWidth = ConfigVisual.Dimensoes.Arvore;
            _arvore.style.alignSelf = Align.Center;
            _arvore.style.marginTop = ConfigVisual.Espacamento.Pequeno;
            _arvore.style.marginBottom = ConfigVisual.Espacamento.Pequeno;
            _arvore.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Trilho);
            SetBorda(_arvore, ConfigVisual.Bordas.Fina, ConfigVisual.Cores.BordaDiscreta);
            SetRaio(_arvore, ConfigVisual.Bordas.RaioPainel);
            // Mantém a árvore quadrada ao estreitar a janela.
            _arvore.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (e.newRect.width > 0 && !Mathf.Approximately(e.newRect.height, e.newRect.width))
                {
                    _arvore.style.height = e.newRect.width;
                }

                _arestas?.MarkDirtyRepaint();
            });
            _arestas = Preencher();
            _arestas.generateVisualContent += DesenharArestas;
            _arvore.Add(_arestas);
            _corpo.Add(_arvore);

            _corpo.Add(MontarLegenda());
            _corpo.Add(MontarPainel());
            _corpo.Add(MontarPerfis());
        }

        protected override void AoAbrir()
        {
            _estado.Mudou += Atualizar;
            Carregar();
        }

        protected override void AoFechar()
        {
            _estado.Mudou -= Atualizar;
        }

        private VisualElement Preencher()
        {
            var elemento = new VisualElement { pickingMode = PickingMode.Ignore };
            elemento.style.position = Position.Absolute;
            elemento.style.left = 0;
            elemento.style.right = 0;
            elemento.style.top = 0;
            elemento.style.bottom = 0;
            return elemento;
        }

        private VisualElement MontarLegenda()
        {
            var legenda = new VisualElement();
            legenda.style.flexDirection = FlexDirection.Row;
            legenda.style.flexWrap = Wrap.Wrap;
            legenda.style.justifyContent = Justify.Center;
            foreach (var estado in EstadosDaLegenda)
            {
                var item = ComponentesVisuais.Texto(TextosTalentos.LegendaDoEstado(estado));
                item.style.marginLeft = ConfigVisual.Espacamento.Pequeno;
                item.style.marginRight = ConfigVisual.Espacamento.Pequeno;
                item.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);
                legenda.Add(item);
            }

            return legenda;
        }

        private VisualElement MontarPainel()
        {
            var painel = ComponentesVisuais.Painel();
            painel.style.marginTop = ConfigVisual.Espacamento.Pequeno;
            painel.style.width = Length.Percent(100);

            _detalhes = new VisualElement();
            painel.Add(_detalhes);

            var linhaBotoes = new VisualElement();
            linhaBotoes.style.flexDirection = FlexDirection.Row;
            linhaBotoes.style.flexWrap = Wrap.Wrap;
            linhaBotoes.style.marginTop = ConfigVisual.Espacamento.Medio;

            _comprar = ComponentesVisuais.Botao(TextosTalentos.Compra, Comprar, principal: true);
            linhaBotoes.Add(_comprar);

            _redistribuir = ComponentesVisuais.Botao(
                TextosTalentos.Redistribuir,
                IniciarRedistribuicao
            );
            _redistribuir.style.marginLeft = ConfigVisual.Espacamento.Medio;
            linhaBotoes.Add(_redistribuir);

            painel.Add(linhaBotoes);

            _confirmacaoRedistribuir = new VisualElement();
            _confirmacaoRedistribuir.style.display = DisplayStyle.None;
            _confirmacaoRedistribuir.style.marginTop = ConfigVisual.Espacamento.Pequeno;
            _confirmacaoRedistribuir.style.paddingTop = ConfigVisual.Espacamento.Pequeno;
            SetBorda(
                _confirmacaoRedistribuir,
                ConfigVisual.Bordas.Fina,
                ConfigVisual.Cores.BordaDiscreta
            );

            _textoConfirmarRedistribuir = ComponentesVisuais.Texto(string.Empty);
            _confirmacaoRedistribuir.Add(_textoConfirmarRedistribuir);

            var linhaBotoesConfirma = new VisualElement();
            linhaBotoesConfirma.style.flexDirection = FlexDirection.Row;
            linhaBotoesConfirma.style.marginTop = ConfigVisual.Espacamento.Pequeno;

            var botaoSim = ComponentesVisuais.Botao(
                "Sim, redistribuir",
                ConfirmarRedistribuicao,
                principal: true
            );
            linhaBotoesConfirma.Add(botaoSim);

            var botaoCancelar = ComponentesVisuais.Botao(
                "Cancelar",
                () => _confirmacaoRedistribuir.style.display = DisplayStyle.None
            );
            botaoCancelar.style.marginLeft = ConfigVisual.Espacamento.Pequeno;
            linhaBotoesConfirma.Add(botaoCancelar);

            _confirmacaoRedistribuir.Add(linhaBotoesConfirma);
            painel.Add(_confirmacaoRedistribuir);

            _retorno = ComponentesVisuais.Texto("");
            _retorno.style.marginTop = ConfigVisual.Espacamento.Pequeno;
            painel.Add(_retorno);
            return painel;
        }

        private VisualElement MontarPerfis()
        {
            _painelPerfis = ComponentesVisuais.Painel();
            _painelPerfis.style.marginTop = ConfigVisual.Espacamento.Pequeno;
            _painelPerfis.style.width = Length.Percent(100);

            var topo = new VisualElement();
            topo.style.flexDirection = FlexDirection.Row;
            topo.style.justifyContent = Justify.SpaceBetween;
            topo.style.alignItems = Align.Center;

            var titulo = ComponentesVisuais.Texto(TextosTalentos.TituloPerfis, subtitulo: true);
            topo.Add(titulo);

            var botaoNovo = ComponentesVisuais.Botao(
                "+ " + TextosTalentos.SalvarPerfil,
                () =>
                {
                    _containerSalvarPerfil.style.display =
                        _containerSalvarPerfil.style.display == DisplayStyle.Flex
                            ? DisplayStyle.None
                            : DisplayStyle.Flex;
                }
            );
            topo.Add(botaoNovo);
            _painelPerfis.Add(topo);

            _containerSalvarPerfil = new VisualElement();
            _containerSalvarPerfil.style.display = DisplayStyle.None;
            _containerSalvarPerfil.style.flexDirection = FlexDirection.Row;
            _containerSalvarPerfil.style.marginTop = ConfigVisual.Espacamento.Pequeno;

            _inputNomePerfil = new TextField
            {
                maxLength = ConfigRedistribuicao.TamanhoMaximoNomePerfil,
            };
            _inputNomePerfil.style.flexGrow = 1;
            _containerSalvarPerfil.Add(_inputNomePerfil);

            var botaoSalvar = ComponentesVisuais.Botao("Salvar", SalvarPerfil, principal: true);
            botaoSalvar.style.marginLeft = ConfigVisual.Espacamento.Pequeno;
            _containerSalvarPerfil.Add(botaoSalvar);

            _painelPerfis.Add(_containerSalvarPerfil);

            _listaPerfis = new VisualElement();
            _listaPerfis.style.marginTop = ConfigVisual.Espacamento.Pequeno;
            _painelPerfis.Add(_listaPerfis);

            return _painelPerfis;
        }

        // Pede a árvore ao servidor. Reaproveitado depois de uma falha de rede, porque a compra
        // pode ter passado mesmo sem resposta.
        private async void Carregar()
        {
            if (!_estado.Carregada)
            {
                MostrarMensagem(TextosTalentos.Carregando, false);
            }

            try
            {
                var resposta = await ClienteApi.Get<ArvoreResponse>(
                    RotasTalentos.ArvoreDe(EntradaNoJogo.PersonagemId)
                );

                // A janela pode ter sido fechada durante a espera (ou a sessão expirou).
                if (!Aberta)
                {
                    return;
                }

                if (!resposta.Sucesso || resposta.Corpo == null)
                {
                    if (!TextosTalentos.FoiCancelada(resposta.CodigoErro))
                    {
                        MostrarMensagem(
                            _estado.Carregada
                                ? TextosTalentos.SemConexao
                                : TextosTalentos.ErroAoCarregar,
                            true
                        );
                    }

                    return;
                }

                _mensagem.style.display = DisplayStyle.None;
                _estado.Aplicar(resposta.Corpo);
                CarregarPerfis();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarMensagem(TextosTalentos.ErroAoCarregar, true);
                }
            }
        }

        private async void Comprar()
        {
            var no = _estado.NoSelecionado;
            if (no == null || _estado.Comprando)
            {
                return;
            }

            var avaliacao = _estado.Avaliar(no);
            if (avaliacao.Erro != null)
            {
                MostrarRetorno(TextosTalentos.Motivo(_estado, no, avaliacao), true);
                return;
            }

            MostrarRetorno("", false);
            var pedido = _estado.MontarPedido(no);
            _estado.DefinirComprando(true);
            try
            {
                var resposta = await ClienteApi.Post<ArvoreResponse>(
                    RotasTalentos.ComprarDe(EntradaNoJogo.PersonagemId),
                    pedido
                );
                if (!Aberta)
                {
                    return;
                }

                _estado.DefinirComprando(false);
                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estado.Aplicar(resposta.Corpo);
                    return;
                }

                if (TextosTalentos.FoiCancelada(resposta.CodigoErro))
                {
                    return;
                }

                MostrarRetorno(TextosTalentos.MensagemDaCompra(resposta.CodigoErro), true);
                if (resposta.Status == 0 || resposta.Status >= 500)
                {
                    // Sem resposta: o servidor pode ter comprado. Confere o estado real.
                    Carregar();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    _estado.DefinirComprando(false);
                    MostrarRetorno(TextosTalentos.Generica, true);
                }
            }
        }

        private void MostrarMensagem(string texto, bool erro)
        {
            _corpo.style.display = _estado.Carregada ? DisplayStyle.Flex : DisplayStyle.None;
            _mensagem.text = texto;
            _mensagem.style.display = DisplayStyle.Flex;
            _mensagem.style.color = EstiloVisual.Cor(
                erro ? ConfigVisual.Cores.Erro : ConfigVisual.Cores.TextoSecundario
            );
        }

        private void MostrarRetorno(string texto, bool erro)
        {
            _retorno.text = texto;
            _retorno.style.display = texto.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _retorno.style.color = EstiloVisual.Cor(
                erro ? ConfigVisual.Cores.Erro : ConfigVisual.Cores.Ganho
            );
        }

        private void Atualizar()
        {
            if (!Aberta || !_estado.Carregada)
            {
                return;
            }

            _corpo.style.display = DisplayStyle.Flex;
            if (!_montouNos)
            {
                MontarNos();
                _montouNos = true;
            }

            _pontos.text = "Pontos livres: " + _estado.PontosLivres;
            _redistribuir.text = TextosTalentos.RedistribuirComCusto(
                _estado.CustoRedistribuicaoCoroas
            );
            _textoConfirmarRedistribuir.text = TextosTalentos.ConfirmarRedistribuicao(
                _estado.CustoRedistribuicaoCoroas
            );
            _atributos.text = TextoDosAtributos(_estado.Atributos);
            foreach (var no in _estado.Configuracao.Nos)
            {
                AtualizarNo(no);
            }

            // O custo no botão de cada perfil acompanha o nível do personagem.
            if (_listaPerfis != null && _perfis.Count > 0)
            {
                AtualizarListaPerfis();
            }

            _arestas.MarkDirtyRepaint();
            AtualizarPainel();
        }

        private static string TextoDosAtributos(AtributosDaArvore a) =>
            TextosTalentos.FonteDosAtributos
            + ": Força "
            + a.Forca
            + " · Vigor "
            + a.Vigor
            + " · Agilidade "
            + a.Agilidade
            + " · Intelecto "
            + a.Intelecto
            + " · Espírito "
            + a.Espirito;

        private static int ValorDe(AtributosDaArvore a, AtributoPrincipal atributo)
        {
            switch (atributo)
            {
                case AtributoPrincipal.Forca:
                    return a.Forca;
                case AtributoPrincipal.Vigor:
                    return a.Vigor;
                case AtributoPrincipal.Agilidade:
                    return a.Agilidade;
                case AtributoPrincipal.Intelecto:
                    return a.Intelecto;
                default:
                    return a.Espirito;
            }
        }

        // --- Nós e arestas -------------------------------------------------------------

        private void MontarNos()
        {
            var extensao = _estado.Extensao();
            foreach (var no in _estado.Configuracao.Nos)
            {
                var elemento = CriarNo(no, extensao);
                _nos[no.Id] = elemento;
                _arvore.Add(elemento);
            }

            foreach (AtributoPrincipal caminho in Enum.GetValues(typeof(AtributoPrincipal)))
            {
                var rotulo = RotuloDoCaminho(caminho, extensao);
                if (rotulo != null)
                {
                    _arvore.Add(rotulo);
                }
            }
        }

        private VisualElement CriarNo(NoDaArvore no, float extensao)
        {
            var grande = no.Tipo == TipoDeNo.Grande || no.Tipo == TipoDeNo.Avancado;
            var lado = grande ? ConfigVisual.Dimensoes.NoGrande : ConfigVisual.Dimensoes.AlvoMinimo;
            var elemento = new VisualElement { name = "talentos.no." + no.Id, focusable = true };
            elemento.tabIndex = 0;
            elemento.style.position = Position.Absolute;
            elemento.style.width = lado;
            elemento.style.height = lado;
            elemento.style.left = Length.Percent(50f + no.X / extensao * 50f);
            elemento.style.top = Length.Percent(50f + no.Y / extensao * 50f);
            elemento.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            elemento.style.alignItems = Align.Center;
            elemento.style.justifyContent = Justify.Center;
            // Nó pequeno é redondo; híbrido, grande e avançado são quadrados de canto suave, para
            // o tipo aparecer na forma e não só na cor.
            if (no.Tipo == TipoDeNo.Pequeno)
            {
                SetRaio(elemento, Length.Percent(50));
            }
            else
            {
                SetRaio(elemento, ConfigVisual.Bordas.RaioCartao);
            }

            var marca = ComponentesVisuais.Texto("");
            marca.pickingMode = PickingMode.Ignore;
            marca.style.unityTextAlign = TextAnchor.MiddleCenter;
            marca.style.whiteSpace = WhiteSpace.NoWrap;
            elemento.Add(marca);
            _marcas[no.Id] = marca;

            var id = no.Id;
            elemento.RegisterCallback<ClickEvent>(e => _estado.Selecionar(id));
            elemento.RegisterCallback<FocusInEvent>(e =>
            {
                if (_estado.NoSelecionadoId != id)
                {
                    _estado.Selecionar(id);
                }
            });
            elemento.RegisterCallback<KeyDownEvent>(e =>
            {
                if (
                    e.keyCode == KeyCode.Return
                    || e.keyCode == KeyCode.KeypadEnter
                    || e.keyCode == KeyCode.Space
                )
                {
                    _estado.Selecionar(id);
                    Comprar();
                    e.StopPropagation();
                }
            });
            return elemento;
        }

        // Nome do caminho, logo depois do nó mais distante dele.
        private Label RotuloDoCaminho(AtributoPrincipal caminho, float extensao)
        {
            NoDaArvore ponta = null;
            var maior = 0f;
            foreach (var no in _estado.Configuracao.Nos)
            {
                if (
                    no.Tipo != TipoDeNo.Pequeno
                    || no.Caminhos.Length == 0
                    || no.Caminhos[0] != caminho
                )
                {
                    continue;
                }

                var distancia = new Vector2(no.X, no.Y).magnitude;
                if (distancia > maior)
                {
                    maior = distancia;
                    ponta = no;
                }
            }

            if (ponta == null)
            {
                return null;
            }

            var direcao = new Vector2(ponta.X, ponta.Y).normalized;
            var posicao = direcao * (maior + RecuoDoRotuloDoCaminho);
            var rotulo = ComponentesVisuais.Texto(TextosTalentos.Rotulo(caminho));
            rotulo.pickingMode = PickingMode.Ignore;
            rotulo.style.position = Position.Absolute;
            rotulo.style.left = Length.Percent(50f + posicao.x / extensao * 50f);
            rotulo.style.top = Length.Percent(50f + posicao.y / extensao * 50f);
            rotulo.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            rotulo.style.whiteSpace = WhiteSpace.NoWrap;
            rotulo.style.color = EstiloVisual.Cor(ConfigVisual.Cores.Marfim);
            return rotulo;
        }

        /// <summary>Distância (unidades da árvore) entre o último nó do caminho e o nome dele.</summary>
        private const float RecuoDoRotuloDoCaminho = 0.65f;

        private void AtualizarNo(NoDaArvore no)
        {
            if (!_nos.TryGetValue(no.Id, out var elemento))
            {
                return;
            }

            var estado = _estado.EstadoDe(no);
            var rank = _estado.RankDe(no);
            var selecionado = _estado.NoSelecionadoId == no.Id;

            string fundo;
            string borda;
            string texto;
            var larguraDaBorda = ConfigVisual.Bordas.Destaque;
            switch (estado)
            {
                case EstadoDoNo.Bloqueado:
                    fundo = ConfigVisual.Cores.Desabilitado;
                    borda = ConfigVisual.Cores.BordaDesabilitado;
                    texto = ConfigVisual.Cores.TextoDesabilitado;
                    larguraDaBorda = ConfigVisual.Bordas.Fina;
                    break;
                case EstadoDoNo.Disponivel:
                    fundo = ConfigVisual.Cores.Petroleo;
                    borda = ConfigVisual.Cores.Ouro;
                    texto = ConfigVisual.Cores.Marfim;
                    break;
                case EstadoDoNo.NoMaximo:
                    fundo = ConfigVisual.Cores.Ouro;
                    borda = ConfigVisual.Cores.BordaPrimario;
                    texto = ConfigVisual.Cores.Petroleo;
                    break;
                default:
                    fundo = ConfigVisual.Cores.Selecionado;
                    borda = ConfigVisual.Cores.Essencia;
                    texto = ConfigVisual.Cores.Marfim;
                    break;
            }

            if (selecionado)
            {
                borda = ConfigVisual.Cores.Marfim;
                larguraDaBorda = ConfigVisual.Bordas.Selecao;
            }

            elemento.style.backgroundColor = EstiloVisual.Cor(fundo);
            SetBorda(elemento, larguraDaBorda, borda);
            elemento.tooltip =
                no.Nome
                + " · "
                + TextosTalentos.NomeDoEstado(estado)
                + " · "
                + rank
                + "/"
                + no.RankMaximo;

            var marca = _marcas[no.Id];
            marca.text =
                estado == EstadoDoNo.Comprado || estado == EstadoDoNo.CondicionalmenteAtivo
                    ? rank + "/" + no.RankMaximo + (no.Condicional ? "~" : "")
                    : TextosTalentos.MarcaDoEstado(estado);
            marca.style.color = EstiloVisual.Cor(texto);
        }

        private void DesenharArestas(MeshGenerationContext contexto)
        {
            var ret = _arestas.contentRect;
            if (ret.width < 1f || !_estado.Carregada)
            {
                return;
            }

            var extensao = _estado.Extensao();
            var centro = ret.center - ret.position;
            var escala = ret.width / 2f / extensao;
            var pincel = contexto.painter2D;
            foreach (var no in _estado.Configuracao.Nos)
            {
                var destino = centro + new Vector2(no.X, no.Y) * escala;
                if (no.Requisitos.Length == 0)
                {
                    // A raiz do caminho sai do centro da árvore.
                    Linha(pincel, centro, destino, false);
                    continue;
                }

                foreach (var requisito in no.Requisitos)
                {
                    var origem = RegrasDaArvore.Procurar(_estado.Configuracao, requisito.NoId);
                    if (origem == null)
                    {
                        continue;
                    }

                    var cumprido =
                        RegrasDaArvore.RankDe(_estado.Ranks, origem.Id) >= requisito.RankMinimo;
                    Linha(
                        pincel,
                        centro + new Vector2(origem.X, origem.Y) * escala,
                        destino,
                        cumprido
                    );
                }
            }
        }

        private static void Linha(Painter2D pincel, Vector2 de, Vector2 para, bool ativa)
        {
            pincel.strokeColor = EstiloVisual.Cor(
                ativa ? ConfigVisual.Cores.Essencia : ConfigVisual.Cores.BordaDiscreta
            );
            pincel.lineWidth = ativa ? ConfigVisual.Bordas.Selecao : ConfigVisual.Bordas.Destaque;
            pincel.BeginPath();
            pincel.MoveTo(de);
            pincel.LineTo(para);
            pincel.Stroke();
        }

        // --- Painel do nó --------------------------------------------------------------

        private void AtualizarPainel()
        {
            _detalhes.Clear();
            var no = _estado.NoSelecionado;
            if (no == null)
            {
                _detalhes.Add(ComponentesVisuais.Texto(TextosTalentos.Dica));
                _comprar.text = TextosTalentos.Compra;
                _comprar.SetEnabled(false);
                return;
            }

            var estado = _estado.EstadoDe(no);
            var rank = _estado.RankDe(no);
            var avaliacao = _estado.Avaliar(no);
            var secundario = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);

            _detalhes.Add(ComponentesVisuais.Texto(no.Nome, subtitulo: true));
            Adicionar(TextosTalentos.TipoDoNo(no), secundario);
            if (!string.IsNullOrEmpty(no.Descricao))
            {
                Adicionar(no.Descricao);
            }

            Adicionar(
                "Estado: "
                    + TextosTalentos.LegendaDoEstado(estado)
                    + "  ·  Rank "
                    + rank
                    + " de "
                    + no.RankMaximo
            );
            foreach (var efeito in no.Efeitos)
            {
                Adicionar(TextosTalentos.Efeito(efeito));
            }

            if (no.Condicional)
            {
                Adicionar("O efeito vale só para as skills compatíveis.", secundario);
            }

            if (rank < no.RankMaximo)
            {
                Adicionar("Próximo rank custa " + no.CustoPorRank[rank] + " pontos.");
            }

            foreach (var requisito in no.Requisitos)
            {
                var origem = RegrasDaArvore.Procurar(_estado.Configuracao, requisito.NoId);
                var cumprido =
                    RegrasDaArvore.RankDe(_estado.Ranks, requisito.NoId) >= requisito.RankMinimo;
                Adicionar(
                    "Requisito: "
                        + (origem == null ? requisito.NoId : origem.Nome)
                        + " (rank "
                        + requisito.RankMinimo
                        + ") — "
                        + (cumprido ? "cumprido" : "pendente"),
                    cumprido ? EstiloVisual.Cor(ConfigVisual.Cores.Ganho) : secundario
                );
            }

            if (avaliacao.Erro != null)
            {
                var motivo = TextosTalentos.Motivo(_estado, no, avaliacao);
                Adicionar(
                    (estado == EstadoDoNo.Bloqueado ? "Bloqueado. " : "") + motivo,
                    EstiloVisual.Cor(ConfigVisual.Cores.Erro)
                );
            }

            if (rank < no.RankMaximo)
            {
                AdicionarPrevia(no);
            }

            _comprar.text =
                _estado.Comprando ? TextosTalentos.Comprando
                : rank >= no.RankMaximo ? TextosTalentos.RankMaximo
                : TextosTalentos.Compra + " (" + no.CustoPorRank[rank] + ")";
            _comprar.SetEnabled(avaliacao.Erro == null && !_estado.Comprando);
        }

        // Antes e depois de comprar o próximo rank, só do que a árvore dá.
        private void AdicionarPrevia(NoDaArvore no)
        {
            var depois = _estado.AtributosDepois(no);
            var vistos = new HashSet<AtributoPrincipal>();
            foreach (var efeito in no.Efeitos)
            {
                if (efeito.Tipo != TipoDeEfeito.AtributoPrincipal || !vistos.Add(efeito.Atributo))
                {
                    continue;
                }

                Adicionar(
                    TextosTalentos.Rotulo(efeito.Atributo)
                        + " (árvore): "
                        + ValorDe(_estado.Atributos, efeito.Atributo)
                        + " → "
                        + ValorDe(depois, efeito.Atributo),
                    EstiloVisual.Cor(ConfigVisual.Cores.Ganho)
                );
            }

            var custo = no.CustoPorRank[_estado.RankDe(no)];
            Adicionar(
                "Pontos livres: " + _estado.PontosLivres + " → " + (_estado.PontosLivres - custo),
                EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario)
            );
        }

        private void Adicionar(string texto, Color? cor = null)
        {
            var linha = ComponentesVisuais.Texto(texto);
            if (cor.HasValue)
            {
                linha.style.color = cor.Value;
            }

            _detalhes.Add(linha);
        }

        private void IniciarRedistribuicao()
        {
            if (_estado.PontosGastos <= 0)
            {
                MostrarRetorno(TextosTalentos.SemPontosParaRedistribuir, true);
                return;
            }

            _confirmacaoRedistribuir.style.display = DisplayStyle.Flex;
        }

        private async void ConfirmarRedistribuicao()
        {
            _confirmacaoRedistribuir.style.display = DisplayStyle.None;
            MostrarRetorno(TextosTalentos.Redistribuindo, false);
            try
            {
                var resposta = await ClienteApi.Post<ArvoreResponse>(
                    RotasTalentos.RedistribuirDe(EntradaNoJogo.PersonagemId),
                    new RedistribuirRequest { VersaoConfiguracao = _estado.Configuracao.Versao }
                );

                if (!Aberta)
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estado.Aplicar(resposta.Corpo);
                    MostrarRetorno("Pontos redistribuídos com sucesso.", false);
                    return;
                }

                MostrarRetorno(TraduzirErro(resposta.CodigoErro), true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarRetorno(TextosTalentos.Generica, true);
                }
            }
        }

        private async void CarregarPerfis()
        {
            try
            {
                var resposta = await ClienteApi.Get<ListaPerfisResponse>(
                    RotasTalentos.PerfisDe(EntradaNoJogo.PersonagemId)
                );
                if (!Aberta || !resposta.Sucesso || resposta.Corpo == null)
                {
                    return;
                }

                _perfis = new List<PerfilTalentosDto>(
                    resposta.Corpo.Perfis ?? Array.Empty<PerfilTalentosDto>()
                );
                AtualizarListaPerfis();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void AtualizarListaPerfis()
        {
            _listaPerfis.Clear();
            if (_perfis.Count == 0)
            {
                var vazio = ComponentesVisuais.Texto("Nenhum perfil de build salvo.");
                vazio.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);
                _listaPerfis.Add(vazio);
                return;
            }

            foreach (var perfil in _perfis)
            {
                var linha = new VisualElement();
                linha.style.flexDirection = FlexDirection.Row;
                linha.style.justifyContent = Justify.SpaceBetween;
                linha.style.alignItems = Align.Center;
                linha.style.marginBottom = ConfigVisual.Espacamento.Minimo;

                var rotulo = ComponentesVisuais.Texto(
                    perfil.Nome + " (" + perfil.PontosTotais + " pts)"
                );
                linha.Add(rotulo);

                var acoes = new VisualElement();
                acoes.style.flexDirection = FlexDirection.Row;

                var idPerfil = perfil.Id;
                var botaoCarregar = ComponentesVisuais.Botao(
                    TextosTalentos.CarregarPerfilComCusto(_estado.CustoRedistribuicaoCoroas),
                    () => CarregarPerfil(idPerfil)
                );
                acoes.Add(botaoCarregar);

                var botaoExcluir = ComponentesVisuais.Botao(
                    TextosTalentos.ExcluirPerfil,
                    () => ExcluirPerfil(idPerfil)
                );
                botaoExcluir.style.marginLeft = ConfigVisual.Espacamento.Pequeno;
                acoes.Add(botaoExcluir);

                linha.Add(acoes);
                _listaPerfis.Add(linha);
            }
        }

        private async void SalvarPerfil()
        {
            var nome = _inputNomePerfil.value?.Trim();
            if (string.IsNullOrEmpty(nome))
            {
                MostrarRetorno(TextosTalentos.NomePerfilInvalido, true);
                return;
            }

            try
            {
                var resposta = await ClienteApi.Post<PerfilTalentosDto>(
                    RotasTalentos.PerfisDe(EntradaNoJogo.PersonagemId),
                    new SalvarPerfilRequest { Nome = nome }
                );

                if (!Aberta)
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _inputNomePerfil.value = "";
                    _containerSalvarPerfil.style.display = DisplayStyle.None;
                    MostrarRetorno(TextosTalentos.PerfilSalvo, false);
                    CarregarPerfis();
                    return;
                }

                MostrarRetorno(TraduzirErro(resposta.CodigoErro), true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarRetorno(TextosTalentos.Generica, true);
                }
            }
        }

        private async void CarregarPerfil(long perfilId)
        {
            MostrarRetorno(TextosTalentos.CarregandoPerfil, false);
            try
            {
                var resposta = await ClienteApi.Post<CarregarPerfilResponse>(
                    RotasTalentos.CarregarPerfilDe(EntradaNoJogo.PersonagemId, perfilId),
                    null
                );

                if (!Aberta)
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estado.Aplicar(resposta.Corpo.Arvore);
                    if (resposta.Corpo.Avisos != null && resposta.Corpo.Avisos.Length > 0)
                    {
                        MostrarRetorno(string.Join(" ", resposta.Corpo.Avisos), false);
                    }
                    else
                    {
                        MostrarRetorno(TextosTalentos.PerfilCarregado, false);
                    }

                    return;
                }

                MostrarRetorno(TraduzirErro(resposta.CodigoErro), true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarRetorno(TextosTalentos.Generica, true);
                }
            }
        }

        private async void ExcluirPerfil(long perfilId)
        {
            try
            {
                var resposta = await ClienteApi.Delete(
                    RotasTalentos.PerfilDe(EntradaNoJogo.PersonagemId, perfilId)
                );

                if (!Aberta)
                {
                    return;
                }

                if (resposta.Sucesso)
                {
                    CarregarPerfis();
                    return;
                }

                MostrarRetorno(TraduzirErro(resposta.CodigoErro), true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Aberta)
                {
                    MostrarRetorno(TextosTalentos.Generica, true);
                }
            }
        }

        private static string TraduzirErro(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroTalentos.EmCombate:
                    return TextosTalentos.EmCombate;
                case CodigosErroTalentos.SaldoInsuficiente:
                    return TextosTalentos.SaldoInsuficiente;
                case CodigosErroTalentos.NenhumPontoGasto:
                    return TextosTalentos.SemPontosParaRedistribuir;
                case CodigosErroTalentos.LimiteDePerfisAtingido:
                    return TextosTalentos.LimitePerfisAtingido;
                case CodigosErroTalentos.NomeDePerfilInvalido:
                    return TextosTalentos.NomePerfilInvalido;
                case CodigosErroTalentos.PontosInsuficientes:
                    return TextosTalentos.SemPontos;
                case CodigosErroTalentos.ArvoreDesatualizada:
                    return TextosTalentos.Desatualizada;
                default:
                    return TextosTalentos.Generica;
            }
        }

        // --- Estilo --------------------------------------------------------------------

        private static void SetBorda(VisualElement elemento, float largura, string cor)
        {
            var c = EstiloVisual.Cor(cor);
            elemento.style.borderTopWidth = largura;
            elemento.style.borderBottomWidth = largura;
            elemento.style.borderLeftWidth = largura;
            elemento.style.borderRightWidth = largura;
            elemento.style.borderTopColor = c;
            elemento.style.borderBottomColor = c;
            elemento.style.borderLeftColor = c;
            elemento.style.borderRightColor = c;
        }

        private static void SetRaio(VisualElement elemento, Length raio)
        {
            elemento.style.borderTopLeftRadius = raio;
            elemento.style.borderTopRightRadius = raio;
            elemento.style.borderBottomLeftRadius = raio;
            elemento.style.borderBottomRightRadius = raio;
        }
    }
}
