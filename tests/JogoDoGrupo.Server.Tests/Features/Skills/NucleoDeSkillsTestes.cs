using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Features.Skills;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Server.Tests.Features.Skills;

/// <summary>A arma que o teste põe e tira (equipar, desequipar, trocar).</summary>
public sealed class ArmaFalsa : IArmaEquipada
{
    public volatile object? Atual;

    public Task<ArmaEquipada?> ObterAsync(long personagemId, CancellationToken c) =>
        Task.FromResult(Atual as ArmaEquipada?);
}

/// <summary>Skills de cada exemplar na memória, sorteadas do pool como o servidor faz.</summary>
public sealed class ArmazemEmMemoria : IArmazemDeSkills
{
    private readonly Dictionary<long, IReadOnlyList<string>> _ids = [];

    public Task<IReadOnlyList<string>> ObterOuSortearAsync(ArmaEquipada arma, CancellationToken c)
    {
        lock (_ids)
        {
            if (!_ids.TryGetValue(arma.InstanciaId, out var ids))
            {
                ids = RegrasSkills.Sortear(arma.Tipo, _ => 0);
                _ids[arma.InstanciaId] = ids;
            }

            return Task.FromResult(ids);
        }
    }
}

/// <summary>Stamina e mana do personagem, com a mesma regra de conferir e descontar de Combate.</summary>
public sealed class CobrancaFalsa : ICobrancaDeRecursos
{
    private int _cobrancas;
    public int Stamina = 100;
    public int Mana = 100;
    public bool Caido;
    public int Cobrancas => Volatile.Read(ref _cobrancas);

    public async Task<(ResultadoDaCobranca, RecursosMudaram?)> TentarCobrarAsync(
        long personagemId,
        RecursoCobravel recurso,
        int custo,
        CancellationToken c
    )
    {
        await Task.Yield();
        lock (this)
        {
            if (Caido)
                return (ResultadoDaCobranca.Caido, null);
            ref var saldo = ref (recurso == RecursoCobravel.Mana ? ref Mana : ref Stamina);
            if (saldo < custo)
                return (ResultadoDaCobranca.Insuficiente, null);
            saldo -= custo;
            Interlocked.Increment(ref _cobrancas);
            return (
                ResultadoDaCobranca.Cobrado,
                new RecursosMudaram { StaminaAtual = Stamina, ManaAtual = Mana }
            );
        }
    }
}

public sealed class NucleoDeSkillsTestes
{
    private const long Personagem = 7;
    private const string Area = "cidade";

    private static readonly ArmaEquipada Espada = new(10, 1, TipoEquipamento.Espada, 1);
    private static readonly ArmaEquipada Cajado = new(11, 2, TipoEquipamento.Cajado, 1);

    private readonly ArmaFalsa _arma = new() { Atual = Espada };
    private readonly CobrancaFalsa _cobranca = new();
    private readonly AlvoFalso _alvo = new(-50) { Ativo = true };
    private readonly BonusFalso _itens = new()
    {
        Equipamentos = new AtributosDosEquipamentos { TemArma = true, DanoDaArma = 20 },
    };
    private long _agoraMs = 1_000_000;
    private readonly NucleoDeSkills _nucleo;

