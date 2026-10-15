using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>
    /// Percebe que o jogador entrou no mundo e monta ali a barra de skills, na mesma cena do
    /// jogador (o "Sair" descarrega tudo junto). Mesmo padrão dos observadores de Combate e Itens.
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Skills.Observador";
        private const string NomeRaiz = "Skills";

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
                // Skills com defeito não pode derrubar a entrada no mundo.
                Debug.LogException(e);
            }
        }

        private static void Montar(GameObject jogador, long personagemId)
        {
            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);

            var documento = raiz.AddComponent<UIDocument>();
            // Abaixo da interface do Core (ordem 0) e perto do painel de Combate.
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel("PainelSkills", -1.5f);
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "SkillsInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);

            raiz.AddComponent<ControleSkills>()
                .Configurar(personagemId, new PainelSkills(container));
        }
    }
}
