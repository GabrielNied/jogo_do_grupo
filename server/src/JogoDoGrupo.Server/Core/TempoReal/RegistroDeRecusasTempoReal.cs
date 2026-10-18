using System.Collections.Concurrent;
using JogoDoGrupo.Shared.Core.Api;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>
/// Monitor de recusas repetidas de tempo real por conta para ferramentas de GM (#523, #211).
/// Agrega recusas em uma janela deslizante e sinaliza contas com volume suspeito sem ban automático.
/// </summary>
public sealed class RegistroDeRecusasTempoReal : IRegistroDeRecusasTempoReal
{
    private readonly ConfiguracaoTempoReal _config;
    private readonly ILogger<RegistroDeRecusasTempoReal> _log;
    private readonly TimeProvider _relogio;
    private readonly ConcurrentDictionary<long, RegistroConta> _contas = new();

    public RegistroDeRecusasTempoReal(
        ConfiguracaoTempoReal config,
        ILogger<RegistroDeRecusasTempoReal> log,
        TimeProvider? relogio = null
    )
    {
        _config = config;
        _log = log;
        _relogio = relogio ?? TimeProvider.System;
    }

    public TimeSpan Janela => TimeSpan.FromMinutes(_config.JanelaRecusasMinutos);
    public int LimiteAlerta => _config.LimiteAlertaRecusasConta;

    public void Observar(ServidorTempoReal tempoReal)
    {
        tempoReal.MensagemRecusada += TratarRecusa;
    }

    private void TratarRecusa(SessaoTempoReal sessao, ushort id, MotivoRecusa motivo)
    {
        if (sessao.ContaId > 0)
        {
            RegistrarRecusa(sessao.ContaId, sessao.PersonagemId, id, motivo);
        }
    }

    public void RegistrarRecusa(
        long contaId,
        long personagemId,
        ushort mensagemId,
        MotivoRecusa motivo
    )
    {
        if (contaId <= 0)
        {
            return;
        }

        var registro = _contas.GetOrAdd(contaId, id => new RegistroConta(id));
        var agora = _relogio.GetUtcNow();
        var (total, novoAlerta, _) = registro.Adicionar(
            personagemId,
            mensagemId,
            motivo,
            agora,
            Janela,
            LimiteAlerta
        );

        if (novoAlerta)
        {
            _log.LogWarning(
                "Alerta GM (#523): conta {ContaId} atingiu {Total} recusas de tempo real na janela de {Minutos} minutos (último motivo: {Motivo}, msg: {MensagemId}, pers: {PersonagemId}).",
                contaId,
                total,
                _config.JanelaRecusasMinutos,
                motivo,
                mensagemId,
                personagemId
            );
        }
    }

    public RelatorioRecusasContaDto? Obter(long contaId)
    {
        if (!_contas.TryGetValue(contaId, out var registro))
        {
            return null;
        }

        var agora = _relogio.GetUtcNow();
        var relatorio = registro.Obter(agora, Janela, LimiteAlerta);
        if (relatorio == null)
        {
            _contas.TryRemove(contaId, out _);
        }

        return relatorio;
    }

    public IReadOnlyList<RelatorioRecusasContaDto> ListarTodas()
    {
        var agora = _relogio.GetUtcNow();
        var resultado = new List<RelatorioRecusasContaDto>();

        foreach (var (contaId, registro) in _contas)
        {
            var relatorio = registro.Obter(agora, Janela, LimiteAlerta);
            if (relatorio != null)
            {
                resultado.Add(relatorio);
            }
            else
            {
                _contas.TryRemove(contaId, out _);
            }
        }

        return resultado;
    }

    public IReadOnlyList<RelatorioRecusasContaDto> ListarSuspeitas()
    {
        var todas = ListarTodas();
        return todas.Where(r => r.AlertaAtivo).ToList();
    }

    public bool Limpar(long contaId)
    {
        return _contas.TryRemove(contaId, out _);
    }

    public int TotalContasComRecusas => ListarTodas().Count;

    public int TotalContasEmAlerta => ListarSuspeitas().Count;

    private sealed class OcorrenciaRecusa(
        DateTimeOffset momento,
        ushort mensagemId,
        MotivoRecusa motivo,
        long personagemId
    )
    {
        public DateTimeOffset Momento { get; } = momento;
        public ushort MensagemId { get; } = mensagemId;
        public MotivoRecusa Motivo { get; } = motivo;
        public long PersonagemId { get; } = personagemId;
    }

    private sealed class RegistroConta(long contaId)
    {
        private readonly Lock _trava = new();
        private readonly List<OcorrenciaRecusa> _ocorrencias = [];
        private bool _alertaDisparado;

        public long ContaId { get; } = contaId;

        public (int Total, bool NovoAlerta, RelatorioRecusasContaDto Relatorio) Adicionar(
            long personagemId,
            ushort mensagemId,
            MotivoRecusa motivo,
            DateTimeOffset agora,
            TimeSpan janela,
            int limiteAlerta
        )
        {
            lock (_trava)
            {
                _ocorrencias.Add(new OcorrenciaRecusa(agora, mensagemId, motivo, personagemId));
                LimparExpiradasInternal(agora, janela, limiteAlerta);

                var total = _ocorrencias.Count;
                var novoAlerta = false;
                if (total >= limiteAlerta && !_alertaDisparado)
                {
                    _alertaDisparado = true;
                    novoAlerta = true;
                }

                return (total, novoAlerta, MontarRelatorioInternal(limiteAlerta));
            }
        }

        public RelatorioRecusasContaDto? Obter(
            DateTimeOffset agora,
            TimeSpan janela,
            int limiteAlerta
        )
        {
            lock (_trava)
            {
                LimparExpiradasInternal(agora, janela, limiteAlerta);
                if (_ocorrencias.Count == 0)
                {
                    return null;
                }

                return MontarRelatorioInternal(limiteAlerta);
            }
        }

        private void LimparExpiradasInternal(
            DateTimeOffset agora,
            TimeSpan janela,
            int limiteAlerta
        )
        {
            var corte = agora - janela;
            _ocorrencias.RemoveAll(o => o.Momento < corte);
            if (_ocorrencias.Count < limiteAlerta)
            {
                _alertaDisparado = false;
            }
        }

        private RelatorioRecusasContaDto MontarRelatorioInternal(int limiteAlerta)
        {
            var ultima = _ocorrencias[^1];
            var porMotivo = new Dictionary<string, int>();
            foreach (var o in _ocorrencias)
            {
                var chave = o.Motivo.ToString();
                porMotivo[chave] = porMotivo.TryGetValue(chave, out var c) ? c + 1 : 1;
            }

            return new RelatorioRecusasContaDto
            {
                ContaId = ContaId,
                PersonagemId = ultima.PersonagemId,
                TotalRecusas = _ocorrencias.Count,
                UltimoMotivo = ultima.Motivo.ToString(),
                UltimoMensagemId = ultima.MensagemId,
                AlertaAtivo = _ocorrencias.Count >= limiteAlerta,
                UltimaRecusaUtc = ultima.Momento.ToString("o"),
                RecusasPorMotivo = porMotivo,
            };
        }
    }
}
