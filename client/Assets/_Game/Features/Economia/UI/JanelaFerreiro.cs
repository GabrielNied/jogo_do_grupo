using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia.UI
{
    /// <summary>
    /// Reparo de equipamentos no ferreiro da cidade (sem atalho; abre com E no marcador).
    /// </summary>
    public sealed class JanelaFerreiro : JanelaDoJogo
    {
        public const string Chave = "economia.ferreiro";
        public const string Titulo = "Ferreiro";
        public const int Ordem = 101;

        private Label _rotuloSaldo;
        private Label _rotuloAviso;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, null, Ordem, () => new JanelaFerreiro());
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloEconomia.Aplicar(conteudo);
            conteudo.AddToClassList("economia-conteudo");
            conteudo.AddToClassList("economia-conteudo-ferreiro");

            var topo = new VisualElement();
            topo.AddToClassList("economia-topo");

            var titulo = new Label("Ferreiro da Cidade");
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.AddToClassList("economia-titulo");
            topo.Add(titulo);

            _rotuloSaldo = new Label($"Coroas: {ControleDeEconomia.SaldoCoroas}");
            _rotuloSaldo.AddToClassList(EstiloCore.Subtitulo);
            _rotuloSaldo.AddToClassList("economia-saldo");
            _rotuloSaldo.style.color = new Color(0.95f, 0.80f, 0.30f);
            topo.Add(_rotuloSaldo);

            conteudo.Add(topo);

            _rotuloAviso = new Label("");
            _rotuloAviso.AddToClassList(EstiloCore.Mensagem);
            _rotuloAviso.AddToClassList("economia-aviso");
            _rotuloAviso.style.color = new Color(0.60f, 0.85f, 0.60f);
            _rotuloAviso.style.display = DisplayStyle.None;
            conteudo.Add(_rotuloAviso);

            var desc = new Label(
                "Reparo de durabilidade: "
                    + $"{ConfigEconomia.CustoReparoPorPontoPerdido} coroa por ponto restaurado."
            );
            desc.AddToClassList(EstiloCore.Texto);
            desc.AddToClassList("economia-descricao");
            conteudo.Add(desc);

            var info = new Label(
                "Equipamentos quebrados (durabilidade 0) não concedem atributos no combate até serem restaurados. "
                    + "Repare pelo inventário (clique direito no item) enquanto esta janela estiver aberta."
            );
            info.AddToClassList(EstiloCore.Texto);
            info.AddToClassList("economia-nota");
            info.style.color = new Color(0.95f, 0.65f, 0.45f);
            conteudo.Add(info);
        }

        protected override void AoAbrir()
        {
            ControleDeEconomia.SaldoMudou += AtualizarSaldo;
            ControleDeEconomia.ItemReparado += AoItemReparado;
            ControleDeEconomia.OperacaoRecusada += AoOperacaoRecusada;

            ControleDeEconomia.ConsultarSaldo();
            AtualizarSaldo();
        }

        protected override void AoFechar()
        {
            ControleDeEconomia.SaldoMudou -= AtualizarSaldo;
            ControleDeEconomia.ItemReparado -= AoItemReparado;
            ControleDeEconomia.OperacaoRecusada -= AoOperacaoRecusada;
        }

        private void AtualizarSaldo()
        {
            if (_rotuloSaldo != null)
                _rotuloSaldo.text = $"Coroas: {ControleDeEconomia.SaldoCoroas}";
        }

        private void AoItemReparado(ItemReparado msg)
        {
            ExibirAviso($"Item reparado com sucesso! Custo: {msg.CustoCoroas} coroas.");
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
