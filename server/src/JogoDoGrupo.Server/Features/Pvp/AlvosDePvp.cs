using System;
using System.Collections.Generic;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Pvp;

/// <summary>
/// Provedor de alvos de combate para PvP (Duelo 1v1, Arena 3v3 e Bandeira no mundo aberto).
/// Implementa <see cref="IAlvosDeCombate"/> para que os ataques e skills de Combate atinjam
/// oponentes elegíveis de forma transparente e autoritativa.
/// </summary>
public sealed class AlvosDePvp : IAlvosDeCombate
{
    public const long BaseAlvoIdPvp = 10_000_000_000L;
    public const double RaioCorpoJogador = 0.5;
    public const double AlturaCorpoJogador = 1.8;

    private readonly ServicoDePvp _servico;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly Func<long> _relogioMs;

    public AlvosDePvp(ServicoDePvp servico, IPosicoesDoMundo posicoes, Func<long>? relogioMs = null)
    {
        _servico = servico;
        _posicoes = posicoes;
        _relogioMs = relogioMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public static long ParaAlvoId(long personagemId) => BaseAlvoIdPvp + personagemId;

    public static long ParaPersonagemId(long alvoId) => alvoId - BaseAlvoIdPvp;

    public static bool EhAlvoPvp(long alvoId) => alvoId >= BaseAlvoIdPvp;

    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus,
        long instanteReferenciaMs = 0
    )
    {
        var oponentes = _servico.ObterCombatentesVivosNaArea(area);
        if (oponentes.Count == 0)
        {
            return Array.Empty<AlvoDeCombate>();
        }

        var lista = new List<AlvoDeCombate>();
        foreach (var (personagemId, vidaAtual, vidaMaxima) in oponentes)
        {
            if (!_posicoes.TentarObter(personagemId, out var pos) || pos.Area != area)
            {
                continue;
            }

            if (
                RegrasAtaque.EstaNoVolume(
                    origemX,
                    origemZ,
                    direcaoYGraus,
                    pos.X,
                    pos.Z,
                    RaioCorpoJogador
                )
            )
            {
                lista.Add(
                    new AlvoDeCombate(
                        Id: ParaAlvoId(personagemId),
                        X: pos.X,
                        Z: pos.Z,
                        Raio: RaioCorpoJogador,
                        VidaAtual: vidaAtual,
                        VidaMaxima: vidaMaxima,
                        PersonagemId: personagemId,
                        Altura: AlturaCorpoJogador,
                        Y: pos.Y
                    )
                );
            }
        }

        return lista;
    }

    public IReadOnlyList<AlvoDeCombate> AlvosProximos(
        string area,
        double origemX,
        double origemZ,
        double alcanceMetros,
        long instanteReferenciaMs = 0
    )
    {
        var oponentes = _servico.ObterCombatentesVivosNaArea(area);
        if (oponentes.Count == 0)
        {
            return Array.Empty<AlvoDeCombate>();
        }

        var lista = new List<AlvoDeCombate>();
        var alcanceMaxComRaio = alcanceMetros + RaioCorpoJogador;
        var alcanceMaxQuadrado = alcanceMaxComRaio * alcanceMaxComRaio;

        foreach (var (personagemId, vidaAtual, vidaMaxima) in oponentes)
        {
            if (!_posicoes.TentarObter(personagemId, out var pos) || pos.Area != area)
            {
                continue;
            }

            var dx = pos.X - origemX;
            var dz = pos.Z - origemZ;
            if ((dx * dx) + (dz * dz) <= alcanceMaxQuadrado)
            {
                lista.Add(
                    new AlvoDeCombate(
                        Id: ParaAlvoId(personagemId),
                        X: pos.X,
                        Z: pos.Z,
                        Raio: RaioCorpoJogador,
                        VidaAtual: vidaAtual,
                        VidaMaxima: vidaMaxima,
                        PersonagemId: personagemId,
                        Altura: AlturaCorpoJogador,
                        Y: pos.Y
                    )
                );
            }
        }

        return lista;
    }

    public bool TentarAplicarDano(
        long alvoId,
        int dano,
        long atacanteId,
        out ResultadoDeDano resultado
    )
    {
        resultado = default;
        if (!EhAlvoPvp(alvoId))
        {
            return false;
        }

        var alvoPersonagemId = ParaPersonagemId(alvoId);
        return _servico.TentarAplicarDanoPvp(
            alvoPersonagemId,
            dano,
            atacanteId,
            _relogioMs(),
            out resultado
        );
    }
}
