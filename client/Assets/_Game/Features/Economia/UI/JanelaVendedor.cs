using System;
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
    /// Compra e venda com os comerciantes na cidade (sem atalho de tecla; abre com E no marcador/NPC) (#173, #1066).
    /// Suporta Armeiro, Armadureiro, Taverneiro, Merceeiro e Comerciante geral.
    /// </summary>
    public sealed class JanelaVendedor : JanelaDoJogo
    {
        public const string Chave = "economia.vendedor";
        public const string ChaveArmeiro = "economia.armeiro";
        public const string ChaveArmadureiro = "economia.armadureiro";
        public const string ChaveTaverneiro = "economia.taverneiro";
        public const string ChaveMerceeiro = "economia.merceeiro";

        public const string Titulo = "Comerciante";
        public const int Ordem = 100;

        private readonly CategoriaLoja _categoria;
        private Label _rotuloSaldo;
        private Label _rotuloAviso;
        private VisualElement _painelComprar;
        private VisualElement _painelVender;
        private Button _abaComprar;
        private Button _abaVender;

        public JanelaVendedor(CategoriaLoja categoria = CategoriaLoja.Nenhuma)
        {
            _categoria = categoria;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    ChaveArmeiro,
                    "Armeiro",
                    null,
                    Ordem,
                    () => new JanelaVendedor(CategoriaLoja.Armeiro)
                )
            );
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    ChaveArmadureiro,
                    "Armadureiro",
                    null,
                    Ordem,
                    () => new JanelaVendedor(CategoriaLoja.Armadureiro)
                )
            );
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    ChaveTaverneiro,
                    "Taverneiro",
                    null,
                    Ordem,
                    () => new JanelaVendedor(CategoriaLoja.Taverneiro)
                )
            );
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    ChaveMerceeiro,
                    "Merceeiro",
                    null,
                    Ordem,
                    () => new JanelaVendedor(CategoriaLoja.Merceeiro)
                )
            );
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                Titulo,
                null,
                Ordem,
                () => new JanelaVendedor(CategoriaLoja.Nenhuma)
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.style.minWidth = 540f;
            conteudo.style.maxWidth = 640f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            var topo = new VisualElement();
            topo.style.flexDirection = FlexDirection.Row;
            topo.style.justifyContent = Justify.SpaceBetween;
            topo.style.alignItems = Align.Center;
            topo.style.marginBottom = 10f;
            topo.style.paddingBottom = 6f;
            topo.style.borderBottomWidth = 1f;
            topo.style.borderBottomColor = EstiloVisual.Cor(ConfigVisual.Cores.BordaDiscreta);

            var textoTitulo = _categoria switch
            {
                CategoriaLoja.Armeiro => "Armeiro da Cidade (Armas)",
                CategoriaLoja.Armadureiro => "Armadureira da Cidade (Armaduras)",
                CategoriaLoja.Taverneiro => "Taverneiro da Cidade (Comida e Bebida)",
                CategoriaLoja.Merceeiro => "Merceeiro da Cidade (Consumíveis)",
                _ => "Comerciante da Cidade",
            };
            var titulo = new Label(textoTitulo);
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.style.fontSize = 15f;
            titulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            titulo.style.color = EstiloVisual.Cor(ConfigVisual.Cores.Marfim);
            topo.Add(titulo);

            _rotuloSaldo = new Label($"Coroas: {ControleDeEconomia.SaldoCoroas}");
            _rotuloSaldo.AddToClassList(EstiloCore.Subtitulo);
            _rotuloSaldo.style.fontSize = 14f;
            _rotuloSaldo.style.unityFontStyleAndWeight = FontStyle.Bold;
            _rotuloSaldo.style.color = new Color(0.95f, 0.80f, 0.30f);
            topo.Add(_rotuloSaldo);

            conteudo.Add(topo);

            var barraAbas = new VisualElement();
            barraAbas.style.flexDirection = FlexDirection.Row;
            barraAbas.style.marginBottom = 10f;

            _abaComprar = new Button(() => SelecionarAba(0)) { text = "Comprar" };
            _abaComprar.AddToClassList(EstiloCore.Botao);
            _abaComprar.style.marginRight = 6f;
            barraAbas.Add(_abaComprar);

            _abaVender = new Button(() => SelecionarAba(1)) { text = "Vender (Lixo)" };
            _abaVender.AddToClassList(EstiloCore.Botao);
            barraAbas.Add(_abaVender);

            conteudo.Add(barraAbas);

            _rotuloAviso = new Label("");
            _rotuloAviso.AddToClassList(EstiloCore.Mensagem);
            _rotuloAviso.style.fontSize = 11f;
            _rotuloAviso.style.color = new Color(0.60f, 0.85f, 0.60f);
            _rotuloAviso.style.marginBottom = 6f;
            _rotuloAviso.style.display = DisplayStyle.None;
            conteudo.Add(_rotuloAviso);

            _painelComprar = new VisualElement();
            MontarPainelComprar(_painelComprar);
            conteudo.Add(_painelComprar);

            _painelVender = new VisualElement();
            MontarPainelVender(_painelVender);
            conteudo.Add(_painelVender);

            SelecionarAba(0);
        }

        private void MontarPainelComprar(VisualElement painel)
        {
            var textoDesc = _categoria switch
            {
                CategoriaLoja.Armeiro => "Armas forjadas e prontas para combate:",
                CategoriaLoja.Armadureiro => "Armaduras e peças defensivas para proteção:",
                CategoriaLoja.Taverneiro => "Comidas e bebidas nutritivas da taverna:",
                CategoriaLoja.Merceeiro => "Poções, consumíveis e provisões essenciais:",
                _ => "Itens e provisões disponíveis para compra:",
            };
            var desc = new Label(textoDesc);
            desc.AddToClassList(EstiloCore.Texto);
            desc.style.fontSize = 11.5f;
            desc.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);
            desc.style.marginBottom = 8f;
            painel.Add(desc);

            var lista = new ScrollView();
            lista.style.maxHeight = 280f;

            foreach (var item in ConfigEconomia.ObterCatalogo(_categoria))
            {
                var linha = new VisualElement();
                linha.style.flexDirection = FlexDirection.Row;
                linha.style.justifyContent = Justify.SpaceBetween;
                linha.style.alignItems = Align.Center;
                linha.style.paddingLeft = 8f;
                linha.style.paddingRight = 8f;
                linha.style.paddingTop = 6f;
                linha.style.paddingBottom = 6f;
                linha.style.marginBottom = 4f;
                linha.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Campo);
                linha.style.borderTopLeftRadius = 4f;
                linha.style.borderTopRightRadius = 4f;
                linha.style.borderBottomLeftRadius = 4f;
                linha.style.borderBottomRightRadius = 4f;

                var rotuloItem = new Label(item.Nome);
                rotuloItem.style.fontSize = 12f;
                rotuloItem.style.unityFontStyleAndWeight = FontStyle.Bold;
                rotuloItem.style.color = new Color(0.90f, 0.92f, 0.98f);
                linha.Add(rotuloItem);

                var direita = new VisualElement();
                direita.style.flexDirection = FlexDirection.Row;
                direita.style.alignItems = Align.Center;

                var rotuloPreco = new Label($"{item.PrecoCoroas} coroas");
                rotuloPreco.style.fontSize = 11.5f;
                rotuloPreco.style.color = new Color(0.95f, 0.80f, 0.30f);
                rotuloPreco.style.marginRight = 10f;
                direita.Add(rotuloPreco);

                var itemId = item.ItemId;
                var botaoComprar = new Button(() => AoClicarComprar(itemId)) { text = "Comprar" };
                botaoComprar.AddToClassList(EstiloCore.Botao);
                botaoComprar.style.paddingLeft = 12f;
                botaoComprar.style.paddingRight = 12f;
                botaoComprar.style.paddingTop = 4f;
                botaoComprar.style.paddingBottom = 4f;
                direita.Add(botaoComprar);

                linha.Add(direita);
                lista.Add(linha);
            }

            painel.Add(lista);
        }

        private void MontarPainelVender(VisualElement painel)
        {
            var desc = new Label(
                "O comerciante compra qualquer peça do inventário por preço fixo baseado no tier (sink de lixo):"
            );
            desc.AddToClassList(EstiloCore.Texto);
            desc.style.fontSize = 11.5f;
            desc.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);
            desc.style.marginBottom = 8f;
            painel.Add(desc);

            var tabelaPrecos = new VisualElement();
            tabelaPrecos.style.flexDirection = FlexDirection.Row;
            tabelaPrecos.style.flexWrap = Wrap.Wrap;
            tabelaPrecos.style.marginBottom = 12f;

            CriarBadgeTier(tabelaPrecos, TierItem.D, ConfigEconomia.PrecoVendaPorTier(TierItem.D));
            CriarBadgeTier(tabelaPrecos, TierItem.C, ConfigEconomia.PrecoVendaPorTier(TierItem.C));
            CriarBadgeTier(tabelaPrecos, TierItem.B, ConfigEconomia.PrecoVendaPorTier(TierItem.B));
            CriarBadgeTier(tabelaPrecos, TierItem.A, ConfigEconomia.PrecoVendaPorTier(TierItem.A));
            CriarBadgeTier(tabelaPrecos, TierItem.S, ConfigEconomia.PrecoVendaPorTier(TierItem.S));
            CriarBadgeTier(
                tabelaPrecos,
                TierItem.SMais,
                ConfigEconomia.PrecoVendaPorTier(TierItem.SMais)
            );

            painel.Add(tabelaPrecos);

            var dica = new Label(
                "Dica: Itens marcados como Favorito são protegidos e não podem ser vendidos."
            );
            dica.AddToClassList(EstiloCore.Texto);
            dica.style.fontSize = 10.5f;
            dica.style.color = new Color(0.60f, 0.65f, 0.75f);
            painel.Add(dica);
        }

        private void CriarBadgeTier(VisualElement container, TierItem tier, long preco)
        {
            var badge = new VisualElement();
            badge.style.paddingLeft = 8f;
            badge.style.paddingRight = 8f;
            badge.style.paddingTop = 4f;
            badge.style.paddingBottom = 4f;
            badge.style.marginRight = 6f;
            badge.style.marginBottom = 6f;
            badge.style.backgroundColor = EstiloVisual.Cor(ConfigVisual.Cores.Retrato);
            badge.style.borderTopLeftRadius = 4f;
            badge.style.borderTopRightRadius = 4f;
            badge.style.borderBottomLeftRadius = 4f;
            badge.style.borderBottomRightRadius = 4f;

            var texto = new Label($"Tier {RegrasTier.Letra(tier)}: {preco} coroas");
            texto.style.fontSize = 11f;
            texto.style.unityFontStyleAndWeight = FontStyle.Bold;
            texto.style.color = new Color(0.85f, 0.88f, 0.95f);
            badge.Add(texto);
            container.Add(badge);
        }

        private void SelecionarAba(int indice)
        {
            _painelComprar.style.display = indice == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _painelVender.style.display = indice == 1 ? DisplayStyle.Flex : DisplayStyle.None;

            Color corAtiva = EstiloVisual.Cor(ConfigVisual.Cores.Selecionado);
            Color corInativa = EstiloVisual.Cor(ConfigVisual.Cores.Desabilitado);

            _abaComprar.style.backgroundColor = indice == 0 ? corAtiva : corInativa;
            _abaVender.style.backgroundColor = indice == 1 ? corAtiva : corInativa;
        }

        private void AoClicarComprar(int itemId)
        {
            ControleDeEconomia.Comprar(
                itemId,
                1,
                _categoria,
                ConfigEconomia.ObterNpcId(_categoria)
            );
        }

        protected override void AoAbrir()
        {
            ControleDeEconomia.SaldoMudou += AtualizarSaldo;
            ControleDeEconomia.ItemComprado += AoItemComprado;
            ControleDeEconomia.ItemVendido += AoItemVendido;
            ControleDeEconomia.OperacaoRecusada += AoOperacaoRecusada;

            ControleDeEconomia.ConsultarSaldo();
            AtualizarSaldo();
        }

        protected override void AoFechar()
        {
            ControleDeEconomia.SaldoMudou -= AtualizarSaldo;
            ControleDeEconomia.ItemComprado -= AoItemComprado;
            ControleDeEconomia.ItemVendido -= AoItemVendido;
            ControleDeEconomia.OperacaoRecusada -= AoOperacaoRecusada;
        }

        private void AtualizarSaldo()
        {
            if (_rotuloSaldo != null)
                _rotuloSaldo.text = $"Coroas: {ControleDeEconomia.SaldoCoroas}";
        }

        private void AoItemComprado(ItemCompradoDaLoja msg)
        {
            ExibirAviso($"Compra realizada com sucesso! Custo: {msg.CustoTotal} coroas.");
        }

        private void AoItemVendido(ItemVendidoParaNpc msg)
        {
            ExibirAviso($"Item vendido! Você recebeu {msg.CoroasRecebidas} coroas.");
        }

        private void AoOperacaoRecusada(string motivo)
        {
            ExibirAviso($"Operação recusada: {motivo}", true);
        }

        private void ExibirAviso(string mensagem, bool erro = false)
        {
            if (_rotuloAviso == null)
                return;

            _rotuloAviso.text = mensagem;
            _rotuloAviso.style.color = erro
                ? new Color(0.95f, 0.40f, 0.40f)
                : new Color(0.50f, 0.90f, 0.50f);
            _rotuloAviso.style.display = DisplayStyle.Flex;
        }
    }
}
