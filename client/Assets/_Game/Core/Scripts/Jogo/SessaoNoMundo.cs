using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using VisualElement = UnityEngine.UIElements.VisualElement;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Fica no jogador local enquanto ele está no mundo: dono das ações de input, abre e fecha o
    /// menu do Esc e as janelas do jogo (<see cref="JanelasDoJogo"/>) e prende o cursor do mouse
    /// quando nada está aberto. Criada pela <see cref="EntradaNoJogo"/>.
    /// </summary>
    internal sealed class SessaoNoMundo : MonoBehaviour
    {
        private AcoesJogador _acoes;
        private ControleTerceiraPessoa _controle;
        private CameraTerceiraPessoa _camera;
        private MenuJogo _menu;
        private GerenteDeJanelas _janelas;
        private ApresentadorDeJanelas _apresentador;
        private AtalhosDeJanelas _atalhos;
        private BarraDeAtalhos _barra;

        public bool MenuAberto => _menu != null && _menu.Aberto;

        public void Configurar(
            ControleTerceiraPessoa controle,
            CameraTerceiraPessoa camera,
            AnimadorDoVisual animador
        )
        {
            _acoes = new AcoesJogador();
            _controle = controle;
            _camera = camera;
            _menu = new MenuJogo(FecharMenu);

            var registro = JanelasDoJogo.Registro;
            _apresentador = new ApresentadorDeJanelas(Navegacao.Container, FecharJanela);
            _janelas = new GerenteDeJanelas(
                registro,
                _apresentador.Mostrar,
                _apresentador.Esconder
            );
            _janelas.Mudou += Sincronizar;
            _atalhos = new AtalhosDeJanelas(registro.Definicoes);
            if (Navegacao.Container != null)
            {
                _barra = new BarraDeAtalhos(Navegacao.Container, registro.Definicoes);
            }

            controle.Configurar(_acoes, camera, animador);
            camera.Configurar(_acoes, controle.transform);

            if (isActiveAndEnabled)
            {
                _acoes.Ativar();
                _atalhos.Ativar();
                JanelasDoJogo.Ligar(_janelas);
            }

            Sincronizar();
        }

        private void OnEnable()
        {
            _acoes?.Ativar();
            _atalhos?.Ativar();
            if (_janelas != null)
            {
                JanelasDoJogo.Ligar(_janelas);
            }

            if (_barra != null && _barra.Raiz.parent == null)
            {
                Navegacao.Container?.Add(_barra.Raiz);
            }
        }

        private void OnDisable()
        {
            _acoes?.Desativar();
            _atalhos?.Desativar();
            if (_janelas != null)
            {
                JanelasDoJogo.Desligar(_janelas);
                _janelas.Mudou -= Sincronizar;
                _janelas.Fechar();
                _janelas.DefinirMenuAberto(false);
                _janelas.Mudou += Sincronizar;
            }

            _menu?.Fechar();
            // Sai da tela já: a cena do mundo só termina de descarregar depois.
            _barra?.Remover();
            LiberarCursor();
        }

        private void OnDestroy()
        {
            _acoes?.Dispose();
            _acoes = null;
            _atalhos?.Dispose();
            _atalhos = null;
            _apresentador?.Esconder();
            _barra?.Remover();
            _barra = null;
            if (_janelas != null)
            {
                _janelas.Mudou -= Sincronizar;
            }
        }

        private void Update()
        {
            if (_acoes == null || _janelas == null)
            {
                return;
            }

            var hud = Navegacao.Container;
            if (_acoes.MenuApertado)
            {
                if (EntradaDeTextoAtiva(hud))
                {
                    if (FocoEmCampoDeTexto.TemCampoComFoco(hud))
                    {
                        FocoEmCampoDeTexto.SoltarFoco(hud);
                    }

                    if (BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame)
                    {
                        BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame = false;
                    }

                    return;
                }

                if (BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame)
                {
                    BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame = false;
                    return;
                }

                if (_menu != null && _menu.OpcoesAbertas)
                {
                    _menu.VoltarDoSubmenu();
                }
                else
                {
                    _janelas.ApertarEsc();
                }
                return;
            }

            if (EntradaDeTextoAtiva(hud))
            {
                return;
            }

            var chave = _atalhos?.ChaveApertada();
            if (chave != null)
            {
                _janelas.Alternar(chave);
            }
        }

        private static bool EntradaDeTextoAtiva(VisualElement hud) =>
            FocoEmCampoDeTexto.TemCampoComFoco(hud)
            || BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados();

        private void FecharMenu()
        {
            _janelas?.DefinirMenuAberto(false);
        }

        private void FecharJanela()
        {
            _janelas?.Fechar();
        }

        /// <summary>
        /// Deixa o menu, o controle, a câmera, o cursor e a barra de atalhos de acordo com o que
        /// está aberto. Chamado a cada mudança.
        /// </summary>
        private void Sincronizar()
        {
            if (_janelas.MenuAberto)
            {
                _menu.Abrir();
            }
            else
            {
                _menu.Fechar();
            }

            if (_barra != null)
            {
                _barra.Destacar(_janelas.ChaveAberta);
                if (_janelas.ChaveAberta != null)
                {
                    _barra.TrazerParaFrente();
                }
            }

            var livre = !_janelas.AlgoAberto;
            DefinirInputAtivo(livre);
            if (livre)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                LiberarCursor();
            }
        }

        private void DefinirInputAtivo(bool ativo)
        {
            if (_controle != null)
            {
                _controle.InputAtivo = ativo;
            }

            if (_camera != null)
            {
                _camera.InputAtivo = ativo;
            }
        }

        private static void LiberarCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
