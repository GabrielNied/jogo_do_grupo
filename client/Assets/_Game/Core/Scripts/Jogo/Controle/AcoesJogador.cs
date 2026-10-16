using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Ações de input do jogador no mundo (Input System), criadas em código com as mesmas teclas
    /// do mapa "Player" de <c>Core/Input/InputSystem_Actions.inputactions</c>: assim não depende
    /// de um asset ligado numa cena. Teclado e mouse apenas (controle fora do escopo da fatia jogável, veja #508).
    /// </summary>
    internal sealed class AcoesJogador : IDisposable
    {
        private readonly InputAction _mover;
        private readonly InputAction _olharMouse;
        private readonly InputAction _zoom;
        private readonly InputAction _andarDevagar;
        private readonly InputAction _pular;
        private readonly InputAction _subir;
        private readonly InputAction _menu;
        private readonly InputAction _girarCamera;
        private readonly InputAction _esconderHud;
        private readonly InputAction _clicar;

        public AcoesJogador()
        {
            _mover = new InputAction(
                "Mover",
                InputActionType.Value,
                expectedControlType: "Vector2"
            );
            _mover
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _mover
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _olharMouse = new InputAction(
                "OlharMouse",
                InputActionType.Value,
                "<Mouse>/delta",
                expectedControlType: "Vector2"
            );

            _zoom = new InputAction(
                "Zoom",
                InputActionType.Value,
                "<Mouse>/scroll/y",
                expectedControlType: "Axis"
            );

            _andarDevagar = new InputAction(
                "AndarDevagar",
                InputActionType.Button,
                "<Keyboard>/leftCtrl"
            );

            _pular = new InputAction("Pular", InputActionType.Button, "<Keyboard>/space");

            _subir = new InputAction("Subir", InputActionType.Value, expectedControlType: "Axis");
            _subir
                .AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/space")
                .With("Negative", "<Keyboard>/leftCtrl");

            _menu = new InputAction("Menu", InputActionType.Button, "<Keyboard>/escape");
            _menu.AddBinding("<Gamepad>/start");
            _girarCamera = new InputAction(
                "GirarCamera",
                InputActionType.Button,
                "<Mouse>/rightButton"
            );
            _esconderHud = new InputAction("EsconderHud", InputActionType.Button, "<Keyboard>/f11");
            _clicar = new InputAction("Clicar", InputActionType.Button, "<Mouse>/leftButton");
            TeclasDoJogo.Mudou += AtualizarTeclas;
            AtualizarTeclas();
        }

        private void AtualizarTeclas()
        {
            _mover.ApplyBindingOverride(1, TeclasDoJogo.Caminho(TeclasDoJogo.Frente));
            _mover.ApplyBindingOverride(2, TeclasDoJogo.Caminho(TeclasDoJogo.Tras));
            _mover.ApplyBindingOverride(3, TeclasDoJogo.Caminho(TeclasDoJogo.Esquerda));
            _mover.ApplyBindingOverride(4, TeclasDoJogo.Caminho(TeclasDoJogo.Direita));
            _andarDevagar.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.AndarDevagar));
            _pular.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Pular));
            _subir.ApplyBindingOverride(1, TeclasDoJogo.Caminho(TeclasDoJogo.Pular));
            _esconderHud.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.EsconderHud));
        }

        /// <summary>Movimento (x = lado, y = frente), de -1 a 1.</summary>
        public Vector2 Mover => _mover.ReadValue<Vector2>();

        /// <summary>Quanto o mouse andou neste frame, em pixels.</summary>
        public Vector2 OlharMouse => _olharMouse.ReadValue<Vector2>();

        /// <summary>
        /// Cliques da roda do mouse neste frame: 1 para frente (aproxima), -1 para trás, 0 parada.
        /// Só o sinal conta, porque o tamanho do passo muda com o sistema.
        /// </summary>
        public float ZoomCliques
        {
            get
            {
                var valor = _zoom.ReadValue<float>();
                return valor > 0f ? 1f
                    : valor < 0f ? -1f
                    : 0f;
            }
        }

        /// <summary>Analógico direito; zero enquanto gamepad estiver fora do escopo (#508).</summary>
        public Vector2 OlharControle => Vector2.zero;

        /// <summary>Se o botão de andar devagar (Ctrl esquerdo) está pressionado; sem ele, corre.</summary>
        public bool AndarDevagar => _andarDevagar.IsPressed();

        /// <summary>Se o botão de pular (Espaço) foi apertado neste frame.</summary>
        public bool PularApertado => _pular.WasPressedThisFrame();

        /// <summary>Subir (Espaço) ou descer (Ctrl esquerdo) no voo de desenvolvimento, de -1 a 1.</summary>
        public float Subir => _subir.ReadValue<float>();

        /// <summary>Se o botão do menu (Esc) foi apertado neste frame.</summary>
        public bool MenuApertado => _menu.WasPressedThisFrame();

        /// <summary>Se o botão de alternar a visibilidade do HUD (F11) foi apertado neste frame.</summary>
        public bool EsconderHudApertado => _esconderHud.WasPressedThisFrame();

        /// <summary>
        /// Se o botão direito do mouse foi apertado neste frame: com janela aberta, começa a
        /// girar a câmera.
        /// </summary>
        public bool GirarCameraApertado => _girarCamera.WasPressedThisFrame();

        /// <summary>Se o botão direito do mouse está segurado.</summary>
        public bool GirarCameraSegurado => _girarCamera.IsPressed();

        /// <summary>
        /// Se o botão esquerdo do mouse foi apertado neste frame: com janela aberta, um clique
        /// no mundo entra no modo combate.
        /// </summary>
        public bool ClicarApertado => _clicar.WasPressedThisFrame();

        /// <summary>Se o botão esquerdo do mouse foi solto neste frame.</summary>
        public bool ClicarSolto => _clicar.WasReleasedThisFrame();

        /// <summary>Posição do mouse na tela, em pixels (origem embaixo à esquerda).</summary>
        public Vector2 PosicaoDoMouse =>
            Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        public void Ativar()
        {
            _mover.Enable();
            _olharMouse.Enable();
            _zoom.Enable();
            _andarDevagar.Enable();
            _pular.Enable();
            _subir.Enable();
            _menu.Enable();
            _girarCamera.Enable();
            _esconderHud.Enable();
            _clicar.Enable();
        }

        public void Desativar()
        {
            _mover.Disable();
            _olharMouse.Disable();
            _zoom.Disable();
            _andarDevagar.Disable();
            _pular.Disable();
            _subir.Disable();
            _menu.Disable();
            _girarCamera.Disable();
            _esconderHud.Disable();
            _clicar.Disable();
        }

        public void Dispose()
        {
            TeclasDoJogo.Mudou -= AtualizarTeclas;
            _mover.Dispose();
            _olharMouse.Dispose();
            _zoom.Dispose();
            _andarDevagar.Dispose();
            _pular.Dispose();
            _subir.Dispose();
            _menu.Dispose();
            _girarCamera.Dispose();
            _esconderHud.Dispose();
            _clicar.Dispose();
        }
    }
}
