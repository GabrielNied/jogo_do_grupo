using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Percebe que o jogador entrou no mundo e monta ali as barras de recursos, na mesma cena do
    /// jogador (o "Sair" descarrega tudo junto). Mesmo padrão do observador de Itens.
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Combate.Observador";
        private const string NomeRaiz = "Combate";

        private static readonly Vector2Int ResolucaoReferencia = new Vector2Int(1920, 1080);

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
                Montar(jogador, EntradaNoJogo.PersonagemId);
            }
            catch (System.Exception e)
            {
                // Combate com defeito não pode derrubar a entrada no mundo.
                Debug.LogException(e);
            }
        }

        private static void Montar(GameObject jogador, long personagemId)
        {
            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);

            var configuracao = CriarConfiguracaoDoPainel();
            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = configuracao;
            raiz.SetActive(true);

            var estilo = Resources.Load<StyleSheet>(EstiloCore.CaminhoEstilo);
            if (estilo != null)
            {
                documento.rootVisualElement.styleSheets.Add(estilo);
            }

            var container = new VisualElement { name = "CombateInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            var painel = new PainelRecursos(container);
            var controle = raiz.AddComponent<ControleRecursos>();
            controle.Configurar(personagemId, painel);
        }

        private static PanelSettings CriarConfiguracaoDoPainel()
        {
            var painel = ScriptableObject.CreateInstance<PanelSettings>();
            painel.name = "PainelCombate";
            painel.hideFlags = HideFlags.DontSave;
            painel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            painel.referenceResolution = ResolucaoReferencia;
            painel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            painel.match = 0.5f;

            // Abaixo da interface do Core (ordem 0) e do painel de Itens.
            painel.sortingOrder = -2f;

            var tema = Resources.Load<ThemeStyleSheet>(EstiloCore.CaminhoTema);
            if (tema != null)
            {
                painel.themeStyleSheet = tema;
            }
            else
            {
                Debug.LogError(
                    $"[Combate] Tema nao encontrado em Resources/{EstiloCore.CaminhoTema}.tss."
                );
            }

            return painel;
        }
    }
}
