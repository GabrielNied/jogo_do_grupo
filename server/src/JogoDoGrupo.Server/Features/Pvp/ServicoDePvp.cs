using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Pvp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Pvp;

public sealed class ServicoDePvp : IHostedService
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly ITransicoesDeAreaDoMundo _transicoes;
    private readonly IDiretorioDePersonagens? _diretorio;
    private readonly ILogger<ServicoDePvp>? _logger;
    private readonly Func<long> _relogioMs;

    private readonly object _trava = new();

    // Duelo
    private readonly Dictionary<long, (long DesafiadoId, long CriadoEmMs)> _convitesPendentes = [];
    private readonly Dictionary<long, DueloAtivo> _duelosPorPersonagem = [];
    private readonly HashSet<DueloAtivo> _duelosAtivos = [];

    // Arena 3v3
    private readonly List<long> _filaArena = [];
    private readonly Dictionary<long, PartidaArena> _partidasPorPersonagem = [];
    private readonly Dictionary<string, PartidaArena> _partidasAtivas = [];
    private readonly ConcurrentDictionary<long, DadosRankingArena> _rankings = new();

    // Bandeira PvP no Mundo Aberto
    private readonly ConcurrentDictionary<long, bool> _bandeirasAtivas = new();

    public ServicoDePvp(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        ITransicoesDeAreaDoMundo transicoes,
        IDiretorioDePersonagens? diretorio = null,
        ILogger<ServicoDePvp>? logger = null,
        Func<long>? relogioMs = null
    )
    {
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _transicoes = transicoes;
        _diretorio = diretorio;
        _logger = logger;
        _relogioMs = relogioMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _tempoReal.AoPasso(TimeSpan.FromSeconds(1), _ => Varrer(), "PvP");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public int DuelosAtivosCount
    {
        get
        {
            lock (_trava)
            {
                return _duelosAtivos.Count;
            }
        }
    }

    public int JogadoresNaFilaArenaCount
    {
        get
        {
            lock (_trava)
            {
                return _filaArena.Count;
            }
        }
    }

    public int PartidasArenaAtivasCount
    {
        get
        {
            lock (_trava)
            {
                return _partidasAtivas.Count;
            }
        }
    }

    // ── Duelo 1v1 ─────────────────────────────────────────────────────────────

    public void SolicitarDuelo(SessaoTempoReal desafiante, SolicitarDuelo msg)
    {
        var desafianteId = desafiante.PersonagemId;
        var alvoId = msg.AlvoPersonagemId;

        if (desafianteId == alvoId || alvoId <= 0)
        {
            RecusarDuelo(desafiante, alvoId, CodigosErroPvp.AlvoInvalido);
            return;
        }

        if (
            !_posicoes.TentarObter(desafianteId, out var posDesafiante)
            || !_posicoes.TentarObter(alvoId, out var posAlvo)
        )
        {
            RecusarDuelo(desafiante, alvoId, CodigosErroPvp.AlvoInvalido);
            return;
        }

        if (posDesafiante.Area != posAlvo.Area)
        {
            RecusarDuelo(desafiante, alvoId, CodigosErroPvp.LongeDemais);
            return;
        }

        if (
            !RegrasPvp.DistanciaValidaParaConvite(
                posDesafiante.X,
                posDesafiante.Z,
                posAlvo.X,
                posAlvo.Z
            )
        )
        {
            RecusarDuelo(desafiante, alvoId, CodigosErroPvp.LongeDemais);
            return;
        }

        lock (_trava)
        {
            if (_duelosPorPersonagem.ContainsKey(desafianteId))
            {
                RecusarDuelo(desafiante, alvoId, CodigosErroPvp.VoceJaEmDuelo);
                return;
            }

            if (_duelosPorPersonagem.ContainsKey(alvoId))
            {
                RecusarDuelo(desafiante, alvoId, CodigosErroPvp.AlvoJaEmDuelo);
                return;
            }

            _convitesPendentes[desafianteId] = (alvoId, _relogioMs());
        }

        if (_tempoReal.TentarObterSessao(alvoId, out var sessaoAlvo) && sessaoAlvo != null)
        {
            var nome = "Desafiante";
            if (
                _diretorio?.TentarObterNome(desafianteId, out var n) == true
                && !string.IsNullOrEmpty(n)
            )
            {
                nome = n;
            }

            _tempoReal.Enviar(
                sessaoAlvo,
                PvpMessageIds.ConviteDeDueloRecebido,
                new ConviteDeDueloRecebido
                {
                    DesafianteId = desafianteId,
                    NomeDesafiante = nome,
                    SegundosParaExpirar = RegrasPvp.TempoExpiracaoConviteDueloSegundos,
                }
            );
        }
    }

    public void ResponderConviteDuelo(SessaoTempoReal desafiado, ResponderConviteDeDuelo msg)
    {
        var desafiadoId = desafiado.PersonagemId;
        var desafianteId = msg.DesafianteId;

        lock (_trava)
        {
            if (
                !_convitesPendentes.TryGetValue(desafianteId, out var pendente)
                || pendente.DesafiadoId != desafiadoId
            )
            {
                return;
            }

            _convitesPendentes.Remove(desafianteId);

            if (!msg.Aceito)
            {
                if (
                    _tempoReal.TentarObterSessao(desafianteId, out var sessaoDesafiante)
                    && sessaoDesafiante != null
                )
                {
                    RecusarDuelo(sessaoDesafiante, desafiadoId, CodigosErroPvp.RecusadoPeloAlvo);
                }
                return;
            }

            if (
                _duelosPorPersonagem.ContainsKey(desafianteId)
                || _duelosPorPersonagem.ContainsKey(desafiadoId)
            )
            {
                return;
            }

            if (
                !_posicoes.TentarObter(desafianteId, out var posDesafiante)
                || !_posicoes.TentarObter(desafiadoId, out var posDesafiado)
                || posDesafiante.Area != posDesafiado.Area
            )
            {
                return;
            }

            var centroX = (posDesafiante.X + posDesafiado.X) / 2.0;
            var centroZ = (posDesafiante.Z + posDesafiado.Z) / 2.0;
            var agora = _relogioMs();

            var duelo = new DueloAtivo
            {
                DesafianteId = desafianteId,
                DesafiadoId = desafiadoId,
                Area = posDesafiante.Area,
                CentroX = centroX,
                CentroZ = centroZ,
                InicioMs = agora,
                LiberadoEmMs = agora + (RegrasPvp.TempoContagemRegressivaDueloSegundos * 1000),
                VidaDesafiante = 1000,
                VidaMaxDesafiante = 1000,
                VidaDesafiado = 1000,
                VidaMaxDesafiado = 1000,
            };

            _duelosPorPersonagem[desafianteId] = duelo;
            _duelosPorPersonagem[desafiadoId] = duelo;
            _duelosAtivos.Add(duelo);

            var inicio = new DueloIniciado
            {
                DesafianteId = desafianteId,
                DesafiadoId = desafiadoId,
                CentroX = centroX,
                CentroZ = centroZ,
                ContagemRegressivaSegundos = RegrasPvp.TempoContagemRegressivaDueloSegundos,
            };

            _tempoReal.Enviar(desafiado, PvpMessageIds.DueloIniciado, inicio);
            if (
                _tempoReal.TentarObterSessao(desafianteId, out var sessaoDesafianteInicio)
                && sessaoDesafianteInicio != null
            )
            {
                _tempoReal.Enviar(sessaoDesafianteInicio, PvpMessageIds.DueloIniciado, inicio);
            }
        }
    }

    public void CancelarDuelo(SessaoTempoReal desafiante)
    {
        lock (_trava)
        {
            _convitesPendentes.Remove(desafiante.PersonagemId);
        }
    }

    public void FinalizarDuelo(
        DueloAtivo duelo,
        long vencedorId,
        long perdedorId,
        MotivoDueloEncerramento motivo
    )
    {
        lock (_trava)
        {
            if (!_duelosAtivos.Remove(duelo))
            {
                return;
            }

            _duelosPorPersonagem.Remove(duelo.DesafianteId);
            _duelosPorPersonagem.Remove(duelo.DesafiadoId);
        }

        var aviso = new DueloEncerrado
        {
            VencedorId = vencedorId,
            PerdedorId = perdedorId,
            Motivo = motivo,
        };

        if (_tempoReal.TentarObterSessao(duelo.DesafianteId, out var s1) && s1 != null)
        {
            _tempoReal.Enviar(s1, PvpMessageIds.DueloEncerrado, aviso);
        }

        if (_tempoReal.TentarObterSessao(duelo.DesafiadoId, out var s2) && s2 != null)
        {
            _tempoReal.Enviar(s2, PvpMessageIds.DueloEncerrado, aviso);
        }
    }

    private void RecusarDuelo(SessaoTempoReal sessao, long alvoId, string motivo)
    {
        _tempoReal.Enviar(
            sessao,
            PvpMessageIds.DueloRecusado,
            new DueloRecusado { AlvoPersonagemId = alvoId, Motivo = motivo }
        );
    }

    // ── Arena 3v3 ──────────────────────────────────────────────────────────────

    public void EntrarNaFilaArena(SessaoTempoReal sessao)
    {
        var id = sessao.PersonagemId;
        List<long>? jogadoresProntos = null;

        lock (_trava)
        {
            if (_partidasPorPersonagem.ContainsKey(id))
            {
                return;
            }

            if (!_filaArena.Contains(id))
            {
                _filaArena.Add(id);
            }

            _tempoReal.Enviar(
                sessao,
                PvpMessageIds.StatusFilaDeArena,
                new StatusFilaDeArena { NaFila = true, SegundosNaFila = 0 }
            );

            if (_filaArena.Count >= RegrasPvp.TotalJogadoresArena)
            {
                jogadoresProntos = _filaArena.Take(RegrasPvp.TotalJogadoresArena).ToList();
                _filaArena.RemoveRange(0, RegrasPvp.TotalJogadoresArena);
            }
        }

        if (jogadoresProntos != null)
        {
            CriarPartidaArena(jogadoresProntos);
        }
    }

    public void SairDaFilaArena(SessaoTempoReal sessao)
    {
        lock (_trava)
        {
            _filaArena.Remove(sessao.PersonagemId);
        }

        _tempoReal.Enviar(
            sessao,
            PvpMessageIds.StatusFilaDeArena,
            new StatusFilaDeArena { NaFila = false, SegundosNaFila = 0 }
        );
    }

    private void CriarPartidaArena(List<long> jogadores)
    {
        var timeAzul = jogadores.Take(RegrasPvp.TamanhoEquipeArena).ToArray();
        var timeVermelho = jogadores.Skip(RegrasPvp.TamanhoEquipeArena).ToArray();

        var partidaId = Guid.NewGuid().ToString("N")[..8];
        var areaArena = AreasDoMundo.Instancia("arena-" + partidaId);
        var agora = _relogioMs();

        var partida = new PartidaArena
        {
            Id = partidaId,
            Area = areaArena,
            TimeAzul = timeAzul,
            TimeVermelho = timeVermelho,
            CriadaEmMs = agora,
            LiberadaEmMs = agora + (RegrasPvp.TempoPreparoArenaSegundos * 1000),
        };

        lock (_trava)
        {
            _partidasAtivas[partidaId] = partida;
            foreach (var p in jogadores)
            {
                _partidasPorPersonagem[p] = partida;
                partida.VidaPorPersonagem[p] = 1000;
                partida.VidaMaxPorPersonagem[p] = 1000;

                if (_posicoes.TentarObter(p, out var posAtual))
                {
                    partida.PosicoesDeRetorno[p] = posAtual;
                }
                else
                {
                    partida.PosicoesDeRetorno[p] = new PosicaoNoMundo(
                        AreasDoMundo.Cidade,
                        0,
                        0,
                        0,
                        0
                    );
                }
            }
        }

        var aviso = new PartidaDeArenaIniciada
        {
            IdPartida = partidaId,
            Area = areaArena,
            TimeAzul = timeAzul,
            TimeVermelho = timeVermelho,
            TempoPreparoSegundos = RegrasPvp.TempoPreparoArenaSegundos,
        };

        // Teleporta combatentes para a arena
        for (var i = 0; i < timeAzul.Length; i++)
        {
            var p = timeAzul[i];
            _transicoes.TentarMoverParaArea(
                p,
                new PosicaoNoMundo(areaArena, -15f + (i * 2f), 0f, 0f, 90f)
            );
            if (_tempoReal.TentarObterSessao(p, out var s) && s != null)
            {
                _tempoReal.Enviar(s, PvpMessageIds.PartidaDeArenaIniciada, aviso);
            }
        }

        for (var i = 0; i < timeVermelho.Length; i++)
        {
            var p = timeVermelho[i];
            _transicoes.TentarMoverParaArea(
                p,
                new PosicaoNoMundo(areaArena, 15f - (i * 2f), 0f, 0f, 270f)
            );
            if (_tempoReal.TentarObterSessao(p, out var s) && s != null)
            {
                _tempoReal.Enviar(s, PvpMessageIds.PartidaDeArenaIniciada, aviso);
            }
        }
    }

    public void EliminarCombatenteArena(PartidaArena partida, long combatenteId)
    {
        int equipeVencedora = -1;

        lock (_trava)
        {
            if (partida.Encerrada || !partida.Eliminados.Add(combatenteId))
            {
                return;
            }

            var equipe = partida.EquipeDe(combatenteId);
            var avisoEliminado = new CombatenteDeArenaEliminado
            {
                IdPartida = partida.Id,
                PersonagemId = combatenteId,
                Equipe = equipe,
            };

            foreach (var p in partida.TimeAzul.Concat(partida.TimeVermelho))
            {
                if (_tempoReal.TentarObterSessao(p, out var s) && s != null)
                {
                    _tempoReal.Enviar(s, PvpMessageIds.CombatenteDeArenaEliminado, avisoEliminado);
                }
            }

            var azulDerrotado = partida.TimeAzul.All(p => partida.Eliminados.Contains(p));
            var vermelhoDerrotado = partida.TimeVermelho.All(p => partida.Eliminados.Contains(p));

            if (azulDerrotado && vermelhoDerrotado)
            {
                equipeVencedora = -1; // Empate
            }
            else if (azulDerrotado)
            {
                equipeVencedora = 1; // Vermelho
            }
            else if (vermelhoDerrotado)
            {
                equipeVencedora = 0; // Azul
            }
        }

        if (equipeVencedora >= 0)
        {
            FinalizarPartidaArena(partida, equipeVencedora);
        }
    }

    public void FinalizarPartidaArena(PartidaArena partida, int equipeVencedora)
    {
        lock (_trava)
        {
            if (partida.Encerrada)
            {
                return;
            }

            partida.Encerrada = true;
            _partidasAtivas.Remove(partida.Id);

            foreach (var p in partida.TimeAzul.Concat(partida.TimeVermelho))
            {
                _partidasPorPersonagem.Remove(p);
            }
        }

        // Atualiza pontuação e notifica participantes
        foreach (var p in partida.TimeAzul.Concat(partida.TimeVermelho))
        {
            var equipe = partida.EquipeDe(p);
            var venceu = equipe == equipeVencedora;
            var ranking = _rankings.GetOrAdd(p, _ => new DadosRankingArena());

            int variacao;
            if (venceu)
            {
                variacao = RegrasPvp.PontosVitoriaArena;
                ranking.Pontos += variacao;
                ranking.Vitorias++;
            }
            else
            {
                var pontosAntes = ranking.Pontos;
                ranking.Pontos = RegrasPvp.CalcularPontosAposPartida(
                    ranking.Pontos,
                    vitoria: false
                );
                variacao = ranking.Pontos - pontosAntes;
                ranking.Derrotas++;
            }

            if (_tempoReal.TentarObterSessao(p, out var s) && s != null)
            {
                _tempoReal.Enviar(
                    s,
                    PvpMessageIds.PartidaDeArenaEncerrada,
                    new PartidaDeArenaEncerrada
                    {
                        IdPartida = partida.Id,
                        EquipeVencedora = equipeVencedora,
                        PontosVariacao = variacao,
                        NovoRanking = ranking.Pontos,
                    }
                );
            }

            // Retorna o jogador para a cidade
            if (partida.PosicoesDeRetorno.TryGetValue(p, out var retorno))
            {
                _transicoes.TentarMoverParaArea(p, retorno);
            }
        }
    }

    public void ConsultarRankingArena(SessaoTempoReal sessao)
    {
        var ranking = _rankings.GetOrAdd(sessao.PersonagemId, _ => new DadosRankingArena());
        _tempoReal.Enviar(
            sessao,
            PvpMessageIds.RespostaRankingArena,
            new RespostaRankingArena
            {
                Pontos = ranking.Pontos,
                Vitorias = ranking.Vitorias,
                Derrotas = ranking.Derrotas,
                Temporada = ranking.Temporada,
            }
        );
    }

    // ── Bandeira PvP no Mundo Aberto ───────────────────────────────────────────

    public void AlternarBandeiraPvp(SessaoTempoReal sessao, bool ativar)
    {
        _bandeirasAtivas[sessao.PersonagemId] = ativar;
        var aviso = new BandeiraPvpAlterada { PersonagemId = sessao.PersonagemId, Ativa = ativar };

        _tempoReal.Enviar(sessao, PvpMessageIds.BandeiraPvpAlterada, aviso);
    }

    public bool TemBandeiraAtiva(long personagemId) =>
        _bandeirasAtivas.TryGetValue(personagemId, out var ativa) && ativa;

    // ── Integração de Combate e Alvos ──────────────────────────────────────────

    public IReadOnlyList<(
        long PersonagemId,
        int VidaAtual,
        int VidaMaxima
    )> ObterCombatentesVivosNaArea(string area)
    {
        var lista = new List<(long PersonagemId, int VidaAtual, int VidaMaxima)>();
        var agora = _relogioMs();

        lock (_trava)
        {
            // Duelos ativos
            foreach (var duelo in _duelosAtivos)
            {
                if (duelo.Area == area && duelo.EstaLiberado(agora))
                {
                    if (duelo.VidaDesafiante > 0)
                    {
                        lista.Add(
                            (duelo.DesafianteId, duelo.VidaDesafiante, duelo.VidaMaxDesafiante)
                        );
                    }
                    if (duelo.VidaDesafiado > 0)
                    {
                        lista.Add((duelo.DesafiadoId, duelo.VidaDesafiado, duelo.VidaMaxDesafiado));
                    }
                }
            }

            // Arenas ativas
            foreach (var partida in _partidasAtivas.Values)
            {
                if (partida.Area == area && partida.EstaLiberada(agora) && !partida.Encerrada)
                {
                    foreach (var p in partida.TimeAzul.Concat(partida.TimeVermelho))
                    {
                        if (!partida.Eliminados.Contains(p))
                        {
                            var vida = partida.VidaPorPersonagem.GetValueOrDefault(p, 1000);
                            var max = partida.VidaMaxPorPersonagem.GetValueOrDefault(p, 1000);
                            lista.Add((p, vida, max));
                        }
                    }
                }
            }
        }

        // Bandeira PvP no mundo aberto (fora de cidade segura)
        if (!AreasDoMundo.EhCidade(area))
        {
            foreach (var kvp in _bandeirasAtivas)
            {
                if (kvp.Value && _posicoes.TentarObter(kvp.Key, out var pos) && pos.Area == area)
                {
                    lista.Add((kvp.Key, 1000, 1000));
                }
            }
        }

        return lista;
    }

    public bool TentarAplicarDanoPvp(
        long alvoPersonagemId,
        int dano,
        long atacanteId,
        long agoraMs,
        out ResultadoDeDano resultado
    )
    {
        resultado = default;
        DueloAtivo? dueloFinalizar = null;
        long vencedorDuelo = 0;
        long perdedorDuelo = 0;

        PartidaArena? arenaEliminar = null;

        lock (_trava)
        {
            // 1. Verifica Duelo
            if (
                _duelosPorPersonagem.TryGetValue(alvoPersonagemId, out var duelo)
                && duelo.Participa(atacanteId)
                && duelo.EstaLiberado(agoraMs)
            )
            {
                int vidaRestante;
                int vidaMax;

                if (alvoPersonagemId == duelo.DesafianteId)
                {
                    duelo.VidaDesafiante = Math.Max(0, duelo.VidaDesafiante - dano);
                    vidaRestante = duelo.VidaDesafiante;
                    vidaMax = duelo.VidaMaxDesafiante;
                }
                else
                {
                    duelo.VidaDesafiado = Math.Max(0, duelo.VidaDesafiado - dano);
                    vidaRestante = duelo.VidaDesafiado;
                    vidaMax = duelo.VidaMaxDesafiado;
                }

                resultado = new ResultadoDeDano(
                    AlvosDePvp.ParaAlvoId(alvoPersonagemId),
                    dano,
                    vidaRestante,
                    vidaMax
                );

                if (vidaRestante <= 0)
                {
                    dueloFinalizar = duelo;
                    vencedorDuelo = atacanteId;
                    perdedorDuelo = alvoPersonagemId;
                }

                if (dueloFinalizar != null)
                {
                    Task.Run(() =>
                        FinalizarDuelo(
                            dueloFinalizar,
                            vencedorDuelo,
                            perdedorDuelo,
                            MotivoDueloEncerramento.VitoriaPorDano
                        )
                    );
                }

                return true;
            }

            // 2. Verifica Arena 3v3
            if (
                _partidasPorPersonagem.TryGetValue(alvoPersonagemId, out var partida)
                && partida.EstaLiberada(agoraMs)
                && !partida.Encerrada
                && partida.SaoOponentes(alvoPersonagemId, atacanteId)
                && !partida.Eliminados.Contains(alvoPersonagemId)
            )
            {
                var vidaAtual = partida.VidaPorPersonagem.GetValueOrDefault(alvoPersonagemId, 1000);
                var vidaMax = partida.VidaMaxPorPersonagem.GetValueOrDefault(
                    alvoPersonagemId,
                    1000
                );
                var novaVida = Math.Max(0, vidaAtual - dano);
                partida.VidaPorPersonagem[alvoPersonagemId] = novaVida;

                resultado = new ResultadoDeDano(
                    AlvosDePvp.ParaAlvoId(alvoPersonagemId),
                    dano,
                    novaVida,
                    vidaMax
                );

                if (novaVida <= 0)
                {
                    arenaEliminar = partida;
                }

                if (arenaEliminar != null)
                {
                    Task.Run(() => EliminarCombatenteArena(arenaEliminar, alvoPersonagemId));
                }

                return true;
            }

            // 3. Verifica Bandeira PvP
            if (
                TemBandeiraAtiva(alvoPersonagemId)
                && TemBandeiraAtiva(atacanteId)
                && _posicoes.TentarObter(alvoPersonagemId, out var posAlvo)
                && _posicoes.TentarObter(atacanteId, out var posAtacante)
                && posAlvo.Area == posAtacante.Area
                && !AreasDoMundo.EhCidade(posAlvo.Area)
            )
            {
                resultado = new ResultadoDeDano(
                    AlvosDePvp.ParaAlvoId(alvoPersonagemId),
                    dano,
                    Math.Max(0, 1000 - dano),
                    1000
                );
                return true;
            }
        }

        return false;
    }

    // ── Varredura Periódica ────────────────────────────────────────────────────

    public void Varrer()
    {
        var agora = _relogioMs();
        List<DueloAtivo>? duelosParaEncerrar = null;
        List<(
            DueloAtivo Duelo,
            long Vencedor,
            long Perdedor,
            MotivoDueloEncerramento Motivo
        )>? encerramentos = null;

        lock (_trava)
        {
            // 1. Limpa convites de duelo expirados
            var expirados = _convitesPendentes
                .Where(kvp =>
                    agora - kvp.Value.CriadoEmMs
                    >= RegrasPvp.TempoExpiracaoConviteDueloSegundos * 1000
                )
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var desafianteId in expirados)
            {
                if (_convitesPendentes.Remove(desafianteId, out var pendente))
                {
                    if (_tempoReal.TentarObterSessao(desafianteId, out var s) && s != null)
                    {
                        RecusarDuelo(s, pendente.DesafiadoId, CodigosErroPvp.ConviteExpirado);
                    }
                }
            }

            // 2. Confere limites e conexões de duelos
            foreach (var duelo in _duelosAtivos)
            {
                var desafianteOnline = _tempoReal.TentarObterSessao(
                    duelo.DesafianteId,
                    out var sDesafiante
                );
                var desafiadoOnline = _tempoReal.TentarObterSessao(
                    duelo.DesafiadoId,
                    out var sDesafiado
                );

                if (!desafianteOnline && !desafiadoOnline)
                {
                    (duelosParaEncerrar ??= []).Add(duelo);
                    continue;
                }

                if (!desafianteOnline)
                {
                    (encerramentos ??= []).Add(
                        (
                            duelo,
                            duelo.DesafiadoId,
                            duelo.DesafianteId,
                            MotivoDueloEncerramento.Desconexao
                        )
                    );
                    continue;
                }

                if (!desafiadoOnline)
                {
                    (encerramentos ??= []).Add(
                        (
                            duelo,
                            duelo.DesafianteId,
                            duelo.DesafiadoId,
                            MotivoDueloEncerramento.Desconexao
                        )
                    );
                    continue;
                }

                var temPos1 = _posicoes.TentarObter(duelo.DesafianteId, out var p1);
                var temPos2 = _posicoes.TentarObter(duelo.DesafiadoId, out var p2);

                if (!temPos1 || p1.Area != duelo.Area)
                {
                    (encerramentos ??= []).Add(
                        (
                            duelo,
                            duelo.DesafiadoId,
                            duelo.DesafianteId,
                            MotivoDueloEncerramento.FugiuDoRaio
                        )
                    );
                    continue;
                }

                if (!temPos2 || p2.Area != duelo.Area)
                {
                    (encerramentos ??= []).Add(
                        (
                            duelo,
                            duelo.DesafianteId,
                            duelo.DesafiadoId,
                            MotivoDueloEncerramento.FugiuDoRaio
                        )
                    );
                    continue;
                }

                if (!RegrasPvp.EstaNoRaioDoDuelo(duelo.CentroX, duelo.CentroZ, p1.X, p1.Z))
                {
                    (encerramentos ??= []).Add(
                        (
                            duelo,
                            duelo.DesafiadoId,
                            duelo.DesafianteId,
                            MotivoDueloEncerramento.FugiuDoRaio
                        )
                    );
                    continue;
                }

                if (!RegrasPvp.EstaNoRaioDoDuelo(duelo.CentroX, duelo.CentroZ, p2.X, p2.Z))
                {
                    (encerramentos ??= []).Add(
                        (
                            duelo,
                            duelo.DesafianteId,
                            duelo.DesafiadoId,
                            MotivoDueloEncerramento.FugiuDoRaio
                        )
                    );
                    continue;
                }
            }
        }

        if (duelosParaEncerrar != null)
        {
            foreach (var d in duelosParaEncerrar)
            {
                FinalizarDuelo(d, 0, 0, MotivoDueloEncerramento.Cancelado);
            }
        }

        if (encerramentos != null)
        {
            foreach (var (duelo, vencedor, perdedor, motivo) in encerramentos)
            {
                FinalizarDuelo(duelo, vencedor, perdedor, motivo);
            }
        }
    }
}
