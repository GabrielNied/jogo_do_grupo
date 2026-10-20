using JogoDoGrupo.Server.Features.Missoes;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Missoes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Missoes;

/// <summary>
/// Testes de integração do motor autoritativo de missões (#179, #1061).
/// </summary>
public sealed class ServicoDeMissoesTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> ComServicoAsync<T>(Func<ServicoDeMissoes, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>());
    }

    [Fact]
    public async Task GarantirMissaoInicial_CriaMissao1ChegadaAoVilarejo()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        var estado = await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        Assert.NotNull(estado);
        Assert.Single(estado.Ativas);
        var missao = estado.Ativas[0];
        Assert.Equal(CatalogoDeMissoes.Missao1Chegada, missao.MissaoId);
        Assert.Equal(EstadoDaMissao.EmAndamento, missao.Estado);
        Assert.True(missao.Acompanhada);
        Assert.Single(missao.Objetivos);
        Assert.Equal(0, missao.Objetivos[0].QuantidadeAtual);
        Assert.False(missao.Objetivos[0].Concluido);

        // Repetir a chamada não duplica a missão
        var repetido = await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));
        Assert.Single(repetido.Ativas);
    }

    [Fact]
    public async Task AceitarMissao_SegundaMissaoRequerPreRequisito()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        // Tenta aceitar a missão 2 sem ter concluído a missão 1
        var (sucesso, erro, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao2PrimeiraCacada, default)
        );

        Assert.False(sucesso);
        Assert.Equal(CodigosErroMissoes.PreRequisitosNaoCumpridos, erro);
    }

    [Fact]
    public async Task FalarComNpc_ConcluiObjetivoEProntaParaEntrega()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        // Interage com o NPC Alden (Id 1)
        var atualizadas = await ComServicoAsync(s => s.RegistrarInteracaoNpcAsync(id, 1, default));

        Assert.Single(atualizadas);
        var missao = atualizadas[0];
        Assert.Equal(CatalogoDeMissoes.Missao1Chegada, missao.MissaoId);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, missao.Estado);
        Assert.True(missao.Objetivos[0].Concluido);
        Assert.Equal(1, missao.Objetivos[0].QuantidadeAtual);
    }

    [Fact]
    public async Task EntregarMissao_ConcedeRecompensasEIniciaProximaPrincipal()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));
        await ComServicoAsync(s => s.RegistrarInteracaoNpcAsync(id, 1, default));

        var saldoAntes = await _ambiente.SaldoAsync(id);

        var (sucesso, erro, concluida, proxima) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1Chegada, null, default)
        );

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(concluida);
        Assert.Equal(EstadoDaMissao.Concluida, concluida.Estado);

        // Verifica coroas creditadas
        var saldoDepois = await _ambiente.SaldoAsync(id);
        Assert.Equal(saldoAntes + 25, saldoDepois);

        // Verifica cadeia de missões principais: Missão 2 foi iniciada automaticamente
        Assert.NotNull(proxima);
        Assert.Equal(CatalogoDeMissoes.Missao2PrimeiraCacada, proxima.MissaoId);
        Assert.Equal(EstadoDaMissao.EmAndamento, proxima.Estado);
    }

    [Fact]
    public async Task MorteInimigo_ProgressoObjetivoMatar()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));
        // Etapa 1 -> Alden
        await ComServicoAsync(s => s.RegistrarInteracaoNpcAsync(id, 1, default));
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1Chegada, null, default)
        );

        // Etapa 2 -> Gareth
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(
                id,
                JogoDoGrupo.Shared.Features.Npcs.CatalogoNpcs.IdArmeiro,
                default
            )
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao2PrimeiraFerramenta,
                JogoDoGrupo.Shared.Features.Itens.CatalogoEquipamentos.PrimeiroModeloDeArma,
                default
            )
        );

        // Etapa 3 -> Brom
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(
                id,
                JogoDoGrupo.Shared.Features.Npcs.CatalogoNpcs.IdFerreiro,
                default
            )
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao3CascaDeFerroECouro,
                JogoDoGrupo.Shared.Features.Itens.CatalogoEquipamentos.PrimeiroModelo,
                default
            )
        );

        // Etapa 4 -> Barnabé e Cassian
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(
                id,
                JogoDoGrupo.Shared.Features.Npcs.CatalogoNpcs.IdTaverneiro,
                default
            )
        );
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(
                id,
                JogoDoGrupo.Shared.Features.Npcs.CatalogoNpcs.IdBanqueiro,
                default
            )
        );
        await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes,
                null,
                default
            )
        );

        // Agora a Missão 5 está em andamento (Derrotar 6 Javalis do Campo, CriaturaId = 1)
        var mortes1 = await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 1, default));
        Assert.Single(mortes1);
        Assert.Equal(1, mortes1[0].Objetivos[0].QuantidadeAtual);
        Assert.False(mortes1[0].Objetivos[0].Concluido);

        for (int i = 0; i < 4; i++)
        {
            await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 1, default));
        }
        var mortesFinal = await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 1, default));

        Assert.Single(mortesFinal);
        Assert.Equal(6, mortesFinal[0].Objetivos[0].QuantidadeAtual);
        Assert.True(mortesFinal[0].Objetivos[0].Concluido);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, mortesFinal[0].Estado);
    }

    [Fact]
    public async Task AbandonarMissao_RecusaMissaoPrincipal()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        var (sucesso, erro) = await ComServicoAsync(s =>
            s.AbandonarMissaoAsync(id, CatalogoDeMissoes.Missao1Chegada, default)
        );

        Assert.False(sucesso);
        Assert.Equal(CodigosErroMissoes.MissaoPrincipalNaoAbandona, erro);
    }

    [Fact]
    public async Task AlternarAcompanhamento_AtualizaEstadoCorretamente()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        var (sucesso, erro) = await ComServicoAsync(s =>
            s.AlternarAcompanhamentoAsync(id, CatalogoDeMissoes.Missao1Chegada, false, default)
        );

        Assert.True(sucesso);
        Assert.Null(erro);

        var estado = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, default));
        Assert.False(estado.Ativas[0].Acompanhada);
    }
}
