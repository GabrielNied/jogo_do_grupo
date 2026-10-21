using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>Dano DoT aplicado por segundo por um efeito de status (Sangramento, Queimadura).</summary>
public readonly record struct DanoDoT(long EntidadeId, TipoEfeitoDeStatus Tipo, int Dano);

/// <summary>Resultado de aplicar um efeito de status numa entidade.</summary>
public enum ResultadoAplicacaoStatus
{
    Aplicado,
    Renovado,
    Acumulado,
    Imune,
    AvancoQuebraBoss,
    QuebrouBoss,
}

/// <summary>
/// Gerenciador autoritativo dos efeitos de status das entidades no combate (#171, combate.md).
/// Controla duração, acúmulo de DoTs, imunidade pós-atordoamento e barra de quebra de chefes.
/// Thread-safe e desacoplado do relógio do sistema.
/// </summary>
public sealed class GerenciadorDeEfeitosDeStatus
{
    private sealed class EfeitoAtivo
    {
        public TipoEfeitoDeStatus Tipo { get; set; }
        public int Acumulos { get; set; }
        public long ExpiraEmMs { get; set; }
        public long DuracaoTotalMs { get; set; }
        public long UltimoTickDoTMs { get; set; }
    }

    private sealed class EntidadeStatus
    {
        public Dictionary<TipoEfeitoDeStatus, EfeitoAtivo> Efeitos { get; } = [];
        public long ImunidadeAtordoadoAteMs { get; set; }
        public int BarraDeQuebraBoss { get; set; }
        public bool EhBoss { get; set; }
    }

    private readonly Lock _trava = new();
    private readonly Dictionary<long, EntidadeStatus> _entidades = [];
    private readonly Func<long> _relogioMs;

    public event Action<long, EfeitosDeStatusMudaram>? EfeitosMudaram;
    public event Action<long, BarraDeQuebraMudou>? BarraDeQuebraMudou;

    public GerenciadorDeEfeitosDeStatus(Func<long>? relogioMs = null)
    {
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
    }

    /// <summary>
    /// Aplica um efeito de status numa entidade.
    /// Para boss: atordoado vira acúmulo na barra de quebra (ao encher ativa vulnerabilidade).
    /// Para não-boss: atordoado respeita a imunidade de 6s após o término anterior.
    /// </summary>
    public ResultadoAplicacaoStatus Aplicar(
        long entidadeId,
        TipoEfeitoDeStatus tipo,
        double? duracaoSegundos = null,
        bool ehBoss = false
    )
    {
        var agora = _relogioMs();
        var duracaoMs = (long)
            Math.Round(
                (duracaoSegundos ?? RegrasEfeitosDeStatus.DuracaoPadraoSegundos(tipo)) * 1000.0
            );

        ResultadoAplicacaoStatus resultado;
        BarraDeQuebraMudou? msgQuebra = null;
        EfeitosDeStatusMudaram? msgEfeitos = null;

        lock (_trava)
        {
            if (!_entidades.TryGetValue(entidadeId, out var status))
            {
                status = new EntidadeStatus { EhBoss = ehBoss };
                _entidades[entidadeId] = status;
            }
            else if (ehBoss)
            {
                status.EhBoss = true;
            }

            if (tipo == TipoEfeitoDeStatus.Atordoado)
            {
                if (status.EhBoss)
                {
                    RegrasEfeitosDeStatus.ProcessarQuebraBoss(
                        status.BarraDeQuebraBoss,
                        ConfigEfeitosDeStatus.AumentoQuebraPorAtordoamentoBoss,
                        out var novaQuebra,
                        out var quebrou
                    );
                    status.BarraDeQuebraBoss = novaQuebra;

                    if (quebrou)
                    {
                        var duracaoQuebraMs = (long)
                            Math.Round(
                                ConfigEfeitosDeStatus.DuracaoVulnerabilidadeQuebraBossSegundos
                                    * 1000.0
                            );
                        status.Efeitos[TipoEfeitoDeStatus.Vulneravel] = new EfeitoAtivo
                        {
                            Tipo = TipoEfeitoDeStatus.Vulneravel,
                            Acumulos = 1,
                            ExpiraEmMs = agora + duracaoQuebraMs,
                            DuracaoTotalMs = duracaoQuebraMs,
                            UltimoTickDoTMs = agora,
                        };

                        resultado = ResultadoAplicacaoStatus.QuebrouBoss;
                        msgQuebra = new BarraDeQuebraMudou
                        {
                            EntidadeId = entidadeId,
                            ValorAtual = 0,
                            CapacidadeMax = ConfigEfeitosDeStatus.CapacidadeBarraDeQuebraBoss,
                            Quebrado = true,
                        };
                        msgEfeitos = MontarRetratoSobTrava(entidadeId, status, agora);
                    }
                    else
                    {
                        resultado = ResultadoAplicacaoStatus.AvancoQuebraBoss;
                        msgQuebra = new BarraDeQuebraMudou
                        {
                            EntidadeId = entidadeId,
                            ValorAtual = status.BarraDeQuebraBoss,
                            CapacidadeMax = ConfigEfeitosDeStatus.CapacidadeBarraDeQuebraBoss,
                            Quebrado = false,
                        };
                    }
                }
                else if (agora < status.ImunidadeAtordoadoAteMs)
                {
                    return ResultadoAplicacaoStatus.Imune;
                }
                else
                {
                    resultado = AplicarEfeitoPadrao(status, tipo, agora, duracaoMs);
                    msgEfeitos = MontarRetratoSobTrava(entidadeId, status, agora);
                }
            }
            else
            {
                resultado = AplicarEfeitoPadrao(status, tipo, agora, duracaoMs);
                msgEfeitos = MontarRetratoSobTrava(entidadeId, status, agora);
            }
        }

        if (msgQuebra != null)
        {
            BarraDeQuebraMudou?.Invoke(entidadeId, msgQuebra);
        }

        if (msgEfeitos != null)
        {
            EfeitosMudaram?.Invoke(entidadeId, msgEfeitos);
        }

        return resultado;
    }

