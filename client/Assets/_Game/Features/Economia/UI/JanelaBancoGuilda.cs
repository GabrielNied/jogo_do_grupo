using System;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia.UI
{
    /// <summary>
    /// Interface Lumora do Banco de Guilda (#338).
    /// Exibe itens guardados, saldo de coroas da guilda e controles de depósito e retirada.
    /// Membros e recrutas podem apenas depositar; retirada de itens e coroas é reservada a líderes e oficiais.
    /// </summary>
    public sealed class JanelaBancoGuilda : JanelaDoJogo
    {
        public const string Chave = "economia.bancoguilda";
        public const string TituloPadrao = "Banco da Guilda";
        public const int Ordem = 104;

        private int _abaAtual = 1;
        private Label _rotuloGuilda;
        private Label _rotuloSaldoCoroas;
        private Label _rotuloAviso;
        private VisualElement _gradeItens;
        private IntegerField _campoCoroas;
        private Button _botaoDepositarCoroas;
        private Button _botaoRetirarCoroas;
        private Button _aba1Botao;
        private Button _aba2Botao;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    Chave,
                    TituloPadrao,
                    null,
                    Ordem,
                    () => new JanelaBancoGuilda()
                )
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloEconomia.Aplicar(conteudo);
            conteudo.AddToClassList("economia-conteudo");
            conteudo.AddToClassList("economia-conteudo-banco");

            // Cabeçalho com nome da guilda e saldo
            var topo = new VisualElement();
            topo.AddToClassList("economia-banco-topo");

            _rotuloGuilda = new Label("Guilda: -");
            _rotuloGuilda.AddToClassList(EstiloCore.Dica);
            _rotuloGuilda.AddToClassList("economia-banco-rotulo");
            topo.Add(_rotuloGuilda);

            _rotuloSaldoCoroas = new Label("Cofre: 0 coroas");
            _rotuloSaldoCoroas.AddToClassList(EstiloCore.TextoGanho);
            _rotuloSaldoCoroas.AddToClassList("economia-banco-rotulo");
            topo.Add(_rotuloSaldoCoroas);

            conteudo.Add(topo);

            // Seção de movimentação de coroas
            var secaoCoroas = new VisualElement();
            secaoCoroas.AddToClassList("economia-banco-coroas");

            _campoCoroas = new IntegerField("Coroas:") { value = 10 };
            _campoCoroas.AddToClassList("economia-banco-campo");
            secaoCoroas.Add(_campoCoroas);

            _botaoDepositarCoroas = new Button(AoClicarDepositarCoroas) { text = "Depositar" };
            _botaoDepositarCoroas.AddToClassList(EstiloCore.BotaoPrincipal);
            _botaoDepositarCoroas.AddToClassList("economia-banco-depositar");
            secaoCoroas.Add(_botaoDepositarCoroas);

            _botaoRetirarCoroas = new Button(AoClicarRetirarCoroas) { text = "Retirar" };
            _botaoRetirarCoroas.AddToClassList(EstiloCore.Botao);
            secaoCoroas.Add(_botaoRetirarCoroas);

            conteudo.Add(secaoCoroas);

            // Mensagem / Aviso
            _rotuloAviso = new Label();
            _rotuloAviso.AddToClassList(EstiloCore.Mensagem);
            _rotuloAviso.AddToClassList("economia-banco-aviso");
            _rotuloAviso.style.display = DisplayStyle.None;
            conteudo.Add(_rotuloAviso);

            // Abas de itens
            var linhaAbas = new VisualElement();
            linhaAbas.AddToClassList("economia-banco-abas");

            _aba1Botao = new Button(() => TrocarAba(1)) { text = "Aba 1" };
            _aba1Botao.AddToClassList(EstiloCore.Botao);
            _aba1Botao.AddToClassList("economia-aba-esquerda");
            linhaAbas.Add(_aba1Botao);

            _aba2Botao = new Button(() => TrocarAba(2)) { text = "Aba 2" };
            _aba2Botao.AddToClassList(EstiloCore.Botao);
            linhaAbas.Add(_aba2Botao);

            conteudo.Add(linhaAbas);

            // Grade de itens
            _gradeItens = new VisualElement();
            _gradeItens.AddToClassList("economia-banco-grade");
            conteudo.Add(_gradeItens);

            AtualizarVisual();
        }

        protected override void AoAbrir()
        {
            base.AoAbrir();
            ControleDeEconomia.BancoGuildaMudou += AtualizarVisual;
            ControleDeEconomia.OperacaoRecusada += AoOperacaoRecusada;
            ControleDeEconomia.ConsultarBancoGuilda();
        }

        protected override void AoFechar()
        {
            base.AoFechar();
            ControleDeEconomia.BancoGuildaMudou -= AtualizarVisual;
            ControleDeEconomia.OperacaoRecusada -= AoOperacaoRecusada;
        }

        private void TrocarAba(int aba)
        {
            _abaAtual = aba;
            AtualizarVisual();
        }

        private void AtualizarVisual()
        {
            var banco = ControleDeEconomia.BancoGuilda;
            if (banco == null)
            {
                _rotuloGuilda.text = "Consultando banco da guilda...";
                _rotuloSaldoCoroas.text = "";
                _botaoRetirarCoroas.SetEnabled(false);
                return;
            }

            _rotuloGuilda.text = $"Guilda: {banco.NomeGuilda}";
            _rotuloSaldoCoroas.text = $"Cofre: {banco.SaldoCoroas:N0} coroas";

            // Permissão de retirada: somente líder e oficial
            _botaoRetirarCoroas.SetEnabled(banco.PodeRetirar);
            if (!banco.PodeRetirar)
            {
                _botaoRetirarCoroas.tooltip = "Apenas Líder e Oficial podem retirar do banco.";
            }
            else
            {
                _botaoRetirarCoroas.tooltip = "";
            }

            // Destaque visual na aba ativa
            _aba1Botao.style.opacity = _abaAtual == 1 ? 1f : 0.6f;
            _aba2Botao.style.opacity = _abaAtual == 2 ? 1f : 0.6f;

            _gradeItens.Clear();

            var itensDaAba = new ItemBancoGuildaDto[ConfigBancoGuilda.CasasPorAba];
            if (banco.Itens != null)
            {
                foreach (var item in banco.Itens)
                {
                    if (
                        item.Aba == _abaAtual
                        && item.Casa >= 0
                        && item.Casa < ConfigBancoGuilda.CasasPorAba
                    )
                    {
                        itensDaAba[item.Casa] = item;
                    }
                }
            }

            // Exibir lista de itens presentes na aba
            bool temItens = false;
            for (int i = 0; i < ConfigBancoGuilda.CasasPorAba; i++)
            {
                var item = itensDaAba[i];
                if (item == null)
                    continue;

                temItens = true;
                var linha = new VisualElement();
                linha.AddToClassList("economia-banco-linha");
                linha.style.borderBottomColor = new Color(1f, 1f, 1f, 0.1f);

                var info = new VisualElement();
                var nome = new Label(
                    string.IsNullOrEmpty(item.Detalhes?.Nome)
                        ? $"Item #{item.ItemId}"
                        : item.Detalhes.Nome
                );
                nome.AddToClassList(EstiloCore.Texto);
                info.Add(nome);

                var dep = new Label(
                    $"Depositado por: {item.DepositadoPorNome} (Casa {item.Casa + 1})"
                );
                dep.AddToClassList(EstiloCore.Dica);
                dep.AddToClassList("economia-banco-deposito");
                info.Add(dep);

                linha.Add(info);

                if (banco.PodeRetirar)
                {
                    var idParaRetirar = item.Id;
                    var btnRetirar = new Button(() =>
                        ControleDeEconomia.RetirarItemBancoGuilda(idParaRetirar)
                    )
                    {
                        text = "Retirar",
                    };
                    btnRetirar.AddToClassList(EstiloCore.Botao);
                    linha.Add(btnRetirar);
                }

                _gradeItens.Add(linha);
            }

            if (!temItens)
            {
                var vazio = new Label("Nenhum item nesta aba do cofre.");
                vazio.AddToClassList(EstiloCore.Dica);
                vazio.AddToClassList("economia-banco-vazio");
                _gradeItens.Add(vazio);
            }
        }

        private void AoClicarDepositarCoroas()
        {
            var valor = _campoCoroas.value;
            if (valor <= 0)
                return;

            ControleDeEconomia.DepositarCoroasBancoGuilda(valor);
        }

        private void AoClicarRetirarCoroas()
        {
            var valor = _campoCoroas.value;
            if (valor <= 0)
                return;

            ControleDeEconomia.RetirarCoroasBancoGuilda(valor);
        }

        private void AoOperacaoRecusada(string motivo)
        {
            _rotuloAviso.text = FormatarErro(motivo);
            _rotuloAviso.style.display = DisplayStyle.Flex;
        }

        private static string FormatarErro(string motivo)
        {
            return motivo switch
            {
                CodigosErroEconomia.SemGuilda => "Você não pertence a nenhuma guilda.",
                CodigosErroEconomia.SemPermissaoBancoGuilda =>
                    "Apenas Líder e Oficial possuem permissão para retirar.",
                CodigosErroEconomia.BancoGuildaCheio =>
                    "O banco da guilda está na capacidade máxima (80 itens).",
                CodigosErroEconomia.ItemNaoEncontradoNoBanco =>
                    "Item já foi retirado por outro membro.",
                CodigosErroEconomia.CoroasInsuficientesNoBanco =>
                    "Saldo de coroas insuficiente no cofre da guilda.",
                CodigosErroEconomia.SaldoInsuficiente =>
                    "Você não possui coroas suficientes na carteira.",
                CodigosErroEconomia.InventarioCheio => "Seu inventário está cheio.",
                CodigosErroEconomia.LongeDoBancoGuilda =>
                    "Aproxime-se do cofre da guilda para interagir.",
                _ => $"Operação recusada ({motivo}).",
            };
        }
    }
}
