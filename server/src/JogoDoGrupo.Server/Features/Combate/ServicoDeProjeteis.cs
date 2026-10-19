using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Estado de um projétil ativo simulado no servidor autoritativo.
/// </summary>
public sealed class InstanciaDeProjetil
{
    public long Id { get; init; }
    public long AtacanteId { get; init; }
    public string Area { get; init; } = "";
    public TipoDeProjetil TipoProjetil { get; init; }
    public TipoDeAtaque TipoAtaque { get; init; }
    public float OrigemX { get; init; }
    public float OrigemY { get; init; }
    public float OrigemZ { get; init; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float DirX { get; init; }
    public float DirY { get; init; }
    public float DirZ { get; init; }
    public float Velocidade { get; init; }
    public float Raio { get; init; }
    public float AlcanceMaximo { get; init; }
    public float DistanciaPercorrida { get; set; }
    public int Dano { get; init; }
    public int Sequencia { get; init; }
    public int Passo { get; init; }
    public long CriadoEmMs { get; init; }
    public long InstanteVistoMs { get; init; }
    public HashSet<long> AlvosAtingidos { get; } = [];
}

/// <summary>
/// Serviço autoritativo de projéteis: gerencia o ciclo de vida, deslocamento físico e detecção
/// de colisão contra alvos no mundo com compensação de lag.
/// </summary>
public sealed class ServicoDeProjeteis : IHostedService, IDisposable, IDisparoDeProjeteis
{
    private readonly ServidorTempoReal _tempoReal;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly IReadOnlyList<IAlvosDeCombate> _fontes;
    private readonly IAliadosDeCombate _aliados;
    private readonly ILogger<ServicoDeProjeteis> _log;
    private readonly Func<long> _relogioMs;
    private readonly AvisosNaArea _avisos;
    private readonly GerenciadorDeEfeitosDeStatus? _efeitos;
    private readonly EventosDoServidor? _eventos;
    private readonly ControleDeAcoes? _controle;
    private readonly ServicoDeQueda? _queda;
    private readonly IServiceScopeFactory? _escopos;

    private readonly List<InstanciaDeProjetil> _projeteis = [];
    private readonly Lock _travaProjeteis = new();
    private readonly Dictionary<long, long> _folgaDosAlvos = [];
    private readonly Lock _travaReacoes = new();
    private readonly CancellationTokenSource _parada = new();

    private long _proximoId;
    private Task? _execucao;

    public ServicoDeProjeteis(
        ServidorTempoReal tempoReal,
        IPosicoesDoMundo posicoes,
        IEnumerable<IAlvosDeCombate> fontes,
        ILogger<ServicoDeProjeteis> log,
        IAliadosDeCombate? aliados = null,
        IVizinhanca? vizinhanca = null,
        GerenciadorDeEfeitosDeStatus? efeitos = null,
        EventosDoServidor? eventos = null,
        ControleDeAcoes? controle = null,
        ServicoDeQueda? queda = null,
        IServiceScopeFactory? escopos = null,
        Func<long>? relogioMs = null
    )
    {
        _tempoReal = tempoReal;
        _posicoes = posicoes;
        _fontes = fontes.ToArray();
        _log = log;
        _aliados = aliados ?? new NinguemEAliado();
        _avisos = new AvisosNaArea(tempoReal, posicoes, vizinhanca);
        _efeitos = efeitos;
        _eventos = eventos;
        _controle = controle;
        _queda = queda;
        _escopos = escopos;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
    }

    public int ProjeteisAtivos
    {
        get
        {
            lock (_travaProjeteis)
            {
                return _projeteis.Count;
            }
        }
    }

    public Task StartAsync(CancellationToken cancelamento)
    {
        _execucao = ExecutarLoopAsync(_parada.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
            if (_execucao != null)
            {
                await Task.WhenAny(_execucao, Task.Delay(Timeout.Infinite, cancelamento));
            }
        }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogDebug(e, "Encerramento do ServicoDeProjeteis concluído.");
        }
    }

    public void Dispose()
    {
        try
        {
            _parada.Cancel();
            _parada.Dispose();
        }
        catch (ObjectDisposedException) { }
    }

    private async Task ExecutarLoopAsync(CancellationToken cancelamento)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        var anteriorMs = _relogioMs();

