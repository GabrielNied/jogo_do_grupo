using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O dano que o personagem recebe de fora de Combate. Hoje só o golpe de inimigo: Mobs decide
/// quem estava no círculo e publica <see cref="GolpeDeInimigoAcertou"/>; aqui, como dono da vida,
/// se decide quanto o golpe tira. Vida 0 vira o estado caído em <see cref="ServicoDeQueda"/>; quem já está caído não leva dano.
/// <para>
/// O evento sai do laço de IA de Mobs e o manipulador não bloqueia: o banco roda numa tarefa,
/// e o aviso ao dono sai pelo <see cref="ServidorTempoReal"/> (seguro para qualquer thread).
/// </para>
/// </summary>
public sealed class ServicoDeDanoRecebido : IHostedService
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ServicoDeDanoRecebido> _log;
    private readonly ServicoDeQueda? _queda;
    private readonly ControleDeAcoes? _acoes;
    private readonly Func<long> _relogioMs;
    private readonly CancellationTokenSource _parada = new();

    public ServicoDeDanoRecebido(
        ServidorTempoReal tempoReal,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeDanoRecebido> log,
        ServicoDeQueda? queda = null,
        ControleDeAcoes? acoes = null,
        Func<long>? relogioMs = null
    )
    {
        _queda = queda;
        _acoes = acoes;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _tempoReal = tempoReal;
        _escopos = escopos;
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

            // Nos primeiros 0,3 s da esquiva o dano direto some (o servidor confere o instante).
            if (_acoes?.EstaInvulneravel(golpe.PersonagemId, _relogioMs()) == true)
            {
                return;
            }

            // Quem está caído não recebe dano.
            if (_queda?.EstaCaido(golpe.PersonagemId) == true)
            {
                return;
            }

            var recursos = await RecursosDoAtaque.TirarVidaAsync(
                _escopos,
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

            // Vida 0 leva a caído; vida acima de 0 interrompe quem este personagem levantava.
            _queda?.AoDano(golpe.PersonagemId, recursos.VidaAtual);
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
