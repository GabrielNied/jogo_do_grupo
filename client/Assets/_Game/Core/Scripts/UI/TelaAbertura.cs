using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Core.Api;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>Pré-carrega a interface e verifica disponibilidade e versão antes do login.</summary>
    internal sealed class TelaAbertura : Tela
    {
        private Label _estado;
        private Button _tentar;

        protected override void Montar(VisualElement raiz)
        {
            var painel = EstiloCore.CriarPainel();
            painel.AddToClassList("abertura-painel");
            painel.Add(ComponentesVisuais.Marca());

            _estado = new Label("Verificando o servidor...");
            _estado.AddToClassList(EstiloCore.Subtitulo);
            _estado.AddToClassList("abertura-estado");
            painel.Add(_estado);

            _tentar = new Button(Verificar) { text = "Tentar de novo" };
            _tentar.AddToClassList(EstiloCore.Botao);
            _tentar.AddToClassList(EstiloCore.BotaoPrincipal);
            _tentar.style.display = DisplayStyle.None;
            painel.Add(_tentar);
            raiz.Add(painel);
        }

        protected override void AoAbrir() => Verificar();

        internal static string FalhaDaSaude(bool sucesso, EstadoDaSaude resposta)
        {
            if (!sucesso || resposta == null || resposta.Estado != "ok")
            {
                return "Servidor fora do ar. Verifique sua conexão e tente de novo.";
            }

            return resposta.Versao == EstadoDaSaude.VersaoAtual
                ? null
                : "A versão do servidor é incompatível com este jogo.";
        }

        private async void Verificar()
        {
            _tentar.style.display = DisplayStyle.None;
            _estado.text = "Verificando o servidor...";
            // O tema e as fontes são carregados ao montar a interface; a rede é a etapa que resta.
            var resposta = await ClienteApi.Get<EstadoDaSaude>("/saude", autenticar: false);
            if (!Aberta || resposta.CodigoErro == CodigosErroCliente.Cancelada)
            {
                return;
            }

            var falha = FalhaDaSaude(resposta.Sucesso, resposta.Corpo);
            if (falha != null)
            {
                _estado.text = falha;
                _tentar.style.display = DisplayStyle.Flex;
                return;
            }

            if (Sessao.Ativa)
            {
                Navegacao.AbrirTelaAposLogin();
            }
            else
            {
                Navegacao.Abrir(TelasCore.Login);
            }
        }
    }
}
