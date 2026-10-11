using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// A linha por cima do jogo com a janela fechada: o convite que espera resposta ("O:
    /// responder") e as mensagens do grupo. Criada pelo <see cref="ObservadorDoGrupo"/> na cena
    /// do mundo, então o "Sair" descarrega tudo junto.
    /// </summary>
    internal sealed class AvisoDoGrupoNaTela : MonoBehaviour
    {
        private Label _aviso;
        private PanelSettings _configuracaoDoPainel;

        /// <summary>Liga o aviso à interface. Chamar antes de ativar.</summary>
        public void Configurar(VisualElement raizDaInterface, PanelSettings configuracaoDoPainel)
        {
            _configuracaoDoPainel = configuracaoDoPainel;

            // O aviso não pode pegar o mouse nem esconder o jogo: texto solto embaixo da tela.
            _aviso = new Label();
            _aviso.pickingMode = PickingMode.Ignore;
            _aviso.AddToClassList(EstiloCore.Dica);
            _aviso.AddToClassList(EstiloSocial.AvisoNaTela);
            raizDaInterface.Add(_aviso);
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
            if (_aviso == null)
            {
                return;
            }

            // Com janela ou menu aberto, o convite aparece dentro da janela do grupo.
            var texto = JanelasDoJogo.AlgoAberto
                ? ""
                : ApresentacaoGrupo.TextoNaTela(GrupoDoClient.Estado, JanelaGrupo.Tecla);
            Mostrar(texto);
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
