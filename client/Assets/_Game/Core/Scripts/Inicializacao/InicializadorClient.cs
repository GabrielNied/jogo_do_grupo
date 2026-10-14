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

        private static PanelSettings _painel;

        // AfterSceneLoad: roda depois do BeforeSceneLoad em que as funcionalidades registram as telas.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inicializar()
        {
            Navegacao.Registrar(TelasCore.Abertura, () => new TelaAbertura());
            Navegacao.Registrar(TelasCore.Carregamento, () => new TelaCarregamento());
            Navegacao.Registrar(TelasCore.Login, () => new TelaLogin());
            Navegacao.Registrar(TelasCore.SemTelaAposLogin, () => new TelaSemTelaAposLogin());

            // Antes de qualquer chamada ao servidor (a sessão restaurada pode chamar a API).
            ConfiguracaoServidor.Aplicar();
            OpcoesGravadas.Carregar();
            Navegacao.Inicializar(CriarInterface());
            Sessao.Restaurar();
            Sessao.Expirou += AoExpirarSessao;

            Navegacao.Abrir(TelasCore.Abertura);
        }

        private static VisualElement CriarInterface()
        {
            // Criado em código: DontSave para não ir para cena/asset, e destruído ao sair do Play
            // para não acumular instâncias órfãs no editor com o domain reload desligado.
            var painel = EstiloCore.CriarConfiguracaoDoPainel("PainelInterface", 0f);
            _painel = painel;
            Application.quitting += AoSair;

            // Inativo enquanto configura, para o UIDocument já ativar com o painel definido.
            var objeto = new GameObject(NomeObjeto);
            objeto.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(objeto);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = painel;
            objeto.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);

            var raizDocumento = documento.rootVisualElement;
            EstiloCore.NaoBloquearCliques(raizDocumento);

            var container = new VisualElement { name = "Telas" };
            container.AddToClassList(EstiloCore.RaizInterface);
            EstiloCore.NaoBloquearCliques(container);
            raizDocumento.Add(container);
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
