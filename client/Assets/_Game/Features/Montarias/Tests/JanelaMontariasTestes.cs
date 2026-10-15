using JogoDoGrupo.Client.Features.Montarias.UI;
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
            Assert.AreEqual("B", definicao.Tecla);
            Assert.AreEqual(50, definicao.Ordem);

            var janela = definicao.Criar();
            Assert.IsNotNull(janela);
            Assert.IsInstanceOf<JanelaMontarias>(janela);
        }
    }
}