        try
        {
            while (
                !cancelamento.IsCancellationRequested
                && await timer.WaitForNextTickAsync(cancelamento)
            )
            {
                var agoraMs = _relogioMs();
                var deltaMs = Math.Max(1, agoraMs - anteriorMs);
                anteriorMs = agoraMs;
                Atualizar(agoraMs, deltaMs / 1000.0);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _log.LogError(e, "Erro no loop de simulação de projéteis.");
        }
        finally
        {
            timer.Dispose();
        }
    }

    /// <summary>
    /// Dispara uma flecha como resultado de um ataque com arco no momento do disparo.
    /// </summary>
    public long DispararArco(long atacanteId, AcaoDeAtaque acao, ParcelasDeDano parcelas)
    {
        if (
            (
                _controle != null
                && (
                    !_controle.EstaAtiva(atacanteId, acao.Id)
                    || _controle.FoiInterrompida(atacanteId, acao.Id)
                )
            )
            || (_queda != null && _queda.EstaCaido(atacanteId))
            || !_posicoes.TentarObter(atacanteId, out var posicao)
        )
        {
            return 0;
        }

        var dano = RegrasAtaque.Dano(acao.Pedido.Tipo, parcelas, acao.Passo);
        if (_efeitos != null)
        {
            dano = Math.Max(
                1,
                (int)Math.Round(dano * _efeitos.MultiplicadorDanoCausado(atacanteId))
            );
        }

        var direcao = (float)
            RegrasAtaque.DirecaoDoGolpe(
                acao.Pedido.Tipo,
                acao.Pedido.DirecaoY,
                posicao.RotacaoY,
                acao.PreparoMs / 1000.0
            );

        return Disparar(
            atacanteId: atacanteId,
            area: posicao.Area,
            tipoProjetil: TipoDeProjetil.Flecha,
            tipoAtaque: acao.Pedido.Tipo,
            origemX: (float)posicao.X,
            origemY: (float)posicao.Y + RegrasProjetil.AlturaOrigemDisparo,
            origemZ: (float)posicao.Z,
            direcaoYGraus: direcao,
            dano: dano,
            elevacaoGraus: acao.Pedido.ElevacaoGraus,
            sequencia: acao.Pedido.Sequencia,
            passo: acao.Passo,
            instanteVistoMs: acao.Pedido.TempoVistoMs
        );
    }

    /// <summary>
    /// Dispara um projétil elemental como resultado de um ataque com cajado no momento do disparo.
    /// O elemento do cajado define se é BolaDeFogo, EsferaDeGelo ou OrbeDeCura.
    /// </summary>
    public long DispararCajado(
        long atacanteId,
        AcaoDeAtaque acao,
        ParcelasDeDano parcelas,
        ElementoDaArma elemento
    )
    {
        if (
            (
                _controle != null
                && (
                    !_controle.EstaAtiva(atacanteId, acao.Id)
                    || _controle.FoiInterrompida(atacanteId, acao.Id)
                )
            )
            || (_queda != null && _queda.EstaCaido(atacanteId))
            || !_posicoes.TentarObter(atacanteId, out var posicao)
        )
        {
            return 0;
        }

        var dano = RegrasAtaque.Dano(acao.Pedido.Tipo, parcelas, acao.Passo);
        if (_efeitos != null)
        {
            dano = Math.Max(
                1,
                (int)Math.Round(dano * _efeitos.MultiplicadorDanoCausado(atacanteId))
            );
        }

        var direcao = (float)
            RegrasAtaque.DirecaoDoGolpe(
                acao.Pedido.Tipo,
                acao.Pedido.DirecaoY,
                posicao.RotacaoY,
                acao.PreparoMs / 1000.0
            );

        var tipoProjetil = elemento switch
        {
            ElementoDaArma.Gelo => TipoDeProjetil.EsferaDeGelo,
            ElementoDaArma.Cura => TipoDeProjetil.OrbeDeCura,
            _ => TipoDeProjetil.BolaDeFogo,
        };

        return Disparar(
            atacanteId: atacanteId,
            area: posicao.Area,
            tipoProjetil: tipoProjetil,
            tipoAtaque: acao.Pedido.Tipo,
            origemX: (float)posicao.X,
            origemY: (float)posicao.Y + RegrasProjetil.AlturaOrigemDisparo,
            origemZ: (float)posicao.Z,
            direcaoYGraus: direcao,
            dano: dano,
            elevacaoGraus: acao.Pedido.ElevacaoGraus,
            sequencia: acao.Pedido.Sequencia,
            passo: acao.Passo,
            instanteVistoMs: acao.Pedido.TempoVistoMs
        );
    }

