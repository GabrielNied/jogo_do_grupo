using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Shared.Tests.Features.Skills;

public class RegrasSkillsTestes
{
    private static readonly TipoEquipamento[] Armas =
    [
        TipoEquipamento.Espada,
        TipoEquipamento.Martelo,
        TipoEquipamento.Arco,
        TipoEquipamento.Cajado,
        TipoEquipamento.Foco,
        TipoEquipamento.Escudo,
    ];

    [Fact]
    public void Toda_arma_tem_pool_para_a_barra_e_cada_skill_declara_suas_propriedades()
    {
        foreach (var tipo in Armas)
        {
            var pool = CatalogoDeSkills.Pool(tipo);
            Assert.True(pool.Count >= ConfigSkills.SkillsPorArma, tipo.ToString());
            foreach (var skill in pool)
            {
                Assert.Same(skill, CatalogoDeSkills.Obter(skill.Id));
                Assert.Equal(skill.Id, skill.Propriedades.Id);
                Assert.True(skill.Custo > 0 && skill.Multiplicador > 0);
                Assert.True(skill.RecargaMs >= ConfigSkills.RecargaMinimaMs);
            }
        }

        Assert.Empty(CatalogoDeSkills.Pool(TipoEquipamento.Peito)); // Armadura não tem ativas.
    }

    [Fact]
    public void Recurso_acompanha_a_categoria_do_dano()
    {
        foreach (var skill in Armas.SelectMany(t => CatalogoDeSkills.Pool(t)))
        {
            var esperado =
                skill.Propriedades.Categoria == CategoriaDeDano.Magica
                    ? RecursoDeSkill.Mana
                    : RecursoDeSkill.Stamina;
            Assert.Equal(esperado, skill.Recurso);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Sorteio_devolve_tres_skills_do_pool_sem_repetir(int indiceFixo)
    {
        foreach (var tipo in Armas)
        {
            var ids = RegrasSkills.Sortear(tipo, limite => Math.Min(indiceFixo, limite - 1));

            Assert.Equal(ConfigSkills.SkillsPorArma, ids.Length);
            Assert.Equal(ids.Length, ids.Distinct().Count());
            var pool = CatalogoDeSkills.Pool(tipo).Select(s => s.Id).ToArray();
            Assert.All(ids, id => Assert.Contains(id, pool));
            Assert.True(RegrasSkills.IdsValidos(tipo, ids));
        }
    }

    [Fact]
    public void Ids_de_outro_tipo_repetidos_ou_incompletos_nao_valem()
    {
        var espada = CatalogoDeSkills.Pool(TipoEquipamento.Espada).Select(s => s.Id).ToArray();
        var cajado = CatalogoDeSkills.Pool(TipoEquipamento.Cajado).Select(s => s.Id).ToArray();

        Assert.True(RegrasSkills.IdsValidos(TipoEquipamento.Espada, espada.Take(3).ToArray()));
        Assert.False(RegrasSkills.IdsValidos(TipoEquipamento.Espada, cajado.Take(3).ToArray()));
        Assert.False(RegrasSkills.IdsValidos(TipoEquipamento.Espada, espada.Take(2).ToArray()));
        Assert.False(
            RegrasSkills.IdsValidos(TipoEquipamento.Espada, [espada[0], espada[0], espada[1]])
        );
        Assert.False(RegrasSkills.IdsValidos(TipoEquipamento.Espada, null));
        Assert.Empty(RegrasSkills.Sortear(TipoEquipamento.Capacete, _ => 0));
    }

    [Fact]
    public void Dano_e_arma_mais_flat_vezes_o_multiplicador_com_o_flat_da_categoria()
    {
        var fisica = CatalogoDeSkills.Obter("espada.lamina-giratoria")!; // x2,0
        var magica = CatalogoDeSkills.Obter("cajado.raio-arcano")!; // x2,0
        var hibrida = CatalogoDeSkills.Obter("arco.linha-incendiaria")!; // x2,4, meio a meio

        Assert.Equal(2 * (20 + 5), RegrasSkills.Dano(fisica, 20, 5, 100));
        Assert.Equal(2 * (20 + 7), RegrasSkills.Dano(magica, 20, 100, 7));
        Assert.Equal((int)Math.Round(2.4 * (20 + 6)), RegrasSkills.Dano(hibrida, 20, 4, 8));
        Assert.Equal(1, RegrasSkills.Dano(fisica, 0, 0, 0)); // Nunca menos que 1.
    }
}
