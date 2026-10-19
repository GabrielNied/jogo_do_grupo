using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Profissoes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Profissoes
{
    /// <summary>
    /// Monta os pontos de coleta e a forja na cena do jogador ao entrar no mundo (#192), pelo
    /// mesmo caminho dos NPCs de economia: sem cena nem prefab de feature.
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Profissoes.Observador";
        private const string NomeRaiz = "Profissoes";
        private const float AlturaDaBusca = 4f;
        private const float AlcanceDaBusca = 12f;

        private static ObservadorDoMundo _instancia;
        private GameObject _jogadorAtendido;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDoMundo>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => _instancia = null;

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null || jogador == _jogadorAtendido)
                return;

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

            var pontos = new List<PontoDeInteracaoProfissoes>();
            foreach (var coleta in ConfigProfissoes.Pontos)
            {
                var posicao = NoChao(new Vector3(coleta.X, 0f, coleta.Z));
                var marcador = MarcadorDeProfissoes.CriarPonto(coleta, posicao);
                SceneManager.MoveGameObjectToScene(marcador, cena);
                marcador.transform.SetParent(raiz.transform, true);
                pontos.Add(
                    new PontoDeInteracaoProfissoes
                    {
                        Transform = marcador.transform,
                        Coleta = coleta,
                    }
                );
            }

            var forja = MarcadorDeProfissoes.CriarForja(
                NoChao(new Vector3(ConfigProfissoes.ForjaX, 0f, ConfigProfissoes.ForjaZ))
            );
            SceneManager.MoveGameObjectToScene(forja, cena);
            forja.transform.SetParent(raiz.transform, true);
            pontos.Add(new PontoDeInteracaoProfissoes { Transform = forja.transform });

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel("PainelProfissoes", -1f);
            raiz.SetActive(true);
            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "ProfissoesInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            raiz.AddComponent<ControleInteracaoProfissoes>().Configurar(jogador, container, pontos);
        }

        // A altura do mapa (a mesma do servidor) dá o chão; o raio só acerta o que o terreno do
        // cliente desenhar de diferente.
        private static Vector3 NoChao(Vector3 posicao)
        {
            posicao.y = MapaDoMundo.AlturaEm(posicao.x, posicao.z);
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
                posicao.y = batida.point.y;
            return posicao;
        }
    }
}
