using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// A linha por cima do jogo com a janela fechada: o convite que espera resposta ("O:
    /// responder"), o pedido de amizade recebido e as mensagens do
    /// grupo. Criada pelo <see cref="ObservadorDoGrupo"/> na cena do mundo, então o "Sair" descarrega tudo junto.
    /// </summary>
    internal sealed class AvisoDoGrupoNaTela : MonoBehaviour
    {
        private Label _aviso;
        private VisualElement _convite;
        private Label _textoConvite;
        private long _conviteAtual;
        private long _pedidoAtual;
        private EstadoDoGrupo _estado;
        private EstadoDeAmigos _amigos;
        private PanelSettings _configuracaoDoPainel;

        /// <summary>Liga o aviso à interface. Chamar antes de ativar.</summary>
        public void Configurar(
            VisualElement raizDaInterface,
            PanelSettings configuracaoDoPainel,
            EstadoDoGrupo estado = null,
            EstadoDeAmigos amigos = null
        )
        {
            _configuracaoDoPainel = configuracaoDoPainel;
            _estado = estado ?? GrupoDoClient.Estado;
            _amigos = amigos ?? EstadoDeAmigos.DoClient;

            // O aviso não pode pegar o mouse nem esconder o jogo: texto solto embaixo da tela.
            _aviso = new Label();
            _aviso.pickingMode = PickingMode.Ignore;
            _aviso.AddToClassList(EstiloCore.Dica);
            _aviso.AddToClassList(EstiloSocial.AvisoNaTela);
            raizDaInterface.Add(_aviso);

            _convite = new VisualElement { name = "social.convite.na-tela" };
            _convite.AddToClassList("social-convite-tela");
            _textoConvite = new Label();
            _textoConvite.AddToClassList(EstiloCore.Texto);
            _convite.Add(_textoConvite);
            var acoes = new VisualElement();
            acoes.AddToClassList(EstiloCore.LinhaBotoes);
            var aceitar = new Button(() => Responder(true)) { text = TeclasDeConvite.TextoAceitar };
            aceitar.AddToClassList(EstiloCore.Botao);
            aceitar.AddToClassList(EstiloCore.BotaoPrincipal);
            var recusar = new Button(() => Responder(false))
            {
                text = TeclasDeConvite.TextoRecusar,
            };
            recusar.AddToClassList(EstiloCore.Botao);
            acoes.Add(aceitar);
            acoes.Add(recusar);
            _convite.Add(acoes);
            raizDaInterface.Add(_convite);
            Mostrar("");
        }

        private void OnDestroy()
        {
            if (_configuracaoDoPainel != null)
            {
                Destroy(_configuracaoDoPainel);
            }
        }

        private void Update()
        {
            Atualizar();
            if (
                _convite?.style.display != DisplayStyle.Flex
                || BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados()
                || Keyboard.current == null
            )
                return;

            var resposta = TeclasDeConvite.Resposta(Keyboard.current);
            if (resposta != 0)
                Responder(resposta > 0);
        }

        internal void Atualizar()
        {
            if (_aviso == null)
            {
                return;
            }

            var estado = _estado;
            // A troca tem a vez: com ela na tela, o pedido de amizade espera (as mesmas teclas).
            var temPedido =
                _amigos.PedidosRecebidos.Count > 0
                && EstadoDaTrocaDoClient.Instancia.Convite == null;
            var visivel = !JanelasDoJogo.AlgoAberto && (estado.Convites.Count > 0 || temPedido);
            _convite.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
            _conviteAtual = 0;
            _pedidoAtual = 0;
            if (visivel && estado.Convites.Count > 0)
            {
                var convite = estado.Convites[estado.Convites.Count - 1];
                _conviteAtual = convite.ConviteId;
                _textoConvite.text = ApresentacaoGrupo.TextoDoConvite(convite);
            }
            else if (visivel)
            {
                var pedido = _amigos.PedidosRecebidos[_amigos.PedidosRecebidos.Count - 1];
                _pedidoAtual = pedido.PedidoId;
                _textoConvite.text = TextosGrupo.PedidoDeAmizade(pedido.Nome);
            }
            Mostrar(visivel || JanelasDoJogo.AlgoAberto ? "" : estado.Mensagem);
        }

        private void Responder(bool aceitar)
        {
            if (_conviteAtual != 0)
                _estado.Responder(_conviteAtual, aceitar);
            else if (_pedidoAtual != 0)
                _amigos.ResponderPedido(_pedidoAtual, aceitar);
        }

        private void Mostrar(string texto)
        {
            if (_aviso.text != texto)
            {
                _aviso.text = texto;
            }

            _aviso.style.display = string.IsNullOrEmpty(texto)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
