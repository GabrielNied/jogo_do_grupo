using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// O laço de IA dos inimigos do campo: <see cref="RegrasInimigo.PassosPorSegundo"/> vezes por
/// segundo lê do Mundo onde estão os jogadores da área, avança o <see cref="GrupoDeInimigos"/>,
/// manda o que mudou a quem está na área e publica os eventos de Mobs
/// (<c>Contratos/Mobs/</c>). A decisão é toda do grupo; aqui só há rede e eventos.
/// </summary>
public sealed class ServicoDeInimigos(
    IReadOnlyList<IInimigosSimulados> grupos,
    ServidorTempoReal tempoReal,
    IPosicoesDoMundo posicoes,
    EventosDoServidor eventos,
    ILogger<ServicoDeInimigos> log,
    ChefesDaDungeon? chefes = null
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var relogio = new PeriodicTimer(TimeSpan.FromSeconds(RegrasInimigo.PassoSegundos));
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                try
                {
                    Avancar();
                }
                catch (Exception erro)
                {
                    log.LogError(erro, "Erro no passo da IA dos inimigos.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>Um passo do laço: avança a IA, envia e publica. Os testes chamam direto.</summary>
    public void Avancar()
    {
        foreach (var grupo in Simulados())
        {
            Avancar(grupo);
        }
    }

    // Os grupos fixos e os bosses de dungeon que existem agora (a lista deles muda).
    private IEnumerable<IInimigosSimulados> Simulados() =>
        chefes == null ? grupos : grupos.Concat(chefes.Ativos());

    private void Avancar(IInimigosSimulados grupo)
    {
        var jogadores = new List<JogadorNoCampo>();
        foreach (var personagemId in posicoes.PersonagensNaArea(grupo.Area))
        {
            if (posicoes.TentarObter(personagemId, out var posicao) && posicao.Area == grupo.Area)
            {
                jogadores.Add(new JogadorNoCampo(personagemId, posicao.X, posicao.Z));
            }
        }

        var passo = grupo.Passo(jogadores);
        if (passo.Vazio)
        {
            return;
        }

        var presentes = jogadores.Select(j => j.PersonagemId).ToHashSet();
        var sessoes = tempoReal.Sessoes.Where(s => presentes.Contains(s.PersonagemId)).ToArray();
        foreach (var sessao in sessoes)
        {
            foreach (var mudanca in passo.Mudancas)
            {
                tempoReal.Enviar(
                    sessao,
                    MobsMessageIds.InimigoMudou,
                    mudanca.Mensagem,
                    mudanca.SoPosicao ? EntregaTempoReal.Sequenciada : EntregaTempoReal.Confiavel
                );
            }

            foreach (var preparo in passo.Preparos)
            {
                tempoReal.Enviar(sessao, MobsMessageIds.InimigoPreparaAtaque, preparo);
            }

            foreach (var morte in passo.Mortes)
            {
                tempoReal.Enviar(sessao, MobsMessageIds.InimigoMorreu, morte);
            }

            foreach (var aparicao in passo.Aparicoes)
            {
                tempoReal.Enviar(sessao, MobsMessageIds.InimigoApareceu, aparicao);
            }

            foreach (var exposto in passo.Expostos)
            {
                tempoReal.Enviar(sessao, MobsMessageIds.InimigoExposto, exposto);
            }
        }

        foreach (var golpe in passo.Golpes)
        {
            Publicar(golpe);
        }

        foreach (var derrota in passo.Derrotas)
        {
            Publicar(derrota);
        }
    }

    /// <summary>Quem entra na área do grupo recebe os inimigos vivos (laço de rede).</summary>
    public void AoEntrarNaArea(JogadorEntrouNaArea evento)
    {
        var daArea = Simulados().Where(g => g.Area == evento.Area).ToArray();
        if (daArea.Length == 0)
        {
            return;
        }

        var sessao = tempoReal.Sessoes.FirstOrDefault(s => s.PersonagemId == evento.PersonagemId);
        if (sessao != null)
        {
            tempoReal.Enviar(
                sessao,
                MobsMessageIds.InimigosDaArea,
                new InimigosDaArea { Inimigos = daArea.SelectMany(g => g.Vivos()).ToArray() }
            );
        }
    }

    // A falha de um assinante não pode parar a IA nem os outros eventos do passo.
    private void Publicar<T>(T evento)
        where T : class
    {
        try
        {
            eventos.Publicar(evento);
        }
        catch (AggregateException erro)
        {
            log.LogError(erro, "Assinante de {Evento} falhou.", typeof(T).Name);
        }
    }
}
