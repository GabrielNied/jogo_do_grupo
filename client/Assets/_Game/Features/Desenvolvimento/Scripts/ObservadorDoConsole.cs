#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JogoDoGrupo.Client.Core.Jogo.Chat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Desenvolvimento
{
    /// <summary>
    /// F12 abre a caixa do chat (mesma do Enter), pronta para comandos com barra. Os comandos de
    /// teste são interpretados pelo chat (<see cref="RegistroDeComandosDeTesteNoChat"/>).
    /// </summary>
    internal sealed class ObservadorDoConsole : MonoBehaviour
    {
        private const string NomeObservador = "Desenvolvimento.ConsoleObservador";

        private static ObservadorDoConsole _instancia;

        private InputAction _f12;

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
            _f12 = new InputAction("ConsoleDeTeste", InputActionType.Button, "<Keyboard>/f12");
            _f12.Enable();
        }

        private void OnDestroy()
        {
            _f12?.Dispose();
        }

        private void Update()
        {
            if (_f12 != null && _f12.WasPressedThisFrame())
            {
                ChatDoJogo.PedirAlternar();
            }
        }
    }
}
#endif
