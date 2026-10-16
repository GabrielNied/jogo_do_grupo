using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

/// <summary>Poderes lendários e sets: dados, contagem e quais efeitos ficam ligados (#629, #630).</summary>
public class PoderesESetsTestes
{
    // Predador (variante A): peito 120, luvas 121, botas 123, anel 125, brinco 126.
    private const int Peito = 120;
    private const int Luvas = 121;
    private const int Botas = 123;
    private const int Anel = 125;
    private const int Brinco = 126;

    private static EntradaInventario Peca(
        PosicaoEquipamento posicao,
        int modelo,
        string poder = "",
        double valor = 50
    )
    {
        var dados = CatalogoEquipamentos.Obter(modelo)!;
        foreach (var atributo in dados.Atributos)
            atributo.Valor = valor;
        dados.PoderId = poder;
        return new EntradaInventario
        {
            InstanciaId = (long)posicao * 100 + modelo,
            Posicao = posicao,
            Equipamento = dados,
        };
    }

    private static string[] Origens(IEnumerable<EntradaInventario> pecas, int conjunto = 1) =>
        RegrasDePoderes.Ativos(pecas, conjunto).Select(e => e.Origem).Distinct().ToArray();

    [Fact]
    public void Toda_regra_do_catalogo_e_de_set_e_valida_e_declara_o_que_afeta()
    {
        foreach (var poder in CatalogoDePoderes.Poderes)
        {
            Assert.NotEmpty(poder.Encaixes);
            Assert.NotEmpty(poder.Afeta);
            Assert.NotEmpty(poder.Regras);
            Assert.All(poder.Regras, r => Assert.True(r.Valida, poder.Id + "/" + r.Id));
        }

        foreach (var set in CatalogoDeSets.Sets)
        {
            Assert.All(set.Marcos, m => Assert.Contains(m.Pecas, new[] { 2, 4, 6 }));
            Assert.All(
                set.Marcos.SelectMany(m => m.Regras),
                r => Assert.True(r.Valida, set.Id + "/" + r.Id)
            );
        }
    }

    [Fact]
    public void Regra_sem_seletor_nao_vale_para_tudo_da_arma()
    {
        var regra = new RegraDePoder
        {
            Id = "tudo",
            Gatilho = GatilhoDePoder.AoUsarSkill,
            Acao = AcaoDePoder.AmplificarDano,
            Valor = 0.5,
        };

        Assert.False(regra.Valida);
        regra.Skill = SeletorDeSkill.PorEtiqueta("fisica");
        Assert.True(regra.Valida);
    }

    [Fact]
    public void Etiquetas_dos_poderes_existem_nas_skills()
    {
        Assert.Equal(EtiquetasDeSkill.Investida, CatalogoDePoderes.EtiquetaInvestida);
        Assert.Equal(EtiquetasDeSkill.Fisica, CatalogoDePoderes.EtiquetaFisica);
        var usadas = CatalogoDePoderes
            .Poderes.SelectMany(p => p.Regras)
            .Concat(CatalogoDeSets.Sets.SelectMany(s => s.Marcos).SelectMany(m => m.Regras))
            .SelectMany(r => new[] { r.Skill.Etiqueta, r.AlvoDaAcao.Etiqueta })
            .Where(e => e != "")
            .Distinct();
        var declaradas = Enum.GetValues<TipoEquipamento>()
            .SelectMany(CatalogoDeSkills.Pool)
            .SelectMany(s => s.Etiquetas);
        Assert.All(usadas, e => Assert.Contains(e, declaradas));
    }

    [Fact]
    public void Poder_so_liga_com_a_peca_vestida_e_sai_ao_desequipar()
    {
        var peito = Peca(PosicaoEquipamento.Peito, 111, CatalogoDePoderes.PoderPeito);

        Assert.Equal(new[] { "poder:" + CatalogoDePoderes.PoderPeito }, Origens(new[] { peito }));

        peito.Posicao = PosicaoEquipamento.Inventario;
        Assert.Empty(Origens(new[] { peito }));
    }

    [Fact]
    public void Poder_em_encaixe_incompativel_ou_desconhecido_nao_liga()
    {
        var luvasComPoderDoPeito = Peca(
            PosicaoEquipamento.Luvas,
            112,
            CatalogoDePoderes.PoderPeito
        );
        var desconhecido = Peca(PosicaoEquipamento.Botas, Botas, "poder.nao-existe");

        Assert.Empty(Origens(new[] { luvasComPoderDoPeito, desconhecido }));
    }

