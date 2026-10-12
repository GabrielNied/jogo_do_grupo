#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Desenvolvimento;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace JogoDoGrupo.Client.Features.Desenvolvimento
{
    /// <summary>
    /// F12 abre o console de teste no editor e no build de desenvolvimento. O texto vai ao
    /// servidor; a resposta volta em <see cref="ResultadoDeComandoDeTeste"/>.
    /// </summary>
    internal sealed class ObservadorDoConsole : MonoBehaviour
    {
        private const string NomeObservador = "Desenvolvimento.ConsoleObservador";

        private static ObservadorDoConsole _instancia;

        private ConsoleDeTeste _console;
        private InputAction _f12;
        private bool _jogoPausadoPeloConsole;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDoConsole>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Awake()
        {
            var configuracao = CriarConfiguracaoDoPainel();
            var documento = gameObject.AddComponent<UIDocument>();
            documento.panelSettings = configuracao;
            EstiloCore.Aplicar(documento.rootVisualElement);
            documento.rootVisualElement.pickingMode = PickingMode.Ignore;

            var container = new VisualElement
            {
                name = "Desenvolvimento",
                pickingMode = PickingMode.Ignore,
            };
            container.AddToClassList(EstiloCore.RaizInterface);
            documento.rootVisualElement.Add(container);

            var dica = new Label("F12: comandos de teste");
            dica.AddToClassList(EstiloCore.Dica);
            dica.pickingMode = PickingMode.Ignore;
            container.Add(dica);

            _console = new ConsoleDeTeste();
            _console.Montar(container, Enviar);
            _console.EscApertado += Fechar;

            _f12 = new InputAction("ConsoleDeTeste", InputActionType.Button, "<Keyboard>/f12");
            _f12.Enable();
            BloqueioDeEntradaPorTexto.RegistrarBloqueioDeAtalhos(() => _console.Aberta);
            ConexaoTempoReal.Assinar<ResultadoDeComandoDeTeste>(
                DesenvolvimentoMessageIds.ResultadoDeComandoDeTeste,
                _console.Mostrar
            );
        }

        private void OnDestroy()
        {
            _f12?.Dispose();
        }

        private void Update()
        {
            if (_f12 != null && _f12.WasPressedThisFrame())
            {
                if (_console.Aberta)
                {
                    Fechar();
                }
                else
                {
                    Abrir();
                }
            }
        }

        private void Abrir()
        {
            _console.Abrir();
            if (!EntradaNoJogo.NoJogo || JanelasDoJogo.AlgoAberto)
            {
                return;
            }

            DefinirJogoAtivo(false);
            _jogoPausadoPeloConsole = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void Fechar()
        {
            _console.Fechar();
            if (!_jogoPausadoPeloConsole)
            {
                return;
            }

            _jogoPausadoPeloConsole = false;
            if (
                BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados()
                || JanelasDoJogo.AlgoAberto
                || !EntradaNoJogo.NoJogo
            )
            {
                return;
            }

            DefinirJogoAtivo(true);
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private static void Enviar(string texto)
        {
            ConexaoTempoReal.Enviar(
                DesenvolvimentoMessageIds.PedirComandoDeTeste,
                new PedirComandoDeTeste { Texto = texto }
            );
        }

        private static void DefinirJogoAtivo(bool ativo)
        {
            var jogador = EntradaNoJogo.Jogador;
            var controle = jogador != null ? jogador.GetComponent<ControleTerceiraPessoa>() : null;
            if (controle != null)
            {
                controle.InputAtivo = ativo;
            }

            var camera = Camera.main;
            var controleDaCamera =
                camera != null ? camera.GetComponent<CameraTerceiraPessoa>() : null;
            if (controleDaCamera != null)
            {
                controleDaCamera.InputAtivo = ativo;
            }
        }

        private static PanelSettings CriarConfiguracaoDoPainel()
        {
            return EstiloCore.CriarConfiguracaoDoPainel("PainelDesenvolvimento", 1f);
        }
    }
}
#endif
