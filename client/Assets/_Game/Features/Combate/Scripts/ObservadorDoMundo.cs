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

            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "CombateInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            var regiaoRecursos = Hud.Regiao(RegiaoDoHud.InferiorEsquerda) ?? container;
            var painel = new PainelRecursos(regiaoRecursos);
            var painelStatus = new PainelEfeitosDeStatus(regiaoRecursos);
            var controleStatus = raiz.AddComponent<ControleEfeitosDeStatus>();
            controleStatus.Configurar(personagemId, painelStatus);

            var regiaoTopo = Hud.Regiao(RegiaoDoHud.TopoCentro) ?? container;
            var painelQuebra = new PainelBarraDeQuebra(regiaoTopo);
            var controleQuebra = raiz.AddComponent<ControleBarraDeQuebra>();
            controleQuebra.Configurar(0, painelQuebra);

            var prontidao = new ProntidaoLocal();
            var controle = raiz.AddComponent<ControleRecursos>();
            controle.Configurar(personagemId, painel, prontidao);

            var impacto = raiz.AddComponent<ControleImpacto>();
            impacto.Configurar(personagemId, new PainelImpacto(container));

            var ataque = raiz.AddComponent<ControleAtaque>();
            ataque.Configurar(
                personagemId,
                new PainelAtaque(container),
                controle,
                impacto,
                prontidao
            );

            raiz.AddComponent<ControleEsquiva>().Configurar(personagemId, prontidao);
            raiz.AddComponent<ControleDefesa>()
                .Configurar(personagemId, prontidao, new PainelDefesa(container));

            // O boneco de treino da cidade (o servidor o trata como alvo fixo, id negativo).
            var boneco = BonecoDeTreinoVisual.Criar();
            SceneManager.MoveGameObjectToScene(boneco.gameObject, jogador.scene);
        }

        private static PanelSettings CriarConfiguracaoDoPainel()
        {
            // Abaixo da interface do Core (ordem 0) e do painel de Itens.
            return EstiloCore.CriarConfiguracaoDoPainel("PainelCombate", -2f);
        }
    }
}
