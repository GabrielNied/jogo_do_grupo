using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Shared.Tests.Features.Talentos;

public class RegrasDaArvoreTestes
{
    private static readonly ConfiguracaoDaArvore Arvore = ArvoreDoJogo.Criar();
    private static readonly string Forca1 = ArvoreDoJogo.IdPrincipal(AtributoPrincipal.Forca, 0);
    private static readonly string Forca2 = ArvoreDoJogo.IdPrincipal(AtributoPrincipal.Forca, 1);
    private static readonly string Vigor1 = ArvoreDoJogo.IdPrincipal(AtributoPrincipal.Vigor, 0);
    private static readonly string Vigor2 = ArvoreDoJogo.IdPrincipal(AtributoPrincipal.Vigor, 1);

    private static Dictionary<string, int> Ranks(params (string No, int Rank)[] itens) =>
        itens.ToDictionary(i => i.No, i => i.Rank);

    [Fact]
    public void Compra_valida_cobra_o_custo_do_proximo_rank()
    {
        var primeira = RegrasDaArvore.AvaliarCompra(Arvore, Ranks(), 5, Forca1, 1);
        var terceira = RegrasDaArvore.AvaliarCompra(Arvore, Ranks((Forca1, 2)), 5, Forca1, 3);

        Assert.Null(primeira.Erro);
        Assert.Equal(ArvoreDoJogo.CustoPrincipal[0], primeira.Custo);
        Assert.Null(terceira.Erro);
        Assert.Equal(ArvoreDoJogo.CustoPrincipal[2], terceira.Custo);
    }

    [Fact]
    public void Sem_pontos_suficientes_recusa()
    {
        var avaliacao = RegrasDaArvore.AvaliarCompra(Arvore, Ranks((Forca1, 2)), 1, Forca1, 3);

        Assert.Equal(CodigosErroTalentos.PontosInsuficientes, avaliacao.Erro);
    }

    [Fact]
    public void Sem_requisito_recusa_e_com_o_rank_minimo_libera()
    {
        var sem = RegrasDaArvore.AvaliarCompra(Arvore, Ranks((Forca1, 1)), 9, Forca2, 1);
        var com = RegrasDaArvore.AvaliarCompra(
            Arvore,
            Ranks((Forca1, ArvoreDoJogo.RankParaAbrirProximo)),
            9,
            Forca2,
            1
        );

        Assert.Equal(CodigosErroTalentos.RequisitoNaoCumprido, sem.Erro);
        Assert.Null(com.Erro);
    }

    [Fact]
    public void No_no_maximo_recusa_e_nunca_passa_do_maximo()
    {
        var cheio = Ranks((Forca1, ArvoreDoJogo.RankMaximoPrincipal));

        var alem = RegrasDaArvore.AvaliarCompra(
            Arvore,
            cheio,
            99,
            Forca1,
            ArvoreDoJogo.RankMaximoPrincipal + 1
        );

        Assert.Equal(CodigosErroTalentos.RankMaximo, alem.Erro);
    }

    [Fact]
    public void Pedido_repetido_ou_atrasado_e_sucesso_sem_custo()
    {
        var ranks = Ranks((Forca1, 2));

        var repetido = RegrasDaArvore.AvaliarCompra(Arvore, ranks, 0, Forca1, 2);
        var atrasado = RegrasDaArvore.AvaliarCompra(Arvore, ranks, 0, Forca1, 1);

        Assert.True(repetido.JaComprado);
        Assert.Null(repetido.Erro);
        Assert.Equal(0, repetido.Custo);
        Assert.True(atrasado.JaComprado);
    }

    [Fact]
    public void Rank_alvo_que_pula_ou_e_menor_que_um_e_no_inexistente_recusam()
    {
        Assert.Equal(
            CodigosErroTalentos.RankAlvoInvalido,
            RegrasDaArvore.AvaliarCompra(Arvore, Ranks(), 9, Forca1, 2).Erro
        );
        Assert.Equal(
            CodigosErroTalentos.RankAlvoInvalido,
            RegrasDaArvore.AvaliarCompra(Arvore, Ranks(), 9, Forca1, 0).Erro
        );
        Assert.Equal(
            CodigosErroTalentos.RankAlvoInvalido,
            RegrasDaArvore.AvaliarCompra(Arvore, Ranks(), 9, Forca1, -3).Erro
        );
        Assert.Equal(
            CodigosErroTalentos.NoInvalido,
            RegrasDaArvore.AvaliarCompra(Arvore, Ranks(), 9, "x", 1).Erro
        );
    }

