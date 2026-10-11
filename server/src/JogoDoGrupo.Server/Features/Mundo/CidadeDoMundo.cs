namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>Um avatar presente na cidade, com a sessão (conexão) dona dele.</summary>
public sealed record AvatarNaCidade<TSessao>(
    long PersonagemId,
    TSessao Sessao,
    PosicaoNaCidade Posicao
);

/// <summary>Resultado de pedir movimento.</summary>
public enum ResultadoMovimento
{
    /// <summary>O jogador não está na cidade.</summary>
    ForaDaCidade,
    Aceito,
    Recusado,
}

/// <summary>
/// Estado da (única) cidade: quem está nela e onde. <typeparamref name="TSessao"/> é a conexão
/// (no servidor, <c>SessaoTempoReal</c>), comparada por referência. Thread-safe: manipuladores
/// rodam no laço de rede, mas <c>SessaoEncerrada</c> e consultas podem vir de outras threads.
/// </summary>
public sealed class CidadeDoMundo<TSessao>(Func<long> relogioMs)
    where TSessao : class
{
    private readonly Lock _trava = new();
    private readonly Dictionary<long, Entrada> _avatares = [];

    public CidadeDoMundo()
        : this(() => Environment.TickCount64) { }

    public int Quantidade
    {
        get
        {
            lock (_trava)
            {
                return _avatares.Count;
            }
        }
    }

    /// <summary>
    /// Põe o personagem no ponto de nascimento. Se ele já está na cidade (pedido repetido, ou
    /// sessão nova no lugar da antiga), mantém a posição e <c>Novo</c> é falso. Devolve o avatar
    /// e os demais presentes.
    /// </summary>
    public (bool Novo, AvatarNaCidade<TSessao> Eu, AvatarNaCidade<TSessao>[] Outros) Entrar(
        long personagemId,
        TSessao sessao
    )
    {
        lock (_trava)
        {
            var novo = !_avatares.TryGetValue(personagemId, out var entrada);
            entrada = novo
                ? new Entrada(sessao, ValidadorDeMovimento.Nascimento, relogioMs())
                : entrada! with
                {
                    Sessao = sessao,
                };
            _avatares[personagemId] = entrada;
            var outros = _avatares
                .Where(p => p.Key != personagemId)
                .Select(p => Converter(p.Key, p.Value))
                .ToArray();
            return (novo, Converter(personagemId, entrada), outros);
        }
    }

    /// <summary>
    /// Aplica a intenção de movimento. Devolve a posição autoritativa (a nova, se aceita; a
    /// antiga, se recusada).
    /// </summary>
    public (ResultadoMovimento Resultado, AvatarNaCidade<TSessao>? Avatar) Mover(
        long personagemId,
        TSessao sessao,
        PosicaoNaCidade desejada
    )
    {
        lock (_trava)
        {
            if (
                !_avatares.TryGetValue(personagemId, out var entrada)
                || !ReferenceEquals(entrada.Sessao, sessao)
            )
            {
                return (ResultadoMovimento.ForaDaCidade, null);
            }

            var agora = relogioMs();
            var decorrido = agora - entrada.UltimoMs;
            var janela = entrada.Janela is { } aberta && agora <= aberta.AteMs ? aberta : null;
            var permitido =
                janela != null
                    ? ValidadorDeMovimento.PermitidoNaJanela(
                        janela.Origem,
                        entrada.Posicao,
                        desejada,
                        decorrido,
                        janela.DistanciaMaxima,
                        janela.Velocidade
                    )
                    : ValidadorDeMovimento.Permitido(entrada.Posicao, desejada, decorrido);
            if (!permitido)
            {
                return (ResultadoMovimento.Recusado, Converter(personagemId, entrada));
            }

            entrada = entrada with { Posicao = desejada, UltimoMs = agora, Janela = janela };
            _avatares[personagemId] = entrada;
            return (ResultadoMovimento.Aceito, Converter(personagemId, entrada));
        }
    }

    /// <summary>
    /// Abre uma janela de deslocamento para o personagem (veja <c>IDeslocamentosDoMundo</c>): até
    /// <paramref name="distanciaMetros"/> de onde ele está agora, em
    /// <paramref name="duracaoSegundos"/>. Falso se ele não está na cidade ou se os números não
    /// servem (zero, negativos ou não finitos).
    /// </summary>
    public bool AbrirJanela(long personagemId, double distanciaMetros, double duracaoSegundos)
    {
        if (
            !double.IsFinite(distanciaMetros)
            || !double.IsFinite(duracaoSegundos)
            || distanciaMetros <= 0
            || duracaoSegundos <= 0
        )
        {
            return false;
        }

        lock (_trava)
        {
            if (!_avatares.TryGetValue(personagemId, out var entrada))
            {
                return false;
            }

            var ateMs =
                relogioMs()
                + (long)Math.Ceiling(duracaoSegundos * 1000.0)
                + ValidadorDeMovimento.FolgaDaJanelaMs;
            _avatares[personagemId] = entrada with
            {
                Janela = new JanelaDeDeslocamento(
                    entrada.Posicao,
                    ateMs,
                    distanciaMetros,
                    distanciaMetros / duracaoSegundos
                ),
            };
            return true;
        }
    }

    /// <summary>
    /// Tira o avatar. Falso se a sessão não estava na cidade (ou já foi substituída por outra
    /// do mesmo personagem).
    /// </summary>
    public bool Sair(long personagemId, TSessao sessao)
    {
        lock (_trava)
        {
            if (
                _avatares.TryGetValue(personagemId, out var entrada)
                && ReferenceEquals(entrada.Sessao, sessao)
            )
            {
                _avatares.Remove(personagemId);
                return true;
            }

            return false;
        }
    }

    /// <summary>O avatar do personagem, ou <c>null</c> se ele não está na cidade.</summary>
    public AvatarNaCidade<TSessao>? Obter(long personagemId)
    {
        lock (_trava)
        {
            return _avatares.TryGetValue(personagemId, out var entrada)
                ? Converter(personagemId, entrada)
                : null;
        }
    }

    /// <summary>Quem está na cidade agora.</summary>
    public AvatarNaCidade<TSessao>[] Presentes()
    {
        lock (_trava)
        {
            return _avatares.Select(p => Converter(p.Key, p.Value)).ToArray();
        }
    }

    private static AvatarNaCidade<TSessao> Converter(long id, Entrada e) =>
        new(id, e.Sessao, e.Posicao);

    private sealed record Entrada(
        TSessao Sessao,
        PosicaoNaCidade Posicao,
        long UltimoMs,
        JanelaDeDeslocamento? Janela = null
    );

    // Onde o personagem estava quando a janela abriu, até quando ela vale e os limites dela.
    private sealed record JanelaDeDeslocamento(
        PosicaoNaCidade Origem,
        long AteMs,
        double DistanciaMaxima,
        double Velocidade
    );
}
