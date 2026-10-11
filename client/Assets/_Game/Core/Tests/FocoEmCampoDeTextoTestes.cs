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
    }
}
