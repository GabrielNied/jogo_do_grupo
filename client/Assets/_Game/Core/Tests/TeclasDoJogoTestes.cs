using JogoDoGrupo.Client.Core.Jogo;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class TeclasDoJogoTestes
    {
        [SetUp]
        public void Preparar() => TeclasDoJogo.RestaurarTudo();

        [TearDown]
        public void Limpar() => TeclasDoJogo.RestaurarTudo();

        [Test]
        public void TrocarTeclaUsadaTrocaAsDuasAcoesEPersiste()
        {
            Assert.IsTrue(TeclasDoJogo.TentarTrocar(TeclasDoJogo.Frente, "<Keyboard>/s", out _));
            Assert.AreEqual("<Keyboard>/s", TeclasDoJogo.Caminho(TeclasDoJogo.Frente));
            Assert.AreEqual("<Keyboard>/w", TeclasDoJogo.Caminho(TeclasDoJogo.Tras));

            var gravado = OpcoesLocais.Padrao().Gravar();
            Assert.IsTrue(gravado.Contains("tecla.frente="));
            OpcoesLocais.Ler(gravado);
            Assert.AreEqual("<Keyboard>/s", TeclasDoJogo.Caminho(TeclasDoJogo.Frente));
            Assert.AreEqual("<Keyboard>/w", TeclasDoJogo.Caminho(TeclasDoJogo.Tras));
        }

        [Test]
        public void TeclaReservadaEMouseEmMovimentoNaoMudamOMapa()
        {
            Assert.IsFalse(
                TeclasDoJogo.TentarTrocar(TeclasDoJogo.Frente, "<Keyboard>/escape", out _)
            );
            Assert.IsFalse(
                TeclasDoJogo.TentarTrocar(TeclasDoJogo.Frente, "<Mouse>/leftButton", out _)
            );
            Assert.AreEqual("<Keyboard>/w", TeclasDoJogo.Caminho(TeclasDoJogo.Frente));
        }

        [Test]
        public void ArquivoComDuplicataVoltaAosPadroes()
        {
            TeclasDoJogo.Carregar("tecla.frente=<Keyboard>/g\ntecla.tras=<Keyboard>/g");
            Assert.AreEqual("<Keyboard>/w", TeclasDoJogo.Caminho(TeclasDoJogo.Frente));
            Assert.AreEqual("<Keyboard>/s", TeclasDoJogo.Caminho(TeclasDoJogo.Tras));
        }

        [Test]
        public void PadroesSempreCorrendo_CtrlAndaDevagarEShiftEsquiva()
        {
            Assert.AreEqual("<Keyboard>/leftCtrl", TeclasDoJogo.Caminho(TeclasDoJogo.AndarDevagar));
            Assert.AreEqual("<Keyboard>/leftShift", TeclasDoJogo.Caminho(TeclasDoJogo.Esquiva));
        }

        [Test]
        public void PreferenciaAntigaDeCorrer_VoltaAoPadraoNovo()
        {
            // Antes do #894: correr no Alt e esquiva no Shift (trocadas pelo jogador).
            TeclasDoJogo.Carregar(
                "tecla.correr=<Keyboard>/leftAlt\ntecla.esquiva=<Keyboard>/leftShift"
            );
            Assert.AreEqual("<Keyboard>/leftCtrl", TeclasDoJogo.Caminho(TeclasDoJogo.AndarDevagar));
            Assert.AreEqual("<Keyboard>/leftShift", TeclasDoJogo.Caminho(TeclasDoJogo.Esquiva));
            Assert.AreEqual("", TeclasDoJogo.Gravar());
        }

        [Test]
        public void RestaurarUmaEReporTodasAsTeclas()
        {
            TeclasDoJogo.TentarTrocar(TeclasDoJogo.Frente, "<Keyboard>/g", out _);
            TeclasDoJogo.Restaurar(TeclasDoJogo.Frente);
            Assert.AreEqual("<Keyboard>/w", TeclasDoJogo.Caminho(TeclasDoJogo.Frente));
            TeclasDoJogo.TentarTrocar(TeclasDoJogo.Ataque, "<Mouse>/middleButton", out _);
            TeclasDoJogo.RestaurarTudo();
            Assert.AreEqual("<Mouse>/leftButton", TeclasDoJogo.Caminho(TeclasDoJogo.Ataque));
        }

        [Test]
        public void EsconderHud_PadraoEF11EPodeSerRemapeado()
        {
            Assert.AreEqual("<Keyboard>/f11", TeclasDoJogo.Caminho(TeclasDoJogo.EsconderHud));
            Assert.IsTrue(
                TeclasDoJogo.TentarTrocar(TeclasDoJogo.EsconderHud, "<Keyboard>/f12", out _)
            );
            Assert.AreEqual("<Keyboard>/f12", TeclasDoJogo.Caminho(TeclasDoJogo.EsconderHud));
        }

        [Test]
        public void LiberarCursor_PadraoELeftAltEPodeSerRemapeado()
        {
            Assert.AreEqual("<Keyboard>/leftAlt", TeclasDoJogo.Caminho(TeclasDoJogo.LiberarCursor));
            Assert.IsTrue(
                TeclasDoJogo.TentarTrocar(TeclasDoJogo.LiberarCursor, "<Keyboard>/tab", out _)
            );
            Assert.AreEqual("<Keyboard>/tab", TeclasDoJogo.Caminho(TeclasDoJogo.LiberarCursor));
        }
    }
}