    [Fact]
    public void Estado_do_no_segue_os_ranks_e_os_requisitos()
    {
        var forca1 = RegrasDaArvore.Procurar(Arvore, Forca1)!;
        var forca2 = RegrasDaArvore.Procurar(Arvore, Forca2)!;

        Assert.Equal(EstadoDoNo.Disponivel, RegrasDaArvore.EstadoDe(Ranks(), forca1));
        Assert.Equal(EstadoDoNo.Bloqueado, RegrasDaArvore.EstadoDe(Ranks(), forca2));
        Assert.Equal(EstadoDoNo.Comprado, RegrasDaArvore.EstadoDe(Ranks((Forca1, 1)), forca1));
        Assert.Equal(
            EstadoDoNo.NoMaximo,
            RegrasDaArvore.EstadoDe(Ranks((Forca1, ArvoreDoJogo.RankMaximoPrincipal)), forca1)
        );
        forca1.Condicional = true;
        Assert.Equal(
            EstadoDoNo.CondicionalmenteAtivo,
            RegrasDaArvore.EstadoDe(Ranks((Forca1, 1)), forca1)
        );
        forca1.Condicional = false;
    }

    [Fact]
    public void Hibrido_pede_os_dois_caminhos_vizinhos()
    {
        var hibrido = Arvore.Nos.Single(n =>
            n.Id == ArvoreDoJogo.IdHibrido(AtributoPrincipal.Forca, AtributoPrincipal.Vigor)
        );
        var rank = ArvoreDoJogo.RankParaAbrirHibrido;
        var soForca = Ranks((Forca1, rank));

        var pendentes = RegrasDaArvore.RequisitosPendentes(soForca, hibrido);

        Assert.Equal(Vigor1, Assert.Single(pendentes).NoId);
        Assert.Equal(
            EstadoDoNo.Disponivel,
            RegrasDaArvore.EstadoDe(Ranks((Forca1, rank), (Vigor1, rank)), hibrido)
        );
    }

    [Fact]
    public void Soma_dos_atributos_vem_so_dos_ranks_comprados()
    {
        var hibrido = ArvoreDoJogo.IdHibrido(AtributoPrincipal.Forca, AtributoPrincipal.Vigor);

        var total = RegrasDaArvore.SomarAtributos(
            Arvore,
            Ranks((Forca1, 3), (Vigor2, 1), (hibrido, 2))
        );

        Assert.Equal(
            3 * ArvoreDoJogo.ValorPorRankPrincipal + 2 * ArvoreDoJogo.ValorPorRankHibrido,
            total.Forca
        );
        Assert.Equal(
            1 * ArvoreDoJogo.ValorPorRankPrincipal + 2 * ArvoreDoJogo.ValorPorRankHibrido,
            total.Vigor
        );
        Assert.Equal(0, total.Agilidade + total.Intelecto + total.Espirito);
        Assert.Equal(0, RegrasDaArvore.SomarAtributos(Arvore, Ranks()).Forca);
    }

    [Fact]
    public void Efeito_secundario_e_aceito_mas_nao_soma_atributo_principal()
    {
        var config = new ConfiguracaoDaArvore
        {
            Versao = 1,
            Nos = new[]
            {
                new NoDaArvore
                {
                    Id = "s",
                    Nome = "s",
                    Caminhos = new[] { AtributoPrincipal.Forca },
                    RankMaximo = 1,
                    CustoPorRank = new[] { 1 },
                    Efeitos = new[]
                    {
                        new EfeitoDeNo
                        {
                            Tipo = TipoDeEfeito.AtributoSecundario,
                            Chave = "critico",
                        },
                    },
                },
            },
        };

        Assert.Empty(ValidadorDaArvore.Validar(config));
        Assert.Equal(0, RegrasDaArvore.SomarAtributos(config, Ranks(("s", 1))).Forca);
    }

