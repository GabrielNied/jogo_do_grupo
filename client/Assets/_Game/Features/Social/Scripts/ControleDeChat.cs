using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Liga o painel do chat ao jogo: Enter abre a caixa, Enter de novo envia e Esc fecha sem
    /// enviar. Com a caixa aberta o personagem e a câmera ficam parados e o cursor, solto, para
    /// as ações do jogo (andar, atacar) não dispararem enquanto a pessoa digita.
    /// </summary>
    internal sealed class ControleDeChat : MonoBehaviour
    {
        private ServicoDeChat _servico;
        private PainelDeChat _painel;
        private AtividadeDoPainel _atividade;
        private InputAction _enter;
        private InputAction _paginaAcima;
        private InputAction _paginaAbaixo;
        private int _frameDaAbertura = -1;

        public void Configurar(ServicoDeChat servico, PainelDeChat painel)
        {
            _servico = servico;
            _painel = painel;
            _atividade = new AtividadeDoPainel(Agora());

            _enter = new InputAction("ChatAbrir", InputActionType.Button, "<Keyboard>/enter");
            _enter.AddBinding("<Keyboard>/numpadEnter");
            _paginaAcima = new InputAction(
                "ChatPaginaAcima",
                InputActionType.Button,
                "<Keyboard>/pageUp"
            );
            _paginaAbaixo = new InputAction(
                "ChatPaginaAbaixo",
                InputActionType.Button,
                "<Keyboard>/pageDown"
            );
            if (isActiveAndEnabled)
            {
                _enter.Enable();
                _paginaAcima.Enable();
                _paginaAbaixo.Enable();
            }

            _servico.Mudou += Redesenhar;
            _painel.EnterApertado += AoApertarEnter;
            _painel.EscApertado += AoApertarEsc;
            _painel.CompletarTexto += CompletadorDeComandosDeChat.Completar;
            _painel.ConfiguracaoMudou += Redesenhar;
            ChatDoJogo.AbrirCaixaDeTexto = AbrirComTexto;
            ChatDoJogo.AlternarCaixaDeTexto = AlternarCaixa;
            BloqueioDeEntradaPorTexto.RegistrarBloqueioDeAtalhos(() => _painel.Aberta);
            Redesenhar();
        }

        private void OnEnable()
        {
            _enter?.Enable();
            _paginaAcima?.Enable();
            _paginaAbaixo?.Enable();
        }

        private void OnDisable()
        {
            _enter?.Disable();
            _paginaAcima?.Disable();
            _paginaAbaixo?.Disable();
        }

        private void OnDestroy()
        {
            if (_servico != null)
            {
                _servico.Mudou -= Redesenhar;
                _painel.ConfiguracaoMudou -= Redesenhar;
            }

            if (ChatDoJogo.AbrirCaixaDeTexto == AbrirComTexto)
            {
                ChatDoJogo.AbrirCaixaDeTexto = null;
            }

            if (ChatDoJogo.AlternarCaixaDeTexto == AlternarCaixa)
            {
                ChatDoJogo.AlternarCaixaDeTexto = null;
            }

            _enter?.Dispose();
            _paginaAcima?.Dispose();
            _paginaAbaixo?.Dispose();
        }

        private void Update()
        {
            if (_painel == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            if (_painel.Aberta)
            {
                // Uma janela ou o menu abriu por cima: larga a caixa e deixa o Core cuidar do cursor.
                if (JanelasDoJogo.AlgoAberto)
                {
                    Fechar(false);
                }
            }
            else if (_enter.WasPressedThisFrame() && !JanelasDoJogo.AlgoAberto)
            {
                Abrir();
            }

            if (!JanelasDoJogo.AlgoAberto && _paginaAcima.WasPressedThisFrame())
                _painel.RolarPagina(true);
            if (!JanelasDoJogo.AlgoAberto && _paginaAbaixo.WasPressedThisFrame())
                _painel.RolarPagina(false);

            _painel.Esmaecer(_atividade.Esmaecido(Agora(), _painel.Aberta));
        }

        private void Abrir()
        {
            AbrirComTexto("");
        }

        private void AlternarCaixa()
        {
            if (_painel == null || !EntradaNoJogo.NoJogo || JanelasDoJogo.AlgoAberto)
            {
                return;
            }

            if (_painel.Aberta)
            {
                Fechar(true);
                return;
            }

            AbrirComTexto("/");
        }

        private void AbrirComTexto(string textoInicial)
        {
            if (_painel == null || !EntradaNoJogo.NoJogo || JanelasDoJogo.AlgoAberto)
            {
                return;
            }

            _frameDaAbertura = Time.frameCount;
            _atividade.Registrar(Agora());
            _painel.Abrir();
            if (!string.IsNullOrEmpty(textoInicial))
            {
                _painel.Texto = textoInicial;
            }

            DefinirJogoAtivo(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Fechar(bool devolverOJogo)
        {
            _painel.Fechar();
            _atividade.Registrar(Agora());
            if (devolverOJogo && !JanelasDoJogo.AlgoAberto)
            {
                DefinirJogoAtivo(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void AoApertarEnter()
        {
            // O Enter que abriu a caixa não pode enviar no mesmo quadro.
            if (Time.frameCount == _frameDaAbertura)
            {
                return;
            }

            if (_servico.Enviar(_painel.Texto))
            {
                Fechar(true);
            }
        }

        private void AoApertarEsc()
        {
            Fechar(true);
        }

        private static void DefinirJogoAtivo(bool ativo)
        {
            var jogador = EntradaNoJogo.Jogador;
            var controle = jogador != null ? jogador.GetComponent<ControleTerceiraPessoa>() : null;
            if (controle != null)
            {
                controle.InputAtivo = ativo;
            }

            var camera = Camera.main;
            var controleDaCamera =
                camera != null ? camera.GetComponent<CameraTerceiraPessoa>() : null;
            if (controleDaCamera != null)
            {
                controleDaCamera.InputAtivo = ativo;
            }
        }

        private void Redesenhar()
        {
            if (_painel == null)
            {
                return;
            }

            _atividade.Registrar(Agora());
            var fuso = TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow);
            var linhas = _servico.Historico.Ultimas(
                HistoricoDeChat.Maximo,
                _servico.Ignorados,
                _servico.Amigos
            );
            _painel.Mostrar(
                VisaoDoChat.Filtrar(
                    linhas,
                    _painel.Filtro,
                    PreferenciasDeAvisos.MostrarSistema,
                    PreferenciasDeAvisos.MostrarCombate
                ),
                fuso
            );
        }

        private static double Agora() => Time.realtimeSinceStartupAsDouble;
    }
}
