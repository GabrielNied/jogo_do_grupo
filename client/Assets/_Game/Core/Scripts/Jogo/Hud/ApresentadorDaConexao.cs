using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo
{
    internal interface IFonteDaConexao
    {
        EstadoTempoReal Estado { get; }
        string UltimoErro { get; }
        int Tentativa { get; }
        int Latencia { get; }
        event Action<EstadoTempoReal> EstadoMudou;
    }

    internal sealed class FonteDaConexaoReal : IFonteDaConexao
    {
        public EstadoTempoReal Estado => ConexaoTempoReal.Estado;
        public string UltimoErro => ConexaoTempoReal.UltimoErro;
        public int Tentativa => ConexaoTempoReal.TentativaReconexao;
        public int Latencia => ConexaoTempoReal.Latencia;
        public event Action<EstadoTempoReal> EstadoMudou
        {
            add => ConexaoTempoReal.EstadoMudou += value;
            remove => ConexaoTempoReal.EstadoMudou -= value;
        }
    }

    /// <summary>Avisos de rede visíveis no HUD e sobre a cena, com assinatura descartável.</summary>
    internal sealed class ApresentadorDaConexao : IDisposable
    {
        private readonly IFonteDaConexao _fonte;
        private readonly VisualElement _faixa;
        private readonly VisualElement _modal;
        private readonly Label _latencia;
        private readonly Label _textoFaixa;
        private readonly Label _detalhe;
        private readonly IVisualElementScheduledItem _atualizacao;
        private bool _descartado;

        internal ApresentadorDaConexao(
            IFonteDaConexao fonte,
            VisualElement container,
            VisualElement areaLatencia,
            Action tentar,
            Action voltar
        )
        {
            _fonte = fonte;
            _latencia = new Label { name = "core.conexao.latencia" };
            _latencia.AddToClassList(EstiloCore.Texto);
            _latencia.AddToClassList("conexao-latencia");
            _latencia.pickingMode = PickingMode.Ignore;
            areaLatencia.Add(_latencia);

            _faixa = new VisualElement { name = "core.conexao.reconectando" };
            _faixa.AddToClassList("conexao-faixa");
            _faixa.pickingMode = PickingMode.Ignore;
            _textoFaixa = new Label();
            _textoFaixa.AddToClassList(EstiloCore.Texto);
            _faixa.Add(_textoFaixa);
            container.Add(_faixa);

            _modal = new VisualElement { name = "core.conexao.perdida" };
            _modal.AddToClassList(EstiloCore.Tela);
            _modal.AddToClassList(EstiloCore.TelaSobreJogo);
            var painel = EstiloCore.CriarPainel();
            painel.AddToClassList("conexao-modal-painel");
            var titulo = new Label("Conexão perdida");
            titulo.AddToClassList(EstiloCore.Titulo);
            painel.Add(titulo);
            _detalhe = new Label();
            _detalhe.AddToClassList(EstiloCore.Mensagem);
            _detalhe.AddToClassList(EstiloCore.MensagemErro);
            painel.Add(_detalhe);
            var acoes = new VisualElement();
            acoes.AddToClassList(EstiloCore.LinhaBotoes);
            var botaoTentar = new Button(() => tentar?.Invoke()) { text = "Tentar de novo" };
            botaoTentar.AddToClassList(EstiloCore.Botao);
            botaoTentar.AddToClassList(EstiloCore.BotaoPrincipal);
            var botaoVoltar = new Button(() => voltar?.Invoke()) { text = "Voltar à seleção" };
            botaoVoltar.AddToClassList(EstiloCore.Botao);
            acoes.Add(botaoTentar);
            acoes.Add(botaoVoltar);
            painel.Add(acoes);
            _modal.Add(painel);
            container.Add(_modal);

            _fonte.EstadoMudou += AoMudarEstado;
            _atualizacao = container.schedule.Execute(Atualizar).Every(500);
            Atualizar();
        }

        private void AoMudarEstado(EstadoTempoReal _) => Atualizar();

        internal void Atualizar()
        {
            if (_descartado)
            {
                return;
            }

            var aviso = EntradaNoJogo.EntradaPendente
                ? AvisoDaConexao.Nenhum
                : ApresentacaoDaConexao.Aviso(_fonte.Estado, _fonte.UltimoErro);
            _latencia.style.display =
                aviso == AvisoDaConexao.Latencia ? DisplayStyle.Flex : DisplayStyle.None;
            _faixa.style.display =
                aviso == AvisoDaConexao.Reconectando ? DisplayStyle.Flex : DisplayStyle.None;
            _modal.style.display =
                aviso == AvisoDaConexao.Perdida || aviso == AvisoDaConexao.AcessoNegado
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            _latencia.text = ApresentacaoDaConexao.TextoDaLatencia(_fonte.Latencia);
            _textoFaixa.text = ApresentacaoDaConexao.TextoDaFaixa(
                _fonte.Tentativa,
                ClienteTempoReal.MaximoTentativas
            );
            _detalhe.text =
                aviso == AvisoDaConexao.AcessoNegado
                    ? "O acesso a este mundo foi negado. Escolha outro personagem ou tente novamente."
                    : "A conexão com o mundo foi interrompida.";
        }

        public void Dispose()
        {
            if (_descartado)
            {
                return;
            }

            _descartado = true;
            _fonte.EstadoMudou -= AoMudarEstado;
            _atualizacao?.Pause();
            _latencia.RemoveFromHierarchy();
            _faixa.RemoveFromHierarchy();
            _modal.RemoveFromHierarchy();
        }
    }
}
