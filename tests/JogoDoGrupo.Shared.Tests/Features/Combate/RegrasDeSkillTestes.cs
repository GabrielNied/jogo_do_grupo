using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasDeSkillTestes
{
    private static readonly AtributosSecundarios Secundarios = new()
    {
        ChanceCritica = 0.5,
        DanoCritico = 2,
        VelocidadeAtaqueFisico = 1.5,
        VelocidadeAtaqueMagico = 1.2,
        PenetracaoFisica = 0.5,
        PenetracaoMagica = 0.25,
    };

    private static PropriedadesDaSkill Skill(
        CategoriaDeDano categoria,
        bool flags = true,
        double fracaoFisica = 0.5
    ) =>
        new()
        {
            Id = "teste",
            Categoria = categoria,
            Tipo = TipoDeEfeito.Dano,
            PodeCritar = flags,
            RecebePenetracao = flags,
            PodeSerAcelerada = flags,
            FracaoFisica = fracaoFisica,
        };

    private static double Dano(
        PropriedadesDaSkill skill,
        double defesaFisica,
        double defesaMagica
    ) => RegrasDeSkill.DanoAposDefesa(skill, Secundarios, 100, defesaFisica, defesaMagica);

    [Fact]
    public void Critico_so_com_a_flag()
    {
        var com = Skill(CategoriaDeDano.Fisica);
        var sem = Skill(CategoriaDeDano.Fisica, flags: false);

        Assert.Equal(2, RegrasDeSkill.MultiplicadorCritico(com, Secundarios, 0.49));
        Assert.Equal(1, RegrasDeSkill.MultiplicadorCritico(com, Secundarios, 0.5));
        Assert.Equal(1, RegrasDeSkill.MultiplicadorCritico(sem, Secundarios, 0));
        Assert.Equal(0, RegrasDeSkill.ChanceCritica(sem, Secundarios));
    }

    [Fact]
    public void Penetracao_e_velocidade_so_com_a_flag()
    {
        var sem = Skill(CategoriaDeDano.Fisica, flags: false);

        Assert.Equal(0, RegrasDeSkill.PenetracaoFisica(sem, Secundarios));
        Assert.Equal(1, RegrasDeSkill.Velocidade(sem, Secundarios));
        // Sem penetração a defesa inteira vale: 100 de dano com defesa 100 = metade.
        Assert.Equal(50, Dano(sem, 100, 0), 9);
    }

    [Fact]
    public void Penetracao_fisica_age_so_na_defesa_fisica()
    {
        var fisica = Skill(CategoriaDeDano.Fisica);

        // Defesa 100 com 50% de penetração = 50: 100 * 100 / 150.
        Assert.Equal(100.0 * 100 / 150, Dano(fisica, 100, 100), 9);
        // A defesa mágica do alvo não conta para golpe físico.
        Assert.Equal(100.0 * 100 / 150, Dano(fisica, 100, 9999), 9);
    }

    [Fact]
    public void Penetracao_magica_age_so_na_defesa_magica()
    {
        var magica = Skill(CategoriaDeDano.Magica);

        // Defesa 100 com 25% de penetração = 75: 100 * 100 / 175.
        Assert.Equal(100.0 * 100 / 175, Dano(magica, 9999, 100), 9);
    }

    [Fact]
    public void Hibrida_divide_o_dano_e_cada_parte_recebe_so_o_seu_tipo()
    {
        var hibrida = Skill(CategoriaDeDano.Ambas, fracaoFisica: 0.6);

        var esperado = 60.0 * 100 / 150 + 40.0 * 100 / 175;

        Assert.Equal(esperado, Dano(hibrida, 100, 100), 9);
    }

    [Fact]
    public void Hibrida_critica_uma_vez_e_usa_uma_velocidade_so()
    {
        var predominaFisica = Skill(CategoriaDeDano.Ambas, fracaoFisica: 0.5);
        var predominaMagica = Skill(CategoriaDeDano.Ambas, fracaoFisica: 0.4);

        // Um sorteio só por golpe: o crítico multiplica o total uma vez (nunca por parte).
        Assert.Equal(2, RegrasDeSkill.MultiplicadorCritico(predominaFisica, Secundarios, 0.1));
        // Velocidade: a da categoria dominante, com empate na física; nunca as duas.
        Assert.Equal(1.5, RegrasDeSkill.Velocidade(predominaFisica, Secundarios));
        Assert.Equal(1.2, RegrasDeSkill.Velocidade(predominaMagica, Secundarios));
    }
}
