using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Posição do grupo para o mapa (#789, #903): aliados longe na mesma área e aliados em outra área
/// ou instância. Quem não é do grupo não recebe.
/// </summary>
public sealed class ServicoDePosicaoDoGrupo(
    Func<IReadOnlyList<AvatarNaCidade<SessaoTempoReal>>> presentes,
    Func<long, HashSet<long>> vistos,
    Func<IConsultaDeGrupo?> grupos,
    IPosicoesDoMundo posicoes,
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

    /// <summary>Manda a cada membro de grupo online os aliados que ele não vê pela presença.</summary>
    public void Sincronizar()
    {
        if (grupos() is not { } consulta)
        {
            return;
        }

        var online = presentes().ToDictionary(a => a.PersonagemId);
        foreach (var eu in online.Values)
        {
            if (consulta.Consultar(eu.PersonagemId) is not { } grupo)
            {
                continue;
            }

            if (!posicoes.TentarObter(eu.PersonagemId, out var posicaoEu))
            {
                continue;
            }

            var perto = posicaoEu.Area == AreasDoMundo.Cidade ? vistos(eu.PersonagemId) : null;
            var longe = new List<AliadoLonge>();
            var outraArea = new List<AliadoEmOutraArea>();
            foreach (var membro in grupo.Membros)
            {
                if (membro == eu.PersonagemId)
                {
                    continue;
                }

                if (!posicoes.TentarObter(membro, out var posicaoAliado))
                {
                    continue;
                }

                if (posicaoAliado.Area != posicaoEu.Area)
                {
                    outraArea.Add(
                        new AliadoEmOutraArea
                        {
                            PersonagemId = membro,
                            Rotulo = RotulosDeArea.Descricao(
                                posicaoAliado.Area,
                                posicaoAliado.X,
                                posicaoAliado.Z
                            ),
                        }
                    );
                    continue;
                }

                if (posicaoEu.Area != AreasDoMundo.Cidade)
                {
                    continue;
                }

                if (perto != null && !perto.Contains(membro))
                {
                    longe.Add(
                        new AliadoLonge
                        {
                            PersonagemId = membro,
                            X = posicaoAliado.X,
                            Z = posicaoAliado.Z,
                        }
                    );
                }
            }

            tempoReal.Enviar(
                eu.Sessao,
                MundoMessageIds.PosicoesDosAliados,
                new PosicoesDosAliados { Aliados = [.. longe], EmOutraArea = [.. outraArea] }
            );
        }
    }
}
