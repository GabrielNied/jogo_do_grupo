using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia.UI
{
    /// <summary>
    /// O leilão (#176): ordens de compra e venda num livro só, aberto com E no leiloeiro da
    /// cidade. Três abas: comprar (lista à venda, compra direta e pedido de compra), vender
    /// (anunciar um item da bolsa) e as minhas ordens (cancelar, receber o que não coube na
    /// bolsa). O servidor decide preço, taxa e casamento; a janela só pede e mostra.
    /// </summary>
    public sealed class JanelaLeilao : JanelaDoJogo
    {
        public const string Chave = "economia.leilao";
        public const int Ordem = 102;

        private const string CaminhoEstilo = "Economia/EstiloMercado";

        private enum Aba
        {
            Comprar,
            Vender,
            Minhas,
        }

        private Aba _aba = Aba.Comprar;
        private Label _saldo;
        private Label _aviso;
        private Button _botaoComprar;
        private Button _botaoVender;
        private Button _botaoMinhas;
        private VisualElement _painelComprar;
        private VisualElement _painelVender;
        private VisualElement _painelMinhas;

        // Comprar
        private TextField _filtro;
        private VisualElement _listaVendas;
        private VisualElement _listaCompras;
        private DropdownField _modelo;
        private IntegerField _quantidade;
        private LongField _precoMaximo;
        private int _modelosMostrados = -1;

        // Vender
        private Label _taxa;
        private VisualElement _listaBolsa;
        private Label _escolhido;
        private LongField _preco;
        private Label _recebe;
        private long _instanciaEscolhida;

        // Minhas
        private VisualElement _listaMinhas;
        private Button _receber;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                TextosMercado.Titulo,
                null,
                Ordem,
                () => new JanelaLeilao()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                conteudo.styleSheets.Add(estilo);
            }

            conteudo.AddToClassList("mercado-janela");

            var topo = new VisualElement();
            topo.AddToClassList("mercado-topo");
            var titulo = new Label("Leiloeiro da Cidade");
            titulo.AddToClassList(EstiloCore.Titulo);
            topo.Add(titulo);
            _saldo = new Label();
            _saldo.AddToClassList(EstiloCore.Texto);
            _saldo.AddToClassList("mercado-saldo");
            topo.Add(_saldo);
            conteudo.Add(topo);

            var abas = new VisualElement();
            abas.AddToClassList("mercado-abas");
            _botaoComprar = NovaAba(TextosMercado.AbaComprar, Aba.Comprar, abas);
            _botaoVender = NovaAba(TextosMercado.AbaVender, Aba.Vender, abas);
            _botaoMinhas = NovaAba(TextosMercado.AbaMinhas, Aba.Minhas, abas);
            conteudo.Add(abas);

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);
            _aviso.style.display = DisplayStyle.None;
            conteudo.Add(_aviso);

            _painelComprar = new VisualElement();
            MontarComprar(_painelComprar);
            conteudo.Add(_painelComprar);

            _painelVender = new VisualElement();
            MontarVender(_painelVender);
            conteudo.Add(_painelVender);

            _painelMinhas = new VisualElement();
            MontarMinhas(_painelMinhas);
            conteudo.Add(_painelMinhas);

            Redesenhar();
        }

        protected override void AoAbrir()
        {
            ControleDeEconomia.MercadoMudou += Redesenhar;
            ControleDeEconomia.SaldoMudou += AtualizarSaldo;
            ControleDeEconomia.OperacaoRecusada += AoRecusar;
            ControleDeEconomia.ConsultarMercado();
            Redesenhar();
        }

        protected override void AoFechar()
        {
            ControleDeEconomia.MercadoMudou -= Redesenhar;
            ControleDeEconomia.SaldoMudou -= AtualizarSaldo;
            ControleDeEconomia.OperacaoRecusada -= AoRecusar;
        }

        // ── Montagem das abas ─────────────────────────────────────────────────────

        private Button NovaAba(string texto, Aba aba, VisualElement pai)
        {
            var botao = new Button(() => Selecionar(aba)) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            botao.AddToClassList("mercado-aba");
            pai.Add(botao);
            return botao;
        }

        private void MontarComprar(VisualElement painel)
        {
            var busca = new VisualElement();
            busca.AddToClassList("mercado-formulario");
            _filtro = new TextField(TextosMercado.Filtro);
            _filtro.AddToClassList(EstiloCore.Campo);
            _filtro.RegisterCallback<KeyDownEvent>(AoTeclaNoFiltro);
            busca.Add(_filtro);
            busca.Add(NovoBotao(TextosMercado.Buscar, Buscar));
            painel.Add(busca);

            painel.Add(Subtitulo(TextosMercado.AVenda));
            _listaVendas = NovaLista();
            painel.Add(_listaVendas);

            painel.Add(Subtitulo(TextosMercado.PedidosAbertos));
            _listaCompras = NovaLista();
            painel.Add(_listaCompras);

            painel.Add(Subtitulo(TextosMercado.FazerPedido));
            var form = new VisualElement();
            form.AddToClassList("mercado-formulario");
            _modelo = new DropdownField("Item", new List<string> { "..." }, 0);
            _modelo.AddToClassList(EstiloCore.Campo);
            form.Add(_modelo);
            _quantidade = new IntegerField(TextosMercado.Quantidade) { value = 1 };
            _quantidade.AddToClassList(EstiloCore.Campo);
            form.Add(_quantidade);
            _precoMaximo = new LongField(TextosMercado.PrecoMaximo) { value = 0 };
            _precoMaximo.AddToClassList(EstiloCore.Campo);
            form.Add(_precoMaximo);
            form.Add(NovoBotao(TextosMercado.Pedir, FazerPedido));
            painel.Add(form);
        }

        private void MontarVender(VisualElement painel)
        {
            _taxa = new Label(TextosMercado.Taxa(ConfigMercado.TaxaVendaPercentual));
            _taxa.AddToClassList(EstiloCore.Mensagem);
            painel.Add(_taxa);

            painel.Add(Subtitulo(TextosMercado.SuaBolsa));
            _listaBolsa = NovaLista();
            painel.Add(_listaBolsa);

            _escolhido = new Label("");
            _escolhido.AddToClassList(EstiloCore.Texto);
            painel.Add(_escolhido);

            var form = new VisualElement();
            form.AddToClassList("mercado-formulario");
            _preco = new LongField(TextosMercado.Preco) { value = 0 };
            _preco.AddToClassList(EstiloCore.Campo);
            _preco.RegisterValueChangedCallback(_ => AtualizarRecebe());
            form.Add(_preco);
            form.Add(NovoBotao(TextosMercado.Anunciar, Anunciar));
            painel.Add(form);

            _recebe = new Label("");
            _recebe.AddToClassList("mercado-detalhe");
            painel.Add(_recebe);
        }

        private void MontarMinhas(VisualElement painel)
        {
            _listaMinhas = NovaLista();
            painel.Add(_listaMinhas);
            _receber = NovoBotao(
                TextosMercado.ReceberDevolucoes,
                ControleDeEconomia.ReceberDevolucoes
            );
            painel.Add(_receber);
        }

        private static Label Subtitulo(string texto)
        {
            var rotulo = new Label(texto);
            rotulo.AddToClassList(EstiloCore.Subtitulo);
            return rotulo;
        }

        private static VisualElement NovaLista()
        {
            var lista = new ScrollView(ScrollViewMode.Vertical);
            lista.AddToClassList("mercado-lista");
            lista.AddToClassList("mercado-secao");
            return lista;
        }

        private static Button NovoBotao(string texto, System.Action aoClicar)
        {
            var botao = new Button(aoClicar) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            botao.AddToClassList("mercado-botao");
            return botao;
        }

        // ── Ações ─────────────────────────────────────────────────────────────────

        private void Selecionar(Aba aba)
        {
            _aba = aba;
            Redesenhar();
        }

        private void Buscar() => ControleDeEconomia.ConsultarMercado(0, _filtro.value);

        private void AoTeclaNoFiltro(KeyDownEvent evento)
        {
            if (evento.keyCode == KeyCode.Return || evento.keyCode == KeyCode.KeypadEnter)
            {
                Buscar();
                evento.StopPropagation();
            }
        }

        private void FazerPedido()
        {
            var estado = ControleDeEconomia.Mercado;
            if (estado == null || estado.Modelos.Length == 0 || _modelo.index < 0)
            {
                return;
            }

            var modelo = estado.Modelos[_modelo.index];
            ControleDeEconomia.OferecerCompra(modelo.ItemId, _quantidade.value, _precoMaximo.value);
        }

        private void Anunciar()
        {
            if (_instanciaEscolhida == 0)
            {
                return;
            }

            ControleDeEconomia.OferecerVenda(_instanciaEscolhida, _preco.value);
            _instanciaEscolhida = 0;
        }

        private void AoRecusar(string motivo)
        {
            Avisar(TextosMercado.Motivo(motivo), true);
        }

        private void Avisar(string texto, bool erro)
        {
            _aviso.text = texto;
            _aviso.EnableInClassList(EstiloCore.MensagemErro, erro);
            _aviso.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        // ── Desenho ───────────────────────────────────────────────────────────────

        private void AtualizarSaldo()
        {
            if (_saldo != null)
            {
                _saldo.text = $"Coroas: {ControleDeEconomia.SaldoCoroas}";
            }
        }

        private void Redesenhar()
        {
            if (_saldo == null)
            {
                return;
            }

            AtualizarSaldo();
            _painelComprar.style.display = Mostrar(_aba == Aba.Comprar);
            _painelVender.style.display = Mostrar(_aba == Aba.Vender);
            _painelMinhas.style.display = Mostrar(_aba == Aba.Minhas);
            _botaoComprar.EnableInClassList("mercado-aba-ativa", _aba == Aba.Comprar);
            _botaoVender.EnableInClassList("mercado-aba-ativa", _aba == Aba.Vender);
            _botaoMinhas.EnableInClassList("mercado-aba-ativa", _aba == Aba.Minhas);

            var estado = ControleDeEconomia.Mercado;
            if (estado == null)
            {
                return;
            }

            _taxa.text = TextosMercado.Taxa(estado.TaxaVendaPercentual);
            DesenharModelos(estado);
            DesenharVendas(estado);
            DesenharCompras(estado);
            DesenharBolsa(estado);
            DesenharMinhas(estado);
            AtualizarRecebe();
        }

        private static DisplayStyle Mostrar(bool visivel) =>
            visivel ? DisplayStyle.Flex : DisplayStyle.None;

        private void DesenharModelos(EstadoDoMercado estado)
        {
            if (_modelosMostrados == estado.Modelos.Length)
            {
                return;
            }

            _modelosMostrados = estado.Modelos.Length;
            var nomes = new List<string>();
            foreach (var modelo in estado.Modelos)
            {
                nomes.Add(modelo.Nome);
            }

            if (nomes.Count == 0)
            {
                nomes.Add("...");
            }

            _modelo.choices = nomes;
            _modelo.index = 0;
        }

        private void DesenharVendas(EstadoDoMercado estado)
        {
            _listaVendas.Clear();
            if (estado.Vendas.Length == 0)
            {
                _listaVendas.Add(Vazio(TextosMercado.NadaAVenda));
                return;
            }

            foreach (var ordem in estado.Vendas)
            {
                var id = ordem.Id;
                var linha = NovaLinha(TextosMercado.Linha(ordem), ordem.Minha);
                linha.Add(Detalhe(TextosMercado.Validade(ordem.ExpiraEm)));
                if (!ordem.Minha)
                {
                    linha.Add(
                        NovoBotao(TextosMercado.Comprar, () => ControleDeEconomia.ComprarOrdem(id))
                    );
                }

                _listaVendas.Add(linha);
            }
        }

        private void DesenharCompras(EstadoDoMercado estado)
        {
            _listaCompras.Clear();
            if (estado.Compras.Length == 0)
            {
                _listaCompras.Add(Vazio(TextosMercado.NadaPedido));
                return;
            }

            foreach (var ordem in estado.Compras)
            {
                var linha = NovaLinha(TextosMercado.Linha(ordem), ordem.Minha);
                linha.Add(Detalhe(TextosMercado.Validade(ordem.ExpiraEm)));
                _listaCompras.Add(linha);
            }
        }

        private void DesenharBolsa(EstadoDoMercado estado)
        {
            _listaBolsa.Clear();
            var existe = false;
            foreach (var item in estado.Bolsa)
            {
                existe |= item.InstanciaId == _instanciaEscolhida;
            }

            if (!existe)
            {
                _instanciaEscolhida = 0;
            }

            _escolhido.text = "";
            if (estado.Bolsa.Length == 0)
            {
                _listaBolsa.Add(Vazio(TextosMercado.BolsaVazia));
                return;
            }

            foreach (var item in estado.Bolsa)
            {
                var id = item.InstanciaId;
                var escolhido = id == _instanciaEscolhida;
                var linha = NovaLinha(TextosMercado.NomeDoItem(item), false);
                linha.EnableInClassList("mercado-linha-selecionada", escolhido);
                linha.Add(
                    NovoBotao(
                        "Escolher",
                        () =>
                        {
                            _instanciaEscolhida = id;
                            Redesenhar();
                        }
                    )
                );
                if (escolhido)
                {
                    _escolhido.text = $"Vender: {TextosMercado.NomeDoItem(item)}";
                }

                _listaBolsa.Add(linha);
            }
        }

        private void DesenharMinhas(EstadoDoMercado estado)
        {
            _listaMinhas.Clear();
            var pendente = false;
            if (estado.Minhas.Length == 0)
            {
                _listaMinhas.Add(Vazio(TextosMercado.SemOrdens));
            }

            foreach (var ordem in estado.Minhas)
            {
                var id = ordem.Id;
                var texto = TextosMercado.Linha(ordem);
                var linha = NovaLinha(texto, true);
                if (ordem.AguardaDevolucao)
                {
                    pendente = true;
                    linha.Add(Detalhe(TextosMercado.AguardandoDevolucao));
                }
                else
                {
                    linha.Add(Detalhe(TextosMercado.Validade(ordem.ExpiraEm)));
                    linha.Add(
                        NovoBotao(
                            TextosMercado.Cancelar,
                            () => ControleDeEconomia.CancelarOrdem(id)
                        )
                    );
                }

                _listaMinhas.Add(linha);
            }

            _receber.style.display = Mostrar(pendente);
        }

        private void AtualizarRecebe()
        {
            if (_recebe != null)
            {
                _recebe.text = TextosMercado.Recebe(_preco.value);
            }
        }

        private static VisualElement NovaLinha(string texto, bool minha)
        {
            var linha = new VisualElement();
            linha.AddToClassList("mercado-linha");
            linha.EnableInClassList("mercado-linha-minha", minha);
            var rotulo = new Label(texto);
            rotulo.AddToClassList(EstiloCore.Texto);
            rotulo.AddToClassList("mercado-texto");
            linha.Add(rotulo);
            return linha;
        }

        private static Label Detalhe(string texto)
        {
            var rotulo = new Label(texto);
            rotulo.AddToClassList("mercado-detalhe");
            return rotulo;
        }

        private static Label Vazio(string texto)
        {
            var rotulo = new Label(texto);
            rotulo.AddToClassList(EstiloCore.Mensagem);
            return rotulo;
        }
    }
}
