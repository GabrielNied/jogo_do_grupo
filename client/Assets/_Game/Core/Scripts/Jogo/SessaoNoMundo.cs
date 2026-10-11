using JogoDoGrupo.Client.Core.Jogo.Controle;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Fica no jogador local enquanto ele está no mundo: dono das ações de input, abre e fecha o
    /// menu do Esc e prende o cursor do mouse fora do menu. Criada pela
    /// <see cref="EntradaNoJogo"/>.
    /// </summary>
    internal sealed class SessaoNoMundo : MonoBehaviour
    {
        private AcoesJogador _acoes;
        private ControleTerceiraPessoa _controle;
        private CameraTerceiraPessoa _camera;
        private MenuJogo _menu;

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

            controle.Configurar(_acoes, camera, animador);
            camera.Configurar(_acoes, controle.transform);

            if (isActiveAndEnabled)
            {
                _acoes.Ativar();
            }

            FecharMenu();
        }

        private void OnEnable()
        {
            _acoes?.Ativar();
        }

        private void OnDisable()
        {
            _acoes?.Desativar();
            _menu?.Fechar();
            LiberarCursor();
        }

        private void OnDestroy()
        {
            _acoes?.Dispose();
            _acoes = null;
        }

        private void Update()
        {
            if (_acoes != null && _acoes.MenuApertado)
            {
                if (MenuAberto)
                {
                    FecharMenu();
                }
                else
                {
                    AbrirMenu();
                }
            }
        }

        private void AbrirMenu()
        {
            _menu.Abrir();
            DefinirInputAtivo(false);
            LiberarCursor();
        }

        private void FecharMenu()
        {
            _menu.Fechar();
            DefinirInputAtivo(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
