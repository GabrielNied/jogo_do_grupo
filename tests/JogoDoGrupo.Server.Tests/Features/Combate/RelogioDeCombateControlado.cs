using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Tests.Core.TempoReal;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// O <see cref="RelogioDeCombate"/> dos testes de rede. Por padrão anda com o tempo real; depois de
/// <see cref="Controlar"/> o tempo congela e só anda com <see cref="Avancar"/>, que acorda as
/// esperas do servidor na ordem dos instantes delas (e no instante certo de cada uma). O teste
/// prova a janela (preparo, recuperação, queda) avançando o relógio, sem dormir de verdade.
/// </summary>
public sealed class RelogioDeCombateControlado : RelogioDeCombate
{
    private readonly object _trava = new();
    private readonly List<(long InstanteMs, TaskCompletionSource Espera)> _esperas = [];
    private bool _controlado;
    private long _agoraMs;

    public override long AgoraMs
    {
        get
        {
            lock (_trava)
            {
                return _controlado ? _agoraMs : base.AgoraMs;
            }
        }
    }

    /// <summary>Quantas esperas do servidor estão penduradas no relógio controlado.</summary>
    public int Esperas
    {
        get
        {
            lock (_trava)
            {
                return _esperas.Count;
            }
        }
    }

    /// <summary>Congela o tempo no instante de agora; daqui em diante só <see cref="Avancar"/> o move.</summary>
    public void Controlar()
    {
        lock (_trava)
        {
            _agoraMs = base.AgoraMs;
            _controlado = true;
        }
    }

    /// <summary>Volta ao tempo real. Esperas que sobraram de um teste (de quem já saiu) ficam sem fim.</summary>
    public void Soltar()
    {
        lock (_trava)
        {
            _controlado = false;
            _esperas.Clear();
        }
    }

    /// <summary>
    /// Anda o relógio <paramref name="ms"/> milissegundos. Cada espera acorda no instante dela, e o
    /// que ela fizer em seguida (acertar, agendar outra espera) já vê esse instante.
    /// </summary>
    public void Avancar(long ms)
    {
        long destino;
        lock (_trava)
        {
            destino = _agoraMs + ms;
        }

        while (true)
        {
            TaskCompletionSource? acordar = null;
            lock (_trava)
            {
                var menor = -1;
                for (var i = 0; i < _esperas.Count; i++)
                {
                    if (
                        _esperas[i].InstanteMs <= destino
                        && (menor < 0 || _esperas[i].InstanteMs < _esperas[menor].InstanteMs)
                    )
                    {
                        menor = i;
                    }
                }

                if (menor < 0)
                {
                    _agoraMs = destino;
                    return;
                }

                _agoraMs = Math.Max(_agoraMs, _esperas[menor].InstanteMs);
                acordar = _esperas[menor].Espera;
                _esperas.RemoveAt(menor);
            }

            acordar.TrySetResult(); // Continua aqui mesmo: o que a espera dispara já correu na volta.
        }
    }

    /// <summary>
    /// Espera o servidor ficar pendurado numa espera do relógio (o ponto em que ele já agendou o
    /// próximo passo) e então anda <paramref name="ms"/>. Sem a espera, o avanço passaria na frente do agendamento.
    /// </summary>
    public async Task AvancarQuandoEsperarAsync(long ms)
    {
        await TempoRealFixture.EsperarAsync(() => Esperas >= 1, "espera do servidor no relógio");
        Avancar(ms);
    }

    public override Task EsperarAteAsync(long instanteMs, CancellationToken cancelamento)
    {
        TaskCompletionSource espera;
        lock (_trava)
        {
            if (!_controlado)
            {
                return base.EsperarAteAsync(instanteMs, cancelamento);
            }

            if (instanteMs <= _agoraMs)
            {
                return Task.CompletedTask;
            }

            espera = new TaskCompletionSource();
            _esperas.Add((instanteMs, espera));
        }

        cancelamento.Register(() => espera.TrySetCanceled(cancelamento));
        return espera.Task;
    }
}
