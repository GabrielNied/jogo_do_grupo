using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Percebe que o jogador entrou no mundo e monta ali o apresentador dos inimigos, na mesma
    /// cena do jogador (o "Sair" descarrega tudo junto). Mesmo padrão do Combate.
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Mobs.Observador";
        private const string NomeRaiz = "Mobs";

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
                // Inimigos com defeito não podem derrubar a entrada no mundo.
                Debug.LogException(e);
            }
        }

        private static void Montar(GameObject jogador)
        {
            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = CriarConfiguracaoDoPainel();
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "MobsInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            raiz.AddComponent<ApresentadorDeInimigos>().Configurar(new PainelDeInimigos(container));
        }

        private static PanelSettings CriarConfiguracaoDoPainel()
        {
            var painel = ScriptableObject.CreateInstance<PanelSettings>();
            painel.name = "PainelMobs";
            painel.hideFlags = HideFlags.DontSave;
            EstiloCore.ConfigurarEscala(painel);

            // Abaixo das barras do Combate (-2), do painel de Itens e da interface do Core (0).
            painel.sortingOrder = -3f;

            var tema = Resources.Load<ThemeStyleSheet>(EstiloCore.CaminhoTema);
            if (tema != null)
            {
                painel.themeStyleSheet = tema;
            }
            else
            {
                Debug.LogError(
                    $"[Mobs] Tema nao encontrado em Resources/{EstiloCore.CaminhoTema}.tss."
                );
            }

            return painel;
        }
    }
}
