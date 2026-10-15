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
    public void Toda_regra_do_catalogo_e_valida_e_declara_o_que_afeta()
    {
        foreach (var poder in CatalogoDePoderes.Poderes)
        {
            Assert.NotEmpty(poder.Encaixes);
            Assert.NotEmpty(poder.Afeta);
            Assert.NotEmpty(poder.Regras);
            Assert.All(poder.Regras, r => Assert.True(r.Valida, poder.Id + "/" + r.Id));
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
    public void Poder_nao_muda_o_tier()
    {
        var item = CatalogoEquipamentos.Obter(111)!;
        foreach (var atributo in item.Atributos)
            atributo.Valor = 70;
        var tierSemPoder = RegrasTier.TierDe(item);
        item.PoderId = CatalogoDePoderes.PoderPeito;
        Assert.Equal(tierSemPoder, RegrasTier.TierDe(item));
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
