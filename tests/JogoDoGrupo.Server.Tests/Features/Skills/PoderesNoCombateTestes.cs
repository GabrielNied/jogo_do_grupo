using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Features.Skills;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Server.Tests.Features.Skills;

/// <summary>O que o personagem veste, como Itens o entrega: as mesmas regras puras do servidor.</summary>
public sealed class EfeitosFalsos : IEfeitosDoEquipamento
{
    public readonly List<EntradaInventario> Pecas = [];
    public int Conjunto = 1;
    public int Leituras;

    /// <summary>Efeitos soltos, sem peça, para testar o motor com regras sob medida.</summary>
    public readonly List<EfeitoAtivo> Extras = [];

    public Task<IReadOnlyList<EfeitoAtivo>> ObterAsync(long personagemId, CancellationToken c)
    {
        Interlocked.Increment(ref Leituras);
        return Task.FromResult<IReadOnlyList<EfeitoAtivo>>(
            RegrasDePoderes.Ativos(Pecas.ToList(), Conjunto).Concat(Extras).ToList()
        );
    }

    public EntradaInventario Vestir(PosicaoEquipamento posicao, int modelo, string poder = "")
    {
        var dados = CatalogoEquipamentos.Obter(modelo)!;
        dados.PoderId = poder;
        var peca = new EntradaInventario
        {
            InstanciaId = Pecas.Count + 1,
            Posicao = posicao,
            Equipamento = dados,
        };
        Pecas.Add(peca);
        return peca;
    }
}

public sealed class ArmazemFixo(params string[] ids) : IArmazemDeSkills
{
    public Task<IReadOnlyList<string>> ObterOuSortearAsync(
        ArmaEquipada arma,
        CancellationToken c
    ) => Task.FromResult<IReadOnlyList<string>>(ids);
}

public sealed class PoderesNoCombateTestes
{
    private const long Personagem = 7;
    private const string Investida = "espada.avanco-devastador";
    private const string Corte = "espada.corte-veloz";
    private const string Lamina = "espada.lamina-giratoria";
    private const int Peito = 120;
    private const int Luvas = 121;
    private const int Botas = 123;
    private const int Anel = 125;
    private const int PeitoB = 111;
    private const int LuvasB = 112;
    private const int BotasB = 114;
    private const int AnelB = 116;

    private static readonly ArmaEquipada Espada = new(10, 1, TipoEquipamento.Espada, 1);

    private readonly EfeitosFalsos _efeitos = new();
    private readonly CobrancaFalsa _cobranca = new() { Stamina = 10_000 };
    private readonly AlvoFalso _alvo = new(-50) { Ativo = true };
    private long _agoraMs = 1_000_000;
    private readonly MotorDePoderes _motor;
    private readonly NucleoDeSkills _nucleo;
    private readonly BonusFalso _itens = new()
    {
        Equipamentos = new AtributosDosEquipamentos { TemArma = true, DanoDaArma = 20 },
    };

    public PoderesNoCombateTestes()
    {
        Func<long> relogio = () => Interlocked.Read(ref _agoraMs);
        _motor = new MotorDePoderes(_efeitos, relogio);
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(Personagem, "cidade", 0, 0);
        _nucleo = new NucleoDeSkills(
            new ArmaFalsa { Atual = Espada },
            new ArmazemFixo(Investida, Corte, Lamina),
            _cobranca,
            _itens,
            posicoes,
            [_alvo],
            relogio,
            null,
            _motor
        );
    }

    private Task<ResultadoDoUso> UsarAsync(int espaco) =>
        _nucleo.UsarAsync(
            Personagem,
            new UsarSkill { Espaco = espaco, Sequencia = 1 },
            CancellationToken.None
        );

    private static int DanoBase(string skill) =>
        RegrasSkills.Dano(CatalogoDeSkills.Obter(skill)!, 20, 0, 0);

