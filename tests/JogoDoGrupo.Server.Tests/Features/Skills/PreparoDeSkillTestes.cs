using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Features.Skills;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Server.Tests.Features.Skills;

/// <summary>Preparo e cancelamento de skill forte (#345): custo só ao concluir ou cancelar, 50% no cancelamento.</summary>
public sealed class PreparoDeSkillTestes
{
    private const long Personagem = 7;
    private const int Fraca = 0;
    private const int Forte = 1;

    private static readonly ArmaEquipada Espada = new(10, 1, TipoEquipamento.Espada, 1);

    private readonly ArmaFalsa _arma = new() { Atual = Espada };
    private readonly CobrancaFalsa _cobranca = new();
    private readonly AlvoFalso _alvo = new(-50) { Ativo = true };
    private readonly NucleoDeSkills _nucleo;
    private readonly DefinicaoDeSkill _definicaoForte;
    private long _agoraMs = 1_000_000;

    public PreparoDeSkillTestes()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.Colocar(Personagem, "cidade", 0, 0);
        _nucleo = new NucleoDeSkills(
            _arma,
            new ArmazemEmMemoria
            {
                Fixos =
                [
                    "espada.corte-veloz",
                    "espada.golpe-do-carrasco",
                    "espada.lamina-giratoria",
                ],
            },
            _cobranca,
            new BonusFalso
            {
                Equipamentos = new AtributosDosEquipamentos { TemArma = true, DanoDaArma = 20 },
            },
            posicoes,
            [_alvo],
            () => Interlocked.Read(ref _agoraMs)
        );
        _definicaoForte = CatalogoDeSkills.Obter("espada.golpe-do-carrasco")!;
    }

    private Task<ResultadoDoUso> UsarAsync(int espaco, int sequencia = 1) =>
        _nucleo.UsarAsync(
            Personagem,
            new UsarSkill { Espaco = espaco, Sequencia = sequencia },
            CancellationToken.None
        );

    [Fact]
    public void So_skill_forte_tem_preparo()
    {
        foreach (var tipo in CatalogoDeSkills.TiposComSkills)
        {
            foreach (var skill in CatalogoDeSkills.Pool(tipo))
            {
                var forte = skill.Multiplicador >= ConfigSkills.MultiplicadorDeSkillForte;
                Assert.Equal(forte ? ConfigSkills.PreparoDaSkillForteMs : 0, skill.PreparoMs);
            }
        }

        Assert.True(_definicaoForte.PreparoMs > 0);
    }

    [Theory]
    [InlineData(40, 20)]
    [InlineData(25, 13)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    public void Custo_ao_cancelar_e_metade_arredondada_para_cima(int custo, int esperado) =>
        Assert.Equal(esperado, RegrasSkills.CustoAoCancelar(custo));

    [Fact]
    public async Task Skill_fraca_continua_instantanea()
    {
        var resultado = await UsarAsync(Fraca);

        Assert.Null(resultado.Preparo);
        Assert.NotNull(resultado.Usada);
        Assert.Equal(1, _cobranca.Cobrancas);
    }

    [Fact]
    public async Task Skill_forte_inicia_o_preparo_sem_cobrar_nem_dar_dano()
    {
        var resultado = await UsarAsync(Forte, sequencia: 4);

        Assert.Null(resultado.Recusa);
        Assert.Null(resultado.Usada);
        Assert.Equal(_definicaoForte.PreparoMs, resultado.Preparo!.PreparoMs);
        Assert.Equal(4, resultado.Preparo.Sequencia);
        Assert.Equal(0, _cobranca.Cobrancas);
        Assert.Equal(100, _cobranca.Stamina);
        Assert.Equal(0, _alvo.Aplicacoes);
    }

    [Fact]
    public async Task Concluir_cobra_o_custo_inteiro_uma_vez_aplica_o_dano_e_comeca_a_recarga()
    {
        await UsarAsync(Forte, sequencia: 4);

        var resultado = await _nucleo.ConcluirPreparoAsync(Personagem, 4, CancellationToken.None);

        Assert.Null(resultado!.Recusa);
        Assert.Equal(100 - _definicaoForte.Custo, _cobranca.Stamina);
        Assert.Equal(1, _cobranca.Cobrancas);
        Assert.Equal(1, _alvo.Aplicacoes);
        Assert.Equal(_definicaoForte.RecargaMs, resultado.Usada!.RecargaMs);
        var barra = await _nucleo.BarraAsync(Personagem, CancellationToken.None);
        Assert.Equal(_definicaoForte.RecargaMs, barra.Skills[Forte].RestanteMs);
        Assert.Null(await _nucleo.ConcluirPreparoAsync(Personagem, 4, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelar_cobra_metade_do_custo_sem_dano_e_sem_recarga()
    {
        await UsarAsync(Forte, sequencia: 4);

        var resultado = await _nucleo.CancelarPreparoAsync(Personagem, CancellationToken.None);

        var custo = RegrasSkills.CustoAoCancelar(_definicaoForte.Custo);
        Assert.Equal(custo, resultado!.Cancelada!.CustoCobrado);
        Assert.Equal(4, resultado.Cancelada.Sequencia);
        Assert.Equal(100 - custo, _cobranca.Stamina);
        Assert.Equal(100 - custo, resultado.Recursos!.StaminaAtual);
        Assert.Equal(1, _cobranca.Cobrancas);
        Assert.Equal(0, _alvo.Aplicacoes);
        var barra = await _nucleo.BarraAsync(Personagem, CancellationToken.None);
        Assert.Equal(0, barra.Skills[Forte].RestanteMs);

        // Depois de cancelar, o fim do preparo não cobra de novo nem aplica nada.
        Assert.Null(await _nucleo.ConcluirPreparoAsync(Personagem, 4, CancellationToken.None));
        Assert.Equal(1, _cobranca.Cobrancas);
        Assert.Null(await _nucleo.CancelarPreparoAsync(Personagem, CancellationToken.None));
        Assert.Equal(1, _cobranca.Cobrancas);
    }

    [Fact]
    public async Task Cancelar_e_concluir_ao_mesmo_tempo_cobram_uma_vez_so()
    {
        for (var rodada = 0; rodada < 20; rodada++)
        {
            _cobranca.Stamina = 100;
            var antes = _cobranca.Cobrancas;
            _agoraMs += _definicaoForte.RecargaMs + 1; // A recarga da rodada anterior acabou.
            await UsarAsync(Forte, sequencia: rodada + 1);

            var resultados = await Task.WhenAll(
                Task.Run(() =>
                    _nucleo.ConcluirPreparoAsync(Personagem, rodada + 1, CancellationToken.None)
                ),
                Task.Run(() => _nucleo.CancelarPreparoAsync(Personagem, CancellationToken.None))
            );

            Assert.Equal(1, _cobranca.Cobrancas - antes);
            Assert.Single(resultados, r => r != null);
        }
    }

    [Fact]
    public async Task Durante_o_preparo_outra_skill_e_recusada_sem_gastar()
    {
        await UsarAsync(Forte);

        var recusado = await UsarAsync(Fraca, sequencia: 2);

        Assert.Equal(CodigosErroSkills.AcaoEmAndamento, recusado.Recusa);
        Assert.Equal(2, recusado.Sequencia);
        Assert.Equal(0, _cobranca.Cobrancas);
    }

    [Fact]
    public async Task Sem_recurso_para_o_custo_recusa_ao_pedir_sem_iniciar_o_preparo()
    {
        _cobranca.Stamina = _definicaoForte.Custo - 1;

        var recusado = await UsarAsync(Forte);

        Assert.Equal(CodigosErroSkills.RecursoInsuficiente, recusado.Recusa);
        Assert.Null(recusado.Preparo);
        Assert.Null(await _nucleo.CancelarPreparoAsync(Personagem, CancellationToken.None));
    }

    [Fact]
    public async Task Gastar_o_recurso_no_meio_do_preparo_recusa_ao_concluir_sem_cobrar()
    {
        await UsarAsync(Forte, sequencia: 3);
        _cobranca.Stamina = 0;

        var resultado = await _nucleo.ConcluirPreparoAsync(Personagem, 3, CancellationToken.None);

        Assert.Equal(CodigosErroSkills.RecursoInsuficiente, resultado!.Recusa);
        Assert.Equal(3, resultado.Sequencia);
        Assert.Equal(0, _cobranca.Cobrancas);
        Assert.Equal(0, _alvo.Aplicacoes);
        Assert.False(_nucleo.EmPreparo(Personagem));
    }

    [Fact]
    public async Task Cancelar_sem_recurso_para_a_metade_cancela_sem_cobrar()
    {
        await UsarAsync(Forte);
        _cobranca.Stamina = 0;

        var resultado = await _nucleo.CancelarPreparoAsync(Personagem, CancellationToken.None);

        Assert.Equal(0, resultado!.Cancelada!.CustoCobrado);
        Assert.Equal(0, _cobranca.Cobrancas);
    }

    [Fact]
    public async Task Trocar_de_arma_no_preparo_cancela_sem_cobrar_ao_concluir()
    {
        await UsarAsync(Forte, sequencia: 3);
        _arma.Atual = new ArmaEquipada(11, 2, TipoEquipamento.Cajado, 1);

        var resultado = await _nucleo.ConcluirPreparoAsync(Personagem, 3, CancellationToken.None);

        Assert.NotNull(resultado!.Cancelada);
        Assert.Equal(0, resultado.Cancelada.CustoCobrado);
        Assert.Equal(0, _cobranca.Cobrancas);
    }

    [Fact]
    public async Task Preparo_de_pedido_antigo_nao_conclui_o_novo()
    {
        await UsarAsync(Forte, sequencia: 1);
        await _nucleo.CancelarPreparoAsync(Personagem, CancellationToken.None);
        await UsarAsync(Forte, sequencia: 2);

        Assert.Null(await _nucleo.ConcluirPreparoAsync(Personagem, 1, CancellationToken.None));
        Assert.True(_nucleo.EmPreparo(Personagem));
    }
}
