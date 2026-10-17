using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Controla um ponto de aparição de chefe raro: a cada intervalo da ficha o servidor sorteia
/// se ele aparece; se não aparecer, só tenta de novo no intervalo seguinte. Enquanto ele está
/// no mundo não há sorteio, e a morte recomeça a espera. Quem chama <see cref="Atualizar"/> e
/// <see cref="RegistrarMorte"/> é o laço do mundo; a classe só guarda o estado e é segura
/// entre threads.
/// </summary>
public sealed class AparicaoDeChefeRaro
{
    private readonly object _trava = new();
    private readonly ISorteadorDeCriaturas _sorteador;
    private readonly TimeSpan _intervalo;
    private readonly TimeSpan _tempoDePermanencia;
    private DateTimeOffset _proximoSorteio;
    private DateTimeOffset _ultimoInstante;
    private TimeSpan _tempoRestante;
    private bool _presente;

    /// <param name="definicao">Ficha de um <see cref="CategoriaDeCriatura.ChefeRaro"/>.</param>
    /// <param name="sorteador">Quem sorteia a aparição.</param>
    /// <param name="inicio">Momento do primeiro sorteio (o servidor subindo).</param>
    /// <param name="tempoDePermanencia">
    /// Tempo máximo no mundo antes de sumir se não for morto (padrão <see cref="ConfigChefeRaro.TempoDePermanenciaSegundos"/>).
    /// </param>
    public AparicaoDeChefeRaro(
        DefinicaoDeCriatura definicao,
        ISorteadorDeCriaturas sorteador,
        DateTimeOffset inicio,
        TimeSpan? tempoDePermanencia = null
    )
    {
        if (definicao.Categoria != CategoriaDeCriatura.ChefeRaro)
        {
            throw new ArgumentException(
                $"A criatura {definicao.Id} não é chefe raro.",
                nameof(definicao)
            );
        }
        if (definicao.IntervaloDeAparicaoSegundos <= 0)
        {
            throw new ArgumentException(
                $"O chefe raro {definicao.Id} precisa de intervalo de aparição maior que zero.",
                nameof(definicao)
            );
        }

        Definicao = definicao;
        _sorteador = sorteador;
        _intervalo = TimeSpan.FromSeconds(definicao.IntervaloDeAparicaoSegundos);
        _tempoDePermanencia =
            tempoDePermanencia ?? TimeSpan.FromSeconds(ConfigChefeRaro.TempoDePermanenciaSegundos);
        _tempoRestante = _tempoDePermanencia;
        _proximoSorteio = inicio;
        _ultimoInstante = inicio;
    }

    public DefinicaoDeCriatura Definicao { get; }

    /// <summary>Se o chefe está no mundo agora.</summary>
    public bool Presente
    {
        get
        {
            lock (_trava)
            {
                return _presente;
            }
        }
    }

    /// <summary>Tempo restante antes de sumir por tempo (pausado enquanto em combate).</summary>
    public TimeSpan TempoRestante
    {
        get
        {
            lock (_trava)
            {
                return _tempoRestante;
            }
        }
    }

    /// <summary>
    /// Avança o controle até <paramref name="agora"/>. Devolve <c>true</c> só na chamada em
    /// que o chefe apareceu.
    /// </summary>
    public bool Atualizar(DateTimeOffset agora) => Atualizar(agora, emCombate: false, out _);

    /// <summary>
    /// Avança o controle até <paramref name="agora"/>. Devolve <c>true</c> só na chamada em
    /// que o chefe apareceu. <paramref name="sumiu"/> indica se o chefe sumiu por tempo esgotado.
    /// Enquanto <paramref name="emCombate"/> for <c>true</c>, o timer de 30 min fica pausado.
    /// </summary>
    public bool Atualizar(DateTimeOffset agora, bool emCombate, out bool sumiu)
    {
        sumiu = false;
        lock (_trava)
        {
            if (_presente)
            {
                var delta = agora - _ultimoInstante;
                if (delta > TimeSpan.Zero)
                {
                    if (!emCombate)
                    {
                        _tempoRestante -= delta;
                        if (_tempoRestante <= TimeSpan.Zero)
                        {
                            _tempoRestante = TimeSpan.Zero;
                            _presente = false;
                            sumiu = true;
                            _proximoSorteio = agora + _intervalo;
                        }
                    }

                    _ultimoInstante = agora;
                }

                return false;
            }

            if (agora < _proximoSorteio)
            {
                _ultimoInstante = agora;
                return false;
            }

            _proximoSorteio = agora + _intervalo;
            _presente = _sorteador.Aparece(Definicao);
            if (_presente)
            {
                _tempoRestante = _tempoDePermanencia;
                _ultimoInstante = agora;
                return true;
            }

            _ultimoInstante = agora;
            return false;
        }
    }

    /// <summary>
    /// Registra que o chefe morreu e recomeça a espera. Devolve <c>false</c> se ele não estava
    /// no mundo (morte repetida), sem mexer na espera.
    /// </summary>
    public bool RegistrarMorte(DateTimeOffset agora)
    {
        lock (_trava)
        {
            if (!_presente)
            {
                return false;
            }

            _presente = false;
            _tempoRestante = TimeSpan.Zero;
            _proximoSorteio = agora + _intervalo;
            _ultimoInstante = agora;
            return true;
        }
    }
}