    private static IEnumerable<AcaoDePoder> Acoes(ResultadoDoUso r) =>
        r.Poderes!.Select(p => p.Acao);

    private async Task Golpes(int quantidade)
    {
        for (var i = 0; i < quantidade; i++)
            await _motor.AoGolpeComumAsync(Personagem, [-50], CancellationToken.None);
    }

    private void MudarEquipamento() => _motor.Invalidar(Personagem);

    [Fact]
    public async Task Sem_poder_a_skill_sai_como_sempre()
    {
        var uso = await UsarAsync(1);

        Assert.Empty(uso.Poderes!);
        Assert.False(uso.BarraMudou);
        Assert.Equal(DanoBase(Corte), uso.Acertos!.Acertos[0].Dano);
    }

    [Fact]
    public async Task Peito_marca_o_alvo_so_com_skill_de_investida()
    {
        _efeitos.Vestir(PosicaoEquipamento.Peito, PeitoB, CatalogoDePoderes.PoderPeito);
        _efeitos.Vestir(PosicaoEquipamento.Anel1, AnelB, CatalogoDePoderes.PoderAnel);

        // Corte Veloz não é investida: não marca, então o anel não tem o que caçar.
        var corte = await UsarAsync(1);
        Assert.Empty(corte.Poderes!);

        var investida = await UsarAsync(0);
        Assert.Contains(AcaoDePoder.MarcarAlvos, Acoes(investida));
        Assert.DoesNotContain(AcaoDePoder.ReduzirRecarga, Acoes(investida));
    }

    [Fact]
    public async Task Anel_em_alvo_marcado_reduz_a_recarga_de_outra_skill_e_atualiza_a_barra()
    {
        _efeitos.Vestir(PosicaoEquipamento.Peito, PeitoB, CatalogoDePoderes.PoderPeito);
        _efeitos.Vestir(PosicaoEquipamento.Anel1, AnelB, CatalogoDePoderes.PoderAnel);
        await UsarAsync(0); // investida marca
        await UsarAsync(2); // lâmina: física, entra em recarga
        _agoraMs += ConfigPoderes.CacadorIntervaloMs;
        var antes = (await _nucleo.BarraAsync(Personagem, CancellationToken.None)).Skills;

        var corte = await UsarAsync(1); // acerta o marcado: tira recarga das outras físicas

        Assert.Contains(AcaoDePoder.ReduzirRecarga, Acoes(corte));
        Assert.True(corte.BarraMudou);
        var depois = (await _nucleo.BarraAsync(Personagem, CancellationToken.None)).Skills;
        Assert.Equal(
            antes[2].RestanteMs - ConfigPoderes.CacadorReducaoDeRecargaMs,
            depois[2].RestanteMs
        );
        // A skill recém-usada fica de fora: a recarga dela começa inteira.
        Assert.Equal(CatalogoDeSkills.Obter(Corte)!.RecargaMs, depois[1].RestanteMs);
    }

    [Fact]
    public async Task Anel_sem_marca_no_alvo_nao_dispara()
    {
        _efeitos.Vestir(PosicaoEquipamento.Anel1, AnelB, CatalogoDePoderes.PoderAnel);

        var corte = await UsarAsync(1);

        Assert.Empty(corte.Poderes!);
        Assert.False(corte.BarraMudou);
    }

    [Fact]
    public async Task Marca_expira()
    {
        _efeitos.Vestir(PosicaoEquipamento.Peito, PeitoB, CatalogoDePoderes.PoderPeito);
        _efeitos.Vestir(PosicaoEquipamento.Anel1, AnelB, CatalogoDePoderes.PoderAnel);
        await UsarAsync(0);
        _agoraMs += ConfigPoderes.MarcaDaInvestidaMs;

        var corte = await UsarAsync(1);

        Assert.DoesNotContain(AcaoDePoder.ReduzirRecarga, Acoes(corte));
    }