    public NucleoDeSkillsTestes()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(Personagem, Area, 0, 0);
        _nucleo = new NucleoDeSkills(
            _arma,
            new ArmazemEmMemoria(),
            _cobranca,
            _itens,
            posicoes,
            [_alvo],
            () => Interlocked.Read(ref _agoraMs)
        );
    }

    private Task<ResultadoDoUso> UsarAsync(int espaco, int sequencia = 1) =>
        _nucleo.UsarAsync(
            Personagem,
            new UsarSkill { Espaco = espaco, Sequencia = sequencia },
            CancellationToken.None
        );

    [Fact]
    public async Task Barra_tem_as_tres_skills_da_arma_equipada()
    {
        var barra = await _nucleo.BarraAsync(Personagem, CancellationToken.None);

        Assert.Equal(Espada.InstanciaId, barra.ArmaId);
        Assert.Equal(ConfigSkills.SkillsPorArma, barra.Skills.Length);
        var pool = CatalogoDeSkills.Pool(TipoEquipamento.Espada).Select(s => s.Id).ToArray();
        Assert.All(barra.Skills, s => Assert.Contains(s.Id, pool));
    }

    [Fact]
    public async Task Usar_cobra_o_custo_aplica_o_dano_e_comeca_a_recarga()
    {
        var skill = CatalogoDeSkills.Obter(
            (await _nucleo.BarraAsync(Personagem, CancellationToken.None)).Skills[0].Id
        )!;

        var resultado = await UsarAsync(0, sequencia: 5);

        Assert.Null(resultado.Recusa);
        var saldo = skill.Recurso == RecursoDeSkill.Mana ? _cobranca.Mana : _cobranca.Stamina;
        Assert.Equal(100 - skill.Custo, saldo);
        Assert.Equal(5, resultado.Usada!.Sequencia);
        Assert.Equal(skill.RecargaMs, resultado.Usada.RecargaMs);
        Assert.Equal(1, _alvo.Aplicacoes);
        var acerto = Assert.Single(resultado.Acertos!.Acertos);
        Assert.Equal(RegrasSkills.Dano(skill, 20, 0, 0), acerto.Dano);
        Assert.Equal(Area, resultado.Area);

        var barra = await _nucleo.BarraAsync(Personagem, CancellationToken.None);
        Assert.Equal(skill.RecargaMs, barra.Skills[0].RestanteMs);
    }

    [Fact]
    public async Task Em_recarga_recusa_sem_gastar_e_volta_quando_acaba()
    {
        await UsarAsync(0);
        var gastoDepoisDoPrimeiro = _cobranca.Cobrancas;

        var recusado = await UsarAsync(0);
        Assert.Equal(CodigosErroSkills.EmRecarga, recusado.Recusa);
        Assert.Equal(gastoDepoisDoPrimeiro, _cobranca.Cobrancas);

        var recarga = (await _nucleo.BarraAsync(Personagem, CancellationToken.None))
            .Skills[0]
            .RecargaMs;
        Interlocked.Add(ref _agoraMs, recarga);
        Assert.Null((await UsarAsync(0)).Recusa);
    }

    [Fact]
    public async Task Sem_recurso_recusa_e_nao_aplica_dano_nem_comeca_recarga()
    {
        _cobranca.Stamina = 0;
        _cobranca.Mana = 0;

        var recusado = await UsarAsync(0);

        Assert.Equal(CodigosErroSkills.RecursoInsuficiente, recusado.Recusa);
        Assert.Equal(0, _alvo.Aplicacoes);
        Assert.Equal(
            0,
            (await _nucleo.BarraAsync(Personagem, CancellationToken.None)).Skills[0].RestanteMs
        );
    }

    [Fact]
    public async Task Caido_recusa()
    {
        _cobranca.Caido = true;

        Assert.Equal(CodigosErroSkills.VoceEstaCaido, (await UsarAsync(0)).Recusa);
    }

    [Fact]
    public async Task Sem_arma_ou_com_espaco_inexistente_recusa()
    {
        Assert.Equal(
            CodigosErroSkills.SemSkill,
            (await UsarAsync(ConfigSkills.SkillsPorArma)).Recusa
        );
        Assert.Equal(CodigosErroSkills.SemSkill, (await UsarAsync(-1)).Recusa);

        _arma.Atual = null; // Desequipou.

        Assert.Equal(CodigosErroSkills.SemSkill, (await UsarAsync(0)).Recusa);
        Assert.Empty((await _nucleo.BarraAsync(Personagem, CancellationToken.None)).Skills);
        Assert.Equal(0, _cobranca.Cobrancas);
    }

    [Fact]
    public async Task Pedidos_simultaneos_cobram_uma_vez_so()
    {
        var resultados = await Task.WhenAll(
            Enumerable.Range(0, 30).Select(i => Task.Run(() => UsarAsync(0, i)))
        );

        Assert.Single(resultados, r => r.Recusa == null);
        Assert.All(
            resultados.Where(r => r.Recusa != null),
            r => Assert.Equal(CodigosErroSkills.EmRecarga, r.Recusa)
        );
        Assert.Equal(1, _cobranca.Cobrancas);
        Assert.Equal(1, _alvo.Aplicacoes);
    }

    [Fact]
    public async Task Recarga_e_da_arma_e_trocar_de_arma_nao_mexe_na_da_outra()
    {
        await UsarAsync(0); // Recarga da espada.

        _arma.Atual = Cajado; // Troca: a barra é a do cajado, pronta.
        var barraDoCajado = await _nucleo.BarraAsync(Personagem, CancellationToken.None);
        Assert.Equal(Cajado.InstanciaId, barraDoCajado.ArmaId);
        Assert.All(barraDoCajado.Skills, s => Assert.Equal(0, s.RestanteMs));
        Assert.Null((await UsarAsync(0)).Recusa);

        _arma.Atual = Espada; // Volta: a recarga da espada continua de onde estava.
        Assert.Equal(CodigosErroSkills.EmRecarga, (await UsarAsync(0)).Recusa);
    }

    [Fact]
    public async Task Trocar_no_meio_da_recarga_nao_altera_o_tempo_de_nenhuma_das_duas_armas()
    {
        await UsarAsync(0); // Espada em recarga.
        _agoraMs += 1_000;
        var espadaAntes = (await _nucleo.BarraAsync(Personagem, CancellationToken.None))
            .Skills[0]
            .RestanteMs;

        _arma.Atual = Cajado;
        await UsarAsync(1); // Cajado em recarga.
        _agoraMs += 700; // Passa o tempo; a troca em si não gasta nem devolve recarga.
        var cajadoAntes = (await _nucleo.BarraAsync(Personagem, CancellationToken.None))
            .Skills[1]
            .RestanteMs;

        _arma.Atual = Espada;
        var espadaDepois = (await _nucleo.BarraAsync(Personagem, CancellationToken.None))
            .Skills[0]
            .RestanteMs;
        _arma.Atual = Cajado;
        var cajadoDepois = (await _nucleo.BarraAsync(Personagem, CancellationToken.None))
            .Skills[1]
            .RestanteMs;

        // Só o relógio gasta a recarga: ir e voltar de arma não zera nem adianta nada.
        Assert.True(espadaAntes > 0);
        Assert.Equal(espadaAntes - 700, espadaDepois);
        Assert.Equal(cajadoAntes, cajadoDepois);
    }

    [Fact]
    public async Task Skill_magica_gasta_mana_e_a_fisica_gasta_stamina()
    {
        _arma.Atual = Cajado;
        await UsarAsync(0);
        Assert.True(_cobranca.Mana < 100);
        Assert.Equal(100, _cobranca.Stamina);

        _arma.Atual = Espada;
        await UsarAsync(0);
        Assert.True(_cobranca.Stamina < 100);
    }
}
