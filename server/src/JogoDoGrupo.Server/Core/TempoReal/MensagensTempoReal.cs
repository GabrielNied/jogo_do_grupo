using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>Por que o servidor descartou uma mensagem de tempo real.</summary>
public enum MotivoRecusa
{
    /// <summary>Pacote menor que o cabeçalho.</summary>
    PacoteInvalido,

    /// <summary>A conexão ainda não se autenticou; só <c>AutenticarTempoReal</c> é aceita.</summary>
    NaoAutenticada,

    /// <summary>Nenhuma funcionalidade registrou a faixa que contém o ID.</summary>
    ForaDaFaixa,

    /// <summary>A faixa é de uma funcionalidade, mas ela não trata esse ID.</summary>
    SemManipulador,

    /// <summary>O corpo não é um JSON válido para o tipo da mensagem.</summary>
    CorpoInvalido,

    /// <summary>A conexão mandou pacotes acima do limite por segundo e foi derrubada.</summary>
    LimiteExcedido,
}

/// <summary>
/// Registro das mensagens de tempo real das funcionalidades. Cada funcionalidade reserva a
/// própria faixa (<see cref="Faixa"/>) e registra um manipulador por ID. Mensagem com ID que
/// não está em nenhuma faixa registrada, ou sem manipulador, é recusada.
/// Registros conflitantes lançam exceção, para o erro aparecer ao subir o servidor.
/// </summary>
public sealed class MensagensTempoReal
{
    private readonly Lock _trava = new();
    private readonly List<MessageIdRange> _faixas = [];

    // Cópia na escrita: a leitura (no laço de rede) não precisa de trava.
    private Dictionary<ushort, Func<SessaoTempoReal, byte[], bool>> _manipuladores = [];

    /// <summary>
    /// Reserva a faixa da funcionalidade (ex.: <c>MundoMessageIds.Faixa</c>). A faixa do Core
    /// (<see cref="CoreMessageIds.Faixa"/>) e faixas já reservadas não podem ser usadas.
    /// </summary>
    public FaixaTempoReal Faixa(MessageIdRange faixa)
    {
        if (faixa.Inicio > faixa.Fim)
        {
            throw new ArgumentException($"Faixa {Texto(faixa)} invertida.", nameof(faixa));
        }

        lock (_trava)
        {
            if (SeSobrepoem(faixa, CoreMessageIds.Faixa))
            {
                throw new InvalidOperationException(
                    $"A faixa {Texto(faixa)} invade a faixa do Core ({Texto(CoreMessageIds.Faixa)})."
                );
            }

            var indice = _faixas.FindIndex(f => SeSobrepoem(f, faixa));
            if (indice >= 0)
            {
                throw new InvalidOperationException(
                    $"A faixa {Texto(faixa)} sobrepõe a faixa {Texto(_faixas[indice])}, já registrada."
                );
            }

            _faixas.Add(faixa);
        }

        return new FaixaTempoReal(this, faixa);
    }

    /// <summary>
    /// Permite ao Core registrar mensagens na faixa reservada do Core (<see cref="CoreMessageIds.Faixa"/>).
    /// </summary>
    public FaixaTempoReal FaixaCore()
    {
        lock (_trava)
        {
            if (!_faixas.Contains(CoreMessageIds.Faixa))
            {
                _faixas.Add(CoreMessageIds.Faixa);
            }
        }

        return new FaixaTempoReal(this, CoreMessageIds.Faixa);
    }

    internal void Registrar<T>(
        MessageIdRange faixa,
        ushort id,
        Action<SessaoTempoReal, T> manipulador
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(manipulador);
        if (!faixa.Contem(id))
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                $"O ID {id} está fora da faixa {Texto(faixa)}."
            );
        }

        lock (_trava)
        {
            if (_manipuladores.ContainsKey(id))
            {
                throw new InvalidOperationException($"O ID {id} já tem manipulador.");
            }

            _manipuladores = new Dictionary<ushort, Func<SessaoTempoReal, byte[], bool>>(
                _manipuladores
            )
            {
                [id] = (sessao, pacote) =>
                {
                    var mensagem = ProtocoloTempoReal.LerCorpo<T>(pacote);
                    if (mensagem == null)
                    {
                        return false;
                    }

                    manipulador(sessao, mensagem);
                    return true;
                },
            };
        }
    }

    /// <summary>
    /// Entrega o pacote ao manipulador do ID. Devolve o motivo da recusa, ou <c>null</c> se
    /// entregou. Exceções do manipulador sobem para quem chamou.
    /// </summary>
    internal MotivoRecusa? Despachar(SessaoTempoReal sessao, ushort id, byte[] pacote)
    {
        if (_manipuladores.TryGetValue(id, out var manipulador))
        {
            return manipulador(sessao, pacote) ? null : MotivoRecusa.CorpoInvalido;
        }

        lock (_trava)
        {
            return _faixas.Any(f => f.Contem(id))
                ? MotivoRecusa.SemManipulador
                : MotivoRecusa.ForaDaFaixa;
        }
    }

    private static bool SeSobrepoem(MessageIdRange a, MessageIdRange b) =>
        a.Inicio <= b.Fim && b.Inicio <= a.Fim;

    private static string Texto(MessageIdRange faixa) => $"{faixa.Inicio}–{faixa.Fim}";
}

/// <summary>Faixa reservada por uma funcionalidade; registra os manipuladores dela.</summary>
public sealed class FaixaTempoReal
{
    private readonly MensagensTempoReal _mensagens;

    internal FaixaTempoReal(MensagensTempoReal mensagens, MessageIdRange faixa)
    {
        _mensagens = mensagens;
        Faixa = faixa;
    }

    public MessageIdRange Faixa { get; }

    /// <summary>
    /// Chama <paramref name="manipulador"/> quando chega a mensagem <paramref name="id"/>
    /// (corpo lido como <typeparamref name="T"/>) de uma sessão autenticada. Roda no laço de
    /// rede: seja rápido e não bloqueie (trabalho com o banco vai para uma <c>Task</c>, que
    /// responde depois com <see cref="ServidorTempoReal.Enviar"/>).
    /// </summary>
    public FaixaTempoReal Ao<T>(ushort id, Action<SessaoTempoReal, T> manipulador)
        where T : class
    {
        _mensagens.Registrar(Faixa, id, manipulador);
        return this;
    }
}
