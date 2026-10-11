using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Ações de input do jogador no mundo (Input System), criadas em código com as mesmas teclas
    /// do mapa "Player" de <c>Core/Input/InputSystem_Actions.inputactions</c>: assim não depende
    /// de um asset ligado numa cena. Teclado e mouse, com controle como alternativa.
    /// </summary>
    internal sealed class AcoesJogador : IDisposable
    {
        private readonly InputAction _mover;
        private readonly InputAction _olharMouse;
        private readonly InputAction _olharControle;
        private readonly InputAction _correr;
        private readonly InputAction _pular;
        private readonly InputAction _menu;

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
            _mover.AddBinding("<Gamepad>/leftStick");

            _olharMouse = new InputAction(
                "OlharMouse",
                InputActionType.Value,
                "<Mouse>/delta",
                expectedControlType: "Vector2"
            );
            _olharControle = new InputAction(
                "OlharControle",
                InputActionType.Value,
                "<Gamepad>/rightStick",
                expectedControlType: "Vector2"
            );

            _correr = new InputAction("Correr", InputActionType.Button, "<Keyboard>/leftShift");
            _correr.AddBinding("<Keyboard>/rightShift");
            _correr.AddBinding("<Gamepad>/leftStickPress");

            _pular = new InputAction("Pular", InputActionType.Button, "<Keyboard>/space");

            _menu = new InputAction("Menu", InputActionType.Button, "<Keyboard>/escape");
            _menu.AddBinding("<Gamepad>/start");
        }

        /// <summary>Movimento (x = lado, y = frente), de -1 a 1.</summary>
        public Vector2 Mover => _mover.ReadValue<Vector2>();

        /// <summary>Quanto o mouse andou neste frame, em pixels.</summary>
        public Vector2 OlharMouse => _olharMouse.ReadValue<Vector2>();

        /// <summary>Analógico direito do controle, de -1 a 1.</summary>
        public Vector2 OlharControle => _olharControle.ReadValue<Vector2>();

        /// <summary>Se o botão de correr está pressionado.</summary>
        public bool Correr => _correr.IsPressed();

        /// <summary>Se o botão de pular (Espaço) foi apertado neste frame.</summary>
        public bool PularApertado => _pular.WasPressedThisFrame();

        /// <summary>Se o botão do menu (Esc) foi apertado neste frame.</summary>
        public bool MenuApertado => _menu.WasPressedThisFrame();

        public void Ativar()
        {
            _mover.Enable();
            _olharMouse.Enable();
            _olharControle.Enable();
            _correr.Enable();
            _pular.Enable();
            _menu.Enable();
        }

        public void Desativar()
        {
            _mover.Disable();
            _olharMouse.Disable();
            _olharControle.Disable();
            _correr.Disable();
            _pular.Disable();
            _menu.Disable();
        }

        public void Dispose()
        {
            _mover.Dispose();
            _olharMouse.Dispose();
            _olharControle.Dispose();
            _correr.Dispose();
            _pular.Dispose();
            _menu.Dispose();
        }
    }
}
