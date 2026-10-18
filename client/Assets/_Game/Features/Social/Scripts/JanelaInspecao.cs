using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Janela de inspeção de jogador (Inspect Gear): visualiza equipamentos, tier e atributos
    /// calculados de outro avatar no mundo ou no grupo em modo estritamente somente leitura.
    /// </summary>
    internal sealed class JanelaInspecao : JanelaDoJogo
    {
        public const string Chave = "social.inspecao";
        public const string TituloPadrao = "Inspeção";
        public const string Tecla = "";
        public const int Ordem = 55;

        private Label _cabecalho;
        private Label _mensagemAviso;
        private VisualElement _secaoEquipamentos;
        private VisualElement _secaoAtributosBase;
        private VisualElement _secaoAtributosCombate;

        public override bool MolduraSocial => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                TituloPadrao,
                Tecla,
                Ordem,
                () => new JanelaInspecao()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloSocial.Aplicar(conteudo);

            _cabecalho = new Label("Nenhum jogador selecionado");
            _cabecalho.AddToClassList(EstiloCore.Titulo);

            _mensagemAviso = new Label();
            _mensagemAviso.AddToClassList(EstiloCore.Mensagem);

            var painelRolagem = new ScrollView(ScrollViewMode.Vertical);
            painelRolagem.style.flexGrow = 1f;

            _secaoEquipamentos = new VisualElement();
            _secaoEquipamentos.AddToClassList(EstiloSocial.Secao);

            _secaoAtributosBase = new VisualElement();
            _secaoAtributosBase.AddToClassList(EstiloSocial.Secao);

            _secaoAtributosCombate = new VisualElement();
            _secaoAtributosCombate.AddToClassList(EstiloSocial.Secao);

            var rodape = new Label("Somente leitura — equipamentos e atributos de outro jogador.");
            rodape.AddToClassList(EstiloCore.Mensagem);

            conteudo.Add(_cabecalho);
            conteudo.Add(_mensagemAviso);
            conteudo.Add(painelRolagem);

            painelRolagem.Add(_secaoEquipamentos);
            painelRolagem.Add(_secaoAtributosBase);
            painelRolagem.Add(_secaoAtributosCombate);
            painelRolagem.Add(rodape);

            Atualizar(EstadoInspecao.Instancia.UltimoRetrato);
        }

        protected override void AoAbrir()
        {
            EstadoInspecao.Instancia.RecebeuRetrato += Atualizar;
            EstadoInspecao.Instancia.Recusada += AoRecusar;
            Atualizar(EstadoInspecao.Instancia.UltimoRetrato);
        }

        protected override void AoFechar()
        {
            EstadoInspecao.Instancia.RecebeuRetrato -= Atualizar;
            EstadoInspecao.Instancia.Recusada -= AoRecusar;
        }

        private void AoRecusar(InspecaoRecusada recusa)
        {
            _mensagemAviso.text = EstadoInspecao.TextoDeErro(recusa.Motivo);
            EstiloSocial.Mostrar(_mensagemAviso, true);
        }

        public void Atualizar(RetratoInspecaoResponse retrato)
        {
            if (retrato == null)
            {
                _cabecalho.text = "Inspecionar jogador";
                _mensagemAviso.text = "Aguardando informações do jogador...";
                EstiloSocial.Mostrar(_mensagemAviso, true);
                _secaoEquipamentos.Clear();
                _secaoAtributosBase.Clear();
                _secaoAtributosCombate.Clear();
                return;
            }

            EstiloSocial.Mostrar(_mensagemAviso, false);
            DefinirTitulo($"Inspecionar: {retrato.Nome}");
            _cabecalho.text =
                $"{retrato.Nome} — Nível {retrato.Nivel} (Conjunto {retrato.ConjuntoAtivo})";

            MontarEquipamentos(retrato);
            MontarAtributosBase(retrato);
            MontarAtributosCombate(retrato);
        }

        private void MontarEquipamentos(RetratoInspecaoResponse retrato)
        {
            _secaoEquipamentos.Clear();
            var titulo = new Label("Equipamento Equipado");
            titulo.AddToClassList(EstiloCore.Subtitulo);
            _secaoEquipamentos.Add(titulo);

            var pecasPorPosicao = new Dictionary<PosicaoEquipamento, EntradaInventario>();
            if (retrato.Equipados != null)
            {
                foreach (var eq in retrato.Equipados)
                {
                    if (eq != null)
                    {
                        pecasPorPosicao[eq.Posicao] = eq;
                    }
                }
            }

            var posicoesParaMostrar = new[]
            {
                PosicaoEquipamento.Capacete,
                PosicaoEquipamento.Peito,
                PosicaoEquipamento.Luvas,
                PosicaoEquipamento.Calca,
                PosicaoEquipamento.Botas,
                PosicaoEquipamento.Cinto,
                PosicaoEquipamento.Anel1,
                PosicaoEquipamento.Anel2,
                PosicaoEquipamento.Brinco1,
                PosicaoEquipamento.Brinco2,
                PosicaoEquipamento.Adorno,
                retrato.ConjuntoAtivo == 2 ? PosicaoEquipamento.Arma2 : PosicaoEquipamento.Arma1,
                retrato.ConjuntoAtivo == 2
                    ? PosicaoEquipamento.Secundaria2
                    : PosicaoEquipamento.Secundaria1,
            };

            foreach (var pos in posicoesParaMostrar)
            {
                var linha = new VisualElement();
                linha.AddToClassList(EstiloCore.LinhaLista);
                linha.AddToClassList(EstiloSocial.Linha);

                var rotuloPosicao = new Label(NomeDaPosicao(pos) + ":");
                rotuloPosicao.AddToClassList(EstiloCore.Texto);
                rotuloPosicao.style.width = 110f;
                linha.Add(rotuloPosicao);

                if (
                    pecasPorPosicao.TryGetValue(pos, out var entrada)
                    && entrada.Equipamento != null
                )
                {
                    var eq = entrada.Equipamento;
                    var tierTexto = RegrasTier.Calcular(eq).ToString();
                    var raridadeTexto = RegrasEquipamentos.NomeDaRaridade(eq.Raridade);

                    var rotuloItem = new Label(
                        $"[{tierTexto}] {entrada.Nome} ({raridadeTexto}, Nv. {eq.Nivel})"
                    );
                    rotuloItem.AddToClassList(EstiloCore.Texto);
                    rotuloItem.AddToClassList(EstiloSocial.Nome);
                    linha.Add(rotuloItem);
                }
                else
                {
                    var vazio = new Label("(Vazio)");
                    vazio.AddToClassList(EstiloCore.Texto);
                    vazio.AddToClassList(EstiloSocial.Offline);
                    linha.Add(vazio);
                }

                _secaoEquipamentos.Add(linha);
            }
        }

        private void MontarAtributosBase(RetratoInspecaoResponse retrato)
        {
            _secaoAtributosBase.Clear();
            var titulo = new Label("Atributos do Personagem");
            titulo.AddToClassList(EstiloCore.Subtitulo);
            _secaoAtributosBase.Add(titulo);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaLista);

            var texto =
                $"FOR: {retrato.Forca}  |  VIG: {retrato.Vigor}  |  AGI: {retrato.Agilidade}  |  INT: {retrato.Intelecto}  |  ESP: {retrato.Espirito}";
            var label = new Label(texto);
            label.AddToClassList(EstiloCore.Texto);
            linha.Add(label);
            _secaoAtributosBase.Add(linha);
        }

        private void MontarAtributosCombate(RetratoInspecaoResponse retrato)
        {
            _secaoAtributosCombate.Clear();
            var titulo = new Label("Combate (Equipamentos)");
            titulo.AddToClassList(EstiloCore.Subtitulo);
            _secaoAtributosCombate.Add(titulo);

            var at = retrato.Atributos ?? new AtributosDosEquipamentos();
            AdicionarLinhaAtributo(
                _secaoAtributosCombate,
                "Dano da Arma",
                at.DanoDaArma.ToString("0.#")
            );
            AdicionarLinhaAtributo(
                _secaoAtributosCombate,
                "Bônus Dano Físico",
                at.BonusDanoFisico.ToString("0.#")
            );
            AdicionarLinhaAtributo(
                _secaoAtributosCombate,
                "Bônus Dano Mágico",
                at.BonusDanoMagico.ToString("0.#")
            );
            AdicionarLinhaAtributo(_secaoAtributosCombate, "Vida Máxima", at.VidaMaxima.ToString());
            AdicionarLinhaAtributo(
                _secaoAtributosCombate,
                "Defesa Física",
                at.DefesaFisica.ToString("0.#")
            );
            AdicionarLinhaAtributo(
                _secaoAtributosCombate,
                "Defesa Mágica",
                at.DefesaMagica.ToString("0.#")
            );
        }

        private static void AdicionarLinhaAtributo(
            VisualElement container,
            string nome,
            string valor
        )
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaLista);

            var lblNome = new Label(nome);
            lblNome.AddToClassList(EstiloCore.Texto);
            lblNome.style.width = 160f;

            var lblValor = new Label(valor);
            lblValor.AddToClassList(EstiloCore.Texto);

            linha.Add(lblNome);
            linha.Add(lblValor);
            container.Add(linha);
        }

        private static string NomeDaPosicao(PosicaoEquipamento posicao)
        {
            switch (posicao)
            {
                case PosicaoEquipamento.Capacete:
                    return "Capacete";
                case PosicaoEquipamento.Peito:
                    return "Peito";
                case PosicaoEquipamento.Luvas:
                    return "Luvas";
                case PosicaoEquipamento.Calca:
                    return "Calça";
                case PosicaoEquipamento.Botas:
                    return "Botas";
                case PosicaoEquipamento.Cinto:
                    return "Cinto";
                case PosicaoEquipamento.Anel1:
                    return "Anel 1";
                case PosicaoEquipamento.Anel2:
                    return "Anel 2";
                case PosicaoEquipamento.Brinco1:
                    return "Brinco 1";
                case PosicaoEquipamento.Brinco2:
                    return "Brinco 2";
                case PosicaoEquipamento.Adorno:
                    return "Adorno";
                case PosicaoEquipamento.Arma1:
                case PosicaoEquipamento.Arma2:
                    return "Arma Principal";
                case PosicaoEquipamento.Secundaria1:
                case PosicaoEquipamento.Secundaria2:
                    return "Mão Secundária";
                default:
                    return posicao.ToString();
            }
        }
    }
}
