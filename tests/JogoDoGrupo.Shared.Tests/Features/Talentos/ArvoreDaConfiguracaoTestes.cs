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
    public void Arvore_de_teste_e_valida_e_tem_os_cinco_caminhos_e_cinco_hibridos()
    {
        var arvore = ArvoreDeTeste.Criar();

        Assert.Empty(ValidadorDaArvore.Validar(arvore));
        Assert.Equal(5, arvore.Nos.Count(n => n.Tipo == TipoDeNo.Hibrido));
        foreach (var caminho in Enum.GetValues<AtributoPrincipal>())
        {
            Assert.Equal(
                3,
                arvore.Nos.Count(n => n.Tipo == TipoDeNo.Pequeno && n.Caminhos[0] == caminho)
            );
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
