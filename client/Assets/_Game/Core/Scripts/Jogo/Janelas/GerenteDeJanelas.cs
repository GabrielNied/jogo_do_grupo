using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Client.Core.Jogo.Janelas
{
    /// <summary>O que o Esc fez (veja <see cref="GerenteDeJanelas.ApertarEsc"/>).</summary>
    internal enum ResultadoDoEsc
    {
        /// <summary>Havia janela aberta: fechou a de cima e o menu continua fechado.</summary>
        FechouJanela,

        /// <summary>Nada estava aberto: abriu o menu.</summary>
        AbriuMenu,

        /// <summary>O menu estava aberto: fechou o menu.</summary>
        FechouMenu,
    }

    /// <summary>
    /// Regras das janelas do jogo, sem interface nem input: várias abertas ao mesmo tempo, a
    /// tecla alterna a sua, a última aberta ou focada fica em cima, o Esc fecha a de cima antes
    /// de abrir o menu e, com o menu aberto, nenhuma janela abre. Regras em
    /// <c>docs/features/interface.md</c>, seção "Janelas do jogo".
    /// </summary>
    internal sealed class GerenteDeJanelas
    {
        private readonly RegistroDeJanelas _registro;
        private readonly Action<DefinicaoDeJanela> _mostrar;
        private readonly Action<DefinicaoDeJanela> _esconder;
        private readonly Action<DefinicaoDeJanela> _focar;

        // Na ordem em que ficaram em cima: a última é a de cima.
        private readonly List<DefinicaoDeJanela> _abertas = new List<DefinicaoDeJanela>();
        private readonly List<string> _chaves = new List<string>();

        /// <param name="mostrar">Chamado para pôr a janela na tela.</param>
        /// <param name="esconder">Chamado para tirar a janela da tela.</param>
        /// <param name="focar">Chamado quando outra janela passa a ser a de cima.</param>
        public GerenteDeJanelas(
            RegistroDeJanelas registro,
            Action<DefinicaoDeJanela> mostrar,
            Action<DefinicaoDeJanela> esconder,
            Action<DefinicaoDeJanela> focar = null
        )
        {
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            _mostrar = mostrar;
            _esconder = esconder;
            _focar = focar;
        }

        /// <summary>Disparado depois de qualquer mudança (janela ou menu abriu, fechou ou subiu).</summary>
        public event Action Mudou;

        /// <summary>Chave da janela de cima (a última aberta ou focada), ou <c>null</c>.</summary>
        public string ChaveAberta => _abertas.Count > 0 ? _abertas[_abertas.Count - 1].Chave : null;

        /// <summary>Chaves das janelas abertas, de baixo para cima.</summary>
        public IReadOnlyList<string> ChavesAbertas => _chaves;

        /// <summary>Se há ao menos uma janela aberta.</summary>
        public bool JanelaAberta => _abertas.Count > 0;

        /// <summary>Se o menu do Esc está aberto.</summary>
        public bool MenuAberto { get; private set; }

        /// <summary>Se há janela ou menu na tela: o cursor fica livre.</summary>
        public bool AlgoAberto => _abertas.Count > 0 || MenuAberto;

        /// <summary>Se a janela com essa chave está aberta.</summary>
        public bool EstaAberta(string chave)
        {
            return Indice(chave) >= 0;
        }

        /// <summary>
        /// Abre a janela por cima das outras. Não abre com o menu aberto nem se a chave não
        /// estiver registrada. Abrir a que já está aberta só a traz para cima.
        /// </summary>
        /// <returns><c>true</c> se a janela está aberta ao final.</returns>
        public bool Abrir(string chave)
        {
            if (MenuAberto)
            {
                return false;
            }

            if (EstaAberta(chave))
            {
                return Focar(chave);
            }

            var definicao = _registro.Procurar(chave);
            if (definicao == null)
            {
                return false;
            }

            _abertas.Add(definicao);
            _chaves.Add(definicao.Chave);
            _mostrar?.Invoke(definicao);
            Mudou?.Invoke();
            return true;
        }

        /// <summary>Traz a janela aberta para cima (a próxima que o Esc fecha).</summary>
        /// <returns><c>true</c> se a janela está aberta.</returns>
        public bool Focar(string chave)
        {
            var indice = Indice(chave);
            if (indice < 0)
            {
                return false;
            }

            if (indice == _abertas.Count - 1)
            {
                return true;
            }

            var definicao = _abertas[indice];
            Remover(indice);
            _abertas.Add(definicao);
            _chaves.Add(definicao.Chave);
            _focar?.Invoke(definicao);
            Mudou?.Invoke();
            return true;
        }

        /// <summary>Fecha a janela de cima.</summary>
        /// <returns><c>true</c> se havia janela aberta.</returns>
        public bool Fechar()
        {
            return _abertas.Count > 0 && Fechar(ChaveAberta);
        }

        /// <summary>Fecha a janela com essa chave, esteja onde estiver.</summary>
        /// <returns><c>true</c> se ela estava aberta.</returns>
        public bool Fechar(string chave)
        {
            var indice = Indice(chave);
            if (indice < 0)
            {
                return false;
            }

            var definicao = _abertas[indice];
            Remover(indice);
            _esconder?.Invoke(definicao);
            // Fechou a de cima: a de baixo dela passa a ser a de cima.
            if (indice == _abertas.Count && _abertas.Count > 0)
            {
                _focar?.Invoke(_abertas[_abertas.Count - 1]);
            }

            Mudou?.Invoke();
            return true;
        }

        /// <summary>Fecha todas as janelas, avisando a mudança uma vez só.</summary>
        /// <returns><c>true</c> se havia janela aberta.</returns>
        public bool FecharTodas()
        {
            if (!EsconderTodas())
            {
                return false;
            }

            Mudou?.Invoke();
            return true;
        }

        /// <summary>A tecla da janela: fecha se ela estiver aberta, senão abre por cima.</summary>
        /// <returns><c>true</c> se a janela está aberta ao final.</returns>
        public bool Alternar(string chave)
        {
            if (EstaAberta(chave))
            {
                Fechar(chave);
                return false;
            }

            return Abrir(chave);
        }

        /// <summary>
        /// O Esc: fecha a janela de cima; sem janela, abre o menu; com o menu aberto, fecha o menu.
        /// </summary>
        public ResultadoDoEsc ApertarEsc()
        {
            if (_abertas.Count > 0)
            {
                Fechar();
                return ResultadoDoEsc.FechouJanela;
            }

            DefinirMenuAberto(!MenuAberto);
            return MenuAberto ? ResultadoDoEsc.AbriuMenu : ResultadoDoEsc.FechouMenu;
        }

        /// <summary>Abre ou fecha o menu. Abrir o menu fecha as janelas abertas.</summary>
        public void DefinirMenuAberto(bool aberto)
        {
            if (MenuAberto == aberto)
            {
                return;
            }

            if (aberto)
            {
                EsconderTodas();
            }

            MenuAberto = aberto;
            Mudou?.Invoke();
        }

        private bool EsconderTodas()
        {
            if (_abertas.Count == 0)
            {
                return false;
            }

            // De cima para baixo, como se o Esc fosse apertado várias vezes.
            for (var i = _abertas.Count - 1; i >= 0; i--)
            {
                var definicao = _abertas[i];
                Remover(i);
                _esconder?.Invoke(definicao);
            }

            return true;
        }

        private int Indice(string chave)
        {
            if (string.IsNullOrEmpty(chave))
            {
                return -1;
            }

            for (var i = 0; i < _abertas.Count; i++)
            {
                if (_abertas[i].Chave == chave)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Remover(int indice)
        {
            _abertas.RemoveAt(indice);
            _chaves.RemoveAt(indice);
        }
    }
}
