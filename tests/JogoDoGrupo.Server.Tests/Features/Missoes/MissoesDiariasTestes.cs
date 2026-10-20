using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Missoes;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Missoes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Missoes;

/// <summary>
/// Testes das Missões Diárias e Semanais (#1062).
/// Valida rotação diária (3 por dia) e semanal por região e nível, reset no horário de Brasília (UTC-3),
/// bloqueio contra repetição no mesmo ciclo e proteção contra pagamento duplo em entrega concorrente.
/// </summary>
public sealed class MissoesDiariasTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> ComServicoAsync<T>(Func<ServicoDeMissoes, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>());
    }

    private async Task SubirNivelAsync(long id, int xp = 2000)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        await escopo
            .ServiceProvider.GetRequiredService<IAjusteDeExperienciaDeTeste>()
            .AjustarAsync(id, xp, default);
    }

    private async Task LiberarRegiaoAsync(long id)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        int[] concluidas =
        {
            CatalogoDeMissoes.Missao1Chegada,
            CatalogoDeMissoes.Missao2PrimeiraCacada,
            CatalogoDeMissoes.Missao3CascaDeFerroECouro,
            CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes,
            CatalogoDeMissoes.Missao5LimpandoATrilhaNorte,
            CatalogoDeMissoes.Missao6OEnxameNaMataFria,
            CatalogoDeMissoes.Missao7OSenhorDaColinaDoVigia,
        };
        foreach (var mId in concluidas)
        {
            db.Set<MissaoPersonagemEntidade>()
                .Add(
                    new MissaoPersonagemEntidade
                    {
                        PersonagemId = id,
                        MissaoId = mId,
                        Estado = EstadoDaMissao.Concluida,
                        ConcluidaEm = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ObjetivosJson = "[]",
                    }
                );
        }
        await db.SaveChangesAsync();
    }

    private async Task CompletarObjetivosAsync(long id, int missaoId)
    {
        var def = CatalogoDeMissoes.Obter(missaoId)!;
        foreach (var obj in def.Objetivos)
        {
            switch (obj.Tipo)
            {
                case TipoDeObjetivo.Matar:
                    for (int i = 0; i < obj.QuantidadeTotal; i++)
                    {
                        await ComServicoAsync(s =>
                            s.RegistrarMorteInimigoAsync(id, obj.AlvoId, default)
                        );
                    }
                    break;
                case TipoDeObjetivo.Coletar:
                    await ComServicoAsync(s =>
                        s.RegistrarColetaItemAsync(id, obj.AlvoId, obj.QuantidadeTotal, default)
                    );
                    break;
                case TipoDeObjetivo.Falar:
                    await ComServicoAsync(s =>
                        s.RegistrarInteracaoNpcAsync(id, obj.AlvoId, default)
                    );
                    break;
                case TipoDeObjetivo.IrA:
                    await ComServicoAsync(s =>
                        s.RegistrarChegadaLocalAsync(
                            id,
                            obj.CoordenadaX,
                            obj.CoordenadaZ,
                            obj.RegiaoAlvo,
                            default
                        )
                    );
                    break;
            }
        }
    }

    [Fact]
    public async Task RotacaoDiaria_RetornaTresMissoes_PermiteAceitarApenasDaRotacao()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await SubirNivelAsync(id, 2000);
        await LiberarRegiaoAsync(id);

        var momento = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.FromHours(-3));
        var estado = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, momento));

        Assert.Equal(ConfigMissoes.QuantidadeDiariasPorDia, estado.RotacaoDiariaIds.Length);
        Assert.True(estado.ProximoResetDiarioSegundos > 0);
        Assert.True(estado.ProximoResetSemanalSegundos > 0);

        // Aceita missão que está na rotação
        var missaoValida = estado.RotacaoDiariaIds[0];
        var (ok, erro, aceita) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, missaoValida, momento)
        );
        Assert.True(ok, erro);
        Assert.NotNull(aceita);
        Assert.Equal(missaoValida, aceita.MissaoId);

        // Missão fora da rotação do dia é recusada
        var todasDiarias = CatalogoDeMissoes
            .Todas.Where(m => m.Tipo == TipoDeMissao.Diaria)
            .Select(m => m.Id)
            .ToList();
        var missaoFora = todasDiarias.FirstOrDefault(mId => !estado.RotacaoDiariaIds.Contains(mId));
        Assert.True(missaoFora > 0);

        var (okFora, erroFora, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, missaoFora, momento)
        );
        Assert.False(okFora);
        Assert.Equal(CodigosErroMissoes.MissaoForaDaRotacao, erroFora);
    }

    [Fact]
    public async Task MissaoDiaria_ConclusaoERecompensas_NaoRepeteNoMesmoCiclo()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await SubirNivelAsync(id, 2000);
        await LiberarRegiaoAsync(id);

        var momento = new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.FromHours(-3));
        var estado = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, momento));
        var missaoId = estado.RotacaoDiariaIds[0];

        var (okAceitar, erroAceitar, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, missaoId, momento)
        );
        Assert.True(okAceitar, erroAceitar);

        await CompletarObjetivosAsync(id, missaoId);

        var saldoAntes = await _ambiente.SaldoAsync(id);
        var def = CatalogoDeMissoes.Obter(missaoId)!;

        var (okEnt, erroEnt, conc, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, missaoId, null, momento)
        );
        Assert.True(okEnt, erroEnt);
        Assert.NotNull(conc);
        Assert.Equal(EstadoDaMissao.Concluida, conc.Estado);

        var saldoDepois = await _ambiente.SaldoAsync(id);
        Assert.Equal(saldoAntes + def.RecompensaCoroas, saldoDepois);

        // No mesmo ciclo diário, a missão não pode ser aceita novamente
        var (okRepetida, erroRepetida, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, missaoId, momento)
        );
        Assert.False(okRepetida);
        Assert.Equal(CodigosErroMissoes.MissaoJaConcluidaNoCiclo, erroRepetida);

        // O estado confirma que consta nas concluídas do ciclo atual
        var estadoApos = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, momento));
        Assert.Contains(missaoId, estadoApos.Concluidas);
    }

    [Fact]
    public async Task ResetDiario_ViradaMeiaNoiteBrasilia_PermiteFazerNovamente()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await SubirNivelAsync(id, 2000);
        await LiberarRegiaoAsync(id);

        var antesReset = new DateTimeOffset(2026, 10, 20, 23, 45, 0, TimeSpan.FromHours(-3));
        var estadoAntes = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, antesReset));
        var missaoId = estadoAntes.RotacaoDiariaIds[0];

        // Aceita, completa e entrega antes da meia-noite
        await ComServicoAsync(s => s.AceitarMissaoAsync(id, missaoId, antesReset));
        await CompletarObjetivosAsync(id, missaoId);
        var (okEnt, _, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, missaoId, null, antesReset)
        );
        Assert.True(okEnt);

        // Logo após a virada da meia-noite de Brasília (00:15 BRT = 03:15 UTC do dia 21)
        var depoisReset = new DateTimeOffset(2026, 10, 21, 0, 15, 0, TimeSpan.FromHours(-3));
        var estadoDepois = await ComServicoAsync(s =>
            s.ObterEstadoDasMissoesAsync(id, depoisReset)
        );

        // A missão concluída ontem não consta mais nas concluídas do novo ciclo diário
        Assert.DoesNotContain(missaoId, estadoDepois.Concluidas);

        // Permite aceitar as missões da nova rotação
        var novaMissao = estadoDepois.RotacaoDiariaIds[0];
        var (okNovo, erroNovo, aceitaNovo) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, novaMissao, depoisReset)
        );
        Assert.True(okNovo, erroNovo);
        Assert.NotNull(aceitaNovo);
        Assert.Equal(novaMissao, aceitaNovo.MissaoId);
    }

    [Fact]
    public async Task RotacaoSemanal_ConclusaoEResetTercaFeira()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await SubirNivelAsync(id, 2000);
        await LiberarRegiaoAsync(id);

        // Segunda-feira às 21:00 BRT
        var segunda = new DateTimeOffset(2026, 10, 26, 21, 0, 0, TimeSpan.FromHours(-3));
        var estadoSegunda = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, segunda));

        Assert.Single(estadoSegunda.RotacaoSemanalIds);
        var missaoSemanal = estadoSegunda.RotacaoSemanalIds[0];

        var (okAceitar, erroAceitar, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, missaoSemanal, segunda)
        );
        Assert.True(okAceitar, erroAceitar);

        await CompletarObjetivosAsync(id, missaoSemanal);

        var (okEnt, erroEnt, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, missaoSemanal, null, segunda)
        );
        Assert.True(okEnt, erroEnt);

        // Rejeita repetição na mesma semana
        var (okRep, erroRep, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, missaoSemanal, segunda)
        );
        Assert.False(okRep);
        Assert.Equal(CodigosErroMissoes.MissaoJaConcluidaNoCiclo, erroRep);

        // Terça-feira às 01:00 BRT (novo ciclo semanal iniciado às 00:00 BRT da terça)
        var terca = new DateTimeOffset(2026, 10, 27, 1, 0, 0, TimeSpan.FromHours(-3));
        var estadoTerca = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, terca));

        // Não consta mais nas concluídas do ciclo semanal atual
        Assert.DoesNotContain(missaoSemanal, estadoTerca.Concluidas);

        // Nova rotação semanal ativa
        Assert.Single(estadoTerca.RotacaoSemanalIds);
    }

    [Fact]
    public async Task EntregaConcorrente_NaoPagaEmDobro()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await SubirNivelAsync(id, 2000);
        await LiberarRegiaoAsync(id);

        var momento = new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.FromHours(-3));
        var estado = await ComServicoAsync(s => s.ObterEstadoDasMissoesAsync(id, momento));
        var missaoId = estado.RotacaoDiariaIds[0];

        var (okAceitar, erroAceitar, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, missaoId, momento)
        );
        Assert.True(okAceitar, erroAceitar);

        await CompletarObjetivosAsync(id, missaoId);

        var saldoAntes = await _ambiente.SaldoAsync(id);
        var def = CatalogoDeMissoes.Obter(missaoId)!;

        // Dispara duas entregas simultâneas da mesma missão
        var task1 = ComServicoAsync(s => s.EntregarMissaoAsync(id, missaoId, null, momento));
        var task2 = ComServicoAsync(s => s.EntregarMissaoAsync(id, missaoId, null, momento));

        var resultados = await Task.WhenAll(task1, task2);

        var sucessos = resultados.Count(r => r.Sucesso);
        var falhas = resultados.Count(r =>
            !r.Sucesso && r.Erro == CodigosErroMissoes.MissaoNaoEncontrada
        );

        Assert.Equal(1, sucessos);
        Assert.Equal(1, falhas);

        // Recompensa foi paga exatamente uma vez
        var saldoDepois = await _ambiente.SaldoAsync(id);
        Assert.Equal(saldoAntes + def.RecompensaCoroas, saldoDepois);
    }
}
