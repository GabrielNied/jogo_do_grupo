using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O dano que o personagem recebe de fora de Combate. Hoje só o golpe de inimigo: Mobs decide
/// quem estava no círculo e publica <see cref="GolpeDeInimigoAcertou"/>; aqui, como dono da vida,
/// se decide quanto o golpe tira. A morte do jogador não é tratada aqui: a vida só para em 0.
/// <para>
/// O evento sai do laço de IA de Mobs e o manipulador não bloqueia: o banco roda numa tarefa,
/// e o aviso ao dono sai pelo <see cref="ServidorTempoReal"/> (seguro para qualquer thread).
/// </para>
/// </summary>
public sealed class ServicoDeDanoRecebido : IHostedService
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IServiceScopeFactory _escopos;
    private readonly TimeProvider _relogio;
    private readonly ILogger<ServicoDeDanoRecebido> _log;
    private readonly CancellationTokenSource _parada = new();

    public ServicoDeDanoRecebido(
        ServidorTempoReal tempoReal,
        IServiceScopeFactory escopos,
        TimeProvider relogio,
        ILogger<ServicoDeDanoRecebido> log
    )
    {
        _tempoReal = tempoReal;
        _escopos = escopos;
        _relogio = relogio;
        _log = log;
    }

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
        }
        catch (ObjectDisposedException) { }

        return Task.CompletedTask;
    }

    /// <summary>
    /// O que o golpe tira de vida. Sem defesa por enquanto: quando ela existir, entra aqui, sem
    /// mexer em quem chama.
    /// </summary>
    public static int DanoFinal(int danoDoGolpe) => Math.Max(0, danoDoGolpe);

    /// <summary>Trata <see cref="GolpeDeInimigoAcertou"/> sem bloquear quem publica.</summary>
    public void AoGolpeDeInimigo(GolpeDeInimigoAcertou golpe) => _ = AplicarAsync(golpe);

    private async Task AplicarAsync(GolpeDeInimigoAcertou golpe)
    {
        try
        {
            var dano = DanoFinal(golpe.Dano);
            if (dano <= 0)
            {
                return;
            }

            var recursos = await RecursosDoAtaque.TirarVidaAsync(
                _escopos,
                _relogio,
                golpe.PersonagemId,
                dano,
                _parada.Token
            );

            var sessao = _tempoReal.Sessoes.FirstOrDefault(s =>
                s.PersonagemId == golpe.PersonagemId
            );
            if (sessao != null)
            {
                _tempoReal.Enviar(sessao, CombateMessageIds.RecursosMudaram, recursos);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(
                e,
                "Erro ao aplicar o golpe do inimigo {Inimigo} no personagem {Personagem}.",
                golpe.InimigoId,
                golpe.PersonagemId
            );
        }
    }
}
