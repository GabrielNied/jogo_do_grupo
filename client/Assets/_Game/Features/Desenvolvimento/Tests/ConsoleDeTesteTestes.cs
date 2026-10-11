#if UNITY_EDITOR || DEVELOPMENT_BUILD
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Desenvolvimento;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Tests
{
    public class ConsoleDeTesteTestes
    {
        [Test]
        public void Enviar_MandaOTextoAparadoELimpaOCampo()
        {
            string enviado = null;
            var console = Montar(texto => enviado = texto);
            console.Abrir();
            console.Campo.value = "  xp 10  ";

            console.Enviar();

            Assert.AreEqual("xp 10", enviado);
            Assert.AreEqual("", console.Campo.value);
        }

        [Test]
        public void Enviar_VazioNaoChama()
        {
            var chamadas = 0;
            var console = Montar(_ => chamadas++);
            console.Abrir();
            console.Campo.value = "   ";

            console.Enviar();

            Assert.AreEqual(0, chamadas);
        }

        [Test]
        public void Mostrar_RecusaMarcaErroEAceiteTira()
        {
            var console = Montar(_ => { });
            console.Mostrar(new ResultadoDeComandoDeTeste { Aceito = false, Texto = "não" });

            Assert.AreEqual("não", console.Resultado.text);
            Assert.IsTrue(console.Resultado.ClassListContains(EstiloCore.MensagemErro));

            console.Mostrar(new ResultadoDeComandoDeTeste { Aceito = true, Texto = "ok" });
            Assert.IsFalse(console.Resultado.ClassListContains(EstiloCore.MensagemErro));
        }

        private static ConsoleDeTeste Montar(System.Action<string> enviar)
        {
            var raiz = new VisualElement();
            var console = new ConsoleDeTeste();
            console.Montar(raiz, enviar);
            return console;
        }
    }
}
#endif
