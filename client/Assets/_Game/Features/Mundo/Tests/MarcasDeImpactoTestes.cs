using JogoDoGrupo.Client.Features.Mundo.MarcasDeImpacto;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class MarcasDeImpactoTestes
    {
        [Test]
        public void Passando_do_teto_a_mais_antiga_sai_primeiro()
        {
            var fila = new FilaDeMarcas<int>();
            for (var i = 1; i <= 32; i++)
            {
                Assert.IsEmpty(fila.Adicionar(i, i, 32));
            }

            var saidas = fila.Adicionar(33, 33, 32);

            CollectionAssert.AreEqual(new[] { 1 }, saidas);
            Assert.AreEqual(32, fila.Quantidade);
        }

        [Test]
        public void Marca_expira_so_depois_da_duracao()
        {
            var fila = new FilaDeMarcas<string>();
            fila.Adicionar("a", 0, 32);
            fila.Adicionar("b", 5, 32);

            Assert.IsEmpty(fila.Expirar(19.9, 20f));
            CollectionAssert.AreEqual(new[] { "a" }, fila.Expirar(20, 20f));
            CollectionAssert.AreEqual(new[] { "b" }, fila.Expirar(25, 20f));
        }

        [Test]
        public void Opacidade_e_cheia_ate_o_fade_e_zera_no_fim()
        {
            Assert.AreEqual(1f, FilaDeMarcas<int>.Opacidade(0f, 20f, 4f), 1e-4f);
            Assert.AreEqual(1f, FilaDeMarcas<int>.Opacidade(16f, 20f, 4f), 1e-4f);
            Assert.AreEqual(0.5f, FilaDeMarcas<int>.Opacidade(18f, 20f, 4f), 1e-4f);
            Assert.AreEqual(0f, FilaDeMarcas<int>.Opacidade(20f, 20f, 4f), 1e-4f);
        }

        [Test]
        public void Decisao_da_marca_por_skill_e_por_golpe()
        {
            Assert.IsTrue(DecisaoDeMarca.DaSkill("martelo.martelada-sismica", out var m));
            Assert.AreEqual(TipoDeMarca.Martelada, m);
            Assert.IsTrue(DecisaoDeMarca.DaSkill("cajado.nova-de-fogo", out var f));
            Assert.AreEqual(TipoDeMarca.Fuligem, f);
            Assert.IsFalse(DecisaoDeMarca.DaSkill("espada.corte-veloz", out _));
            Assert.IsTrue(DecisaoDeMarca.DoGolpePesadoDeMartelo(true, TipoEquipamento.Martelo));
            Assert.IsFalse(DecisaoDeMarca.DoGolpePesadoDeMartelo(false, TipoEquipamento.Martelo));
            Assert.IsFalse(DecisaoDeMarca.DoGolpePesadoDeMartelo(true, TipoEquipamento.Espada));
        }
    }
}
