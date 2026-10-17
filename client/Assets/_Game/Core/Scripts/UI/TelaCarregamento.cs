using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Visual;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    internal enum EtapaDoCarregamento
    {
        Cenario,
        Conexao,
        Posicao,
    }

    /// <summary>Mostra apenas etapas concluídas de verdade durante a entrada no mundo.</summary>
    internal sealed class TelaCarregamento : Tela
    {
        private readonly Label[] _etapas = new Label[3];
        private Label _estado;
        private Label _erro;
        private VisualElement _acoes;

        internal static TelaCarregamento Atual { get; private set; }

        protected override void Montar(VisualElement raiz)
        {
            raiz.AddToClassList("entrada-mundo-tela");
            var painel = EstiloCore.CriarPainel();
            painel.AddToClassList("entrada-mundo-painel");
            painel.Add(ComponentesVisuais.Marca());

            var titulo = new Label("Entrando no mundo");
            titulo.AddToClassList(EstiloCore.Titulo);
            painel.Add(titulo);

            var lema = new Label(TextosDoLivro.EntradaNoMundo);
            lema.AddToClassList(EstiloCore.Texto);
            lema.AddToClassList("entrada-mundo-lema");
            painel.Add(lema);

            _estado = new Label();
            _estado.AddToClassList(EstiloCore.Subtitulo);
            _estado.AddToClassList("entrada-mundo-estado");
            painel.Add(_estado);

            var nomes = new[] { "Cenário", "Conexão", "Posição" };
            for (var i = 0; i < nomes.Length; i++)
            {
                _etapas[i] = new Label(nomes[i]);
                _etapas[i].AddToClassList(EstiloCore.Texto);
                _etapas[i].AddToClassList("entrada-mundo-etapa");
                painel.Add(_etapas[i]);
            }

            _erro = new Label();
            _erro.AddToClassList(EstiloCore.Mensagem);
            _erro.AddToClassList(EstiloCore.MensagemErro);
            _erro.AddToClassList("entrada-mundo-erro");
            painel.Add(_erro);

            _acoes = new VisualElement();
            _acoes.AddToClassList(EstiloCore.LinhaBotoes);
            var tentar = new Button(EntradaNoJogo.TentarNovamente) { text = "Tentar de novo" };
            tentar.AddToClassList(EstiloCore.Botao);
            tentar.AddToClassList(EstiloCore.BotaoPrincipal);
            var voltar = new Button(EntradaNoJogo.Sair) { text = "Voltar à seleção" };
            voltar.AddToClassList(EstiloCore.Botao);
            _acoes.Add(tentar);
            _acoes.Add(voltar);
            painel.Add(_acoes);
            raiz.Add(painel);
            MostrarEtapa(EtapaDoCarregamento.Cenario);
        }

        protected override void AoAbrir() => Atual = this;

        protected override void AoFechar()
        {
            if (Atual == this)
            {
                Atual = null;
            }
        }

        internal void MostrarEtapa(EtapaDoCarregamento etapa)
        {
            _estado.text = etapa switch
            {
                EtapaDoCarregamento.Cenario => "Preparando o cenário...",
                EtapaDoCarregamento.Conexao => "Conectando ao mundo...",
                _ => "Confirmando sua posição...",
            };
            for (var i = 0; i < _etapas.Length; i++)
            {
                _etapas[i].EnableInClassList("entrada-mundo-etapa-ativa", i == (int)etapa);
                _etapas[i].EnableInClassList("entrada-mundo-etapa-pronta", i < (int)etapa);
            }

            _erro.style.display = DisplayStyle.None;
            _acoes.style.display = DisplayStyle.None;
        }

        internal void MostrarFalha(string titulo, string detalhe)
        {
            _estado.text = titulo;
            foreach (var etapa in _etapas)
            {
                etapa.style.display = DisplayStyle.None;
            }

            _erro.text = detalhe;
            _erro.style.display = DisplayStyle.Flex;
            _acoes.style.display = DisplayStyle.Flex;
        }
    }
}
