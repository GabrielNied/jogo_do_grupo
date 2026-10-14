namespace JogoDoGrupo.Server.Core.Eventos;

/// <summary>
/// Eventos entre funcionalidades do servidor: quem publica não conhece quem escuta (ADR 0016).
/// A entrega é síncrona, na ordem em que os assinantes se inscreveram, e a fila de eventos
/// mantém a sequência mesmo quando diferentes threads publicam ao mesmo tempo. Assine no
/// <c>Inicializar</c> do módulo e publique do laço de rede; o manipulador não pode bloquear.
/// </summary>
public sealed class EventosDoServidor
{
    private readonly Lock _trava = new();
    private readonly Dictionary<Type, List<Delegate>> _assinantes = [];

    /// <summary>
    /// Fila da publicação em andamento nesta thread. É <see cref="ThreadLocal{T}"/> e não
    /// <c>AsyncLocal</c> de propósito: uma Task ou timer criado dentro de um manipulador
    /// herdaria a fila já esvaziada, e o evento que publicasse depois nunca seria entregue.
    /// </summary>
    private readonly ThreadLocal<Queue<Action>?> _filaAtual = new();

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
    /// assinantes, não faz nada. Eventos publicados de dentro de um manipulador entram na fila da
    /// publicação corrente e só são entregues depois que o evento atual chegou a todos, então todos
    /// veem os eventos na mesma ordem. Falhas dos assinantes são logadas e não interrompem o
    /// publicador nem os outros.
    /// </summary>
    public void Publicar<T>(T evento)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(evento);

        lock (_trava)
        {
            var fila = _filaAtual.Value;
            if (fila != null)
            {
                fila.Enqueue(() => Entregar(evento));
                return;
            }

            fila = new Queue<Action>();
            _filaAtual.Value = fila;
            try
            {
                fila.Enqueue(() => Entregar(evento));
                while (fila.Count > 0)
                {
                    fila.Dequeue()();
                }
            }
            finally
            {
                _filaAtual.Value = null;
            }
        }
    }

    private void Entregar<T>(T evento)
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
                Console.Error.WriteLine($"Assinante de {typeof(T).Name} falhou: {erro}");
            }
        }
    }
}
