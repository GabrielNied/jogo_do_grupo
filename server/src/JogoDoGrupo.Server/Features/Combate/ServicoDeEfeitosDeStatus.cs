using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Serviço de tempo real e laço de ticks para os efeitos de status e barra de quebra de chefes (#171, combate.md).
/// Envia o retrato de status ao conectar, notifica mudanças no status (6040) e na barra de quebra (6041),
/// e processa expirações e dano periódico (DoT).
/// </summary>
public sealed class ServicoDeEfeitosDeStatus : IHostedService, IDisposable
{
    private const int IntervaloDoTickMs = 200;

    private readonly GerenciadorDeEfeitosDeStatus _efeitos;
    private readonly ServidorTempoReal _tempoReal;
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ServicoDeEfeitosDeStatus> _log;
    private readonly IPosicoesDoMundo? _posicoes;
    private readonly AvisosNaArea? _avisos;
    private readonly ServicoDeQueda? _queda;
    private readonly Func<long> _relogioMs;
    private readonly CancellationTokenSource _parada = new();
    private Task _laco = Task.CompletedTask;

    public ServicoDeEfeitosDeStatus(
        GerenciadorDeEfeitosDeStatus efeitos,
        ServidorTempoReal tempoReal,
        IServiceScopeFactory escopos,
        ILogger<ServicoDeEfeitosDeStatus> log,
        IPosicoesDoMundo? posicoes = null,
        ServicoDeQueda? queda = null,
        Func<long>? relogioMs = null,
        IVizinhanca? vizinhanca = null
    )
    {
        _efeitos = efeitos;
        _tempoReal = tempoReal;
        _escopos = escopos;
        _log = log;
        _posicoes = posicoes;
        _avisos = posicoes != null ? new AvisosNaArea(tempoReal, posicoes, vizinhanca) : null;
        _queda = queda;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);

        _efeitos.EfeitosMudaram += AoMudarEfeitos;
        _efeitos.BarraDeQuebraMudou += AoMudarBarraDeQuebra;
        _tempoReal.SessaoAutenticada += AoAutenticar;
        _tempoReal.SessaoEncerrada += AoEncerrar;
    }

    public Task StartAsync(CancellationToken cancelamento)
    {
        _laco = Task.Run(ExecutarLacoAsync, CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancelamento)
    {
        try
        {
            _parada.Cancel();
        }
        catch (ObjectDisposedException) { }

        try
        {
            await _laco;
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        _efeitos.EfeitosMudaram -= AoMudarEfeitos;
        _efeitos.BarraDeQuebraMudou -= AoMudarBarraDeQuebra;
        _tempoReal.SessaoAutenticada -= AoAutenticar;
        _tempoReal.SessaoEncerrada -= AoEncerrar;
        _parada.Dispose();
    }

    private void AoAutenticar(SessaoTempoReal sessao)
    {
        if (
            _efeitos.ObterRetrato(sessao.PersonagemId, out var lista, out var imunidade)
            && (lista.Length > 0 || imunidade > 0f)
        )
        {
            _tempoReal.Enviar(
                sessao,
                CombateMessageIds.EfeitosDeStatusMudaram,
                new EfeitosDeStatusMudaram
                {
                    EntidadeId = sessao.PersonagemId,
                    Efeitos = lista,
                    ImunidadeAtordoadoRestanteSegundos = imunidade,
                }
            );
        }
    }

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        _efeitos.Limpar(sessao.PersonagemId);
    }

    private void AoMudarEfeitos(long entidadeId, EfeitosDeStatusMudaram msg)
    {
        if (_tempoReal.TentarObterSessao(entidadeId, out var sessao) && sessao != null)
        {
            _tempoReal.Enviar(sessao, CombateMessageIds.EfeitosDeStatusMudaram, msg);
        }

        if (_posicoes?.TentarObter(entidadeId, out var pos) == true && _avisos != null)
        {
            _avisos.Enviar(
                pos.Area,
                entidadeId,
                CombateMessageIds.EfeitosDeStatusMudaram,
                msg,
                incluirDono: false
            );
        }
    }

    private void AoMudarBarraDeQuebra(long bossId, BarraDeQuebraMudou msg)
    {
        if (_posicoes?.TentarObter(bossId, out var pos) == true && _avisos != null)
        {
            _avisos.Enviar(
                pos.Area,
                bossId,
                CombateMessageIds.BarraDeQuebraMudou,
                msg,
                incluirDono: true
            );
            return;
        }

        // Se a posição não é de personagem no mundo (ex: boss mob/dungeon), envia a todas as sessões
        foreach (var sessao in _tempoReal.Sessoes)
        {
            _tempoReal.Enviar(sessao, CombateMessageIds.BarraDeQuebraMudou, msg);
        }
    }

    private async Task ExecutarLacoAsync()
    {
        while (!_parada.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(IntervaloDoTickMs, _parada.Token);
                var agora = _relogioMs();
                _efeitos.Atualizar(agora, out var danosDoT, out _);

                foreach (var dano in danosDoT)
                {
                    await ProcessarDanoDoTAsync(dano);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Erro no laço de atualização de efeitos de status.");
            }
        }
    }

    private async Task ProcessarDanoDoTAsync(DanoDoT dano)
    {
        try
        {
            if (
                _queda?.EstaCaido(dano.EntidadeId) == true
                || _queda?.EstaImuneAposLevantar(dano.EntidadeId) == true
            )
            {
                return;
            }

            var recursos = await RecursosDoAtaque.TirarVidaAsync(
                _escopos,
                dano.EntidadeId,
                dano.Dano,
                _parada.Token
            );

            if (_tempoReal.TentarObterSessao(dano.EntidadeId, out var sessao) && sessao != null)
            {
                _tempoReal.Enviar(sessao, CombateMessageIds.RecursosMudaram, recursos);
            }

            _queda?.AoDano(dano.EntidadeId, recursos.VidaAtual);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Erro ao processar dano de DoT {Tipo} na entidade {Entidade}.",
                dano.Tipo,
                dano.EntidadeId
            );
        }
    }
}
