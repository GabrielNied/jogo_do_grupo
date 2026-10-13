#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JogoDoGrupo.Client.Core.Jogo.Chat;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Tests
{
    public class RegistroDeComandosDeTesteNoChatTestes
    {
        [Test]
        public void MontarTexto_JuntaVerboEResto()
        {
            Assert.AreEqual("xp 10", RegistroDeComandosDeTesteNoChat.MontarTexto("xp", "10"));
            Assert.AreEqual("spawn", RegistroDeComandosDeTesteNoChat.MontarTexto("spawn", ""));
        }

        [Test]
        public void Executar_InvalidoMostraAvisoNoSistema()
        {
            string sistema = null;
            var contexto = new ContextoDeComandoDeBarra { MostrarSistema = t => sistema = t };

            var resultado = RegistroDeComandosDeTesteNoChat.Executar("xp", "", contexto);

            Assert.IsNotNull(sistema);
            Assert.IsTrue(sistema.Contains("xp"));
            Assert.IsTrue(resultado.FecharCaixa);
        }

        [Test]
        public void Executar_AjudaListaComandos()
        {
            string sistema = null;
            var contexto = new ContextoDeComandoDeBarra { MostrarSistema = t => sistema = t };

            RegistroDeComandosDeTesteNoChat.Executar("ajuda", "", contexto);

            StringAssert.Contains("/xp", sistema);
        }
    }
}
#endif