    [Fact]
    public void O_mesmo_poder_em_dois_aneis_vale_uma_vez()
    {
        var um = Peca(PosicaoEquipamento.Anel1, 116, CatalogoDePoderes.PoderAnel);
        var dois = Peca(PosicaoEquipamento.Anel2, 116, CatalogoDePoderes.PoderAnel);

        var efeitos = RegrasDePoderes.Ativos(new[] { um, dois }, 1);

        Assert.Single(efeitos);
    }

    [Fact]
    public void Dois_pares_de_poderes_de_pecas_diferentes_ligam_juntos()
    {
        var pecas = new[]
        {
            Peca(PosicaoEquipamento.Peito, 111, CatalogoDePoderes.PoderPeito),
            Peca(PosicaoEquipamento.Anel1, 116, CatalogoDePoderes.PoderAnel),
            Peca(PosicaoEquipamento.Botas, 114, CatalogoDePoderes.PoderBotas),
        };

        Assert.Equal(3, Origens(pecas).Length);
    }

    [Fact]
    public void Marco_de_2_liga_com_duas_pecas_e_o_de_4_com_quatro()
    {
        var set = CatalogoDeSets.Obter(CatalogoDeSets.SetCacadaDoPredador)!;
        var pecas = new List<EntradaInventario>
        {
            Peca(PosicaoEquipamento.Peito, Peito),
            Peca(PosicaoEquipamento.Luvas, Luvas),
        };

        Assert.Equal(2, RegrasDeSets.Contar(set, pecas, 1));
        Assert.Equal(new[] { 2 }, RegrasDeSets.MarcosAtivos(set, 2).Select(m => m.Pecas));
        Assert.Contains("set:" + set.Id + ":2", Origens(pecas));
        Assert.DoesNotContain("set:" + set.Id + ":4", Origens(pecas));

        pecas.Add(Peca(PosicaoEquipamento.Botas, Botas));
        pecas.Add(Peca(PosicaoEquipamento.Anel1, Anel));
        Assert.Equal(4, RegrasDeSets.Contar(set, pecas, 1));
        Assert.Equal(new[] { 2, 4 }, RegrasDeSets.MarcosAtivos(set, 4).Select(m => m.Pecas));
        Assert.Contains("set:" + set.Id + ":4", Origens(pecas));
    }

    [Fact]
    public void Tirar_uma_peca_abaixo_do_marco_desliga_o_bonus()
    {
        var set = CatalogoDeSets.Obter(CatalogoDeSets.SetCacadaDoPredador)!;
        var pecas = new List<EntradaInventario>
        {
            Peca(PosicaoEquipamento.Peito, Peito),
            Peca(PosicaoEquipamento.Luvas, Luvas),
            Peca(PosicaoEquipamento.Botas, Botas),
            Peca(PosicaoEquipamento.Anel1, Anel),
        };
        Assert.Contains("set:" + set.Id + ":4", Origens(pecas));

        pecas[3].Posicao = PosicaoEquipamento.Inventario;
        Assert.DoesNotContain("set:" + set.Id + ":4", Origens(pecas));
        Assert.Contains("set:" + set.Id + ":2", Origens(pecas));

        pecas[2].Posicao = PosicaoEquipamento.Inventario;
        Assert.Contains("set:" + set.Id + ":2", Origens(pecas));

        pecas[1].Posicao = PosicaoEquipamento.Inventario;
        Assert.DoesNotContain("set:" + set.Id + ":2", Origens(pecas));
        Assert.Empty(RegrasDePoderes.Ativos(pecas, 1));
    }

    [Fact]
    public void Duas_pecas_iguais_nos_encaixes_de_par_contam_uma_so()
    {
        var set = CatalogoDeSets.Obter(CatalogoDeSets.SetCacadaDoPredador)!;
        var aneis = new[]
        {
            Peca(PosicaoEquipamento.Anel1, Anel),
            Peca(PosicaoEquipamento.Anel2, Anel),
        };

        Assert.Equal(1, RegrasDeSets.Contar(set, aneis, 1));
        Assert.Empty(RegrasDePoderes.Ativos(aneis, 1));

        var comBrinco = aneis.Append(Peca(PosicaoEquipamento.Brinco1, Brinco));
        Assert.Equal(2, RegrasDeSets.Contar(set, comBrinco, 1));
    }

