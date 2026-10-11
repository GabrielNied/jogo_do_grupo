using System;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>O que o Esc fez (veja <see cref="GerenteDeJanelas.ApertarEsc"/>).</summary>
    internal enum ResultadoDoEsc
    {
        /// <summary>Havia uma janela aberta: fechou a janela e o menu continua fechado.</summary>
        FechouJanela,

        /// <summary>Nada estava aberto: abriu o menu.</summary>
        AbriuMenu,

        /// <summary>O menu estava aberto: fechou o menu.</summary>
        FechouMenu,
    }

    /// <summary>
    /// Regras das janelas do jogo, sem interface nem input: uma janela por vez, a tecla
    /// alterna, o Esc fecha a janela antes de abrir o menu e, com o menu aberto, nenhuma
    /// janela abre. Regras em <c>docs/features/interface.md</c>, seção "Janelas do jogo".
    /// </summary>
    internal sealed class GerenteDeJanelas
    {
        private readonly RegistroDeJanelas _registro;
        private readonly Action<DefinicaoDeJanela> _mostrar;
        private readonly Action _esconder;

        private DefinicaoDeJanela _aberta;

        /// <param name="mostrar">Chamado para pôr a janela na tela.</param>
        /// <param name="esconder">Chamado para tirar a janela da tela.</param>
        public GerenteDeJanelas(
            RegistroDeJanelas registro,
            Action<DefinicaoDeJanela> mostrar,
            Action esconder
        )
        {
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            _mostrar = mostrar;
            _esconder = esconder;
        }

        /// <summary>Disparado depois de qualquer mudança (janela ou menu abriu ou fechou).</summary>
        public event Action Mudou;

        /// <summary>Chave da janela aberta, ou <c>null</c>.</summary>
        public string ChaveAberta => _aberta?.Chave;

        /// <summary>Se o menu do Esc está aberto.</summary>
        public bool MenuAberto { get; private set; }

        /// <summary>
        /// Se há janela ou menu na tela: o personagem fica parado, a câmera não gira e o
        /// cursor fica livre.
        /// </summary>
        public bool AlgoAberto => _aberta != null || MenuAberto;

        /// <summary>
        /// Abre a janela, fechando a que estiver aberta. Não abre com o menu aberto nem se a
        /// chave não estiver registrada. Abrir a que já está aberta não faz nada.
        /// </summary>
        /// <returns><c>true</c> se a janela está aberta ao final.</returns>
        public bool Abrir(string chave)
        {
            if (MenuAberto)
            {
                return false;
            }

            var definicao = _registro.Procurar(chave);
            if (definicao == null)
            {
                return false;
            }

            if (_aberta == definicao)
            {
                return true;
            }

            if (_aberta != null)
            {
                _aberta = null;
                _esconder?.Invoke();
            }

            _aberta = definicao;
            _mostrar?.Invoke(definicao);
            Mudou?.Invoke();
            return true;
        }

        /// <summary>Fecha a janela aberta.</summary>
        /// <returns><c>true</c> se havia uma janela aberta.</returns>
        public bool Fechar()
        {
            if (_aberta == null)
            {
                return false;
            }

            _aberta = null;
            _esconder?.Invoke();
            Mudou?.Invoke();
            return true;
        }

        /// <summary>A tecla da janela: fecha se for a que está aberta, senão abre.</summary>
        /// <returns><c>true</c> se a janela está aberta ao final.</returns>
        public bool Alternar(string chave)
        {
            if (_aberta != null && _aberta.Chave == chave)
            {
                Fechar();
                return false;
            }

            return Abrir(chave);
        }

        /// <summary>
        /// O Esc: fecha a janela aberta; sem janela, abre o menu; com o menu aberto, fecha o menu.
        /// </summary>
        public ResultadoDoEsc ApertarEsc()
        {
            if (_aberta != null)
            {
                Fechar();
                return ResultadoDoEsc.FechouJanela;
            }

            DefinirMenuAberto(!MenuAberto);
            return MenuAberto ? ResultadoDoEsc.AbriuMenu : ResultadoDoEsc.FechouMenu;
        }

        /// <summary>Abre ou fecha o menu. Abrir o menu fecha a janela aberta.</summary>
        public void DefinirMenuAberto(bool aberto)
        {
            if (MenuAberto == aberto)
            {
                return;
            }

            if (aberto && _aberta != null)
            {
                _aberta = null;
                _esconder?.Invoke();
            }

            MenuAberto = aberto;
            Mudou?.Invoke();
        }
    }
}