    [Fact]
    public async Task Anel_respeita_a_folga_entre_ativacoes()
    {
        _efeitos.Vestir(PosicaoEquipamento.Peito, PeitoB, CatalogoDePoderes.PoderPeito);
        _efeitos.Vestir(PosicaoEquipamento.Anel1, AnelB, CatalogoDePoderes.PoderAnel);
        await UsarAsync(0);
        var primeira = await UsarAsync(1);
        Assert.Contains(AcaoDePoder.ReduzirRecarga, Acoes(primeira));

        var segunda = await UsarAsync(2);
        Assert.DoesNotContain(AcaoDePoder.ReduzirRecarga, Acoes(segunda));
    }

    [Fact]
    public async Task Golpes_seguidos_dao_cargas_ate_o_limite_e_a_proxima_skill_fisica_gasta_todas()
    {
        _efeitos.Vestir(PosicaoEquipamento.Luvas, LuvasB, CatalogoDePoderes.PoderLuvas);
        await Golpes(ConfigPoderes.EmbaloMaximoDeCargas + 2);

        var uso = await UsarAsync(1);

        var aviso = Assert.Single(uso.Poderes!, p => p.Acao == AcaoDePoder.AmplificarDano);
        var esperado = ConfigPoderes.EmbaloMaximoDeCargas * ConfigPoderes.EmbaloBonusPorCarga;
        Assert.Equal((int)Math.Round(esperado * 100), aviso.Valor);
        Assert.Equal(
            RegrasSkills.ComPoderes(DanoBase(Corte), 1 + esperado),
            uso.Acertos!.Acertos[0].Dano
        );
        Assert.True(uso.Acertos.Acertos[0].Dano > DanoBase(Corte));

        // Gastou tudo: a skill seguinte sai sem bônus.
        var outra = await UsarAsync(2);
        Assert.Empty(outra.Poderes!);
        Assert.Equal(DanoBase(Lamina), outra.Acertos!.Acertos[0].Dano);
    }

    [Fact]
    public async Task Cargas_expiram()
    {
        _efeitos.Vestir(PosicaoEquipamento.Luvas, LuvasB, CatalogoDePoderes.PoderLuvas);
        await Golpes(2);
        _agoraMs += ConfigPoderes.EmbaloDuracaoMs;

        var uso = await UsarAsync(1);

        Assert.Empty(uso.Poderes!);
    }

    [Fact]
    public async Task Poder_de_luvas_nao_afeta_skill_que_nao_e_fisica()
    {
        _efeitos.Vestir(PosicaoEquipamento.Luvas, LuvasB, CatalogoDePoderes.PoderLuvas);
        await Golpes(2);
        var cajado = new NucleoDeSkills(
            new ArmaFalsa { Atual = new ArmaEquipada(11, 2, TipoEquipamento.Cajado, 1) },
            new ArmazemFixo("cajado.raio-arcano"),
            _cobranca,
            _itens,
            ComPosicao(),
            [_alvo],
            () => Interlocked.Read(ref _agoraMs),
            null,
            _motor
        );

        var uso = await cajado.UsarAsync(
            Personagem,
            new UsarSkill { Espaco = 0, Sequencia = 1 },
            CancellationToken.None
        );

        Assert.Null(uso.Recusa);
        Assert.Empty(uso.Poderes!);
        Assert.Equal(DanoBase("cajado.raio-arcano"), uso.Acertos!.Acertos[0].Dano);
    }

