using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O convite de troca por cima do jogo, com a janela fechada: "Aceitar (Y)" e "Recusar (N)".
    /// As teclas só valem quando não há convite de grupo na tela (que usa as mesmas teclas). Criada
    /// pelo <see cref="ObservadorDoGrupo"/> na cena do mundo.
    /// </summary>
    internal sealed class ConviteDeTrocaNaTela : MonoBehaviour
    {
        private VisualElement _convite;
        private Label _texto;
        private EstadoDaTrocaDoClient _estado;
        private EstadoDoGrupo _grupo;
        private string _textoAtual = "";

        /// <summary>Liga o convite à interface. Chamar antes de ativar.</summary>
        public void Configurar(
            VisualElement raizDaInterface,
            EstadoDaTrocaDoClient estado = null,
            EstadoDoGrupo grupo = null
        )
        {
            _estado = estado ?? EstadoDaTrocaDoClient.Instancia;
            _grupo = grupo ?? GrupoDoClient.Estado;

            _convite = new VisualElement { name = "social.convite-troca.na-tela" };
            _convite.AddToClassList("social-convite-tela");
            _convite.AddToClassList(EstiloSocial.ConviteDeTrocaNaTela);
            _texto = new Label();
            _texto.AddToClassList(EstiloCore.Texto);
            _convite.Add(_texto);

            var acoes = new VisualElement();
            acoes.AddToClassList(EstiloCore.LinhaBotoes);
            var aceitar = new Button(() => _estado.ResponderConvite(true)) { text = "Aceitar (Y)" };
            aceitar.AddToClassList(EstiloCore.Botao);
            aceitar.AddToClassList(EstiloCore.BotaoPrincipal);
            var recusar = new Button(() => _estado.ResponderConvite(false))
            {
                text = "Recusar (N)",
            };
            recusar.AddToClassList(EstiloCore.Botao);
            acoes.Add(aceitar);
            acoes.Add(recusar);
            _convite.Add(acoes);
            raizDaInterface.Add(_convite);
            _convite.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (_convite == null)
            {
                return;
            }

            _estado.Atualizar(Time.unscaledDeltaTime);
            var convite = _estado.Convite;
            var visivel = convite != null && !JanelasDoJogo.AlgoAberto;
            _convite.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visivel)
            {
                return;
            }

            var texto = TextosTroca.ConviteRecebido(convite.NomeDe);
            if (texto != _textoAtual)
            {
                _textoAtual = texto;
                _texto.text = texto;
            }

            if (
                _grupo.Convites.Count > 0
                || BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados()
                || Keyboard.current == null
            )
            {
                return;
            }

            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                _estado.ResponderConvite(true);
            }
            else if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                _estado.ResponderConvite(false);
            }
        }
    }
}
