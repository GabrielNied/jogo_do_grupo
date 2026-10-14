using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Core.Contas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

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

    public class NavegacaoTestes
    {
        private const string ChaveToken = "jogodogrupo.sessao.token";
        private const string ChaveExpira = "jogodogrupo.sessao.expira";

        private VisualElement _container;
        private string _tokenAntes;
        private string _expiraAntes;

        [SetUp]
        public void Preparar()
        {
            _tokenAntes = PlayerPrefs.GetString(ChaveToken, "");
            _expiraAntes = PlayerPrefs.GetString(ChaveExpira, "");
            Sessao.EsquecerMemoria();
            Navegacao.Reiniciar();
            _container = new VisualElement();
            Navegacao.Inicializar(_container);
        }

        [TearDown]
        public void Limpar()
        {
            Navegacao.FecharTelaAtual();
            Navegacao.Reiniciar();
            Sessao.EsquecerMemoria();
            RestaurarPref(ChaveToken, _tokenAntes);
            RestaurarPref(ChaveExpira, _expiraAntes);
            PlayerPrefs.Save();
        }

        [Test]
        public void Abrir_TelaRegistradaCriaInstanciaNovaEFechaAAtual()
        {
            Navegacao.Registrar("teste.principal", () => new TelaDeTeste());

            Assert.IsTrue(Navegacao.Abrir("teste.principal"));
            var primeira = Navegacao.TelaAtual;
            var raizPrimeira = primeira.Raiz;

            Assert.IsTrue(Navegacao.Abrir("teste.principal"));

            Assert.AreNotSame(primeira, Navegacao.TelaAtual);
            Assert.IsFalse(primeira.Aberta);
            Assert.AreEqual("teste.principal", Navegacao.ChaveAtual);
            Assert.AreSame(_container, Navegacao.TelaAtual.Raiz.parent);
            Assert.IsNull(raizPrimeira.parent);
        }

        [Test]
        public void Abrir_ChaveAusenteMantemATelaAtual()
        {
            Navegacao.Registrar("teste.principal", () => new TelaDeTeste());
            Navegacao.Abrir("teste.principal");
            var atual = Navegacao.TelaAtual;
            LogAssert.Expect(LogType.Error, new Regex("Nenhuma tela registrada com a chave"));

            Assert.IsFalse(Navegacao.Abrir("teste.inexistente"));

            Assert.AreSame(atual, Navegacao.TelaAtual);
            Assert.AreEqual("teste.principal", Navegacao.ChaveAtual);
        }

        [Test]
        public void Abrir_FabricaLancaExcecaoMantemATelaAtual()
        {
            Navegacao.Registrar("teste.principal", () => new TelaDeTeste());
            Navegacao.Registrar(
                "teste.falha",
                () => throw new InvalidOperationException("fábrica falhou")
            );
            Navegacao.Abrir("teste.principal");
            var atual = Navegacao.TelaAtual;
            LogAssert.Expect(
                LogType.Exception,
                new Regex("InvalidOperationException: fábrica falhou")
            );

            Assert.IsFalse(Navegacao.Abrir("teste.falha"));

            Assert.AreSame(atual, Navegacao.TelaAtual);
            Assert.AreEqual("teste.principal", Navegacao.ChaveAtual);
        }

        [Test]
        public void AbrirTelaAposLogin_ChaveNaoRegistradaAbreFallbackDoCore()
        {
            Navegacao.Registrar(TelasCore.SemTelaAposLogin, () => new TelaDeTeste());
            Navegacao.DefinirTelaAposLogin("personagem.selecao");
            LogAssert.Expect(LogType.Warning, new Regex("não foi registrada"));

            Navegacao.AbrirTelaAposLogin();

            Assert.AreEqual(TelasCore.SemTelaAposLogin, Navegacao.ChaveAtual);
        }

        [Test]
        public void VoltarParaLogin_EncerraSessaoAbreLoginEEntregaAvisoUmaVez()
        {
            Navegacao.Registrar(TelasCore.Login, () => new TelaDeTeste());
            Sessao.Iniciar(
                new LoginResponse
                {
                    Token = "sessao-de-teste",
                    ExpiraEmUnix = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                }
            );

            Navegacao.VoltarParaLogin("Sua sessão expirou.");

            Assert.IsFalse(Sessao.Ativa);
            Assert.AreEqual(TelasCore.Login, Navegacao.ChaveAtual);
            Assert.AreEqual("Sua sessão expirou.", Navegacao.ConsumirAvisoLogin());
            Assert.IsNull(Navegacao.ConsumirAvisoLogin());
        }

        private static void RestaurarPref(string chave, string valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                PlayerPrefs.DeleteKey(chave);
            }
            else
            {
                PlayerPrefs.SetString(chave, valor);
            }
        }

        private sealed class TelaDeTeste : Tela
        {
            protected override void Montar(VisualElement raiz)
            {
                raiz.Add(new Label("teste"));
            }
        }
    }

    internal class DecisaoDeEntradaTestes
    {
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, false, false)]
        public void Cursor_SoCapturadoQuandoLivreConectadoESemEntradaPendente(
            bool algoAberto,
            bool entradaPendente,
            bool conectado,
            bool esperado
        )
        {
            Assert.AreEqual(
                esperado,
                DecisaoDeEntradaNoMundo.DeveCapturarCursor(algoAberto, entradaPendente, conectado)
            );
        }

        [TestCase(true, false, false, false, DecisaoDoEsc.IgnorarPorEntradaDeTexto)]
        [TestCase(false, true, false, false, DecisaoDoEsc.IgnorarPorEntradaDeTexto)]
        [TestCase(false, false, true, false, DecisaoDoEsc.IgnorarPorConsumoAnterior)]
        [TestCase(false, false, false, true, DecisaoDoEsc.VoltarDoSubmenu)]
        [TestCase(false, false, false, false, DecisaoDoEsc.AlternarMenu)]
        public void Esc_FocoOuBloqueioDeTextoConsomeEscAntesDeAcionarMenu(
            bool campoComFoco,
            bool bloqueadoPorTexto,
            bool escConsumido,
            bool submenuAberto,
            DecisaoDoEsc esperado
        )
        {
            Assert.AreEqual(
                esperado,
                DecisaoDeEntradaNoMundo.DecidirEsc(
                    campoComFoco,
                    bloqueadoPorTexto,
                    escConsumido,
                    submenuAberto
                )
            );
        }

        [Test]
        public void Atalhos_CampoFocadoOuBloqueioDeTextoImpedemAcaoNoJogo()
        {
            Assert.IsFalse(DecisaoDeEntradaNoMundo.PodeExecutarAtalho(true, false));
            Assert.IsFalse(DecisaoDeEntradaNoMundo.PodeExecutarAtalho(false, true));
            Assert.IsTrue(DecisaoDeEntradaNoMundo.PodeExecutarAtalho(false, false));
        }
    }
}
