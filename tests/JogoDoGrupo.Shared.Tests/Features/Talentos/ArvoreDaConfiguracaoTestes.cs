using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Shared.Tests.Features.Talentos;

public class ArvoreDaConfiguracaoTestes
{
    private static NoDaArvore No(string id, params RequisitoDeNo[] requisitos) =>
        new()
        {
            Id = id,
            Nome = id,
            Caminhos = new[] { AtributoPrincipal.Forca },
            RankMaximo = 2,
            CustoPorRank = new[] { 1, 2 },
            Requisitos = requisitos,
            Efeitos = new[] { new EfeitoDeNo { Atributo = AtributoPrincipal.Forca } },
        };

    private static RequisitoDeNo Pede(string id, int rank = 1) =>
        new() { NoId = id, RankMinimo = rank };

    private static ConfiguracaoDaArvore Config(params NoDaArvore[] nos) =>
        new() { Versao = 1, Nos = nos };

    [Fact]
    public void Arvore_do_jogo_e_valida_e_tem_cinco_caminhos_completos_e_cinco_hibridos()
    {
        var arvore = ArvoreDoJogo.Criar();

        Assert.Empty(ValidadorDaArvore.Validar(arvore));
        Assert.Equal(5, arvore.Nos.Count(n => n.Tipo == TipoDeNo.Hibrido));
        foreach (var caminho in Enum.GetValues<AtributoPrincipal>())
        {
            // 3 principais e 2 secundários, 1 grande e 1 marco por caminho.
            Assert.Equal(
                5,
                arvore.Nos.Count(n => n.Tipo == TipoDeNo.Pequeno && n.Caminhos[0] == caminho)
            );
            Assert.Equal(
                1,
                arvore.Nos.Count(n => n.Tipo == TipoDeNo.Grande && n.Caminhos[0] == caminho)
            );
            Assert.Equal(
                1,
                arvore.Nos.Count(n => n.Tipo == TipoDeNo.Avancado && n.Caminhos[0] == caminho)
            );
        }
    }

    [Fact]
    public void Os_pontos_do_nivel_100_nao_compram_a_arvore_toda_mas_fecham_um_caminho()
    {
        var arvore = ArvoreDoJogo.Criar();
        var pontos =
            JogoDoGrupo.Shared.Features.Personagem.ConfigExperiencia.PontosPorNivel
            * (JogoDoGrupo.Shared.Features.Personagem.ConfigExperiencia.NivelMaximo - 1);
        var caminho = arvore
            .Nos.Where(n => n.Caminhos.Length == 1 && n.Caminhos[0] == AtributoPrincipal.Forca)
            .Sum(n => RegrasDaArvore.CustoPago(n, n.RankMaximo));

        Assert.Equal(99, pontos);
        Assert.True(ArvoreDoJogo.CustoTotal(arvore) > 3 * pontos);
        Assert.InRange(caminho, 50, pontos);
    }

    [Fact]
    public void Todo_no_tem_custo_crescente_ou_igual_e_so_o_marco_tem_um_rank()
    {
        foreach (var no in ArvoreDoJogo.Criar().Nos)
        {
            for (var i = 1; i < no.CustoPorRank.Length; i++)
            {
                Assert.True(no.CustoPorRank[i] >= no.CustoPorRank[i - 1], no.Id);
            }

            Assert.Equal(no.Tipo == TipoDeNo.Avancado, no.RankMaximo == 1);
        }
    }

    [Fact]
    public void Id_repetido_e_recusado_com_o_motivo()
    {
        var problemas = ValidadorDaArvore.Validar(Config(No("a"), No("a")));

        Assert.Contains(problemas, p => p.Contains("repetido") && p.Contains("'a'"));
    }

    [Fact]
    public void Referencia_a_no_inexistente_e_recusada()
    {
        var problemas = ValidadorDaArvore.Validar(Config(No("a", Pede("fantasma"))));

        Assert.Contains(problemas, p => p.Contains("não existe") && p.Contains("fantasma"));
    }

    [Fact]
    public void Ciclo_de_pre_requisito_e_recusado()
    {
        var indireto = ValidadorDaArvore.Validar(
            Config(No("a", Pede("c")), No("b", Pede("a")), No("c", Pede("b")))
        );
        var consigoMesmo = ValidadorDaArvore.Validar(Config(No("a", Pede("a"))));

        Assert.Contains(indireto, p => p.Contains("Ciclo"));
        Assert.Contains(consigoMesmo, p => p.Contains("Ciclo"));
    }

    [Fact]
    public void Dois_nos_pedindo_o_mesmo_requisito_nao_e_ciclo()
    {
        var problemas = ValidadorDaArvore.Validar(
            Config(
                No("raiz"),
                No("a", Pede("raiz")),
                No("b", Pede("raiz")),
                No("c", Pede("a"), Pede("b"))
            )
        );

        Assert.Empty(problemas);
    }

    [Fact]
    public void Custo_invalido_e_recusado()
    {
        var semCusto = No("a");
        semCusto.CustoPorRank = new[] { 1 };
        var zero = No("b");
        zero.CustoPorRank = new[] { 1, 0 };
        var negativo = No("c");
        negativo.CustoPorRank = new[] { -1, 2 };

        var problemas = ValidadorDaArvore.Validar(Config(semCusto, zero, negativo));

        Assert.Contains(problemas, p => p.Contains("'a'") && p.Contains("custo"));
        Assert.Contains(problemas, p => p.Contains("'b'") && p.Contains("custo"));
        Assert.Contains(problemas, p => p.Contains("'c'") && p.Contains("custo"));
    }

    [Fact]
    public void Rank_maximo_requisito_e_efeito_fora_da_regra_sao_recusados()
    {
        var semRank = No("a");
        semRank.RankMaximo = 0;
        semRank.CustoPorRank = new int[0];
        var rankAlto = No("b", Pede("a", 5));
        var semEfeito = No("c");
        semEfeito.Efeitos = new EfeitoDeNo[0];
        var secundarioSemChave = No("d");
        secundarioSemChave.Efeitos = new[]
        {
            new EfeitoDeNo { Tipo = TipoDeEfeito.AtributoSecundario },
        };
        var hibridoComUmCaminho = No("e");
        hibridoComUmCaminho.Tipo = TipoDeNo.Hibrido;

        var problemas = ValidadorDaArvore.Validar(
            Config(semRank, rankAlto, semEfeito, secundarioSemChave, hibridoComUmCaminho)
        );

        Assert.Contains(problemas, p => p.Contains("'a'") && p.Contains("rank máximo"));
        Assert.Contains(problemas, p => p.Contains("'b'") && p.Contains("pede rank 5"));
        Assert.Contains(problemas, p => p.Contains("'c'") && p.Contains("não tem efeito"));
        Assert.Contains(problemas, p => p.Contains("'d'") && p.Contains("sem chave"));
        Assert.Contains(problemas, p => p.Contains("'e'") && p.Contains("híbrido"));
    }

    [Fact]
    public void Configuracao_vazia_ou_sem_versao_e_recusada()
    {
        Assert.NotEmpty(ValidadorDaArvore.Validar(null));
        Assert.NotEmpty(ValidadorDaArvore.Validar(Config()));
        var semVersao = Config(No("a"));
        semVersao.Versao = 0;
        Assert.Contains(ValidadorDaArvore.Validar(semVersao), p => p.Contains("versão"));
    }
}
