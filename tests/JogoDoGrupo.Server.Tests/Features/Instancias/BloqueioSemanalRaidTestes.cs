using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Instancias;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Tests.Features.Instancias;

public sealed class BloqueioSemanalRaidTestes : IDisposable
{
    private readonly SqliteConnection _conexao;
    private readonly JogoDbContext _db;
    private readonly BloqueioSemanalRaidServico _servico;

    public BloqueioSemanalRaidTestes()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        var opcoes = new DbContextOptionsBuilder<JogoDbContext>().UseSqlite(_conexao).Options;
        _db = new JogoDbContext(opcoes);
        _db.Database.EnsureCreated();
        _servico = new BloqueioSemanalRaidServico(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conexao.Dispose();
    }

    [Fact]
    public async Task Registra_conclusao_e_consulta_etapas_corretamente()
    {
        const long personagemId = 1001;
        const string semanaAtual = "2026-10-20";
        var agora = DateTimeOffset.UtcNow;

        // Inicialmente nenhuma etapa concluída
        Assert.False(await _servico.JaConcluiuEtapaAsync(personagemId, semanaAtual, 1));
        var iniciais = await _servico.ObterEtapasConcluidasAsync(personagemId, semanaAtual);
        Assert.Empty(iniciais);

        // Registra etapa 1
        await _servico.RegistrarConclusaoAsync(personagemId, semanaAtual, 1, agora);
        Assert.True(await _servico.JaConcluiuEtapaAsync(personagemId, semanaAtual, 1));
        Assert.False(await _servico.JaConcluiuEtapaAsync(personagemId, semanaAtual, 2));

        // Registra etapa 2
        await _servico.RegistrarConclusaoAsync(personagemId, semanaAtual, 2, agora);
        var concluidas = await _servico.ObterEtapasConcluidasAsync(personagemId, semanaAtual);
        Assert.Equal(new[] { 1, 2 }, concluidas.OrderBy(x => x));

        // Tentar registrar novamente a mesma etapa é idempotente (não duplica nem lança erro)
        await _servico.RegistrarConclusaoAsync(personagemId, semanaAtual, 1, agora);
        var aposDuplicata = await _servico.ObterEtapasConcluidasAsync(personagemId, semanaAtual);
        Assert.Equal(2, aposDuplicata.Count);
    }

    [Fact]
    public async Task Semanas_diferentes_sao_independentes()
    {
        const long personagemId = 2002;
        const string semanaPassada = "2026-10-13";
        const string semanaNova = "2026-10-20";
        var agora = DateTimeOffset.UtcNow;

        await _servico.RegistrarConclusaoAsync(personagemId, semanaPassada, 1, agora);

        // Na semana passada já concluiu a etapa 1
        Assert.True(await _servico.JaConcluiuEtapaAsync(personagemId, semanaPassada, 1));

        // Na semana nova ainda não concluiu
        Assert.False(await _servico.JaConcluiuEtapaAsync(personagemId, semanaNova, 1));
        var concluidasNova = await _servico.ObterEtapasConcluidasAsync(personagemId, semanaNova);
        Assert.Empty(concluidasNova);
    }
}
