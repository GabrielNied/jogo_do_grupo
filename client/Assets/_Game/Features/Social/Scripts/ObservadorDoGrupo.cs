using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Percebe que o jogador entrou no mundo: diz ao <see cref="EstadoDoGrupo"/> quem é o
    /// personagem (e o esquece ao sair), avança o tempo dos convites e monta na cena do jogador
    /// o aviso por cima do jogo. Social não toma o construtor de mundo, que é do Mundo: o
    /// <see cref="EntradaNoJogo"/> não avisa quando entra, então este objeto confere
    /// <see cref="EntradaNoJogo.NoJogo"/> a cada frame.
    /// </summary>
    internal sealed class ObservadorDoGrupo : MonoBehaviour
    {
        private const string NomeObservador = "Social.Observador";
        private const string NomeRaiz = "Social";

        private static ObservadorDoGrupo _instancia;

        private GameObject _jogadorAtendido;
        private PainelListaGrupoHud _painelHud;
        private VisualElement _regiaoAtendida;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDoGrupo>();
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, o campo estático sobrevive.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia?.DescartarHud();
            _instancia = null;
            EstadoMarcadoresTaticos.Instancia?.Limpar();
        }

        private void OnDestroy()
        {
            DescartarHud();
        }

        private void Update()
        {
            var estado = GrupoDoClient.Estado;
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null)
            {
                // Fora do mundo não há grupo: esquece o do personagem que saiu.
                _jogadorAtendido = null;
                estado.DefinirPersonagem(0);
                EstadoMarcadoresTaticos.Instancia.Limpar();
                DescartarHud();
                return;
            }

            estado.DefinirPersonagem(EntradaNoJogo.PersonagemId);
            estado.Atualizar(Time.unscaledDeltaTime);
            GarantirHud(estado);

            if (jogador == _jogadorAtendido)
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
                // O aviso com defeito não pode derrubar a entrada no mundo.
                Debug.LogException(e);
            }
        }

        private static void Montar(GameObject jogador)
        {
            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);

            var configuracao = CriarConfiguracaoDoPainel();
            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = configuracao;
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);

            var container = new VisualElement { name = "SocialInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            EstiloSocial.Aplicar(container);
            documento.rootVisualElement.Add(container);

            var aviso = raiz.AddComponent<AvisoDoGrupoNaTela>();
            aviso.Configurar(container, configuracao);

            ApresentadorDeMarcadoresTaticos.Criar(jogador.scene);
        }

        private static PanelSettings CriarConfiguracaoDoPainel()
        {
            // Abaixo da interface do Core (ordem 0), para o menu do Esc ficar por cima.
            return EstiloCore.CriarConfiguracaoDoPainel("PainelSocial", -1f);
        }

        private void GarantirHud(EstadoDoGrupo estado)
        {
            var regiao = Hud.Regiao(RegiaoDoHud.LateralEsquerda);
            if (regiao == null)
            {
                return;
            }

            if (_regiaoAtendida == regiao && _painelHud != null)
            {
                return;
            }

            DescartarHud();
            _painelHud = new PainelListaGrupoHud(regiao, estado);
            _regiaoAtendida = regiao;
        }

        private void DescartarHud()
        {
            _painelHud?.Descartar();
            _painelHud = null;
            _regiaoAtendida = null;
        }
    }
}
