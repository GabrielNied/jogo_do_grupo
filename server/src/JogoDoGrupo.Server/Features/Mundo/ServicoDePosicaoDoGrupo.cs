using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Posição do grupo para o mapa (#789). Desde a #500 a presença só chega a quem está perto, e
/// um aliado do outro lado da cidade sumia do mapa e do minimapa. Aqui, a cada
/// <see cref="IntervaloEmSegundos"/>, cada membro de grupo na cidade recebe a posição dos
/// aliados da cidade que ele não vê pela grade. Quem não é do grupo não recebe nada, e o
/// modelo 3D continua só perto. O grupo vem do contrato do Social (<see cref="IConsultaDeGrupo"/>).
/// </summary>
public sealed class ServicoDePosicaoDoGrupo(
    Func<IReadOnlyList<AvatarNaCidade<SessaoTempoReal>>> presentes,
    Func<long, HashSet<long>> vistos,
    Func<IConsultaDeGrupo?> grupos,
    ServidorTempoReal tempoReal,
    ILogger<ServicoDePosicaoDoGrupo> log
) : BackgroundService
{
    /// <summary>Intervalo entre os envios (s): um ponto no mapa não precisa de mais.</summary>
    public const double IntervaloEmSegundos = 1.0;

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var relogio = new PeriodicTimer(TimeSpan.FromSeconds(IntervaloEmSegundos));
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                try
                {
                    Sincronizar();
                }
                catch (Exception erro)
                {
                    log.LogError(erro, "Erro ao enviar a posição do grupo.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>Manda a cada membro de grupo na cidade os aliados que ele não vê. O laço e os testes chamam.</summary>
    public void Sincronizar()
    {
        if (grupos() is not { } consulta)
        {
            return;
        }

        var naCidade = presentes().ToDictionary(a => a.PersonagemId);
        foreach (var eu in naCidade.Values)
        {
            if (consulta.Consultar(eu.PersonagemId) is not { } grupo)
            {
                continue;
            }

            var perto = vistos(eu.PersonagemId);
            var longe = new List<AliadoLonge>();
            foreach (var membro in grupo.Membros)
            {
                if (
                    membro != eu.PersonagemId
                    && !perto.Contains(membro)
                    && naCidade.TryGetValue(membro, out var aliado)
                )
                {
                    longe.Add(
                        new AliadoLonge
                        {
                            PersonagemId = membro,
                            X = aliado.Posicao.X,
                            Z = aliado.Posicao.Z,
                        }
                    );
                }
            }

            tempoReal.Enviar(
                eu.Sessao,
                MundoMessageIds.PosicoesDosAliados,
                new PosicoesDosAliados { Aliados = [.. longe] }
            );
        }
    }
}
