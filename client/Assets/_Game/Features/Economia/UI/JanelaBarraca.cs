using System;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia.UI
{
    /// <summary>
    /// Interface Lumora para Barracas de Jogador e Vendedores Offline na praça (#905).
    /// </summary>
    public sealed class JanelaBarraca : JanelaDoJogo
    {
        public const string Chave = "economia.barraca";
        public const string TituloPadrao = "Barracas da Praça";
        public const int Ordem = 103;

        private enum Aba
        {
            Praca,
            Vitrine,
            Gerenciar,
        }

        private Aba _abaAtual = Aba.Praca;
        private Label _rotuloAviso;
        private VisualElement _painelPraca;
        private VisualElement _painelVitrine;
        private VisualElement _painelGerenciar;
        private Button _abaPracaBotao;
        private Button _abaVitrineBotao;
        private Button _abaGerenciarBotao;

        private TextField _campoTitulo;
        private IntegerField _campoDias;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(Chave, TituloPadrao, null, Ordem, () => new JanelaBarraca())
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.style.minWidth = 500f;
            conteudo.style.maxWidth = 600f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            var abas = new VisualElement();
            abas.style.flexDirection = FlexDirection.Row;
            abas.style.marginBottom = 10f;

            _abaPracaBotao = CriarBotaoAba("Praça", Aba.Praca, abas);
            _abaVitrineBotao = CriarBotaoAba("Vitrine", Aba.Vitrine, abas);
            _abaGerenciarBotao = CriarBotaoAba("Minha Barraca", Aba.Gerenciar, abas);
            conteudo.Add(abas);

            _rotuloAviso = new Label();
            _rotuloAviso.AddToClassList(EstiloCore.Mensagem);
            _rotuloAviso.style.display = DisplayStyle.None;
            conteudo.Add(_rotuloAviso);

            _painelPraca = new VisualElement();
            conteudo.Add(_painelPraca);

            _painelVitrine = new VisualElement();
            conteudo.Add(_painelVitrine);

            _painelGerenciar = new VisualElement();
            conteudo.Add(_painelGerenciar);

            MontarGerenciar();
            AtualizarAbas();
        }

        protected override void AoAbrir()
        {
            EstadoDeBarracas.BarracasMudaram += RedesenharPraca;
            EstadoDeBarracas.BarracaMudou += AoMudarBarraca;
            EstadoDeBarracas.ConsultarBarracasNaArea();
            RedesenharPraca();
        }

        protected override void AoFechar()
        {
            EstadoDeBarracas.BarracasMudaram -= RedesenharPraca;
            EstadoDeBarracas.BarracaMudou -= AoMudarBarraca;
        }

        private Button CriarBotaoAba(string texto, Aba aba, VisualElement pai)
        {
            var btn = new Button(() =>
            {
                _abaAtual = aba;
                AtualizarAbas();
            })
            {
                text = texto,
            };
            btn.AddToClassList(EstiloCore.Botao);
            btn.style.marginRight = 6f;
            pai.Add(btn);
            return btn;
        }

        private void AtualizarAbas()
        {
            _painelPraca.style.display =
                _abaAtual == Aba.Praca ? DisplayStyle.Flex : DisplayStyle.None;
            _painelVitrine.style.display =
                _abaAtual == Aba.Vitrine ? DisplayStyle.Flex : DisplayStyle.None;
            _painelGerenciar.style.display =
                _abaAtual == Aba.Gerenciar ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RedesenharPraca()
        {
            _painelPraca.Clear();
            var titulo = new Label("Barracas ativas na Praça da Cidade (Taxa presencial de 3%):");
            titulo.style.fontSize = 13f;
            titulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            titulo.style.marginBottom = 8f;
            _painelPraca.Add(titulo);

            var lista = EstadoDeBarracas.BarracasAtivas;
            if (lista == null || lista.Length == 0)
            {
                var vazio = new Label("Nenhuma barraca montada na praça no momento.");
                vazio.style.color = new Color(0.6f, 0.6f, 0.6f);
                _painelPraca.Add(vazio);
                return;
            }

            var scroll = new ScrollView();
            scroll.style.maxHeight = 300f;

            foreach (var b in lista)
            {
                var card = new VisualElement();
                card.style.flexDirection = FlexDirection.Row;
                card.style.justifyContent = Justify.SpaceBetween;
                card.style.alignItems = Align.Center;
                card.style.paddingTop = 6f;
                card.style.paddingBottom = 6f;
                card.style.borderBottomWidth = 1f;
                card.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);

                var info = new VisualElement();
                var nome = new Label(
                    string.IsNullOrEmpty(b.TituloBarraca)
                        ? $"Barraca de {b.NomeDono}"
                        : $"{b.TituloBarraca} (de {b.NomeDono})"
                );
                nome.style.unityFontStyleAndWeight = FontStyle.Bold;
                var tipo = new Label(
                    b.EhVendedorOffline
                        ? "Manequim Offline · 3% taxa"
                        : "Vendedor Presencial · 3% taxa"
                );
                tipo.style.fontSize = 11f;
                tipo.style.color = new Color(0.7f, 0.7f, 0.7f);

                info.Add(nome);
                info.Add(tipo);

                var btnVer = new Button(() =>
                {
                    EstadoDeBarracas.ConsultarBarraca(b.PersonagemId);
                    _abaAtual = Aba.Vitrine;
                    AtualizarAbas();
                })
                {
                    text = $"Ver Vitrine ({b.QuantidadeItens})",
                };
                btnVer.AddToClassList(EstiloCore.Botao);

                card.Add(info);
                card.Add(btnVer);
                scroll.Add(card);
            }

            _painelPraca.Add(scroll);
        }

        private void AoMudarBarraca(DadosDaBarraca barraca)
        {
            _painelVitrine.Clear();
            if (barraca == null)
            {
                _painelVitrine.Add(new Label("Nenhuma barraca selecionada."));
                return;
            }

            var topo = new Label(
                $"{barraca.Titulo} (Dono: {barraca.NomeDono}) — Selo 'na barraca de {barraca.NomeDono}'"
            );
            topo.style.unityFontStyleAndWeight = FontStyle.Bold;
            topo.style.marginBottom = 6f;
            _painelVitrine.Add(topo);

            var taxa = new Label(
                $"Taxa reduzida presencial: {barraca.TaxaPercentual}% (no leilão geral é 5%)"
            );
            taxa.style.color = new Color(0.4f, 0.8f, 0.4f);
            taxa.style.marginBottom = 10f;
            _painelVitrine.Add(taxa);

            if (barraca.Itens == null || barraca.Itens.Length == 0)
            {
                _painelVitrine.Add(new Label("Esta barraca não tem itens à venda no momento."));
                return;
            }

            var scroll = new ScrollView();
            scroll.style.maxHeight = 280f;

            foreach (var ordem in barraca.Itens)
            {
                var linha = new VisualElement();
                linha.style.flexDirection = FlexDirection.Row;
                linha.style.justifyContent = Justify.SpaceBetween;
                linha.style.alignItems = Align.Center;
                linha.style.paddingTop = 4f;
                linha.style.paddingBottom = 4f;

                var desc = new Label(TextosMercado.Linha(ordem));
                linha.Add(desc);

                if (!ordem.Minha)
                {
                    var idOrdem = ordem.Id;
                    var idVendedor = barraca.PersonagemId;
                    var btnComprar = new Button(() =>
                    {
                        EstadoDeBarracas.ComprarDaBarraca(idOrdem, idVendedor);
                    })
                    {
                        text = "Comprar na Barraca",
                    };
                    btnComprar.AddToClassList(EstiloCore.Botao);
                    linha.Add(btnComprar);
                }

                scroll.Add(linha);
            }

            _painelVitrine.Add(scroll);
        }

        private void MontarGerenciar()
        {
            _painelGerenciar.Clear();

            var rotulo = new Label("Armar barraca na Praça da Cidade:");
            rotulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            rotulo.style.marginBottom = 6f;
            _painelGerenciar.Add(rotulo);

            _campoTitulo = new TextField("Título da Barraca");
            _campoTitulo.value = "Minhas Ofertas Especiais";
            _painelGerenciar.Add(_campoTitulo);

            var linhaPresencial = new VisualElement();
            linhaPresencial.style.flexDirection = FlexDirection.Row;
            linhaPresencial.style.marginTop = 8f;
            linhaPresencial.style.marginBottom = 14f;

            var btnMontarPresencial = new Button(() =>
            {
                EstadoDeBarracas.MontarBarraca(
                    _campoTitulo.value,
                    ConfigBarraca.PracaCidadeX,
                    ConfigBarraca.PracaCidadeZ
                );
            })
            {
                text = "Montar Barraca Presencial",
            };
            btnMontarPresencial.AddToClassList(EstiloCore.Botao);

            var btnDesmontar = new Button(() =>
            {
                EstadoDeBarracas.DesmontarBarraca();
            })
            {
                text = "Desmontar",
            };
            btnDesmontar.AddToClassList(EstiloCore.Botao);
            btnDesmontar.style.marginLeft = 8f;

            linhaPresencial.Add(btnMontarPresencial);
            linhaPresencial.Add(btnDesmontar);
            _painelGerenciar.Add(linhaPresencial);

            var divisor = new VisualElement();
            divisor.style.height = 1f;
            divisor.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            divisor.style.marginBottom = 10f;
            _painelGerenciar.Add(divisor);

            var rotuloOffline = new Label("Contratar Vendedor Offline (Manequim):");
            rotuloOffline.style.unityFontStyleAndWeight = FontStyle.Bold;
            rotuloOffline.style.marginBottom = 4f;
            _painelGerenciar.Add(rotuloOffline);

            var precoDiaria = new Label(
                $"Custo: {ConfigBarraca.DiariaVendedorOfflineCoroas} coroas por diária (24 horas)."
            );
            precoDiaria.style.fontSize = 11f;
            precoDiaria.style.color = new Color(0.7f, 0.7f, 0.7f);
            precoDiaria.style.marginBottom = 6f;
            _painelGerenciar.Add(precoDiaria);

            _campoDias = new IntegerField("Quantidade de Diárias");
            _campoDias.value = 1;
            _painelGerenciar.Add(_campoDias);

            var linhaOffline = new VisualElement();
            linhaOffline.style.flexDirection = FlexDirection.Row;
            linhaOffline.style.marginTop = 8f;

            var btnContratar = new Button(() =>
            {
                EstadoDeBarracas.ContratarVendedorOffline(
                    _campoTitulo.value,
                    _campoDias.value,
                    ConfigBarraca.PracaCidadeX,
                    ConfigBarraca.PracaCidadeZ
                );
            })
            {
                text = "Contratar Vendedor Offline",
            };
            btnContratar.AddToClassList(EstiloCore.Botao);

            var btnEncerrar = new Button(() =>
            {
                EstadoDeBarracas.EncerrarVendedorOffline();
            })
            {
                text = "Encerrar Manequim",
            };
            btnEncerrar.AddToClassList(EstiloCore.Botao);
            btnEncerrar.style.marginLeft = 8f;

            linhaOffline.Add(btnContratar);
            linhaOffline.Add(btnEncerrar);
            _painelGerenciar.Add(linhaOffline);
        }
    }
}
