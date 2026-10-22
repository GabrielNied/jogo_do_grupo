using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>
    /// Instancia e liga os componentes de PvP quando o jogador entra no jogo.
    /// </summary>
    internal sealed class ObservadorDePvp : MonoBehaviour
    {
        private const string NomeObservador = "Pvp.Observador";

        private static ObservadorDePvp _instancia;
        private GameObject _jogadorAtendido;
        private EstadoDePvp _estado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDePvp>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null || jogador == _jogadorAtendido)
            {
                return;
            }

            _jogadorAtendido = jogador;
            try
            {
                Montar(jogador);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Montar(GameObject jogador)
        {
            if (_estado == null)
            {
                _estado = new EstadoDePvp();
            }

            var controle = jogador.AddComponent<ControleDePvp>();
            controle.Configurar(_estado);
            ConviteDeDueloNaTela.Criar(jogador, _estado);
        }

        private void OnDestroy()
        {
            _estado?.Dispose();
            _estado = null;
        }
    }
}
