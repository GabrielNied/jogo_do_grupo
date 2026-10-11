using System.Data;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>
/// Concede a experiência de uma morte e informa os pontos já distribuídos. A gravação roda
/// fora do laço de IA de Mobs. A mesma morte (inimigo + sequência) não premia duas vezes.
/// </summary>
public sealed class ExperienciaDoPersonagem : IPontosDistribuidos
{
    private readonly IServiceScopeFactory _escopos;
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly ILogger<ExperienciaDoPersonagem> _log;

    public ExperienciaDoPersonagem(
        IServiceScopeFactory escopos,
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        ILogger<ExperienciaDoPersonagem> log
    )
    {
        _escopos = escopos;
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _log = log;
    }

    /// <summary>Não bloqueia quem publicou a morte. Falha de um prêmio não derruba o laço.</summary>
    public void Agendar(InimigoDerrotado derrota)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await ConcederAsync(derrota, CancellationToken.None);
            }
            catch (Exception e)
            {
                _log.LogError(
                    e,
                    "Falha ao conceder experiência da morte {Inimigo}/{Sequencia}.",
                    derrota.InimigoId,
                    derrota.Sequencia
                );
            }
        });
    }

    /// <summary>Cada participante recebe <see cref="InimigoDerrotado.Experiencia"/> inteira.</summary>
    public async Task ConcederAsync(InimigoDerrotado derrota, CancellationToken cancelamento)
    {
        if (derrota.Participantes.Count == 0 || derrota.Experiencia <= 0)
        {
            return;
        }

        using var escopo = _escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancelamento
        );

        db.Set<MortePremiada>()
            .Add(
                new MortePremiada { InimigoId = derrota.InimigoId, Sequencia = derrota.Sequencia }
            );
        try
        {
            await db.SaveChangesAsync(cancelamento);
        }
        catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())
        {
            return;
        }

        var avisos = new List<(long PersonagemId, ExperienciaGanha Ganho, bool Subiu)>();
        foreach (var personagemId in derrota.Participantes.Distinct())
        {
            var personagem = await db.Set<Personagem>()
                .FirstOrDefaultAsync(p => p.Id == personagemId, cancelamento);
            if (personagem == null)
            {
                continue;
            }

            var nivelAntes = personagem.Nivel;
            var resultado = RegrasExperiencia.AplicarGanho(
                personagem.Nivel,
                personagem.XpAtual,
                derrota.Experiencia
            );
            personagem.Nivel = resultado.Nivel;
            personagem.XpAtual = resultado.Xp;
            personagem.PontosLivres = checked(personagem.PontosLivres + resultado.PontosGanhos);
            avisos.Add(
                (
                    personagemId,
                    new ExperienciaGanha
                    {
                        Quantidade = derrota.Experiencia,
                        XpAtual = personagem.XpAtual,
                        XpDoNivel = RegrasExperiencia.XpParaOProximoNivel(personagem.Nivel),
                        Nivel = personagem.Nivel,
                        PontosLivres = personagem.PontosLivres,
                    },
                    resultado.Nivel > nivelAntes
                )
            );
        }

        try
        {
            await db.SaveChangesAsync(cancelamento);
            await transacao.CommitAsync(cancelamento);
        }
        catch (Exception e) when (e.EhBancoOcupado())
        {
            _log.LogWarning(
                e,
                "Banco ocupado ao conceder a experiência da morte {Inimigo}/{Sequencia}.",
                derrota.InimigoId,
                derrota.Sequencia
            );
            return;
        }

        foreach (var (personagemId, ganho, subiu) in avisos)
        {
            EnviarAoDono(personagemId, PersonagemMessageIds.ExperienciaGanha, ganho);
            if (subiu)
            {
                EnviarNaArea(
                    derrota.Area,
                    PersonagemMessageIds.SubiuDeNivel,
                    new SubiuDeNivel { PersonagemId = personagemId, Nivel = ganho.Nivel }
                );
            }
        }
    }

    public async Task<PontosDeAtributos> ObterAsync(
        long personagemId,
        CancellationToken cancelamento
    )
    {
        using var escopo = _escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var personagem = await db.Set<Personagem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personagemId, cancelamento);
        return personagem?.ParaPontos() ?? new PontosDeAtributos();
    }

    private void EnviarAoDono<T>(long personagemId, ushort id, T mensagem)
    {
        foreach (var sessao in _tempoReal.Sessoes)
        {
            if (sessao.PersonagemId == personagemId)
            {
                _tempoReal.Enviar(sessao, id, mensagem);
            }
        }
    }

    private void EnviarNaArea<T>(string area, ushort id, T mensagem)
    {
        var presentes = _posicoes.PersonagensNaArea(area);
        foreach (var sessao in _tempoReal.Sessoes)
        {
            if (presentes.Contains(sessao.PersonagemId))
            {
                _tempoReal.Enviar(sessao, id, mensagem);
            }
        }
    }
}
