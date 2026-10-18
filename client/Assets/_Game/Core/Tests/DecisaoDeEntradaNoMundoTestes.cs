using JogoDoGrupo.Client.Core.Jogo;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class DecisaoDeEntradaNoMundoTestes
    {
        [Test]
        public void DeveLerInput_SemDigitacaoSemMenuConectado_RetornaVerdadeiro()
        {
            var podeLer = DecisaoDeEntradaNoMundo.DeveLerInput(
                menuAberto: false,
                entradaPendente: false,
                conectado: true,
                entradaDeTextoAtiva: false
            );

            Assert.IsTrue(podeLer);
        }

        [Test]
        public void DeveLerInput_ComDigitacaoAtiva_RetornaFalso()
        {
            var podeLer = DecisaoDeEntradaNoMundo.DeveLerInput(
                menuAberto: false,
                entradaPendente: false,
                conectado: true,
                entradaDeTextoAtiva: true
            );

            Assert.IsFalse(podeLer);
        }

        [Test]
        public void DeveLerInput_ComMenuAberto_RetornaFalso()
        {
            var podeLer = DecisaoDeEntradaNoMundo.DeveLerInput(
                menuAberto: true,
                entradaPendente: false,
                conectado: true,
                entradaDeTextoAtiva: false
            );

            Assert.IsFalse(podeLer);
        }

        [Test]
        public void DeveLerInput_ComEntradaPendente_RetornaFalso()
        {
            var podeLer = DecisaoDeEntradaNoMundo.DeveLerInput(
                menuAberto: false,
                entradaPendente: true,
                conectado: true,
                entradaDeTextoAtiva: false
            );

            Assert.IsFalse(podeLer);
        }

        [Test]
        public void DeveLerInput_Desconectado_RetornaFalso()
        {
            var podeLer = DecisaoDeEntradaNoMundo.DeveLerInput(
                menuAberto: false,
                entradaPendente: false,
                conectado: false,
                entradaDeTextoAtiva: false
            );

            Assert.IsFalse(podeLer);
        }

        [Test]
        public void DeveLerInputDeMovimento_RespeitaEntradaDeTexto()
        {
            Assert.IsTrue(
                DecisaoDeEntradaNoMundo.DeveLerInputDeMovimento(
                    menuAberto: false,
                    entradaPendente: false,
                    conectado: true,
                    entradaDeTextoAtiva: false
                )
            );

            Assert.IsFalse(
                DecisaoDeEntradaNoMundo.DeveLerInputDeMovimento(
                    menuAberto: false,
                    entradaPendente: false,
                    conectado: true,
                    entradaDeTextoAtiva: true
                )
            );
        }

        [Test]
        public void PodeExecutarAtalho_ComFocoOuBloqueio_RetornaFalso()
        {
            Assert.IsFalse(
                DecisaoDeEntradaNoMundo.PodeExecutarAtalho(
                    campoComFoco: true,
                    bloqueadoPorTexto: false
                )
            );
            Assert.IsFalse(
                DecisaoDeEntradaNoMundo.PodeExecutarAtalho(
                    campoComFoco: false,
                    bloqueadoPorTexto: true
                )
            );
            Assert.IsFalse(
                DecisaoDeEntradaNoMundo.PodeExecutarAtalho(
                    campoComFoco: true,
                    bloqueadoPorTexto: true
                )
            );
            Assert.IsTrue(
                DecisaoDeEntradaNoMundo.PodeExecutarAtalho(
                    campoComFoco: false,
                    bloqueadoPorTexto: false
                )
            );
        }

        [Test]
        public void DecidirEsc_ComCampoComFoco_IgnoraPorEntradaDeTexto()
        {
            var decisao = DecisaoDeEntradaNoMundo.DecidirEsc(
                campoComFoco: true,
                bloqueadoPorTexto: false,
                escConsumido: false,
                submenuAberto: false
            );

            Assert.AreEqual(DecisaoDoEsc.IgnorarPorEntradaDeTexto, decisao);
        }

        [Test]
        public void DecidirEsc_ComBloqueadoPorTexto_IgnoraPorEntradaDeTexto()
        {
            var decisao = DecisaoDeEntradaNoMundo.DecidirEsc(
                campoComFoco: false,
                bloqueadoPorTexto: true,
                escConsumido: false,
                submenuAberto: false
            );

            Assert.AreEqual(DecisaoDoEsc.IgnorarPorEntradaDeTexto, decisao);
        }

        [Test]
        public void DecidirEsc_ComEscConsumido_IgnoraPorConsumoAnterior()
        {
            var decisao = DecisaoDeEntradaNoMundo.DecidirEsc(
                campoComFoco: false,
                bloqueadoPorTexto: false,
                escConsumido: true,
                submenuAberto: false
            );

            Assert.AreEqual(DecisaoDoEsc.IgnorarPorConsumoAnterior, decisao);
        }

        [Test]
        public void DecidirEsc_ComSubmenuAberto_VoltaDoSubmenu()
        {
            var decisao = DecisaoDeEntradaNoMundo.DecidirEsc(
                campoComFoco: false,
                bloqueadoPorTexto: false,
                escConsumido: false,
                submenuAberto: true
            );

            Assert.AreEqual(DecisaoDoEsc.VoltarDoSubmenu, decisao);
        }

        [Test]
        public void DecidirEsc_Normal_AlternaMenu()
        {
            var decisao = DecisaoDeEntradaNoMundo.DecidirEsc(
                campoComFoco: false,
                bloqueadoPorTexto: false,
                escConsumido: false,
                submenuAberto: false
            );

            Assert.AreEqual(DecisaoDoEsc.AlternarMenu, decisao);
        }
    }
}
