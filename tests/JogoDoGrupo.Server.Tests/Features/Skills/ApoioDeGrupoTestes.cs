using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Features.Skills;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Server.Tests.Features.Skills;

public sealed class ApoioDeGrupoTestes
{
    private sealed class ApoioFalso : IApoioDeCombate
    {
        public List<long> Escudos { get; } = [];
        public List<(long Alvo, TipoEfeitoDeStatus Tipo)> Buffs { get; } = [];

        public Task<bool> AplicarEscudoAsync(
            long personagemId,
            double fracaoDaVidaMaxima,
            double duracaoSegundos,
            CancellationToken cancelamento
        )
        {
            Escudos.Add(personagemId);
            return Task.FromResult(true);
        }

        public bool AplicarBuff(long personagemId, TipoEfeitoDeStatus tipo, double duracaoSegundos)
        {
            Buffs.Add((personagemId, tipo));
            return true;
        }
    }

    private sealed class GrupoFalso(params long[] membros) : IConsultaDeGrupo
    {
        public GrupoAtual? Consultar(long personagemId) =>
            membros.Contains(personagemId) ? new GrupoAtual(1, membros[0], membros) : null;
    }

    private const long Conjurador = 7;
    private const long Perto = 8;
    private const long Longe = 9;
    private const long ForaDoGrupo = 10;
    private const string Area = "cidade";

    private readonly CobrancaFalsa _cobranca = new();
    private readonly ApoioFalso _apoio = new();
    private readonly AlvoFalso _alvo = new(-50) { Ativo = true };
    private readonly long _agoraMs = 1_000_000;

    private NucleoDeSkills Nucleo(TipoEquipamento tipo, string skillId, long[] grupo)
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(Conjurador, Area, 0, 0);
        posicoes.Colocar(Perto, Area, 5, 5);
        posicoes.Colocar(Longe, Area, ConfigApoioDeGrupo.AlcanceDoApoioMetros + 1, 0);
        posicoes.Colocar(ForaDoGrupo, Area, 1, 1);
        return new NucleoDeSkills(
            new ArmaFalsa { Atual = new ArmaEquipada(20, 1, tipo, 1) },
            new ArmazemEmMemoria { Fixos = [skillId] },
            _cobranca,
            new BonusFalso
            {
                Equipamentos = new AtributosDosEquipamentos { TemArma = true, DanoDaArma = 20 },
            },
            posicoes,
            [_alvo],
            () => _agoraMs,
            apoio: _apoio,
            grupos: new GrupoFalso(grupo)
        );
    }

    private static Task<ResultadoDoUso> UsarAsync(NucleoDeSkills nucleo) =>
        nucleo.UsarAsync(
            Conjurador,
            new UsarSkill { Espaco = 0, Sequencia = 1 },
            CancellationToken.None
        );

    [Fact]
    public async Task Escudo_vai_a_quem_usa_e_ao_grupo_no_alcance_sem_causar_dano()
    {
        var nucleo = Nucleo(
            TipoEquipamento.Cajado,
            "cajado.manto-protetor",
            [Conjurador, Perto, Longe]
        );

        var resultado = await UsarAsync(nucleo);

        Assert.Null(resultado.Recusa);
        Assert.Equal(new long[] { Conjurador, Perto }, _apoio.Escudos);
        Assert.Equal(0, _alvo.Aplicacoes);
        Assert.Empty(resultado.Acertos!.Acertos);
        Assert.Equal(70, _cobranca.Mana);
    }

    [Fact]
    public async Task Sem_grupo_o_apoio_vale_so_para_quem_usa()
    {
        var nucleo = Nucleo(TipoEquipamento.Cajado, "cajado.manto-protetor", []);

        await UsarAsync(nucleo);

        Assert.Equal(new long[] { Conjurador }, _apoio.Escudos);
    }

    [Theory]
    [InlineData(TipoEquipamento.Foco, "foco.fonte-arcana", TipoEfeitoDeStatus.Fortalecido)]
    [InlineData(TipoEquipamento.Martelo, "martelo.brado-do-guardiao", TipoEfeitoDeStatus.Protegido)]
    public async Task Buff_de_grupo_vai_ao_grupo_no_alcance(
        TipoEquipamento arma,
        string skill,
        TipoEfeitoDeStatus esperado
    )
    {
        var nucleo = Nucleo(arma, skill, [Conjurador, Perto, Longe]);

        await UsarAsync(nucleo);

        Assert.Equal([(Conjurador, esperado), (Perto, esperado)], _apoio.Buffs);
        Assert.Empty(_apoio.Escudos);
        Assert.Equal(0, _alvo.Aplicacoes);
    }

    [Fact]
    public async Task Sem_recurso_recusa_e_nao_aplica_apoio()
    {
        _cobranca.Mana = 0;
        var nucleo = Nucleo(TipoEquipamento.Cajado, "cajado.manto-protetor", [Conjurador, Perto]);

        var resultado = await UsarAsync(nucleo);

        Assert.Equal(CodigosErroSkills.RecursoInsuficiente, resultado.Recusa);
        Assert.Empty(_apoio.Escudos);
    }
}
