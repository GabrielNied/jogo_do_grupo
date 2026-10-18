using JogoDoGrupo.Client.Features.Desenvolvimento.Admin;
using JogoDoGrupo.Shared.Features.Desenvolvimento;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Tests
{
    public class PedidosDeAdminTestes
    {
        [Test]
        public void Item_CarregaOsCamposEPassaNaValidacao()
        {
            var pedido = PedidosDeAdmin.Item(
                CatalogoEquipamentos.PrimeiroModelo,
                GrauEquipamento.Forte,
                OrigemDoLoot.Chefe,
                3
            );

            Assert.AreEqual(AcaoDeAdmin.GerarItem, pedido.Acao);
            Assert.AreEqual(GrauEquipamento.Forte, pedido.Grau);
            Assert.AreEqual(OrigemDoLoot.Chefe, pedido.Origem);
            Assert.AreEqual(3, pedido.Quantidade);
            Assert.IsNull(RegrasAdmin.Validar(pedido));
        }

        [Test]
        public void ComValor_UsaAAcaoEOValor()
        {
            var pedido = PedidosDeAdmin.ComValor(AcaoDeAdmin.AjustarCoroas, -50);

            Assert.AreEqual(AcaoDeAdmin.AjustarCoroas, pedido.Acao);
            Assert.AreEqual(-50, pedido.Valor);
        }

        [Test]
        public void Teleporte_GuardaAsTresCoordenadas()
        {
            var pedido = PedidosDeAdmin.Teleporte(1f, 2f, 3f);

            Assert.AreEqual(AcaoDeAdmin.Teleportar, pedido.Acao);
            Assert.AreEqual(2f, pedido.Y);
            Assert.AreEqual(3f, pedido.Z);
        }

        [Test]
        public void LimitarQuantidade_FicaNaFaixa()
        {
            Assert.AreEqual(1, PedidosDeAdmin.LimitarQuantidade(-4, 20));
            Assert.AreEqual(7, PedidosDeAdmin.LimitarQuantidade(7, 20));
            Assert.AreEqual(20, PedidosDeAdmin.LimitarQuantidade(99, 20));
        }

        [Test]
        public void TextoDoResultado_UsaOTextoOuUmPadrao()
        {
            Assert.AreEqual(
                "Entrou.",
                PedidosDeAdmin.TextoDoResultado(
                    new ResultadoDeAcaoDeAdmin { Aceito = true, Texto = "Entrou." }
                )
            );
            Assert.AreEqual(
                "Feito.",
                PedidosDeAdmin.TextoDoResultado(new ResultadoDeAcaoDeAdmin { Aceito = true })
            );
            Assert.AreEqual(
                "Recusado.",
                PedidosDeAdmin.TextoDoResultado(new ResultadoDeAcaoDeAdmin())
            );
            Assert.AreEqual("Recusado.", PedidosDeAdmin.TextoDoResultado(null));
        }

        [Test]
        public void NomeDoModelo_MostraNomeEId()
        {
            var texto = PedidosDeAdmin.NomeDoModelo(CatalogoEquipamentos.PrimeiroModelo);

            StringAssert.EndsWith($"({CatalogoEquipamentos.PrimeiroModelo})", texto);
            Assert.AreEqual("99999", PedidosDeAdmin.NomeDoModelo(99999));
        }
    }
}
