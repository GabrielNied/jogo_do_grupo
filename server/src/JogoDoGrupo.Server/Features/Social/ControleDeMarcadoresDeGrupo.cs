using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Gerencia de forma autoritativa os marcadores táticos de alvo atribuídos pelos grupos de jogadores.
/// Cada grupo possui seu próprio conjunto de marcadores em memória sincronizado em tempo real.
/// </summary>
public sealed class ControleDeMarcadoresDeGrupo(
    IConsultaDeGrupo consultaGrupo,
    ISaidaDeMarcadores saida,
    ILogger<ControleDeMarcadoresDeGrupo> log
)
{
    private readonly ConcurrentDictionary<
        long,
        Dictionary<long, TipoMarcadorDeAlvo>
    > _marcadoresPorGrupo = new();
    private readonly Lock _trava = new();

    /// <summary>
    /// Processa a intenção de um jogador de atribuir ou remover um marcador tático em um alvo.
    /// </summary>
    public void DefinirMarcador(long solicitanteId, DefinirMarcadorDeAlvo pedido)
    {
        var grupo = consultaGrupo.Consultar(solicitanteId);
        if (grupo == null)
        {
            saida.Recusada(
                solicitanteId,
                new DefinirMarcadorRecusado { Motivo = CodigoErroMarcador.NaoEstaEmGrupo }
            );
            return;
        }

        if (grupo.LiderId != solicitanteId)
        {
            saida.Recusada(
                solicitanteId,
                new DefinirMarcadorRecusado { Motivo = CodigoErroMarcador.ApenasLiderPodeMarcar }
            );
            return;
        }

        if (pedido.AlvoId <= 0)
        {
            saida.Recusada(
                solicitanteId,
                new DefinirMarcadorRecusado { Motivo = CodigoErroMarcador.AlvoInvalido }
            );
            return;
        }

        MarcadorAlvoDto[] snapshot;
        lock (_trava)
        {
            var marcadores = _marcadoresPorGrupo.GetOrAdd(
                grupo.GrupoId,
                _ => new Dictionary<long, TipoMarcadorDeAlvo>()
            );

            if (pedido.Marcador != TipoMarcadorDeAlvo.Nenhum)
            {
                // Um marcador exclusivo (ex: Caveira) só pode estar em um alvo por vez no grupo
                long? alvoComMesmoMarcador = null;
                foreach (var par in marcadores)
                {
                    if (par.Value == pedido.Marcador && par.Key != pedido.AlvoId)
                    {
                        alvoComMesmoMarcador = par.Key;
                        break;
                    }
                }

                if (alvoComMesmoMarcador.HasValue)
                {
                    marcadores.Remove(alvoComMesmoMarcador.Value);
                }

                marcadores[pedido.AlvoId] = pedido.Marcador;
            }
            else
            {
                marcadores.Remove(pedido.AlvoId);
            }

            snapshot = marcadores.Select(p => new MarcadorAlvoDto(p.Key, p.Value)).ToArray();
        }

        log.LogDebug(
            "Marcador atualizado no grupo {GrupoId} por {SolicitanteId}",
            grupo.GrupoId,
            solicitanteId
        );
        var mensagem = new MarcadoresDeAlvoMudaram { Marcadores = snapshot };
        foreach (var membroId in grupo.Membros)
        {
            saida.Marcadores(membroId, mensagem);
        }
    }

    /// <summary>
    /// Remove um alvo de todos os grupos quando o alvo morre ou é destruído.
    /// </summary>
    public void LimparAlvo(long alvoId)
    {
        if (alvoId <= 0)
        {
            return;
        }

        List<(long GrupoId, MarcadorAlvoDto[] Snapshot)> gruposAfetados = new();

        lock (_trava)
        {
            foreach (var par in _marcadoresPorGrupo)
            {
                if (par.Value.Remove(alvoId))
                {
                    var snapshot = par
                        .Value.Select(p => new MarcadorAlvoDto(p.Key, p.Value))
                        .ToArray();
                    gruposAfetados.Add((par.Key, snapshot));
                }
            }
        }

        foreach (var (grupoId, snapshot) in gruposAfetados)
        {
            // Pega um membro arbitrário para achar o grupo atual
            // (a consulta é por personagem)
            var grupo = _marcadoresPorGrupo.ContainsKey(grupoId) ? ObterGrupoPorId(grupoId) : null;
            if (grupo != null)
            {
                var mensagem = new MarcadoresDeAlvoMudaram { Marcadores = snapshot };
                foreach (var membroId in grupo.Membros)
                {
                    saida.Marcadores(membroId, mensagem);
                }
            }
        }
    }

    /// <summary>
    /// Limpa os marcadores de um grupo que foi desfeito.
    /// </summary>
    public void AoDesfazerGrupo(long grupoId)
    {
        lock (_trava)
        {
            _marcadoresPorGrupo.TryRemove(grupoId, out _);
        }
    }

    /// <summary>
    /// Envia o estado atual de marcadores para um jogador recém-conectado ao grupo.
    /// </summary>
    public void EnviarMarcadoresAtuais(long personagemId, long grupoId)
    {
        MarcadorAlvoDto[] snapshot;
        lock (_trava)
        {
            if (
                !_marcadoresPorGrupo.TryGetValue(grupoId, out var marcadores)
                || marcadores.Count == 0
            )
            {
                return;
            }

            snapshot = marcadores.Select(p => new MarcadorAlvoDto(p.Key, p.Value)).ToArray();
        }

        saida.Marcadores(personagemId, new MarcadoresDeAlvoMudaram { Marcadores = snapshot });
    }

    /// <summary>
    /// Retorna os marcadores atuais de um grupo (para inspeção e testes).
    /// </summary>
    public MarcadorAlvoDto[] ObterMarcadores(long grupoId)
    {
        lock (_trava)
        {
            if (!_marcadoresPorGrupo.TryGetValue(grupoId, out var marcadores))
            {
                return Array.Empty<MarcadorAlvoDto>();
            }

            return marcadores.Select(p => new MarcadorAlvoDto(p.Key, p.Value)).ToArray();
        }
    }

    private GrupoAtual? ObterGrupoPorId(long grupoId)
    {
        // Se a consulta expuser busca por id ou membro
        // podemos consultar pelos membros do grupo
        return null;
    }
}
