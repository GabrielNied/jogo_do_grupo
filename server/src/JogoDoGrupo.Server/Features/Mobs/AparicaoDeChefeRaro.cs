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
    private DateTimeOffset _proximoSorteio;
    private bool _presente;

    /// <param name="definicao">Ficha de um <see cref="CategoriaDeCriatura.ChefeRaro"/>.</param>
    /// <param name="sorteador">Quem sorteia a aparição.</param>
    /// <param name="inicio">Momento do primeiro sorteio (o servidor subindo).</param>
    public AparicaoDeChefeRaro(
        DefinicaoDeCriatura definicao,
        ISorteadorDeCriaturas sorteador,
        DateTimeOffset inicio
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
        _proximoSorteio = inicio;
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

    /// <summary>
    /// Avança o controle até <paramref name="agora"/>. Devolve <c>true</c> só na chamada em
    /// que o chefe apareceu. Faz no máximo um sorteio por chamada: intervalos que passaram
    /// sem ninguém chamar não viram sorteios acumulados.
    /// </summary>
    public bool Atualizar(DateTimeOffset agora)
    {
        lock (_trava)
        {
            if (_presente || agora < _proximoSorteio)
            {
                return false;
            }

            _proximoSorteio = agora + _intervalo;
            _presente = _sorteador.Aparece(Definicao);
            return _presente;
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
            _proximoSorteio = agora + _intervalo;
            return true;
        }
    }
}