    private static PosicoesFalsas ComPosicao()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(Personagem, "cidade", 0, 0);
        return posicoes;
    }

    [Fact]
    public async Task Esquiva_fortalece_a_proxima_investida_e_so_ela()
    {
        _efeitos.Vestir(PosicaoEquipamento.Botas, BotasB, CatalogoDePoderes.PoderBotas);
        await _motor.AoEsquivarAsync(Personagem, CancellationToken.None);

        var corte = await UsarAsync(1);
        Assert.Empty(corte.Poderes!);

        var investida = await UsarAsync(0);
        Assert.Equal(
            RegrasSkills.ComPoderes(DanoBase(Investida), 1 + ConfigPoderes.ImpulsoBonus),
            investida.Acertos!.Acertos[0].Dano
        );
    }

    [Fact]
    public async Task Dois_poderes_somam_seus_bonus_na_mesma_skill()
    {
        _efeitos.Vestir(PosicaoEquipamento.Luvas, LuvasB, CatalogoDePoderes.PoderLuvas);
        _efeitos.Vestir(PosicaoEquipamento.Botas, BotasB, CatalogoDePoderes.PoderBotas);
        await Golpes(2);
        await _motor.AoEsquivarAsync(Personagem, CancellationToken.None);

        var investida = await UsarAsync(0);

        var bonus = 2 * ConfigPoderes.EmbaloBonusPorCarga + ConfigPoderes.ImpulsoBonus;
        Assert.Equal(
            RegrasSkills.ComPoderes(DanoBase(Investida), 1 + bonus),
            investida.Acertos!.Acertos[0].Dano
        );
        Assert.Equal(2, Acoes(investida).Count(a => a == AcaoDePoder.AmplificarDano));
    }

    [Fact]
    public async Task Tirar_a_peca_desliga_o_efeito_e_apaga_o_estado()
    {
        var luvas = _efeitos.Vestir(PosicaoEquipamento.Luvas, LuvasB, CatalogoDePoderes.PoderLuvas);
        await Golpes(3);
        luvas.Posicao = PosicaoEquipamento.Inventario;
        MudarEquipamento();

        var semLuvas = await UsarAsync(1);
        Assert.Empty(semLuvas.Poderes!);

        // Vestir de novo começa do zero: as cargas de antes não voltam.
        luvas.Posicao = PosicaoEquipamento.Luvas;
        MudarEquipamento();
        _agoraMs += CatalogoDeSkills.Obter(Lamina)!.RecargaMs;
        var comLuvas = await UsarAsync(2);
        Assert.Empty(comLuvas.Poderes!);
    }

    [Fact]
    public async Task Efeitos_ficam_em_cache_ate_o_equipamento_mudar()
    {
        _efeitos.Vestir(PosicaoEquipamento.Luvas, LuvasB, CatalogoDePoderes.PoderLuvas);

        await Golpes(3);
        Assert.Equal(1, _efeitos.Leituras);

        MudarEquipamento();
        await Golpes(1);
        Assert.Equal(2, _efeitos.Leituras);
    }

    [Fact]
    public async Task Efeito_criado_no_mesmo_evento_nao_conta_para_a_condicao_dele()
    {
        // Uma regra dá carga ao usar skill física; outra a gasta na física seguinte. A primeira
        // skill não se fortalece com a carga que ela mesma deu (sem encadeamento no evento).
        var fisica = SeletorDeSkill.PorEtiqueta("fisica");
        _efeitos.Extras.Add(
            new EfeitoAtivo
            {
                Origem = "teste",
                Nome = "Teste",
                Regra = new RegraDePoder
                {
                    Id = "dar",
                    Gatilho = GatilhoDePoder.AoUsarSkill,
                    Skill = fisica,
                    Acao = AcaoDePoder.GanharCarga,
                    Estado = "x",
                    DuracaoMs = 10_000,
                },
            }
        );
        _efeitos.Extras.Add(
            new EfeitoAtivo
            {
                Origem = "teste",
                Nome = "Teste",
                Regra = new RegraDePoder
                {
                    Id = "gastar",
                    Gatilho = GatilhoDePoder.AoUsarSkill,
                    Skill = fisica,
                    Condicao = CondicaoDePoder.TemCarga,
                    Estado = "x",
                    Acao = AcaoDePoder.AmplificarDano,
                    Valor = 0.5,
                },
            }
        );

        var primeira = await UsarAsync(1);
        Assert.DoesNotContain(AcaoDePoder.AmplificarDano, Acoes(primeira));
        Assert.Contains(AcaoDePoder.GanharCarga, Acoes(primeira));

        var segunda = await UsarAsync(2);
        Assert.Equal(
            RegrasSkills.ComPoderes(DanoBase(Lamina), 1.5),
            segunda.Acertos!.Acertos[0].Dano
        );
    }

    [Fact]
    public async Task Marco_de_2_pecas_da_ritmo_e_a_proxima_fisica_gasta()
    {
        // O set de 2 peças dá Ritmo ao usar skill física e gasta Ritmo na física seguinte: a
        // primeira skill não se fortalece com a carga que ela mesma deu (sem encadeamento).
        _efeitos.Vestir(PosicaoEquipamento.Peito, Peito);
        _efeitos.Vestir(PosicaoEquipamento.Luvas, Luvas);

        var primeira = await UsarAsync(1);
        Assert.DoesNotContain(AcaoDePoder.AmplificarDano, Acoes(primeira));
        Assert.Contains(AcaoDePoder.GanharCarga, Acoes(primeira));

        var segunda = await UsarAsync(2);
        var aviso = Assert.Single(segunda.Poderes!, p => p.Acao == AcaoDePoder.AmplificarDano);
        Assert.Equal((int)Math.Round(ConfigPoderes.RitmoBonusPorCarga * 100), aviso.Valor);
        Assert.Equal(
            RegrasSkills.ComPoderes(DanoBase(Lamina), 1 + ConfigPoderes.RitmoBonusPorCarga),
            segunda.Acertos!.Acertos[0].Dano
        );
    }

    [Fact]
    public async Task Perder_o_marco_do_set_ao_tirar_uma_peca_desliga_o_bonus()
    {
        _efeitos.Vestir(PosicaoEquipamento.Peito, Peito);
        var luvas = _efeitos.Vestir(PosicaoEquipamento.Luvas, Luvas);
        await UsarAsync(1);

        luvas.Posicao = PosicaoEquipamento.Inventario;
        MudarEquipamento();
        var depois = await UsarAsync(2);

        Assert.Empty(depois.Poderes!);
        Assert.Equal(DanoBase(Lamina), depois.Acertos!.Acertos[0].Dano);
    }

    [Fact]
    public async Task Marco_de_4_pecas_tira_recarga_das_outras_skills_fisicas()
    {
        _efeitos.Vestir(PosicaoEquipamento.Peito, Peito);
        _efeitos.Vestir(PosicaoEquipamento.Luvas, Luvas);
        _efeitos.Vestir(PosicaoEquipamento.Botas, Botas);
        _efeitos.Vestir(PosicaoEquipamento.Anel1, Anel);
        await UsarAsync(0);
        _agoraMs += ConfigPoderes.FolegoIntervaloMs;

        var corte = await UsarAsync(1);

        Assert.True(corte.BarraMudou);
        Assert.Contains(AcaoDePoder.ReduzirRecarga, Acoes(corte));
    }

    [Fact]
    public async Task Falha_ao_ler_os_efeitos_nao_derruba_a_skill()
    {
        var motor = new MotorDePoderes(new EfeitosQueFalham(), () => _agoraMs);

        var resultado = await motor.AoUsarSkillAsync(
            Personagem,
            CatalogoDeSkills.Obter(Corte)!,
            CancellationToken.None
        );

        Assert.Equal(1.0, resultado.MultiplicadorDoDano);
        Assert.Empty(resultado.Avisos);
    }

    private sealed class EfeitosQueFalham : IEfeitosDoEquipamento
    {
        public Task<IReadOnlyList<EfeitoAtivo>> ObterAsync(
            long personagemId,
            CancellationToken c
        ) => throw new InvalidOperationException("banco fora");
    }
}
