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

            var y = jogador.transform.position.y;
            var posFerreiro = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Ferreiro, y)
            );
            var posArmeiro = PosicaoNoChao(MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Armeiro, y));
            var posArmadureiro = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Armadureiro, y)
            );
            var posTaverneiro = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Taverneiro, y)
            );
            var posMerceeiro = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Merceeiro, y)
            );

            var ferreiro = MarcadorNpcEconomia.Criar(posFerreiro, TipoNpcEconomia.Ferreiro);
            var armeiro = MarcadorNpcEconomia.Criar(posArmeiro, TipoNpcEconomia.Armeiro);
            var armadureiro = MarcadorNpcEconomia.Criar(
                posArmadureiro,
                TipoNpcEconomia.Armadureiro
            );
            var taverneiro = MarcadorNpcEconomia.Criar(posTaverneiro, TipoNpcEconomia.Taverneiro);
            var merceeiro = MarcadorNpcEconomia.Criar(posMerceeiro, TipoNpcEconomia.Merceeiro);
            var posLeiloeiro = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.Leiloeiro, y)
            );
            var leiloeiro = MarcadorNpcEconomia.Criar(posLeiloeiro, TipoNpcEconomia.Leiloeiro);
            var posBancoGuilda = PosicaoNoChao(
                MarcadorNpcEconomia.Posicao(TipoNpcEconomia.BancoGuilda, y)
            );
            var bancoGuilda = MarcadorNpcEconomia.Criar(
                posBancoGuilda,
                TipoNpcEconomia.BancoGuilda
            );

            SceneManager.MoveGameObjectToScene(ferreiro, cena);
            SceneManager.MoveGameObjectToScene(armeiro, cena);
            SceneManager.MoveGameObjectToScene(armadureiro, cena);
            SceneManager.MoveGameObjectToScene(taverneiro, cena);
            SceneManager.MoveGameObjectToScene(merceeiro, cena);
            SceneManager.MoveGameObjectToScene(leiloeiro, cena);
            SceneManager.MoveGameObjectToScene(bancoGuilda, cena);

            ferreiro.transform.SetParent(raiz.transform, true);
            armeiro.transform.SetParent(raiz.transform, true);
            armadureiro.transform.SetParent(raiz.transform, true);
            taverneiro.transform.SetParent(raiz.transform, true);
            merceeiro.transform.SetParent(raiz.transform, true);
            leiloeiro.transform.SetParent(raiz.transform, true);
            bancoGuilda.transform.SetParent(raiz.transform, true);

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel("PainelEconomia", -1f);
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "EconomiaInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            var controle = raiz.AddComponent<ControleInteracaoEconomia>();
            controle.Configurar(
                jogador,
                container,
                new PontoInteracaoEconomia(
                    ferreiro.transform,
                    "falar com o ferreiro",
                    UI.JanelaFerreiro.Chave
                ),
                new PontoInteracaoEconomia(
                    armeiro.transform,
                    "falar com o armeiro",
                    UI.JanelaVendedor.ChaveArmeiro
                ),
                new PontoInteracaoEconomia(
                    armadureiro.transform,
                    "falar com a armadureira",
                    UI.JanelaVendedor.ChaveArmadureiro
                ),
                new PontoInteracaoEconomia(
                    taverneiro.transform,
                    "falar com o taverneiro",
                    UI.JanelaVendedor.ChaveTaverneiro
                ),
                new PontoInteracaoEconomia(
                    merceeiro.transform,
                    "falar com o merceeiro",
                    UI.JanelaVendedor.ChaveMerceeiro
                ),
                new PontoInteracaoEconomia(
                    leiloeiro.transform,
                    "falar com o leiloeiro",
                    UI.JanelaLeilao.Chave
                ),
                new PontoInteracaoEconomia(
                    bancoGuilda.transform,
                    "abrir o banco da guilda",
                    UI.JanelaBancoGuilda.Chave
                )
            );
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
