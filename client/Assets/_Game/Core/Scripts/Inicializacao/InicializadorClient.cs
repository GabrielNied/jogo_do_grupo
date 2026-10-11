using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Inicializacao
{
    /// <summary>
    /// Sobe o client sem depender de cena (ADR 0011): cria um objeto persistente com
    /// <see cref="UIDocument"/> e <see cref="PanelSettings"/> em código, registra as telas do Core
    /// e abre a abertura. Roda em qualquer cena que der Play.
    /// </summary>
    internal static class InicializadorClient
    {
        private const string NomeObjeto = "JogoDoGrupo.Interface";
        private const string AvisoSessaoExpirada = "Sua sessão expirou. Entre de novo.";
        private static readonly Vector2Int ResolucaoReferencia = new Vector2Int(1920, 1080);

        private static PanelSettings _painel;

        // AfterSceneLoad: roda depois do BeforeSceneLoad em que as funcionalidades registram as telas.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inicializar()
        {
            Navegacao.Registrar(TelasCore.Abertura, () => new TelaAbertura());
            Navegacao.Registrar(TelasCore.Login, () => new TelaLogin());
            Navegacao.Registrar(TelasCore.SemTelaAposLogin, () => new TelaSemTelaAposLogin());

            Navegacao.Inicializar(CriarInterface());
            Sessao.Restaurar();
            Sessao.Expirou += AoExpirarSessao;

            Navegacao.Abrir(TelasCore.Abertura);
        }

        private static VisualElement CriarInterface()
        {
            // Criado em código: DontSave para não ir para cena/asset, e destruído ao sair do Play
            // para não acumular instâncias órfãs no editor com o domain reload desligado.
            var painel = ScriptableObject.CreateInstance<PanelSettings>();
            painel.name = "PainelInterface";
            painel.hideFlags = HideFlags.DontSave;
            _painel = painel;
            Application.quitting += AoSair;
            painel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            painel.referenceResolution = ResolucaoReferencia;
            painel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            painel.match = 0.5f;

            var tema = Resources.Load<ThemeStyleSheet>(EstiloCore.CaminhoTema);
            if (tema != null)
            {
                painel.themeStyleSheet = tema;
            }
            else
            {
                Debug.LogError(
                    $"[InicializadorClient] Tema não encontrado em Resources/{EstiloCore.CaminhoTema}.tss."
                );
            }

            // Inativo enquanto configura, para o UIDocument já ativar com o painel definido.
            var objeto = new GameObject(NomeObjeto);
            objeto.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(objeto);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = painel;
            objeto.SetActive(true);

            var estilo = Resources.Load<StyleSheet>(EstiloCore.CaminhoEstilo);
            if (estilo != null)
            {
                documento.rootVisualElement.styleSheets.Add(estilo);
            }
            else
            {
                Debug.LogError(
                    $"[InicializadorClient] Estilo não encontrado em Resources/{EstiloCore.CaminhoEstilo}.uss."
                );
            }

            var container = new VisualElement { name = "Telas" };
            container.AddToClassList(EstiloCore.RaizInterface);
            documento.rootVisualElement.Add(container);
            return container;
        }

        private static void AoSair()
        {
            Application.quitting -= AoSair;
            Sessao.Expirou -= AoExpirarSessao;
            if (_painel != null)
            {
                // Instância de runtime (não asset): destrói na hora, sem depender do fim do frame.
                UnityEngine.Object.DestroyImmediate(_painel);
            }

            _painel = null;
        }

        private static void AoExpirarSessao()
        {
            // Se estava no mundo, desmonta antes de mostrar o login.
            EntradaNoJogo.Desmontar();
            Navegacao.VoltarParaLogin(AvisoSessaoExpirada);
        }
    }
}