    /// <summary>
    /// Dispara um novo projétil no mundo, avisando a área e registrando para simulação.
    /// </summary>
    public long Disparar(
        long atacanteId,
        string area,
        TipoDeProjetil tipoProjetil,
        TipoDeAtaque tipoAtaque,
        float origemX,
        float origemY,
        float origemZ,
        float direcaoYGraus,
        int dano,
        float elevacaoGraus = 0f,
        int sequencia = 0,
        int passo = 0,
        long instanteVistoMs = 0
    )
    {
        RegrasProjetil.DirecaoVetorial3D(
            direcaoYGraus,
            elevacaoGraus,
            out var dirX,
            out var dirY,
            out var dirZ
        );
        var velocidade = RegrasProjetil.Velocidade(tipoProjetil, tipoAtaque);
        var raio = RegrasProjetil.Raio(tipoProjetil);
        var alcanceMaximo = RegrasProjetil.AlcanceMaximo(tipoProjetil, tipoAtaque);
        var id = Interlocked.Increment(ref _proximoId);
        var agoraMs = _relogioMs();

        var instancia = new InstanciaDeProjetil
        {
            Id = id,
            AtacanteId = atacanteId,
            Area = area,
            TipoProjetil = tipoProjetil,
            TipoAtaque = tipoAtaque,
            OrigemX = origemX,
            OrigemY = origemY,
            OrigemZ = origemZ,
            PosX = origemX,
            PosY = origemY,
            PosZ = origemZ,
            DirX = dirX,
            DirY = dirY,
            DirZ = dirZ,
            Velocidade = velocidade,
            Raio = raio,
            AlcanceMaximo = alcanceMaximo,
            DistanciaPercorrida = 0f,
            Dano = dano,
            Sequencia = sequencia,
            Passo = passo,
            CriadoEmMs = agoraMs,
            InstanteVistoMs = instanteVistoMs > 0 ? instanteVistoMs : agoraMs,
        };

        lock (_travaProjeteis)
        {
            _projeteis.Add(instancia);
        }

        _avisos.Enviar(
            area,
            atacanteId,
            CombateMessageIds.ProjetilLancado,
            new ProjetilLancado
            {
                ProjetilId = id,
                AtacanteId = atacanteId,
                TipoProjetil = tipoProjetil,
                TipoAtaque = tipoAtaque,
                OrigemX = origemX,
                OrigemY = origemY,
                OrigemZ = origemZ,
                DirecaoX = dirX,
                DirecaoY = dirY,
                DirecaoZ = dirZ,
                Velocidade = velocidade,
                Raio = raio,
                AlcanceMaximo = alcanceMaximo,
            }
        );

        return id;
    }