    [Fact]
    public void So_a_arma_do_conjunto_ativo_conta_para_o_set()
    {
        var set = new DefinicaoDeSet
        {
            Id = "set.teste",
            Modelos = new[] { Peito, 100 },
            Marcos = new[] { new MarcoDeSet { Pecas = 2 } },
        };
        var pecas = new[]
        {
            Peca(PosicaoEquipamento.Peito, Peito),
            Peca(PosicaoEquipamento.Arma2, 100),
        };

        Assert.Equal(1, RegrasDeSets.Contar(set, pecas, 1));
        Assert.Empty(RegrasDeSets.MarcosAtivos(set, RegrasDeSets.Contar(set, pecas, 1)));
        Assert.Equal(2, RegrasDeSets.Contar(set, pecas, 2));
        Assert.Single(RegrasDeSets.MarcosAtivos(set, RegrasDeSets.Contar(set, pecas, 2)));
    }

    [Fact]
    public void Peca_na_bolsa_nao_conta_para_o_set()
    {
        var set = CatalogoDeSets.Obter(CatalogoDeSets.SetCacadaDoPredador)!;
        var pecas = new[]
        {
            Peca(PosicaoEquipamento.Inventario, Peito),
            Peca(PosicaoEquipamento.Inventario, Luvas),
        };

        Assert.Equal(0, RegrasDeSets.Contar(set, pecas, 1));
    }

    [Fact]
    public void Tier_nao_muda_nada_no_set_nem_no_poder_e_poder_nao_muda_o_tier()
    {
        var fraco = new[]
        {
            Peca(PosicaoEquipamento.Peito, Peito, valor: 5),
            Peca(PosicaoEquipamento.Luvas, Luvas, valor: 5),
        };
        var forte = new[]
        {
            Peca(PosicaoEquipamento.Peito, Peito, valor: 95),
            Peca(PosicaoEquipamento.Luvas, Luvas, valor: 95),
        };
        Assert.NotEqual(
            RegrasTier.TierDe(fraco[0].Equipamento),
            RegrasTier.TierDe(forte[0].Equipamento)
        );
        Assert.Equal(Origens(fraco), Origens(forte));

        var item = CatalogoEquipamentos.Obter(111)!;
        foreach (var atributo in item.Atributos)
            atributo.Valor = 70;
        var tierSemPoder = RegrasTier.TierDe(item);
        item.PoderId = CatalogoDePoderes.PoderPeito;
        Assert.Equal(tierSemPoder, RegrasTier.TierDe(item));
    }

    [Fact]
    public void Nenhum_set_e_obrigatorio_a_maioria_dos_modelos_nao_tem_set()
    {
        var modelos = Enumerable.Range(
            CatalogoEquipamentos.PrimeiroModelo,
            CatalogoEquipamentos.QuantidadeModelos
        );
        var semSet = modelos.Count(m => CatalogoDeSets.DoModelo(m) == null);

        Assert.True(semSet > CatalogoEquipamentos.QuantidadeModelos / 2);
        Assert.Empty(RegrasDePoderes.Ativos(Array.Empty<EntradaInventario>(), 1));
        // Peça fora de set mantém o poder e não precisa de set.
        var sozinha = Peca(PosicaoEquipamento.Peito, 111, CatalogoDePoderes.PoderPeito);
        Assert.Single(Origens(new[] { sozinha }));
    }

    [Fact]
    public void Resumo_do_set_diz_quantas_pecas_e_quais_marcos_estao_ativos()
    {
        var pecas = new[]
        {
            Peca(PosicaoEquipamento.Peito, Peito),
            Peca(PosicaoEquipamento.Luvas, Luvas),
            Peca(PosicaoEquipamento.Botas, Botas),
        };

        var resumo = RegrasDeSets.Resumir(Peito, pecas, 1)!;

        Assert.Equal(3, resumo.Vestidas);
        Assert.Equal(5, resumo.Total);
        Assert.Equal(new[] { true, false }, resumo.Marcos.Select(m => m.Ativo));
        Assert.Null(RegrasDeSets.Resumir(111, pecas, 1));
    }

    [Fact]
    public void Sorteio_do_poder_so_escolhe_entre_os_candidatos_do_modelo()
    {
        var luvasB = CatalogoEquipamentos.Obter(112)!;
        var luvasA = CatalogoEquipamentos.Obter(Luvas)!;
        var arma = CatalogoEquipamentos.Obter(100)!;

        Assert.Equal(CatalogoDePoderes.PoderLuvas, CatalogoDePoderes.Sortear(luvasB, n => n - 1));
        Assert.Equal("", CatalogoDePoderes.Sortear(luvasA, n => n - 1));
        Assert.Equal("", CatalogoDePoderes.Sortear(arma, n => n - 1));
    }
}
