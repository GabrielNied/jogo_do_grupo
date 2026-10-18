using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class FocoEmCampoDeTextoTestes
    {
        [TearDown]
        public void Limpar()
        {
            BloqueioDeEntradaPorTexto.LimparRegistros();
        }

        [Test]
        public void CampoNaHierarquia_AchaOTextFieldPai()
        {
            var campo = new TextField();
            var interno = new VisualElement { name = "unity-text-input" };
            campo.Add(interno);

            Assert.AreSame(campo, FocoEmCampoDeTexto.CampoNaHierarquia(interno));
        }

        [Test]
        public void CampoNaHierarquia_RotuloRetornaNulo()
        {
            Assert.IsNull(FocoEmCampoDeTexto.CampoNaHierarquia(new Label("x")));
        }

        [Test]
        public void TemCampoComFoco_SemPainel_RetornaFalso()
        {
            var hud = new VisualElement();
            var campo = new TextField();
            hud.Add(campo);

            Assert.IsFalse(FocoEmCampoDeTexto.TemCampoComFoco(hud));
        }

        [Test]
        public void AtalhosDoJogoBloqueados_QuandoRegistroAtivo()
        {
            BloqueioDeEntradaPorTexto.RegistrarBloqueioDeAtalhos(() => true);

            Assert.IsTrue(BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados());
        }

        [Test]
        public void DigitacaoAtiva_QuandoCondicaoRegistrada_RetornaVerdadeiro()
        {
            Assert.IsFalse(BloqueioDeEntradaPorTexto.DigitacaoAtiva());
            BloqueioDeEntradaPorTexto.RegistrarBloqueioDeAtalhos(() => true);
            Assert.IsTrue(BloqueioDeEntradaPorTexto.DigitacaoAtiva());
        }

        [Test]
        public void DigitacaoMudou_NotificaMudancaDeEstado()
        {
            var notificou = false;
            var novoEstado = false;

            BloqueioDeEntradaPorTexto.DigitacaoMudou += ativa =>
            {
                notificou = true;
                novoEstado = ativa;
            };

            BloqueioDeEntradaPorTexto.AtualizarEstado(true);
            Assert.IsTrue(notificou);
            Assert.IsTrue(novoEstado);

            notificou = false;
            BloqueioDeEntradaPorTexto.AtualizarEstado(true);
            Assert.IsFalse(notificou, "Não deve notificar se o estado não mudou");

            BloqueioDeEntradaPorTexto.AtualizarEstado(false);
            Assert.IsTrue(notificou);
            Assert.IsFalse(novoEstado);
        }

        [Test]
        public void RegistrarEDesregistrarRaiz_NaoGeraExcecaoETemCampoComFocoFunciona()
        {
            var raizExtra = new VisualElement();
            var campo = new TextField();
            raizExtra.Add(campo);

            BloqueioDeEntradaPorTexto.RegistrarRaiz(raizExtra);
            Assert.IsFalse(FocoEmCampoDeTexto.TemCampoComFoco());

            BloqueioDeEntradaPorTexto.DesregistrarRaiz(raizExtra);
            Assert.IsFalse(FocoEmCampoDeTexto.TemCampoComFoco());
        }

        [Test]
        public void SoltarFoco_SemElementosFocados_NaoLancaExcecao()
        {
            Assert.DoesNotThrow(() => FocoEmCampoDeTexto.SoltarFoco());
        }
    }
}
