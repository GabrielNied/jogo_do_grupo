using JogoDoGrupo.Client.Features.Montarias.UI;
using JogoDoGrupo.Shared.Features.Montarias;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Montarias.Tests
{
    public class JanelaMontariasTestes
    {
        [Test]
        public void CriarDefinicao_RetornaChaveETeclaEsperadas()
        {
            var definicao = JanelaMontarias.CriarDefinicao();

            Assert.IsNotNull(definicao);
            Assert.AreEqual("montarias.colecao", definicao.Chave);
            Assert.AreEqual("Montarias", definicao.Titulo);
            Assert.AreEqual("P", definicao.Tecla);
            Assert.AreEqual(50, definicao.Ordem);

            var janela = definicao.Criar();
            Assert.IsNotNull(janela);
            Assert.IsInstanceOf<JanelaMontarias>(janela);
        }

        [Test]
        public void CriarDefinicaoEstabulo_RetornaChaveETituloEsperados()
        {
            var definicao = JanelaEstabulo.CriarDefinicao();

            Assert.IsNotNull(definicao);
            Assert.AreEqual("montarias.estabulo", definicao.Chave);
            Assert.AreEqual("Estábulo", definicao.Titulo);
            Assert.IsNull(definicao.Tecla);
            Assert.AreEqual(104, definicao.Ordem);

            var janela = definicao.Criar();
            Assert.IsNotNull(janela);
            Assert.IsInstanceOf<JanelaEstabulo>(janela);
        }

        [Test]
        public void Selo_RetornaNomeEmMaiusculoESemSimbolosParaCadaMontariaDoCatalogo()
        {
            foreach (var tipo in RegrasMontaria.TiposDoCatalogo)
            {
                var selo = JanelaMontarias.TextoDoSelo(tipo);
                var esperado = RegrasMontaria.ObterNome(tipo).ToUpperInvariant();

                Assert.AreEqual(esperado, selo);
                StringAssert.DoesNotContain("✦", selo);
                StringAssert.DoesNotContain("❖", selo);
            }

            Assert.AreEqual(
                "CORCEL CASTANHO",
                JanelaMontarias.TextoDoSelo(RegrasMontaria.TipoCastanho)
            );
            Assert.AreEqual(
                "CORCEL NOTURNO",
                JanelaMontarias.TextoDoSelo(RegrasMontaria.TipoNoturno)
            );
            Assert.AreEqual(
                "LOBO DE PRESA",
                JanelaMontarias.TextoDoSelo(RegrasMontaria.TipoLoboDePresa)
            );
            Assert.AreEqual(
                "CORCEL DE CRISTAL",
                JanelaMontarias.TextoDoSelo(RegrasMontaria.TipoCorcelDeCristal)
            );
            Assert.AreEqual(
                "MULA DE CARGA",
                JanelaMontarias.TextoDoSelo(RegrasMontaria.TipoMulaDeCarga)
            );
        }

        [Test]
        public void Selo_RetornaClasseDeRaridadeCorreta()
        {
            Assert.AreEqual(
                "raridade-rara",
                JanelaMontarias.ClasseDeRaridadeDoSelo(RegrasMontaria.TipoNoturno)
            );
            Assert.AreEqual(
                "raridade-rara",
                JanelaMontarias.ClasseDeRaridadeDoSelo(RegrasMontaria.TipoLoboDePresa)
            );
            Assert.AreEqual(
                "raridade-rara",
                JanelaMontarias.ClasseDeRaridadeDoSelo(RegrasMontaria.TipoCorcelDeCristal)
            );
            Assert.AreEqual(
                "raridade-comum",
                JanelaMontarias.ClasseDeRaridadeDoSelo(RegrasMontaria.TipoCastanho)
            );
            Assert.AreEqual(
                "raridade-comum",
                JanelaMontarias.ClasseDeRaridadeDoSelo(RegrasMontaria.TipoMulaDeCarga)
            );
            Assert.AreEqual(
                "raridade-comum",
                JanelaMontarias.ClasseDeRaridadeDoSelo(RegrasMontaria.TipoCavaloComum)
            );
        }
    }
}