    /// <summary>
    /// Avança a simulação dos projéteis ativos pelo delta time especificado.
    /// </summary>
    public void Atualizar(long agoraMs, double dt)
    {
        if (dt <= 0.0)
        {
            return;
        }

        List<InstanciaDeProjetil> copia;
        lock (_travaProjeteis)
        {
            if (_projeteis.Count == 0)
            {
                return;
            }
            copia = new List<InstanciaDeProjetil>(_projeteis);
        }

        var aRemover = new List<long>();

        foreach (var proj in copia)
        {
            var tempoMaxMs = RegrasProjetil.TempoDeVidaMaximoMs(proj.TipoProjetil, proj.TipoAtaque);
            if (agoraMs - proj.CriadoEmMs > tempoMaxMs)
            {
                aRemover.Add(proj.Id);
                continue;
            }

            var passoDistancia = (float)(proj.Velocidade * dt);
            var x0 = proj.PosX;
            var y0 = proj.PosY;
            var z0 = proj.PosZ;

            var x1 = x0 + proj.DirX * passoDistancia;
            var y1 = y0 + proj.DirY * passoDistancia;
            var z1 = z0 + proj.DirZ * passoDistancia;

            var instanteTeste = RegrasCompensacaoDeLag.InstanteParaTeste(
                proj.InstanteVistoMs,
                proj.CriadoEmMs
            );

            var raioBusca = Math.Max(2.0, proj.Raio + passoDistancia);
            bool acertou = false;

            // Orbe de cura colide primeiro com aliados do grupo na área
            if (proj.TipoProjetil == TipoDeProjetil.OrbeDeCura)
            {
                var personagensNaArea = _posicoes.PersonagensNaArea(proj.Area);
                foreach (var aliadoId in personagensNaArea)
                {
                    if (aliadoId == proj.AtacanteId)
                    {
                        continue;
                    }

                    if (!_aliados.SaoAliados(proj.AtacanteId, aliadoId))
                    {
                        continue;
                    }

                    if (_posicoes.TentarObter(aliadoId, out var posAliado))
                    {
                        if (
                            RegrasProjetil.ColideComAlvo(
                                x0,
                                y0,
                                z0,
                                x1,
                                y1,
                                z1,
                                proj.Raio,
                                posAliado.X,
                                posAliado.Y,
                                posAliado.Z,
                                0.4,
                                1.8
                            )
                        )
                        {
                            if (proj.AlvosAtingidos.Add(aliadoId))
                            {
                                AplicarCuraEmAliado(proj, aliadoId);
                                acertou = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!acertou)
            {
                foreach (var fonte in _fontes)
                {
                    var candidatos = fonte.AlvosProximos(
                        proj.Area,
                        x1,
                        z1,
                        raioBusca,
                        instanteTeste
                    );
                    foreach (var alvo in candidatos)
                    {
                        if (alvo.Id == proj.AtacanteId)
                        {
                            continue;
                        }

                        if (!SelecaoDeAlvos.PodeAcertar(proj.AtacanteId, alvo, _aliados))
                        {
                            continue;
                        }

                        if (
                            RegrasProjetil.ColideComAlvo(
                                x0,
                                y0,
                                z0,
                                x1,
                                y1,
                                z1,
                                proj.Raio,
                                alvo.X,
                                alvo.Z,
                                alvo.Raio,
                                alvo.Altura
                            )
                        )
                        {
                            if (!proj.AlvosAtingidos.Add(alvo.Id))
                            {
                                continue;
                            }

                            var danoFinal = proj.Dano;
                            if (_efeitos != null)
                            {
                                danoFinal = Math.Max(
                                    1,
                                    (int)
                                        Math.Round(
                                            danoFinal * _efeitos.MultiplicadorDanoRecebido(alvo.Id)
                                        )
                                );
                            }

                            if (
                                fonte.TentarAplicarDano(
                                    alvo.Id,
                                    danoFinal,
                                    proj.AtacanteId,
                                    out var r
                                )
                            )
                            {
                                _avisos.Enviar(
                                    proj.Area,
                                    proj.AtacanteId,
                                    CombateMessageIds.AtaqueAcertou,
                                    new AtaqueAcertou
                                    {
                                        AtacanteId = proj.AtacanteId,
                                        Tipo = proj.TipoAtaque,
                                        Sequencia = proj.Sequencia,
                                        Passo = proj.Passo,
                                        Acertos =
                                        [
                                            new AcertoDeAtaque
                                            {
                                                AlvoId = r.AlvoId,
                                                Dano = r.Dano,
                                                VidaRestante = r.VidaRestante,
                                                VidaMaxima = r.VidaMaxima,
                                            },
                                        ],
                                    }
                                );

                                _eventos?.Publicar(
                                    new GolpeComumAcertou(proj.AtacanteId, [r.AlvoId])
                                );

                                if (
                                    proj.TipoProjetil == TipoDeProjetil.BolaDeFogo
                                    && proj.TipoAtaque == TipoDeAtaque.Pesado
                                )
                                {
                                    _efeitos?.Aplicar(alvo.Id, TipoEfeitoDeStatus.Queimadura);
                                }
                                else if (proj.TipoProjetil == TipoDeProjetil.EsferaDeGelo)
                                {
                                    _efeitos?.Aplicar(alvo.Id, TipoEfeitoDeStatus.Lentidao);
                                }

                                if (
                                    alvo.PersonagemId == 0
                                    && TentarInterromper(fonte, alvo.Id, agoraMs)
                                )
                                {
                                    _avisos.Enviar(
                                        proj.Area,
                                        proj.AtacanteId,
                                        CombateMessageIds.AcaoInterrompida,
                                        new AcaoInterrompida
                                        {
                                            AlvoId = alvo.Id,
                                            Jogador = false,
                                            DuracaoMs = RegrasReacao.AtordoamentoNoAcertoMs,
                                        }
                                    );
                                }

                                acertou = true;
                                break;
                            }
                        }
                    }

                    if (acertou)
                    {
                        break;
                    }
                }
            }

            if (!acertou)
            {
                var personagens = _posicoes.PersonagensNaArea(proj.Area);
                for (var i = 0; i < personagens.Count; i++)
                {
                    var persId = personagens[i];
                    if (persId == proj.AtacanteId)
                    {
                        continue;
                    }

                    if (_aliados.SaoAliados(proj.AtacanteId, persId))
                    {
                        continue;
                    }

                    if (!_posicoes.TentarObter(persId, out var pos))
                    {
                        continue;
                    }

                    if (
                        RegrasProjetil.ColideComAlvo(
                            x0,
                            y0,
                            z0,
                            x1,
                            y1,
                            z1,
                            proj.Raio,
                            pos.X,
                            pos.Z,
                            0.4,
                            1.8
                        )
                    )
                    {
                        if (!proj.AlvosAtingidos.Add(persId))
                        {
                            continue;
                        }

                        var danoFinal = proj.Dano;
                        if (_efeitos != null)
                        {
                            danoFinal = Math.Max(
                                1,
                                (int)
                                    Math.Round(
                                        danoFinal * _efeitos.MultiplicadorDanoRecebido(persId)
                                    )
                            );
                        }

                        _eventos?.Publicar(
                            new GolpeDeInimigoAcertou(
                                proj.AtacanteId,
                                CriaturaId: 0,
                                PersonagemId: persId,
                                Dano: danoFinal,
                                Area: proj.Area,
                                Tipo: TipoDeAtaqueDeInimigo.Disparo
                            )
                        );

                        _avisos.Enviar(
                            proj.Area,
                            proj.AtacanteId,
                            CombateMessageIds.AtaqueAcertou,
                            new AtaqueAcertou
                            {
                                AtacanteId = proj.AtacanteId,
                                Tipo = proj.TipoAtaque,
                                Sequencia = proj.Sequencia,
                                Passo = proj.Passo,
                                Acertos =
                                [
                                    new AcertoDeAtaque
                                    {
                                        AlvoId = persId,
                                        Dano = danoFinal,
                                        VidaRestante = 0,
                                        VidaMaxima = 0,
                                    },
                                ],
                            }
                        );

                        acertou = true;
                        break;
                    }
                }
            }

            if (acertou)
            {
                aRemover.Add(proj.Id);
            }
            else
            {
                proj.DistanciaPercorrida += passoDistancia;
                proj.PosX = x1;
                proj.PosY = y1;
                proj.PosZ = z1;

                if (proj.DistanciaPercorrida >= proj.AlcanceMaximo)
                {
                    aRemover.Add(proj.Id);
                }
            }
        }

        if (aRemover.Count > 0)
        {
            lock (_travaProjeteis)
            {
                _projeteis.RemoveAll(p => aRemover.Contains(p.Id));
            }
        }
    }

    private bool TentarInterromper(IAlvosDeCombate fonte, long alvoId, long agoraMs)
    {
        lock (_travaReacoes)
        {
            if (
                _folgaDosAlvos.TryGetValue(alvoId, out var folgaAteMs)
                && !RegrasReacao.PodeReagir(agoraMs, folgaAteMs)
            )
            {
                return false;
            }

            if (!fonte.Interromper(alvoId, RegrasReacao.AtordoamentoNoAcertoMs))
            {
                return false;
            }

            _folgaDosAlvos[alvoId] = RegrasReacao.FimDaFolgaMs(agoraMs);
            return true;
        }
    }

    private void AplicarCuraEmAliado(InstanciaDeProjetil proj, long aliadoId)
    {
        _avisos.Enviar(
            proj.Area,
            proj.AtacanteId,
            CombateMessageIds.AtaqueAcertou,
            new AtaqueAcertou
            {
                AtacanteId = proj.AtacanteId,
                Tipo = proj.TipoAtaque,
                Sequencia = proj.Sequencia,
                Passo = proj.Passo,
                Acertos =
                [
                    new AcertoDeAtaque
                    {
                        AlvoId = aliadoId,
                        Dano = -proj.Dano,
                        VidaRestante = 0,
                        VidaMaxima = 0,
                    },
                ],
            }
        );

        _eventos?.Publicar(new PersonagemCurouAliado(proj.AtacanteId, aliadoId, proj.Area));

        if (_escopos != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var recursos = await RecursosDoAtaque.CurarVidaAsync(
                        _escopos,
                        aliadoId,
                        proj.Dano,
                        _parada.Token
                    );

                    if (_tempoReal.TentarObterSessao(aliadoId, out var sessao))
                    {
                        _tempoReal.Enviar(sessao, CombateMessageIds.RecursosMudaram, recursos);
                    }

                    _eventos?.Publicar(
                        new VidaDoPersonagemMudou(aliadoId, recursos.VidaAtual, recursos.VidaMaxima)
                    );
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Erro ao curar aliado {Aliado} com orbe de cura.", aliadoId);
                }
            });
        }
    }
}