    private static ResultadoAplicacaoStatus AplicarEfeitoPadrao(
        EntidadeStatus status,
        TipoEfeitoDeStatus tipo,
        long agora,
        long duracaoMs
    )
    {
        if (status.Efeitos.TryGetValue(tipo, out var ativo))
        {
            ativo.ExpiraEmMs = agora + duracaoMs;
            ativo.DuracaoTotalMs = duracaoMs;

            if (RegrasEfeitosDeStatus.PodeAcumular(tipo))
            {
                if (ativo.Acumulos < RegrasEfeitosDeStatus.MaximoAcumulos(tipo))
                {
                    ativo.Acumulos++;
                    return ResultadoAplicacaoStatus.Acumulado;
                }
            }

            return ResultadoAplicacaoStatus.Renovado;
        }

        status.Efeitos[tipo] = new EfeitoAtivo
        {
            Tipo = tipo,
            Acumulos = 1,
            ExpiraEmMs = agora + duracaoMs,
            DuracaoTotalMs = duracaoMs,
            UltimoTickDoTMs = agora,
        };

        return ResultadoAplicacaoStatus.Aplicado;
    }

    /// <summary>Remove um efeito imediatamente. Se era Atordoado, inicia a imunidade de 6s.</summary>
    public bool Remover(long entidadeId, TipoEfeitoDeStatus tipo)
    {
        var agora = _relogioMs();
        EfeitosDeStatusMudaram? msgEfeitos = null;
        bool removeu;

        lock (_trava)
        {
            if (!_entidades.TryGetValue(entidadeId, out var status))
            {
                return false;
            }

            removeu = status.Efeitos.Remove(tipo);
            if (removeu)
            {
                if (tipo == TipoEfeitoDeStatus.Atordoado && !status.EhBoss)
                {
                    status.ImunidadeAtordoadoAteMs =
                        agora
                        + (long)
                            Math.Round(ConfigEfeitosDeStatus.ImunidadeAtordoadoSegundos * 1000.0);
                }
                msgEfeitos = MontarRetratoSobTrava(entidadeId, status, agora);
            }
        }

        if (msgEfeitos != null)
        {
            EfeitosMudaram?.Invoke(entidadeId, msgEfeitos);
        }

        return removeu;
    }

