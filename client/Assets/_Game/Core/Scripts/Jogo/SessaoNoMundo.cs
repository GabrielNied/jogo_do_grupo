using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using DisplayStyle = UnityEngine.UIElements.DisplayStyle;
using VisualElement = UnityEngine.UIElements.VisualElement;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Fica no jogador local enquanto ele está no mundo: dono das ações de input, abre e fecha o
    /// menu do Esc e as janelas do jogo (<see cref="JanelasDoJogo"/>) e prende o cursor do mouse
    /// quando nada está aberto. Com janela aberta o personagem anda, o mouse vira cursor e a
    /// câmera só gira segurando o botão direito fora das janelas. Criada pela <see cref="EntradaNoJogo"/>.
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
        private bool _girando;

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
            _apresentador = new ApresentadorDeJanelas(
                Navegacao.Container,
                FecharJanela,
                FocarJanela
            );
            _janelas = new GerenteDeJanelas(
                registro,
                _apresentador.Mostrar,
                _apresentador.Esconder,
                _apresentador.Focar
            );
            _janelas.Mudou += Sincronizar;
            ConexaoTempoReal.EstadoMudou += AoMudarConexao;
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
                _janelas.FecharTodas();
                _janelas.DefinirMenuAberto(false);
                _janelas.Mudou += Sincronizar;
            }

            _menu?.Fechar();
            _girando = false;
            Hud.DefinirVisivel(true);
            // Sai da tela já: a cena do mundo só termina de descarregar depois.
            _barra?.Remover();
            LiberarCursor();
        }

        private void OnDestroy()
        {
            ConexaoTempoReal.EstadoMudou -= AoMudarConexao;
            _acoes?.Dispose();
            _acoes = null;
            _atalhos?.Dispose();
            _atalhos = null;
            _apresentador?.Esconder();
            _barra?.Destruir();
            _barra = null;
            if (_janelas != null)
            {
                _janelas.Mudou -= Sincronizar;
            }
        }

        private void Update()
        {
            if (EntradaNoJogo.EntradaPendente || !ConexaoTempoReal.Conectado)
            {
                return;
            }

            if (_acoes == null || _janelas == null)
            {
                return;
            }

            AtualizarGiroComJanela();

            var hud = Navegacao.Container;
            if (_acoes.MenuApertado)
            {
                if (_menu != null && (_menu.CapturaAtiva || _menu.CancelouCapturaNesteFrame))
                    return;

                var campoComFoco = FocoEmCampoDeTexto.TemCampoComFoco(hud);
                var decisao = DecisaoDeEntradaNoMundo.DecidirEsc(
                    campoComFoco,
                    !campoComFoco && BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados(),
                    BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame,
                    _menu != null && _menu.OpcoesAbertas
                );
                if (decisao == DecisaoDoEsc.IgnorarPorEntradaDeTexto)
                {
                    if (BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame)
                    {
                        BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame = false;
                    }

                    if (campoComFoco)
                    {
                        FocoEmCampoDeTexto.SoltarFoco(hud);
                    }

                    return;
                }

                if (decisao == DecisaoDoEsc.IgnorarPorConsumoAnterior)
                {
                    BloqueioDeEntradaPorTexto.EscConsumidoNesteFrame = false;
                    return;
                }

                if (decisao == DecisaoDoEsc.VoltarDoSubmenu)
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

        private static bool EntradaDeTextoAtiva(VisualElement hud)
        {
            var campoComFoco = FocoEmCampoDeTexto.TemCampoComFoco(hud);
            return !DecisaoDeEntradaNoMundo.PodeExecutarAtalho(
                campoComFoco,
                !campoComFoco && BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados()
            );
        }

        private void AoMudarConexao(EstadoTempoReal _) => Sincronizar();

        private void FecharMenu()
        {
            _janelas?.DefinirMenuAberto(false);
        }

        private void FecharJanela(string chave)
        {
            _janelas?.Fechar(chave);
        }

        private void FocarJanela(string chave)
        {
            _janelas?.Focar(chave);
        }

        /// <summary>
        /// Com janela aberta: a câmera só gira enquanto o botão direito, apertado fora das
        /// janelas, está segurado (o cursor some e fica na janela do jogo); a roda só aproxima
        /// com o mouse fora das janelas. Sem janela, tudo livre como antes.
        /// </summary>
        private void AtualizarGiroComJanela()
        {
            if (_camera == null)
            {
                return;
            }

            var comCursor = _janelas.JanelaAberta && !_janelas.MenuAberto && _camera.InputAtivo;
            if (!comCursor)
            {
                if (_girando)
                {
                    PararDeGirar();
                }

                _camera.GiroBloqueado = false;
                _camera.ZoomBloqueado = false;
                return;
            }

            var sobreJanela = _apresentador.ContemPontoDaTela(_acoes.PosicaoDoMouse);
            if (
                !_girando
                && DecisaoDeEntradaNoMundo.ComecaAGirar(_acoes.GirarCameraApertado, sobreJanela)
            )
            {
                _girando = true;
                // Confined, não Locked: o combate lê Locked como "no jogo" e o botão direito
                // seria a guarda.
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else if (_girando && !_acoes.GirarCameraSegurado)
            {
                PararDeGirar();
            }

            _camera.GiroBloqueado = !_girando;
            _camera.ZoomBloqueado = sobreJanela && !_girando;
        }

        private void PararDeGirar()
        {
            _girando = false;
            LiberarCursor();
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
                _barra.Raiz.style.display =
                    _apresentador.TelaInteiraAberta || EntradaNoJogo.EntradaPendente
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
                _barra.DestacarAbertas(_janelas.ChavesAbertas);
                if (_janelas.JanelaAberta && !_apresentador.TelaInteiraAberta)
                {
                    _barra.TrazerParaFrente();
                }
            }

            var livre = DecisaoDeEntradaNoMundo.DeveCapturarCursor(
                _janelas.AlgoAberto,
                EntradaNoJogo.EntradaPendente,
                ConexaoTempoReal.Conectado
            );
            Hud.DefinirVisivel(!_apresentador.TelaInteiraAberta);

            // Com janelas abertas o personagem anda; só o menu, a entrada e a queda param.
            DefinirInputAtivo(
                DecisaoDeEntradaNoMundo.DeveLerInput(
                    _janelas.MenuAberto,
                    EntradaNoJogo.EntradaPendente,
                    ConexaoTempoReal.Conectado
                )
            );
            _girando = false;
            if (_camera != null)
            {
                _camera.GiroBloqueado = _janelas.JanelaAberta;
                _camera.ZoomBloqueado = false;
            }

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

        internal void LiberarEntrada() => Sincronizar();

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
