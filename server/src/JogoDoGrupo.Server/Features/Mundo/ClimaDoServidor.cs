namespace JogoDoGrupo.Server.Features.Mundo;

using JogoDoGrupo.Shared.Features.Mundo;

/// <summary>
/// Mantém o clima autoritativo do mundo no servidor (issue #902).
/// Sorteia novos climas periodicamente ou recebe clima forçado para testes.
/// </summary>
public sealed class ClimaDoServidor
{
    private readonly Func<long> _relogioMs;
    private readonly Random _random;
    private readonly object _trava = new();

    private TipoDeClima _climaAtual = TipoDeClima.Limpo;
    private long _inicioClimaMs;
    private int _duracaoSegundos = ConfigClima.DuracaoPadraoClimaSegundos;

    public ClimaDoServidor(Func<long>? relogioMs = null, Random? random = null)
    {
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _random = random ?? new Random();
        _inicioClimaMs = _relogioMs();
    }

    public TipoDeClima ClimaAtual
    {
        get
        {
            lock (_trava)
            {
                return _climaAtual;
            }
        }
    }

    public int DuracaoSegundos
    {
        get
        {
            lock (_trava)
            {
                return _duracaoSegundos;
            }
        }
    }

    /// <summary>
    /// Define o clima explicitamente (para testes, eventos ou comandos).
    /// </summary>
    public void DefinirClima(
        TipoDeClima clima,
        int duracaoSegundos = ConfigClima.DuracaoPadraoClimaSegundos
    )
    {
        lock (_trava)
        {
            _climaAtual = clima;
            _duracaoSegundos = duracaoSegundos;
            _inicioClimaMs = _relogioMs();
        }
    }

    /// <summary>
    /// Confere se a duração do clima atual expirou; se sim, sorteia o próximo.
    /// Retorna true se o clima mudou.
    /// </summary>
    public bool Atualizar(long agoraMs = 0)
    {
        var agora = agoraMs > 0 ? agoraMs : _relogioMs();
        lock (_trava)
        {
            var decorridoSegundos = (agora - _inicioClimaMs) / 1000.0;
            if (decorridoSegundos >= _duracaoSegundos)
            {
                SortearProximoClimaInterno(agora);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Sorteia um novo clima e reseta o tempo de início.
    /// </summary>
    public TipoDeClima SortearProximoClima(long agoraMs = 0)
    {
        var agora = agoraMs > 0 ? agoraMs : _relogioMs();
        lock (_trava)
        {
            SortearProximoClimaInterno(agora);
            return _climaAtual;
        }
    }

    private void SortearProximoClimaInterno(long agora)
    {
        // Distribuição: Limpo (40%), Chuva (25%), Neblina (20%), Nevasca (15%)
        var rolagem = _random.Next(100);
        var novo = rolagem switch
        {
            < 40 => TipoDeClima.Limpo,
            < 65 => TipoDeClima.Chuva,
            < 85 => TipoDeClima.Neblina,
            _ => TipoDeClima.Nevasca,
        };

        _climaAtual = novo;
        _duracaoSegundos = _random.Next(
            ConfigClima.DuracaoMinimaSegundos,
            ConfigClima.DuracaoMaximaSegundos + 1
        );
        _inicioClimaMs = agora;
    }

    /// <summary>Mensagem de rede pronta para envio.</summary>
    public ClimaDoMundo Mensagem()
    {
        lock (_trava)
        {
            return new ClimaDoMundo
            {
                Clima = _climaAtual,
                FatorVelocidade = ConfigClima.ObterFatorVelocidade(_climaAtual),
                DistanciaVisao = ConfigClima.ObterDistanciaVisao(_climaAtual),
                DensidadeNevoa = ConfigClima.ObterDensidadeNevoa(_climaAtual),
                DuracaoSegundos = _duracaoSegundos,
            };
        }
    }
}