    /// <summary>Se a entidade está com o status Atordoado ativo.</summary>
    public bool EstaAtordoado(long entidadeId)
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            if (_entidades.TryGetValue(entidadeId, out var status))
            {
                return status.Efeitos.TryGetValue(TipoEfeitoDeStatus.Atordoado, out var e)
                    && agora < e.ExpiraEmMs;
            }
            return false;
        }
    }

    /// <summary>Se a entidade está no período de imunidade ao atordoamento.</summary>
    public bool EstaImuneAAtordoado(long entidadeId)
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            if (_entidades.TryGetValue(entidadeId, out var status))
            {
                return status.EhBoss || agora < status.ImunidadeAtordoadoAteMs;
            }
            return false;
        }
    }

    /// <summary>Se a entidade tem o efeito de status especificado ativo.</summary>
    public bool TemEfeito(long entidadeId, TipoEfeitoDeStatus tipo)
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            return _entidades.TryGetValue(entidadeId, out var status)
                && status.Efeitos.TryGetValue(tipo, out var e)
                && agora < e.ExpiraEmMs;
        }
    }

    /// <summary>Se a entidade tem Lentidão ativa.</summary>
    public bool TemLentidao(long entidadeId) => TemEfeito(entidadeId, TipoEfeitoDeStatus.Lentidao);

    /// <summary>Se a entidade tem Vulnerável ativo (+15% de dano recebido).</summary>
    public bool TemVulneravel(long entidadeId)
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            return _entidades.TryGetValue(entidadeId, out var status)
                && status.Efeitos.TryGetValue(TipoEfeitoDeStatus.Vulneravel, out var e)
                && agora < e.ExpiraEmMs;
        }
    }

    /// <summary>Se a entidade tem Fortalecido ativo (+15% de dano causado).</summary>
    public bool TemFortalecido(long entidadeId)
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            return _entidades.TryGetValue(entidadeId, out var status)
                && status.Efeitos.TryGetValue(TipoEfeitoDeStatus.Fortalecido, out var e)
                && agora < e.ExpiraEmMs;
        }
    }

    /// <summary>Multiplicador no dano causado pelo atacante (1,15 se fortalecido, 1,0 normal).</summary>
    public double MultiplicadorDanoCausado(long entidadeId) =>
        RegrasEfeitosDeStatus.MultiplicadorDanoCausado(TemFortalecido(entidadeId));

    /// <summary>Se a entidade tem Protegido ativo (-15% de dano recebido).</summary>
    public bool TemProtegido(long entidadeId) =>
        TemEfeito(entidadeId, TipoEfeitoDeStatus.Protegido);

    /// <summary>
    /// Multiplicador no dano recebido pelo defensor (+15% se vulnerável, -15% se protegido, 1,0
    /// normal).
    /// </summary>
    public double MultiplicadorDanoRecebido(long entidadeId) =>
        RegrasEfeitosDeStatus.MultiplicadorDanoRecebido(
            TemVulneravel(entidadeId),
            TemProtegido(entidadeId)
        );

    /// <summary>Modificador de velocidade da entidade (0,70 se lentidão, 1,0 normal).</summary>
    public double ModificadorVelocidade(long entidadeId) =>
        RegrasEfeitosDeStatus.ModificadorVelocidade(TemLentidao(entidadeId));

    /// <summary>Obtém a barra de quebra atual de um chefe (0 a 100).</summary>
    public int ObterQuebraBoss(long entidadeId)
    {
        lock (_trava)
        {
            return _entidades.TryGetValue(entidadeId, out var s) ? s.BarraDeQuebraBoss : 0;
        }
    }

    /// <summary>
    /// Retrato dos efeitos ativos da entidade para envio em mensagem de rede.
    /// </summary>
    public bool ObterRetrato(
        long entidadeId,
        out InstanciaEfeitoDeStatus[] efeitos,
        out float imunidadeRestanteSegundos
    )
    {
        var agora = _relogioMs();
        lock (_trava)
        {
            if (!_entidades.TryGetValue(entidadeId, out var status))
            {
                efeitos = [];
                imunidadeRestanteSegundos = 0f;
                return false;
            }

            var retrato = MontarRetratoSobTrava(entidadeId, status, agora);
            efeitos = retrato.Efeitos;
            imunidadeRestanteSegundos = retrato.ImunidadeAtordoadoRestanteSegundos;
            return true;
        }
    }

    private static EfeitosDeStatusMudaram MontarRetratoSobTrava(
        long entidadeId,
        EntidadeStatus? status,
        long agora
    )
    {
        if (status == null)
        {
            return new EfeitosDeStatusMudaram
            {
                EntidadeId = entidadeId,
                Efeitos = [],
                ImunidadeAtordoadoRestanteSegundos = 0f,
            };
        }

        var lista = new List<InstanciaEfeitoDeStatus>();
        foreach (var kvp in status.Efeitos)
        {
            var e = kvp.Value;
            if (agora < e.ExpiraEmMs)
            {
                var restanteSegundos = Math.Max(0f, (float)((e.ExpiraEmMs - agora) / 1000.0));
                var totalSegundos = (float)(e.DuracaoTotalMs / 1000.0);
                lista.Add(
                    new InstanciaEfeitoDeStatus
                    {
                        Tipo = e.Tipo,
                        Acumulos = e.Acumulos,
                        DuracaoRestanteSegundos = restanteSegundos,
                        DuracaoTotalSegundos = totalSegundos,
                    }
                );
            }
        }

        var imunidade =
            agora < status.ImunidadeAtordoadoAteMs
                ? Math.Max(0f, (float)((status.ImunidadeAtordoadoAteMs - agora) / 1000.0))
                : 0f;

        return new EfeitosDeStatusMudaram
        {
            EntidadeId = entidadeId,
            Efeitos = lista.ToArray(),
            ImunidadeAtordoadoRestanteSegundos = imunidade,
        };
    }

    /// <summary>
    /// Atualiza expirações e gera ticks de dano DoT (a cada 1000 ms).
    /// </summary>
    public void Atualizar(
        long agoraMs,
        out List<DanoDoT> danosDoT,
        out List<long> entidadesComMudanca
    )
    {
        danosDoT = [];
        entidadesComMudanca = [];
        var notificacoesEfeitos = new List<EfeitosDeStatusMudaram>();
        var notificacoesQuebra = new List<BarraDeQuebraMudou>();

        lock (_trava)
        {
            foreach (var (entidadeId, status) in _entidades)
            {
                var mudou = false;
                var expirados = new List<TipoEfeitoDeStatus>();

                foreach (var (tipo, efeito) in status.Efeitos)
                {
                    if (agoraMs >= efeito.ExpiraEmMs)
                    {
                        expirados.Add(tipo);
                        continue;
                    }

                    // Processa DoT (a cada 1000 ms)
                    if (RegrasEfeitosDeStatus.PodeAcumular(tipo))
                    {
                        if (agoraMs - efeito.UltimoTickDoTMs >= 1000)
                        {
                            var dano = RegrasEfeitosDeStatus.CalcularDanoDoT(tipo, efeito.Acumulos);
                            danosDoT.Add(new DanoDoT(entidadeId, tipo, dano));
                            efeito.UltimoTickDoTMs = agoraMs;
                        }
                    }
                }

                foreach (var exp in expirados)
                {
                    status.Efeitos.Remove(exp);
                    mudou = true;

                    if (exp == TipoEfeitoDeStatus.Atordoado && !status.EhBoss)
                    {
                        status.ImunidadeAtordoadoAteMs =
                            agoraMs
                            + (long)
                                Math.Round(
                                    ConfigEfeitosDeStatus.ImunidadeAtordoadoSegundos * 1000.0
                                );
                    }
                    else if (exp == TipoEfeitoDeStatus.Vulneravel && status.EhBoss)
                    {
                        // Quando vulnerabilidade do boss expira, quebra reseta e avisa
                        notificacoesQuebra.Add(
                            new BarraDeQuebraMudou
                            {
                                EntidadeId = entidadeId,
                                ValorAtual = 0,
                                CapacidadeMax = ConfigEfeitosDeStatus.CapacidadeBarraDeQuebraBoss,
                                Quebrado = false,
                            }
                        );
                    }
                }

                if (mudou)
                {
                    entidadesComMudanca.Add(entidadeId);
                    notificacoesEfeitos.Add(MontarRetratoSobTrava(entidadeId, status, agoraMs));
                }
            }
        }

        foreach (var msg in notificacoesQuebra)
        {
            BarraDeQuebraMudou?.Invoke(msg.EntidadeId, msg);
        }

        foreach (var msg in notificacoesEfeitos)
        {
            EfeitosMudaram?.Invoke(msg.EntidadeId, msg);
        }
    }

    /// <summary>Limpa todos os efeitos da entidade (morte, respawn ou desconexão).</summary>
    public void Limpar(long entidadeId)
    {
        bool removeu;
        lock (_trava)
        {
            removeu = _entidades.Remove(entidadeId);
        }

        if (removeu)
        {
            EfeitosMudaram?.Invoke(
                entidadeId,
                new EfeitosDeStatusMudaram
                {
                    EntidadeId = entidadeId,
                    Efeitos = [],
                    ImunidadeAtordoadoRestanteSegundos = 0f,
                }
            );
        }
    }
}
