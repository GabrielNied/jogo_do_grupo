using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Observa a entrada do jogador no mundo e instancia o ApresentadorDeNpcs na mesma cena (#1063).
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Npcs.Observador";
        private const string NomeRaiz = "Npcs";

        private static ObservadorDoMundo _instancia;
        private GameObject _jogadorAtendido;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDoMundo>();
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

        private static void Montar(GameObject jogador)
        {
            var raiz = new GameObject(NomeRaiz);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);
            raiz.AddComponent<ApresentadorDeNpcs>();
            EtiquetasDeNpcs.Criar(jogador.scene);
        }
    }
}
