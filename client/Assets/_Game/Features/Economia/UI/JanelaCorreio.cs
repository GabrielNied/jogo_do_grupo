using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia.UI
{
    /// <summary>
    /// Interface Lumora para o Correio entre jogadores (#212).
    /// Permite ler cartas recebidas, coletar anexos e compor novas mensagens.
    /// </summary>
    public sealed class JanelaCorreio : JanelaDoJogo
    {
        public const string Chave = "economia.correio";
        public const string TituloPadrao = "Correio";
        public const int Ordem = 104;

        private enum Aba
        {
            Entrada,
            Escrever,
        }

        private Aba _abaAtual = Aba.Entrada;

        private VisualElement _painelEntrada;
        private VisualElement _painelEscrever;
        private VisualElement _listaCartas;
        private VisualElement _leitorCarta;
        private Label _rotuloAviso;

        private Button _botaoAbaEntrada;
        private Button _botaoAbaEscrever;

        // Composição
        private TextField _campoDestinatario;
        private TextField _campoAssunto;
        private TextField _campoMensagem;
        private LongField _campoCoroas;
        private Label _rotuloCusto;
        private Button _botaoEnviar;

        private long _cartaSelecionadaId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(Chave, TituloPadrao, null, Ordem, () => new JanelaCorreio())
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.style.minWidth = 520f;
            conteudo.style.maxWidth = 640f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            // Barra de abas
            var abas = new VisualElement();
            abas.style.flexDirection = FlexDirection.Row;
            abas.style.marginBottom = 10f;

            _botaoAbaEntrada = CriarBotaoAba(
                "Caixa de Entrada",
                true,
                () => TrocarAba(Aba.Entrada)
            );
            _botaoAbaEscrever = CriarBotaoAba(
                "Escrever Carta",
                false,
                () => TrocarAba(Aba.Escrever)
            );

            abas.Add(_botaoAbaEntrada);
            abas.Add(_botaoAbaEscrever);

            var espacador = new VisualElement();
            espacador.style.flexGrow = 1f;
            abas.Add(espacador);

            var botaoAtualizar = CriarBotao(
                "Atualizar",
                () => ControleDeEconomia.ConsultarCorreio()
            );
            botaoAtualizar.style.height = 28f;
            abas.Add(botaoAtualizar);

            conteudo.Add(abas);

            _rotuloAviso = new Label();
            _rotuloAviso.AddToClassList(EstiloCore.Mensagem);
            _rotuloAviso.style.display = DisplayStyle.None;
            conteudo.Add(_rotuloAviso);

            _painelEntrada = new VisualElement();
            conteudo.Add(_painelEntrada);

            _painelEscrever = new VisualElement();
            conteudo.Add(_painelEscrever);

            MontarPainelEntrada();
            MontarPainelEscrever();

            TrocarAba(Aba.Entrada);
        }

        protected override void AoAbrir()
        {
            ControleDeEconomia.CorreioMudou += RedesenharEntrada;
            ControleDeEconomia.CartaAbertaMudou += RedesenharLeitor;
            ControleDeEconomia.OperacaoRecusada += MostrarErro;

            ControleDeEconomia.ConsultarSaldo();
            ControleDeEconomia.ConsultarCorreio();
            RedesenharEntrada();
        }

        protected override void AoFechar()
        {
            ControleDeEconomia.CorreioMudou -= RedesenharEntrada;
            ControleDeEconomia.CartaAbertaMudou -= RedesenharLeitor;
            ControleDeEconomia.OperacaoRecusada -= MostrarErro;
        }

        private static Button CriarBotao(string texto, Action acao)
        {
            var botao = new Button(acao) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            return botao;
        }

        private static Button CriarBotaoPrincipal(string texto, Action acao)
        {
            var botao = CriarBotao(texto, acao);
            botao.AddToClassList(EstiloCore.BotaoPrincipal);
            return botao;
        }

        private static Button CriarBotaoAba(string texto, bool ativa, Action acao)
        {
            var botao = CriarBotao(texto, acao);
            botao.EnableInClassList(EstiloCore.BotaoPrincipal, ativa);
            botao.style.marginRight = 6f;
            return botao;
        }

        private void TrocarAba(Aba novaAba)
        {
            _abaAtual = novaAba;
            _botaoAbaEntrada.EnableInClassList(EstiloCore.BotaoPrincipal, novaAba == Aba.Entrada);
            _botaoAbaEscrever.EnableInClassList(EstiloCore.BotaoPrincipal, novaAba == Aba.Escrever);

            _painelEntrada.style.display =
                novaAba == Aba.Entrada ? DisplayStyle.Flex : DisplayStyle.None;
            _painelEscrever.style.display =
                novaAba == Aba.Escrever ? DisplayStyle.Flex : DisplayStyle.None;
            _rotuloAviso.style.display = DisplayStyle.None;

            if (novaAba == Aba.Entrada)
                RedesenharEntrada();
            else
                AtualizarCustoEnvio();
        }

        private void MontarPainelEntrada()
        {
            _listaCartas = new ScrollView();
            _listaCartas.style.maxHeight = 240f;
            _listaCartas.style.marginBottom = 10f;
            _painelEntrada.Add(_listaCartas);

            _leitorCarta = new VisualElement();
            _leitorCarta.style.borderTopWidth = 1f;
            _leitorCarta.style.borderTopColor = new Color(0.3f, 0.3f, 0.35f, 0.6f);
            _leitorCarta.style.paddingTop = 8f;
            _painelEntrada.Add(_leitorCarta);
        }

        private void RedesenharEntrada()
        {
            _listaCartas.Clear();
            var correio = ControleDeEconomia.Correio;

            if (correio == null || correio.Cartas.Length == 0)
            {
                var vazio = new Label("Sua caixa de correio está vazia.");
                vazio.AddToClassList(EstiloCore.Mensagem);
                _listaCartas.Add(vazio);
                RedesenharLeitor();
                return;
            }

            foreach (var carta in correio.Cartas)
            {
                var id = carta.Id;
                var linha = new VisualElement();
                linha.style.flexDirection = FlexDirection.Row;
                linha.style.justifyContent = Justify.SpaceBetween;
                linha.style.alignItems = Align.Center;
                linha.style.paddingLeft = 8f;
                linha.style.paddingRight = 8f;
                linha.style.paddingTop = 6f;
                linha.style.paddingBottom = 6f;
                linha.style.marginBottom = 3f;
                linha.style.backgroundColor =
                    (id == _cartaSelecionadaId)
                        ? new Color(0.25f, 0.35f, 0.5f, 0.4f)
                        : new Color(0.12f, 0.12f, 0.15f, 0.6f);

                var textoBox = new VisualElement();
                var titulo = new Label(carta.Assunto);
                titulo.style.unityFontStyleAndWeight = carta.Lida
                    ? FontStyle.Normal
                    : FontStyle.Bold;
                titulo.style.color = carta.Lida ? Color.white : new Color(1f, 0.85f, 0.4f);

                var subtitulo = new Label(
                    $"De: {carta.Remetente} {(carta.TemAnexos ? "• [Anexo]" : "")}"
                );
                subtitulo.style.fontSize = 11f;
                subtitulo.style.color = new Color(0.7f, 0.7f, 0.7f);

                textoBox.Add(titulo);
                textoBox.Add(subtitulo);
                linha.Add(textoBox);

                var botaoLer = CriarBotao(
                    "Ler",
                    () =>
                    {
                        _cartaSelecionadaId = id;
                        ControleDeEconomia.LerCarta(id);
                        RedesenharEntrada();
                    }
                );
                botaoLer.style.height = 26f;
                linha.Add(botaoLer);

                _listaCartas.Add(linha);
            }

            RedesenharLeitor();
        }

        private void RedesenharLeitor()
        {
            _leitorCarta.Clear();
            var carta = ControleDeEconomia.CartaAberta;
            if (carta == null)
            {
                var ajuda = new Label("Selecione uma carta acima para ler seu conteúdo.");
                ajuda.style.color = new Color(0.6f, 0.6f, 0.65f);
                ajuda.style.fontSize = 12f;
                _leitorCarta.Add(ajuda);
                return;
            }

            var cabecalho = new Label($"De: {carta.Remetente}  |  Assunto: {carta.Assunto}");
            cabecalho.style.unityFontStyleAndWeight = FontStyle.Bold;
            cabecalho.style.marginBottom = 6f;
            _leitorCarta.Add(cabecalho);

            var texto = new Label(carta.Mensagem);
            texto.style.whiteSpace = WhiteSpace.Normal;
            texto.style.marginBottom = 10f;
            texto.style.paddingLeft = 6f;
            texto.style.paddingRight = 6f;
            texto.style.paddingTop = 6f;
            texto.style.paddingBottom = 6f;
            texto.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Trilho);
            _leitorCarta.Add(texto);

            // Anexos
            var temAnexos = !carta.AnexosColetados && (carta.Coroas > 0 || carta.Itens.Length > 0);
            if (temAnexos)
            {
                var boxAnexos = new VisualElement();
                boxAnexos.style.paddingTop = 6f;
                boxAnexos.style.paddingBottom = 6f;
                boxAnexos.style.paddingLeft = 6f;
                boxAnexos.style.paddingRight = 6f;
                boxAnexos.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Campo);
                boxAnexos.style.marginBottom = 10f;

                var rotuloAnexos = new Label("Anexos:");
                rotuloAnexos.style.unityFontStyleAndWeight = FontStyle.Bold;
                boxAnexos.Add(rotuloAnexos);

                if (carta.Coroas > 0)
                {
                    boxAnexos.Add(new Label($"• {carta.Coroas} coroas"));
                }

                foreach (var anexo in carta.Itens)
                {
                    var nome = anexo.Item != null ? anexo.Item.Nome : $"Item #{anexo.ItemId}";
                    boxAnexos.Add(new Label($"• {nome}"));
                }

                _leitorCarta.Add(boxAnexos);
            }

            // Ações
            var barraAcoes = new VisualElement();
            barraAcoes.style.flexDirection = FlexDirection.Row;
            barraAcoes.style.justifyContent = Justify.FlexEnd;

            if (temAnexos)
            {
                var botaoColetar = CriarBotaoPrincipal(
                    "Coletar Anexos",
                    () =>
                    {
                        ControleDeEconomia.ColetarAnexos(carta.Id);
                    }
                );
                botaoColetar.style.marginRight = 8f;
                barraAcoes.Add(botaoColetar);
            }
            else
            {
                var botaoExcluir = CriarBotao(
                    "Excluir Carta",
                    () =>
                    {
                        ControleDeEconomia.ExcluirCarta(carta.Id);
                    }
                );
                barraAcoes.Add(botaoExcluir);
            }

            _leitorCarta.Add(barraAcoes);
        }

        private void MontarPainelEscrever()
        {
            _campoDestinatario = new TextField("Destinatário");
            _campoDestinatario.style.marginBottom = 6f;
            _painelEscrever.Add(_campoDestinatario);

            _campoAssunto = new TextField("Assunto");
            _campoAssunto.style.marginBottom = 6f;
            _painelEscrever.Add(_campoAssunto);

            _campoMensagem = new TextField("Mensagem") { multiline = true };
            _campoMensagem.style.minHeight = 80f;
            _campoMensagem.style.marginBottom = 8f;
            _painelEscrever.Add(_campoMensagem);

            _campoCoroas = new LongField("Coroas Anexadas");
            _campoCoroas.value = 0;
            _campoCoroas.RegisterValueChangedCallback(_ => AtualizarCustoEnvio());
            _campoCoroas.style.marginBottom = 8f;
            _painelEscrever.Add(_campoCoroas);

            _rotuloCusto = new Label(
                $"Taxa de envio: {ConfigCorreio.TaxaEnvioCoroas} coroas | Total a pagar: {ConfigCorreio.TaxaEnvioCoroas} coroas"
            );
            _rotuloCusto.style.fontSize = 11f;
            _rotuloCusto.style.color = new Color(0.8f, 0.8f, 0.85f);
            _rotuloCusto.style.marginBottom = 10f;
            _painelEscrever.Add(_rotuloCusto);

            _botaoEnviar = CriarBotaoPrincipal("Enviar Carta", ExecutarEnvio);
            _painelEscrever.Add(_botaoEnviar);
        }

        private void AtualizarCustoEnvio()
        {
            var coroas = Math.Max(0, _campoCoroas.value);
            var total = ConfigCorreio.TaxaEnvioCoroas + coroas;
            _rotuloCusto.text =
                $"Taxa de envio: {ConfigCorreio.TaxaEnvioCoroas} coroas | Total a pagar: {total} coroas (Seu saldo: {ControleDeEconomia.SaldoCoroas})";
            _botaoEnviar.SetEnabled(ControleDeEconomia.SaldoCoroas >= total);
        }

        private void ExecutarEnvio()
        {
            var dest = _campoDestinatario.value?.Trim() ?? "";
            var assunto = _campoAssunto.value?.Trim() ?? "";
            var mensagem = _campoMensagem.value ?? "";
            var coroas = Math.Max(0, _campoCoroas.value);

            if (string.IsNullOrEmpty(dest))
            {
                MostrarErro("Informe o nome do destinatário.");
                return;
            }

            if (string.IsNullOrEmpty(assunto))
            {
                MostrarErro("Informe o assunto da carta.");
                return;
            }

            ControleDeEconomia.EnviarCarta(dest, assunto, mensagem, coroas, Array.Empty<long>());
            _campoDestinatario.value = "";
            _campoAssunto.value = "";
            _campoMensagem.value = "";
            _campoCoroas.value = 0;
            TrocarAba(Aba.Entrada);
        }

        private void MostrarErro(string motivo)
        {
            _rotuloAviso.style.display = DisplayStyle.Flex;
            _rotuloAviso.text = motivo switch
            {
                CodigosErroEconomia.DestinatarioNaoEncontrado =>
                    "Destinatário não encontrado no servidor.",
                CodigosErroEconomia.DestinatarioInvalido =>
                    "Você não pode enviar uma carta para si mesmo.",
                CodigosErroEconomia.SaldoInsuficiente =>
                    "Saldo de coroas insuficiente para taxa e anexo.",
                CodigosErroEconomia.InventarioCheio =>
                    "Bolsa cheia! Abra espaço para coletar os itens.",
                CodigosErroEconomia.CartaComAnexos => "Recolha os anexos antes de excluir a carta.",
                CodigosErroEconomia.LongeDoCorreio =>
                    "Você precisa estar perto do correio para enviar.",
                _ => $"Operação recusada: {motivo}",
            };
        }
    }
}
