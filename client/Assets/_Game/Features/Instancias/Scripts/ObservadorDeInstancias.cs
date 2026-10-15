using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// Percebe que o jogador entrou no mundo e monta ali a porta da Mina do Reflexo, os salões e
    /// a interface (dica, resultado), na mesma cena do jogador: o "Sair" descarrega tudo junto.
    /// Instâncias não toma o construtor de mundo, que é do Mundo (mesmo padrão de Itens e Mobs).
    /// </summary>
    internal sealed class ObservadorDeInstancias : MonoBehaviour
    {
        private const string NomeObservador = "Instancias.Observador";
        private const string NomeRaiz = "Instancias";

        private static ObservadorDeInstancias _instancia;

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
            _instancia = objeto.AddComponent<ObservadorDeInstancias>();
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, o campo estático sobrevive.
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
                // A dungeon com defeito não pode derrubar a entrada no mundo.
                Debug.LogException(e);
            }
        }

        private static void Montar(GameObject jogador)
        {
            var cena = jogador.scene;
            ConstrutorDaEntrada.Montar(cena);
            ConstrutorDosSaloes.Montar(cena);

            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, cena);

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelInstancias",
                -3.5f
            );
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);
            var container = new VisualElement { name = "InstanciasInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            raiz.AddComponent<ControleDaEntrada>()
                .Configurar(jogador, container, InstanciasDoClient.Estado);
        }
    }
}
