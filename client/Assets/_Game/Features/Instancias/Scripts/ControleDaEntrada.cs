using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Instancias;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// A porta da dungeon em jogo: mostra "E: entrar" perto dela e, com E, pede
    /// <see cref="EntrarNaDungeon"/> ao servidor (que decide). Também leva as recusas ao aviso do
    /// HUD e o resultado da corrida ao painel. Criado pelo <see cref="ObservadorDeInstancias"/>
    /// na cena do mundo.
    /// </summary>
    internal sealed class ControleDaEntrada : MonoBehaviour
    {
        private InputAction _interagir;
        private Label _dica;
        private GameObject _jogador;
        private EstadoDeInstancias _estado;
        private TelaDeResultadoDaDungeon _tela;

        public void Configurar(
            GameObject jogador,
            VisualElement raizDaInterface,
            EstadoDeInstancias estado
        )
        {
            _jogador = jogador;
            _estado = estado;

            _dica = new Label();
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            raizDaInterface.Add(_dica);
            MostrarDica(false);

            _tela = new TelaDeResultadoDaDungeon(
                raizDaInterface,
                TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir)
            );

            _interagir = new InputAction(
                "InteragirDungeon",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Interagir)
            );
            TeclasDoJogo.Mudou += AtualizarTecla;
            _interagir.AddBinding("<Gamepad>/buttonWest");
            if (isActiveAndEnabled)
            {
                _interagir.Enable();
            }

            _estado.EntradaRecusada += AoRecusar;
            _estado.ChefeDerrotado += AoChefeDerrotado;
            _estado.DungeonEncerrada += AoEncerrar;
        }

        private void OnEnable()
        {
            _interagir?.Enable();
        }

        private void OnDisable()
        {
            _interagir?.Disable();
        }

        private void OnDestroy()
        {
            if (_estado != null)
            {
                _estado.EntradaRecusada -= AoRecusar;
                _estado.ChefeDerrotado -= AoChefeDerrotado;
                _estado.DungeonEncerrada -= AoEncerrar;
            }

            TeclasDoJogo.Mudou -= AtualizarTecla;
            _interagir?.Dispose();
            _interagir = null;
        }

        private void AtualizarTecla() =>
            _interagir?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Interagir));

        private static void AoRecusar(EntradaRecusada recusa) =>
            AvisosDoJogo.Mostrar(TextosInstancias.Recusa(recusa.Codigo), TipoDeAviso.Aviso);

        private static void AoChefeDerrotado(ChefeDerrotadoNaDungeon _) =>
            AvisosDoJogo.Mostrar(
                "Chefe derrotado! +1 Essência de instância obtida. Colete seus itens antes de sair.",
                TipoDeAviso.Informacao
            );

        private void AoEncerrar(DungeonEncerrada encerrada) => _tela?.Mostrar(encerrada.Resultado);

        private void Update()
        {
            if (_dica == null || _jogador == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            _tela.Atualizar();
            var apertou = _interagir.WasPressedThisFrame();
            if (_tela.Visivel && apertou)
            {
                _tela.Fechar();
                return;
            }

            if (_estado.NaInstancia && _estado.VitoriaAlcancada && !JanelasDoJogo.MenuAberto)
            {
                MostrarDica(true);
                _dica.text = TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir) + ": sair da dungeon";
                if (apertou)
                {
                    _estado.PedirSair();
                }
                return;
            }

            var posicao = _jogador.transform.position;
            var perto =
                !_estado.NaInstancia
                && !JanelasDoJogo.MenuAberto
                && LayoutDaMinaDoReflexo.PertoDaEntrada(posicao.x, posicao.z);
            MostrarDica(perto);
            if (perto)
            {
                _dica.text =
                    TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir)
                    + ": entrar na "
                    + TextosInstancias.NomeDaDungeon;
                if (apertou)
                {
                    ConexaoTempoReal.Enviar(
                        InstanciasMessageIds.EntrarNaDungeon,
                        new EntrarNaDungeon(),
                        EntregaTempoReal.Confiavel
                    );
                }
            }
        }

        private void MostrarDica(bool mostrar)
        {
            _dica.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
