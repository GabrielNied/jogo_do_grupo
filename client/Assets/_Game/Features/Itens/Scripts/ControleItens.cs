using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// O baú no mundo, enquanto o jogador está nele: mostra a dica "E: abrir o baú" perto dele
    /// e, com E, abre a <see cref="JanelaInventario"/> pedindo o baú ao servidor. O inventário
    /// (tecla I), o Esc, o cursor e o personagem parado são das <see cref="JanelasDoJogo"/>.
    /// Criado pelo <see cref="ObservadorDoMundo"/> na cena do mundo, então o "Sair" descarrega
    /// tudo junto.
    /// </summary>
    internal sealed class ControleItens : MonoBehaviour
    {
        private const string TextoDicaBau = "E: abrir o baú";

        private InputAction _interagir;

        private Label _dica;
        private PanelSettings _configuracaoDoPainel;
        private Transform _bau;
        private GameObject _jogador;

        /// <summary>Liga o controle ao jogador e ao baú. Chamar antes de ativar.</summary>
        public void Configurar(
            GameObject jogador,
            Transform bau,
            VisualElement raizDaInterface,
            PanelSettings configuracaoDoPainel
        )
        {
            _jogador = jogador;
            _bau = bau;
            _configuracaoDoPainel = configuracaoDoPainel;

            // A dica não pode pegar o mouse nem esconder o jogo: texto solto embaixo da tela.
            _dica = new Label(TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir) + ": abrir o baú");
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            raizDaInterface.Add(_dica);
            MostrarDica(false);

            _interagir = new InputAction(
                "Interagir",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Interagir)
            );
            TeclasDoJogo.Mudou += AtualizarTecla;
            _interagir.AddBinding("<Gamepad>/buttonWest");

            if (isActiveAndEnabled)
            {
                _interagir.Enable();
            }
        }

        private void OnEnable()
        {
            _interagir?.Enable();
        }

        private void AtualizarTecla() =>
            _interagir?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Interagir));

        private void OnDisable()
        {
            _interagir?.Disable();
        }

        private void OnDestroy()
        {
            TeclasDoJogo.Mudou -= AtualizarTecla;
            _interagir?.Dispose();
            _interagir = null;

            if (_configuracaoDoPainel != null)
            {
                Destroy(_configuracaoDoPainel);
            }
        }

        private void Update()
        {
            if (_dica == null || _jogador == null || _bau == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            // Com janela ou menu do Esc aberto, não mostra a dica nem abre o baú por baixo.
            var perto =
                !JanelasDoJogo.AlgoAberto
                && BauNoMundo.EstaPerto(_jogador.transform.position, _bau.position);
            MostrarDica(perto);

            // Em recarga a dica mostra a espera e o E não pede nada (o servidor recusaria).
            var espera = JanelaInventario.BauLivreEm - Time.unscaledTime;
            var recarregando = espera > 0f;
            if (perto)
            {
                _dica.text = recarregando
                    ? "Baú recarregando (" + Mathf.CeilToInt(espera) + " s)"
                    : TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir) + ": abrir o baú";
            }

            if (perto && !recarregando && _interagir.WasPressedThisFrame())
            {
                JanelaInventario.AbrirComBau();
            }
        }

        private void MostrarDica(bool mostrar)
        {
            _dica.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
