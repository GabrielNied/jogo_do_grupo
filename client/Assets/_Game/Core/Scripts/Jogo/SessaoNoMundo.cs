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

        // Com janela aberta: o clique esquerdo começou no mundo (fora da interface) e, ao
        // soltar, prende o cursor. O clique que entra não vira golpe (o combate só ataca no
        // aperto com o cursor já preso).
        private bool _cliqueNoMundo;

        // Modo combate com janelas abertas: cursor preso como sem janela, golpe, guarda,
        // esquiva, skills e troca de arma valem e a câmera gira livre.
        private bool _modoCombate;

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
            _cliqueNoMundo = false;
            _modoCombate = false;
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

            var hud = Navegacao.Container;
            var textoAtivo = BloqueioDeEntradaPorTexto.EntradaDeTextoAtiva(hud);
            BloqueioDeEntradaPorTexto.AtualizarEstado(textoAtivo);

            if (_controle != null)
            {
                _controle.InputAtivo = DecisaoDeEntradaNoMundo.DeveLerInputDeMovimento(
                    _janelas.MenuAberto,
                    EntradaNoJogo.EntradaPendente,
                    ConexaoTempoReal.Conectado,
                    textoAtivo
                );
            }

            AtualizarGiroComJanela();

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
                else if (
                    DecisaoDeEntradaNoMundo.EscSaiDoModoCombate(_modoCombate, _janelas.JanelaAberta)
                )
                {
                    SairDoModoCombate();
                }
                else
                {
                    _janelas.ApertarEsc();
                }
                return;
            }

            if (textoAtivo)
            {
                return;
            }

            if (_acoes.EsconderHudApertado)
            {
                Hud.AlternarVisivel();
            }

            var chave = _atalhos?.ChaveApertada();
            if (chave != null)
            {
                _janelas.Alternar(chave);
            }
        }

        private static bool EntradaDeTextoAtiva(VisualElement hud) =>
            BloqueioDeEntradaPorTexto.EntradaDeTextoAtiva(hud);

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

                _cliqueNoMundo = false;
                _modoCombate = false;
                _camera.GiroBloqueado = false;
                _camera.ZoomBloqueado = false;
                return;
            }

            if (_modoCombate)
            {
                // Outra coisa soltou o cursor (ex.: o chat abriu): volta ao modo de cursor.
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    SairDoModoCombate();
                }
                else
                {
                    _camera.GiroBloqueado = false;
                    _camera.ZoomBloqueado = false;
                    return;
                }
            }

            if (AtualizarCliqueNoMundo())
            {
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

        /// <summary>
        /// Clique esquerdo que começa no mundo, fora da interface: ao soltar, prende o cursor e
        /// entra no modo combate, com as janelas ainda abertas.
        /// </summary>
        /// <returns><c>true</c> se entrou no modo combate neste frame.</returns>
        private bool AtualizarCliqueNoMundo()
        {
            if (_acoes.ClicarApertado)
            {
                _cliqueNoMundo =
                    !_girando
                    && !EntradaDeTextoAtiva(Navegacao.Container)
                    && DecisaoDeEntradaNoMundo.ComecaCliqueNoMundo(
                        true,
                        _apresentador.SobreInterface(_acoes.PosicaoDoMouse)
                    );
            }

            if (!_cliqueNoMundo || !_acoes.ClicarSolto)
            {
                return false;
            }

            _cliqueNoMundo = false;
            _girando = false;
            _modoCombate = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _camera.GiroBloqueado = false;
            _camera.ZoomBloqueado = false;
            return true;
        }

        private void SairDoModoCombate()
        {
            _modoCombate = false;
            _cliqueNoMundo = false;
            if (_camera != null)
            {
                _camera.GiroBloqueado = true;
            }

            LiberarCursor();
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

            var textoAtivo = BloqueioDeEntradaPorTexto.EntradaDeTextoAtiva(Navegacao.Container);
            BloqueioDeEntradaPorTexto.AtualizarEstado(textoAtivo);

            // Com janelas abertas o personagem anda; só o menu, a entrada, a queda ou a digitação param.
            DefinirInputAtivo(
                DecisaoDeEntradaNoMundo.DeveLerInput(
                    _janelas.MenuAberto,
                    EntradaNoJogo.EntradaPendente,
                    ConexaoTempoReal.Conectado,
                    textoAtivo
                )
            );
            // Abrir, fechar ou subir janela solta o cursor do modo combate.
            _girando = false;
            _cliqueNoMundo = false;
            _modoCombate = false;
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
