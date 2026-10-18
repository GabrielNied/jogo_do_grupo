using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia
{
    /// <summary>
    /// Monta vendedor, ferreiro e interação na cena do jogador ao entrar no mundo (#173).
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Economia.Observador";
        private const string NomeRaiz = "Economia";
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
            var cena = jogador.scene;

            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, cena);

            var posVendedor = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Vendedor, jogador.transform.position.y)
            );
            var posFerreiro = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Ferreiro, jogador.transform.position.y)
            );

            var vendedor = MarcadorNpcEconomia.Criar(posVendedor, TipoNpcEconomia.Vendedor);
            var ferreiro = MarcadorNpcEconomia.Criar(posFerreiro, TipoNpcEconomia.Ferreiro);
            SceneManager.MoveGameObjectToScene(vendedor, cena);
            SceneManager.MoveGameObjectToScene(ferreiro, cena);
            vendedor.transform.SetParent(raiz.transform, true);
            ferreiro.transform.SetParent(raiz.transform, true);

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel("PainelEconomia", -1f);
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "EconomiaInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            var controle = raiz.AddComponent<ControleInteracaoEconomia>();
            controle.Configurar(jogador, vendedor.transform, ferreiro.transform, container);
        }

        private static Vector3 PosicaoNoChao(Vector3 posicao)
        {
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
    }
}
