using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Percebe que o jogador entrou no mundo e monta ali o baú de teste e a dica do baú,
    /// na mesma cena do jogador (o "Sair" descarrega tudo junto). Itens não toma o construtor de
    /// mundo, que é do Mundo: o <see cref="EntradaNoJogo"/> não avisa quando entra, então este
    /// objeto confere <see cref="EntradaNoJogo.NoJogo"/> a cada frame.
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Itens.Observador";
        private const string NomeRaiz = "Itens";
        private const float AlturaDaBusca = 3f;
        private const float AlcanceDaBusca = 10f;

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
                Montar(jogador, EntradaNoJogo.PersonagemId);
            }
            catch (System.Exception e)
            {
                // Itens com defeito não pode derrubar a entrada no mundo.
                Debug.LogException(e);
            }
        }

        private static void Montar(GameObject jogador, long personagemId)
        {
            var cena = jogador.scene;

            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, cena);

            var posicaoDoBau = PosicaoDoBau(jogador.transform);
            var bau = BauNoMundo.Criar(posicaoDoBau);
            SceneManager.MoveGameObjectToScene(bau, cena);
            bau.transform.SetParent(raiz.transform, true);
            var olhar = jogador.transform.position - posicaoDoBau;
            olhar.y = 0f;
            if (olhar.sqrMagnitude > 0.0001f)
            {
                bau.transform.rotation = Quaternion.LookRotation(olhar);
            }

            var configuracao = CriarConfiguracaoDoPainel();
            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = configuracao;
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "ItensInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            var controle = raiz.AddComponent<ControleItens>();
            controle.Configurar(jogador, bau.transform, container, configuracao);
        }

        /// <summary>Baú à frente e à direita do jogador, com a base no chão (se achar).</summary>
        private static Vector3 PosicaoDoBau(Transform jogador)
        {
            var posicao = BauNoMundo.PosicaoPerto(jogador.position, jogador.forward, jogador.right);

            var origem = posicao + Vector3.up * AlturaDaBusca;
            if (
                Physics.Raycast(
                    origem,
                    Vector3.down,
                    out var batida,
                    AlcanceDaBusca,
                    ~0,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                posicao.y = batida.point.y;
            }

            return posicao;
        }

        private static CameraTerceiraPessoa ProcurarCamera(Scene cena)
        {
            foreach (var raiz in cena.GetRootGameObjects())
            {
                var camera = raiz.GetComponentInChildren<CameraTerceiraPessoa>(true);
                if (camera != null)
                {
                    return camera;
                }
            }

            return null;
        }

        private static PanelSettings CriarConfiguracaoDoPainel()
        {
            // Criado em código, como o do Core: DontSave para não ir para cena nem asset.
            var painel = ScriptableObject.CreateInstance<PanelSettings>();
            painel.name = "PainelItens";
            painel.hideFlags = HideFlags.DontSave;
            painel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            painel.referenceResolution = EstiloCore.ResolucaoReferencia;
            painel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            painel.match = 0.5f;

            // Abaixo da interface do Core (ordem 0), para o menu do Esc ficar por cima.
            painel.sortingOrder = -1f;

            var tema = Resources.Load<ThemeStyleSheet>(EstiloCore.CaminhoTema);
            if (tema != null)
            {
                painel.themeStyleSheet = tema;
            }
            else
            {
                Debug.LogError(
                    $"[Itens] Tema nao encontrado em Resources/{EstiloCore.CaminhoTema}.tss."
                );
            }

            return painel;
        }
    }
}
