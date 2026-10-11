namespace JogoDoGrupo.Server.Core.Eventos;

/// <summary>
/// Eventos entre funcionalidades do servidor: quem publica não conhece quem escuta (ADR 0016).
/// A entrega é síncrona, na thread de quem publica e na ordem em que os assinantes se
/// inscreveram. Assine no <c>Inicializar</c> do módulo e publique do laço de rede; o
/// manipulador não pode bloquear.
/// </summary>
public sealed class EventosDoServidor
{
    private readonly Lock _trava = new();
    private readonly Dictionary<Type, List<Delegate>> _assinantes = [];

    /// <summary>Publicados de dentro de um manipulador, à espera da vez (por thread).</summary>
    private readonly ThreadLocal<Queue<Action<List<Exception>>>?> _pendentes = new();

    /// <summary>
    /// Inscreve um manipulador para o tipo exato <typeparamref name="T"/> (tipos derivados não
    /// contam). O mesmo manipulador inscrito duas vezes é chamado duas vezes.
    /// </summary>
    public void Assinar<T>(Action<T> manipulador)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(manipulador);
        lock (_trava)
        {
            if (!_assinantes.TryGetValue(typeof(T), out var lista))
            {
                lista = [];
                _assinantes[typeof(T)] = lista;
            }

            lista.Add(manipulador);
        }
    }

    /// <summary>
    /// Entrega o evento a todos os assinantes de <typeparamref name="T"/> antes de voltar. Sem
    /// assinantes, não faz nada. Um evento publicado de dentro de um manipulador só é entregue
    /// depois que o evento atual chegou a todos, então todos veem os eventos na mesma ordem.
    /// Se algum assinante lançar exceção, os outros recebem o evento mesmo assim e, no fim, as
    /// falhas saem juntas numa <see cref="AggregateException"/>.
    /// </summary>
    public void Publicar<T>(T evento)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(evento);

        var pendentes = _pendentes.Value;
        if (pendentes != null)
        {
            pendentes.Enqueue(erros => Entregar(evento, erros));
            return;
        }

        pendentes = new Queue<Action<List<Exception>>>();
        _pendentes.Value = pendentes;
        var falhas = new List<Exception>();
        try
        {
            Entregar(evento, falhas);
            while (pendentes.TryDequeue(out var proximo))
            {
                proximo(falhas);
            }
        }
        finally
        {
            _pendentes.Value = null;
        }

        if (falhas.Count > 0)
        {
            throw new AggregateException("Assinantes de eventos do servidor falharam.", falhas);
        }
    }

    private void Entregar<T>(T evento, List<Exception> falhas)
        where T : class
    {
        Delegate[] assinantes;
        lock (_trava)
        {
            assinantes = _assinantes.TryGetValue(typeof(T), out var lista) ? [.. lista] : [];
        }

        foreach (var assinante in assinantes)
        {
            try
            {
                ((Action<T>)assinante)(evento);
            }
            catch (Exception erro)
            {
                falhas.Add(erro);
            }
        }
    }
}
