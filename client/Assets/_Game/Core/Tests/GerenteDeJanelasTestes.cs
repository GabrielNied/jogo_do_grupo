using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Regras das janelas do jogo (docs/features/interface.md, "Janelas do jogo"): uma por
    /// vez, a tecla alterna, o Esc fecha a janela antes de abrir o menu e nada abre por baixo
    /// do menu.
    /// </summary>
    public class GerenteDeJanelasTestes
    {
        private const string Personagem = "personagem.ficha";
        private const string Inventario = "itens.inventario";

        private List<string> _eventos;
        private int _mudancas;
        private GerenteDeJanelas _gerente;

        [SetUp]
        public void Preparar()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(Janela(Personagem, "C"));
            registro.Registrar(Janela(Inventario, "I"));

            _eventos = new List<string>();
            _mudancas = 0;
            _gerente = new GerenteDeJanelas(
                registro,
                definicao => _eventos.Add("mostrar " + definicao.Chave),
                () => _eventos.Add("esconder")
            );
            _gerente.Mudou += () => _mudancas++;
        }

        private static DefinicaoDeJanela Janela(string chave, string tecla)
        {
            return new DefinicaoDeJanela(
                chave,
                "Título",
                tecla,
                0,
                () => new JanelaEmConstrucao("")
            );
        }

        [Test]
        public void Inicio_NadaAberto()
        {
            Assert.IsNull(_gerente.ChaveAberta);
            Assert.IsFalse(_gerente.MenuAberto);
            Assert.IsFalse(_gerente.AlgoAberto);
        }

        [Test]
        public void Alternar_AbreAJanela_EParaOJogo()
        {
            Assert.IsTrue(_gerente.Alternar(Inventario));

            Assert.AreEqual(Inventario, _gerente.ChaveAberta);
            Assert.IsTrue(_gerente.AlgoAberto);
            CollectionAssert.AreEqual(new[] { "mostrar " + Inventario }, _eventos);
            Assert.AreEqual(1, _mudancas);
        }

        [Test]
        public void Alternar_MesmaTeclaDeNovo_Fecha()
        {
            _gerente.Alternar(Inventario);

            Assert.IsFalse(_gerente.Alternar(Inventario));

            Assert.IsNull(_gerente.ChaveAberta);
            Assert.IsFalse(_gerente.AlgoAberto);
            CollectionAssert.AreEqual(new[] { "mostrar " + Inventario, "esconder" }, _eventos);
        }

        [Test]
        public void Alternar_OutraJanela_FechaAAtualEAbreANova_UmaPorVez()
        {
            _gerente.Alternar(Inventario);
            _mudancas = 0;

            Assert.IsTrue(_gerente.Alternar(Personagem));

            Assert.AreEqual(Personagem, _gerente.ChaveAberta);
            CollectionAssert.AreEqual(
                new[] { "mostrar " + Inventario, "esconder", "mostrar " + Personagem },
                _eventos
            );
            Assert.AreEqual(1, _mudancas, "A troca avisa uma vez só, já com a janela nova.");
        }

        [Test]
        public void Abrir_AQueJaEstaAberta_NaoRemonta()
        {
            _gerente.Abrir(Inventario);

            Assert.IsTrue(_gerente.Abrir(Inventario));

            Assert.AreEqual(1, _eventos.Count);
            Assert.AreEqual(1, _mudancas);
        }

        [Test]
        public void Abrir_ChaveDesconhecida_NaoAbreNemFechaAAtual()
        {
            _gerente.Abrir(Inventario);

            Assert.IsFalse(_gerente.Abrir("mundo.mapa"));
            Assert.IsFalse(_gerente.Abrir(null));

            Assert.AreEqual(Inventario, _gerente.ChaveAberta);
        }

        [Test]
        public void Fechar_SemJanela_NaoFazNada()
        {
            Assert.IsFalse(_gerente.Fechar());

            Assert.AreEqual(0, _eventos.Count);
            Assert.AreEqual(0, _mudancas);
        }

        [Test]
        public void Esc_ComJanela_FechaAJanela_SemAbrirOMenu()
        {
            _gerente.Alternar(Inventario);

            Assert.AreEqual(ResultadoDoEsc.FechouJanela, _gerente.ApertarEsc());

            Assert.IsNull(_gerente.ChaveAberta);
            Assert.IsFalse(_gerente.MenuAberto);
            Assert.IsFalse(_gerente.AlgoAberto);
        }

        [Test]
        public void Esc_SemJanela_AbreOMenu_EDeNovoFecha()
        {
            Assert.AreEqual(ResultadoDoEsc.AbriuMenu, _gerente.ApertarEsc());
            Assert.IsTrue(_gerente.MenuAberto);
            Assert.IsTrue(_gerente.AlgoAberto);

            Assert.AreEqual(ResultadoDoEsc.FechouMenu, _gerente.ApertarEsc());
            Assert.IsFalse(_gerente.MenuAberto);
            Assert.IsFalse(_gerente.AlgoAberto);
            Assert.AreEqual(2, _mudancas);
        }

        [Test]
        public void ComOMenuAberto_AtalhoNaoAbreJanela()
        {
            _gerente.ApertarEsc();

            Assert.IsFalse(_gerente.Alternar(Inventario));
            Assert.IsFalse(_gerente.Abrir(Personagem));

            Assert.IsNull(_gerente.ChaveAberta);
            Assert.AreEqual(0, _eventos.Count);
        }

        [Test]
        public void AbrirOMenuPorCodigo_FechaAJanelaAberta()
        {
            _gerente.Alternar(Inventario);

            _gerente.DefinirMenuAberto(true);

            Assert.IsNull(_gerente.ChaveAberta);
            Assert.IsTrue(_gerente.MenuAberto);
            CollectionAssert.AreEqual(new[] { "mostrar " + Inventario, "esconder" }, _eventos);
        }

        [Test]
        public void DefinirMenuAberto_SemMudar_NaoAvisa()
        {
            _gerente.DefinirMenuAberto(false);

            Assert.AreEqual(0, _mudancas);
        }

        [Test]
        public void DepoisDeFecharOMenu_AJanelaVoltaAAbrir()
        {
            _gerente.ApertarEsc();
            _gerente.DefinirMenuAberto(false);

            Assert.IsTrue(_gerente.Alternar(Personagem));
            Assert.AreEqual(Personagem, _gerente.ChaveAberta);
        }
    }
}