    [Fact]
    public void CalcularPontosGastos_soma_corretamente_o_custo_de_cada_rank()
    {
        var forca1 = Forca1;
        var ranks = Ranks((forca1, 3)); // custos: 1 + 1 + 2 = 4
        var total = RegrasDaArvore.CalcularPontosGastos(Arvore, ranks);

        Assert.Equal(4, total);
        Assert.Equal(0, RegrasDaArvore.CalcularPontosGastos(Arvore, Ranks()));
    }

    [Fact]
    public void ValidarECalcularPerfil_com_perfil_valido_calcula_ranks_e_custo()
    {
        var ranks = new[]
        {
            new RankDoNo { NoId = Forca1, Rank = 3 },
            new RankDoNo { NoId = Forca2, Rank = 1 },
        };

        var resultado = RegrasDaArvore.ValidarECalcularPerfil(Arvore, ranks, 10);

        Assert.Null(resultado.Erro);
        Assert.Empty(resultado.Avisos);
        Assert.Equal(3, resultado.RanksAplicaveis[Forca1]);
        Assert.Equal(1, resultado.RanksAplicaveis[Forca2]);
        Assert.Equal(5, resultado.CustoTotal); // Forca1 rank 3: 1 + 1 + 2 = 4; Forca2 rank 1: 1
    }

    [Fact]
    public void ValidarECalcularPerfil_com_pontos_insuficientes_retorna_erro()
    {
        var ranks = new[]
        {
            new RankDoNo { NoId = Forca1, Rank = 3 },
        };

        var resultado = RegrasDaArvore.ValidarECalcularPerfil(Arvore, ranks, 2);

        Assert.Equal(CodigosErroTalentos.PontosInsuficientes, resultado.Erro);
        Assert.Equal(4, resultado.CustoTotal);
    }

    [Fact]
    public void ValidarECalcularPerfil_com_no_inexistente_ignora_e_gera_aviso()
    {
        var ranks = new[]
        {
            new RankDoNo { NoId = Forca1, Rank = 1 },
            new RankDoNo { NoId = "no_que_foi_apagado", Rank = 3 },
        };

        var resultado = RegrasDaArvore.ValidarECalcularPerfil(Arvore, ranks, 10);

        Assert.Null(resultado.Erro);
        Assert.Single(resultado.Avisos);
        Assert.Contains("no_que_foi_apagado", resultado.Avisos[0]);
        Assert.Equal(1, resultado.RanksAplicaveis[Forca1]);
        Assert.False(resultado.RanksAplicaveis.ContainsKey("no_que_foi_apagado"));
    }

    [Fact]
    public void ValidarECalcularPerfil_com_requisito_quebrado_remove_em_cascata_com_aviso()
    {
        // Forca2 pede Forca1 com rank mínimo 2. Sem Forca1 no perfil, Forca2 deve ser removido.
        var ranks = new[]
        {
            new RankDoNo { NoId = Forca2, Rank = 1 },
        };

        var resultado = RegrasDaArvore.ValidarECalcularPerfil(Arvore, ranks, 10);

        Assert.Null(resultado.Erro);
        Assert.Single(resultado.Avisos);
        Assert.False(resultado.RanksAplicaveis.ContainsKey(Forca2));
        Assert.Equal(0, resultado.CustoTotal);
    }

    [Fact]
    public void ValidarECalcularPerfil_com_rank_acima_do_maximo_ajusta_para_maximo_com_aviso()
    {
        var ranks = new[]
        {
            new RankDoNo { NoId = Forca1, Rank = 10 },
        };

        var resultado = RegrasDaArvore.ValidarECalcularPerfil(Arvore, ranks, 10);

        Assert.Null(resultado.Erro);
        Assert.Single(resultado.Avisos);
        Assert.Equal(ArvoreDoJogo.RankMaximoPrincipal, resultado.RanksAplicaveis[Forca1]);
    }
}
