using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>
    /// O convite de duelo por cima do jogo, sem abrir janela: "Aceitar (F1)" e "Recusar (F2)",
    /// por tecla ou clique. Some ao responder ou quando o prazo do convite acaba. Criada pelo
    /// <see cref="ObservadorDePvp"/> na cena do mundo.
    /// </summary>
    internal sealed class ConviteDeDueloNaTela : MonoBehaviour
    {
        public const string NomeDoPainel = "PainelPvp";
        private const string NomeRaiz = "Pvp.Convite";

        private VisualElement _convite;
        private Label _texto;
        private EstadoDePvp _estado;
        private string _textoAtual = "";

        /// <summary>Cria a raiz da interface na cena do jogador e liga o convite.</summary>
        public static ConviteDeDueloNaTela Criar(GameObject jogador, EstadoDePvp estado)
        {
            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);

            var documento = raiz.AddComponent<UIDocument>();
            // Abaixo da interface do Core (ordem 0), para o menu do Esc ficar por cima.
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(NomeDoPainel, -1f);
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);
            var container = new VisualElement { name = "PvpInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            EstiloPvp.Aplicar(container);
            documento.rootVisualElement.Add(container);

            var convite = raiz.AddComponent<ConviteDeDueloNaTela>();
            convite.Configurar(container, estado);
            return convite;
        }

        internal void Configurar(VisualElement raizDaInterface, EstadoDePvp estado)
        {
            _estado = estado;

            _convite = new VisualElement { name = "pvp.convite.na-tela" };
            _convite.AddToClassList(EstiloPvp.ConviteNaTela);
            _texto = new Label();
            _texto.AddToClassList(EstiloCore.Texto);
            _convite.Add(_texto);

            var acoes = new VisualElement();
            acoes.AddToClassList(EstiloCore.LinhaBotoes);
            var aceitar = new Button(() => Responder(true))
            {
                text = TeclasDeConviteDeDuelo.TextoAceitar,
            };
            aceitar.AddToClassList(EstiloCore.Botao);
            aceitar.AddToClassList(EstiloCore.BotaoPrincipal);
            var recusar = new Button(() => Responder(false))
            {
                text = TeclasDeConviteDeDuelo.TextoRecusar,
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
            if (_convite == null || _estado == null)
            {
                return;
            }

            _estado.Atualizar(Time.unscaledDeltaTime);
            var convite = _estado.ConvitePendente;
            var visivel = convite != null;
            _convite.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visivel)
            {
                return;
            }

            var texto = TextosPvp.ConviteNaTela(convite.NomeDesafiante);
            if (texto != _textoAtual)
            {
                _textoAtual = texto;
                _texto.text = texto;
            }

            if (BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados())
            {
                return;
            }

            var resposta = TeclasDeConviteDeDuelo.Resposta(Keyboard.current);
            if (resposta != 0)
            {
                Responder(resposta > 0);
            }
        }

        private void Responder(bool aceitar)
        {
            var convite = _estado.ConvitePendente;
            if (convite == null)
            {
                return;
            }

            if (aceitar)
            {
                _estado.AceitarDuelo(convite.DesafianteId);
            }
            else
            {
                _estado.RecusarDuelo(convite.DesafianteId);
            }
        }
    }
}
