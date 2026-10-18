using JogoDoGrupo.Client.Features.Skills.UI;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Skills.Tests
{
    public class JanelaSkillsTestes
    {
        [Test]
        public void CriarDefinicao_RetornaChaveETeclaEsperadas()
        {
            var definicao = JanelaSkills.CriarDefinicao();

            Assert.IsNotNull(definicao);
            Assert.AreEqual("skills.livro", definicao.Chave);
            Assert.AreEqual("Skills", definicao.Titulo);
            Assert.AreEqual("K", definicao.Tecla);
            Assert.AreEqual(35, definicao.Ordem);

            var janela = definicao.Criar();
            Assert.IsNotNull(janela);
            Assert.IsInstanceOf<JanelaSkills>(janela);
        }

        [Test]
        public void ApresentacaoSkills_NomeDaArma_FormataCorretamente()
        {
            Assert.AreEqual("Espada", ApresentacaoSkills.NomeDaArma(TipoEquipamento.Espada));
            Assert.AreEqual("Martelo", ApresentacaoSkills.NomeDaArma(TipoEquipamento.Martelo));
            Assert.AreEqual("Arco", ApresentacaoSkills.NomeDaArma(TipoEquipamento.Arco));
            Assert.AreEqual("Cajado", ApresentacaoSkills.NomeDaArma(TipoEquipamento.Cajado));
            Assert.AreEqual("Foco", ApresentacaoSkills.NomeDaArma(TipoEquipamento.Foco));
            Assert.AreEqual("Escudo", ApresentacaoSkills.NomeDaArma(TipoEquipamento.Escudo));
        }

        [Test]
        public void ApresentacaoSkills_FormatadoresDeCatalogo()
        {
            var espadaSkill = CatalogoDeSkills.Obter("espada.corte-veloz");
            Assert.IsNotNull(espadaSkill);
            Assert.AreEqual("15 Stamina", ApresentacaoSkills.Custo(espadaSkill));
            Assert.AreEqual("8 s", ApresentacaoSkills.RecargaEmSegundos(espadaSkill.RecargaMs));
            Assert.AreEqual("130% do dano base", ApresentacaoSkills.DescricaoDano(espadaSkill));
            Assert.AreEqual("Física", ApresentacaoSkills.DescricaoCategoria(espadaSkill));

            var cajadoSkill = CatalogoDeSkills.Obter("cajado.estilhaco-glacial");
            Assert.IsNotNull(cajadoSkill);
            Assert.AreEqual("20 Mana", ApresentacaoSkills.Custo(cajadoSkill));
            Assert.AreEqual("10 s", ApresentacaoSkills.RecargaEmSegundos(cajadoSkill.RecargaMs));
            Assert.AreEqual("Mágica", ApresentacaoSkills.DescricaoCategoria(cajadoSkill));
        }

        [Test]
        public void EstadoDaBarra_IdentificaArmaESkills()
        {
            var estado = new EstadoDaBarra();
            Assert.IsFalse(estado.TemSkills);
            Assert.IsNull(estado.TipoDaArmaAtiva());

            var barra = new BarraDeSkillsMudou
            {
                ArmaId = 42,
                Skills = new[]
                {
                    new SkillDaBarra
                    {
                        Espaco = 0,
                        Id = "espada.corte-veloz",
                        Nome = "Corte Veloz",
                    },
                    new SkillDaBarra
                    {
                        Espaco = 1,
                        Id = "espada.ruptura-sangrenta",
                        Nome = "Ruptura Sangrenta",
                    },
                    new SkillDaBarra
                    {
                        Espaco = 2,
                        Id = "espada.lamina-giratoria",
                        Nome = "Lâmina Giratória",
                    },
                },
            };

            estado.Aplicar(barra, 100.0);

            Assert.IsTrue(estado.TemSkills);
            Assert.AreEqual(42, estado.ArmaId);
            Assert.AreEqual(TipoEquipamento.Espada, estado.TipoDaArmaAtiva());
            Assert.IsTrue(estado.ContemSkill("espada.corte-veloz"));
            Assert.IsTrue(estado.ContemSkill("espada.lamina-giratoria"));
            Assert.IsFalse(estado.ContemSkill("cajado.surto-solar"));
            Assert.AreEqual(0, estado.EspacoDaSkill("espada.corte-veloz"));
            Assert.AreEqual(2, estado.EspacoDaSkill("espada.lamina-giratoria"));
            Assert.AreEqual(-1, estado.EspacoDaSkill("inexistente"));
        }
    }
}
