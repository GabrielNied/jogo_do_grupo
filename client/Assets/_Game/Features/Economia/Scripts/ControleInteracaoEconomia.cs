using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Features.Economia.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia
{
    /// <summary>
    /// Dicas e tecla E nos marcadores de vendedor e ferreiro; abre as janelas correspondentes.
    /// </summary>
    internal sealed class ControleInteracaoEconomia : MonoBehaviour
    {
        private InputAction _interagir;
        private Label _dica;
        private GameObject _jogador;
        private Transform _vendedor;
        private Transform _ferreiro;

        public void Configurar(
            GameObject jogador,
            Transform vendedor,
            Transform ferreiro,
            VisualElement raizDaInterface
        )
        {
            _jogador = jogador;
            _vendedor = vendedor;
            _ferreiro = ferreiro;

            _dica = new Label("");
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            raizDaInterface.Add(_dica);
            MostrarDica(false);

            _interagir = new InputAction(
                "InteragirEconomia",
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
        }

        private void Update()
        {
            if (_dica == null || _jogador == null || _vendedor == null || _ferreiro == null)
            {
                return;
            }

            if (!EntradaNoJogo.NoJogo || JanelasDoJogo.MenuAberto)
            {
                MostrarDica(false);
                return;
            }

            var pos = _jogador.transform.position;
            var pertoVendedor = MarcadorNpcEconomia.EstaPerto(pos, _vendedor.position);
            var pertoFerreiro = MarcadorNpcEconomia.EstaPerto(pos, _ferreiro.position);

            if (!pertoVendedor && !pertoFerreiro)
            {
                MostrarDica(false);
                return;
            }

            var rotulo = TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir);
            if (pertoVendedor && pertoFerreiro)
            {
                var dv = DistanciaHorizontal(pos, _vendedor.position);
                var df = DistanciaHorizontal(pos, _ferreiro.position);
                _dica.text =
                    dv <= df
                        ? rotulo + ": falar com o comerciante"
                        : rotulo + ": falar com o ferreiro";
            }
            else if (pertoVendedor)
            {
                _dica.text = rotulo + ": falar com o comerciante";
            }
            else
            {
                _dica.text = rotulo + ": falar com o ferreiro";
            }

            MostrarDica(true);

            if (!_interagir.WasPressedThisFrame())
            {
                return;
            }

            if (pertoVendedor && pertoFerreiro)
            {
                var dv = DistanciaHorizontal(pos, _vendedor.position);
                var df = DistanciaHorizontal(pos, _ferreiro.position);
                if (dv <= df)
                {
                    JanelasDoJogo.Abrir(JanelaVendedor.Chave);
                }
                else
                {
                    JanelasDoJogo.Abrir(JanelaFerreiro.Chave);
                }
            }
            else if (pertoVendedor)
            {
                JanelasDoJogo.Abrir(JanelaVendedor.Chave);
            }
            else
            {
                JanelasDoJogo.Abrir(JanelaFerreiro.Chave);
            }
        }

        private static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private void MostrarDica(bool mostrar)
        {
            _dica.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
