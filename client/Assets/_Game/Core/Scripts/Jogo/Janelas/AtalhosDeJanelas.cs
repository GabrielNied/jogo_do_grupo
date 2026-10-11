using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>
    /// Uma ação de input (Input System) por janela registrada, criada em código com a tecla
    /// da <see cref="DefinicaoDeJanela"/>.
    /// </summary>
    internal sealed class AtalhosDeJanelas : IDisposable
    {
        private readonly List<KeyValuePair<string, InputAction>> _acoes =
            new List<KeyValuePair<string, InputAction>>();

        public AtalhosDeJanelas(IReadOnlyList<DefinicaoDeJanela> definicoes)
        {
            foreach (var definicao in definicoes)
            {
                var acao = new InputAction(
                    "Janela." + definicao.Chave,
                    InputActionType.Button,
                    CaminhoDaTecla(definicao.Tecla)
                );
                _acoes.Add(new KeyValuePair<string, InputAction>(definicao.Chave, acao));
            }
        }

        /// <summary>Caminho do Input System para a letra (ex.: "I" vira <c>&lt;Keyboard&gt;/i</c>).</summary>
        internal static string CaminhoDaTecla(string tecla)
        {
            return "<Keyboard>/" + tecla.ToLowerInvariant();
        }

        /// <summary>Chave da janela cuja tecla foi apertada neste frame, ou <c>null</c>.</summary>
        public string ChaveApertada()
        {
            foreach (var par in _acoes)
            {
                if (par.Value.WasPressedThisFrame())
                {
                    return par.Key;
                }
            }

            return null;
        }

        public void Ativar()
        {
            foreach (var par in _acoes)
            {
                par.Value.Enable();
            }
        }

        public void Desativar()
        {
            foreach (var par in _acoes)
            {
                par.Value.Disable();
            }
        }

        public void Dispose()
        {
            foreach (var par in _acoes)
            {
                par.Value.Dispose();
            }

            _acoes.Clear();
        }
    }
}
