using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>Velocidades de um personagem (m/s) e a sequência da mudança que as trouxe.</summary>
public readonly record struct VelocidadesNaCidade(float Andar, float Correr, int Sequencia)
{
    public static readonly VelocidadesNaCidade Padrao = new(
        RegrasVelocidade.AndarPadrao,
        RegrasVelocidade.CorrerPadrao,
        0
    );

    /// <summary>A maior das duas: o limite do deslocamento.</summary>
    public float Maxima => RegrasVelocidade.Maxima(Andar, Correr);
}

/// <summary>Um avatar presente na cidade, com a sessão (conexão) dona dele.</summary>
public sealed record AvatarNaCidade<TSessao>(
    long PersonagemId,
    TSessao Sessao,
    PosicaoNaCidade Posicao,
    int SequenciaReplica = 0,
    long UltimoMs = 0
);

/// <summary>Resultado de pedir movimento.</summary>
public enum ResultadoMovimento
{
    /// <summary>O jogador não está na cidade.</summary>
    ForaDaCidade,
    Aceito,

    /// <summary>Aceito, mas além do orçamento (dentro da tolerância): contou como suspeita.</summary>
    AceitoComSuspeita,

    /// <summary>
    /// Impossível: a posição do servidor ficou, e o client precisa receber
    /// <c>PosicaoForcada</c> (<see cref="CidadeDoMundo{TSessao}.PosicaoForcadaPendente"/>).
    /// </summary>
    Recusado,

    /// <summary>Ignorado: há uma posição forçada esperando a confirmação do client.</summary>
    AguardandoConfirmacao,
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
        TSessao sessao,
        PosicaoNaCidade? nascimento = null
    )
    {
        lock (_trava)
        {
            var novo = !_avatares.TryGetValue(personagemId, out var entrada);
            entrada = novo
                ? new Entrada(
                    sessao,
                    nascimento ?? ValidadorDeMovimento.Nascimento,
                    relogioMs(),
                    VelocidadesNaCidade.Padrao,
                    ValidadorDeMovimento.TetoDoOrcamento(VelocidadesNaCidade.Padrao.Maxima)
                )
                : entrada! with
                {
                    Sessao = sessao,
                    // Reconexão: o client pode ter andado sem conexão; não vira posição forçada.
                    Orcamento =
                        LimiteDeVelocidade(entrada)
                        * ValidadorDeMovimento.OrcamentoAoReconectarSegundos,
                    UltimoMs = relogioMs(),
                    ForcadaPendente = null,
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
    /// Aplica a posição que o client informou. Número não finito é ignorado e não move
    /// ninguém. Não envia posição forçada: isso fica em <see cref="ForcarPosicao"/>.
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
            if (entrada.ForcadaPendente != null)
            {
                // Movimento de antes do teleporte: só volta a valer depois da confirmação.
                return (ResultadoMovimento.AguardandoConfirmacao, Converter(personagemId, entrada));
            }

            var decorrido = agora - entrada.UltimoMs;
            var limite = LimiteDeVelocidade(entrada);
            var orcamento = ValidadorDeMovimento.Recarregar(entrada.Orcamento, decorrido, limite);
            var suspeitas = Math.Max(
                0,
                entrada.Suspeitas
                    - (
                        Math.Max(0, decorrido)
                        / 1000.0
                        * ValidadorDeMovimento.SuspeitasEsvaziadasPorSegundo
                    )
            );

            // Janela de deslocamento (ex.: esquiva aceita): o passo vale pelas regras dela e não
            // gasta o orçamento.
            var janela = entrada.Janela is { } aberta && agora <= aberta.AteMs ? aberta : null;
            if (
                janela != null
                && ValidadorDeMovimento.PermitidoNaJanela(
                    janela.Origem,
                    entrada.Posicao,
                    desejada,
                    decorrido,
                    janela.DistanciaMaxima,
                    janela.Velocidade
                )
            )
            {
                entrada = entrada with
                {
                    Posicao = desejada,
                    UltimoMs = agora,
                    Janela = janela,
                    Orcamento = orcamento,
                    Suspeitas = suspeitas,
                    SequenciaReplica = entrada.SequenciaReplica + 1,
                };
                _avatares[personagemId] = entrada;
                return (ResultadoMovimento.Aceito, Converter(personagemId, entrada));
            }

            // O jogador se move livremente e só informa. Número não finito é ignorado, sem
            // puxar o client. PosicaoForcada fica para teleporte explícito (ForcarPosicao).
            if (!PosicaoFinita(desejada))
            {
                return (ResultadoMovimento.Recusado, Converter(personagemId, entrada));
            }

            // Teto de velocidade (ex.: preparo do golpe): longe demais de onde o teto começou é
            // recusado e conta como suspeita; o servidor fica com a última posição aceita.
            var teto = entrada.Teto is { } ativo && agora <= ativo.AteMs ? ativo : null;
            if (
                teto != null
                && ValidadorDeMovimento.DistanciaHorizontal(teto.Origem, desejada)
                    > ValidadorDeMovimento.AlcanceNoTeto(limite * teto.Fator, agora - teto.InicioMs)
            )
            {
                _avatares[personagemId] = entrada with
                {
                    TotalDeSuspeitas = entrada.TotalDeSuspeitas + 1,
                };
                return (ResultadoMovimento.Recusado, Converter(personagemId, entrada));
            }

            var julgamento = ValidadorDeMovimento.Julgar(entrada.Posicao, desejada, orcamento);
            if (julgamento != JulgamentoDoPasso.Normal)
            {
                suspeitas += 1;
            }

            var andou = ValidadorDeMovimento.DistanciaHorizontal(entrada.Posicao, desejada);
            var suspeito = julgamento != JulgamentoDoPasso.Normal;
            entrada = entrada with
            {
                Posicao = desejada,
                UltimoMs = agora,
                Janela = janela,
                Teto = teto,
                Orcamento = Math.Max(0, orcamento - andou),
                Suspeitas = suspeitas,
                TotalDeSuspeitas = entrada.TotalDeSuspeitas + (suspeito ? 1 : 0),
                SequenciaReplica = entrada.SequenciaReplica + 1,
            };
            _avatares[personagemId] = entrada;
            return (
                suspeito ? ResultadoMovimento.AceitoComSuspeita : ResultadoMovimento.Aceito,
                Converter(personagemId, entrada)
            );
        }
    }

    /// <summary>
    /// Sequência da posição forçada que espera a confirmação do client, ou <c>null</c>.
    /// </summary>
    public int? PosicaoForcadaPendente(long personagemId)
    {
        lock (_trava)
        {
            return _avatares.TryGetValue(personagemId, out var entrada)
                ? entrada.ForcadaPendente
                : null;
        }
    }

    /// <summary>
    /// Manda o personagem para <paramref name="posicao"/> (teleporte, renascer, empurrão).
    /// Devolve a sequência para a <c>PosicaoForcada</c>, ou <c>null</c> se ele não está na
    /// cidade. Até a confirmação, o movimento do client é ignorado.
    /// </summary>
    public int? ForcarPosicao(long personagemId, PosicaoNaCidade posicao)
    {
        lock (_trava)
        {
            if (!_avatares.TryGetValue(personagemId, out var entrada))
            {
                return null;
            }

            var sequencia = entrada.SequenciaForcada + 1;
            _avatares[personagemId] = entrada with
            {
                Posicao = posicao,
                SequenciaForcada = sequencia,
                ForcadaPendente = sequencia,
                Janela = null,
                SequenciaReplica = entrada.SequenciaReplica + 1,
                UltimoMs = relogioMs(),
            };
            return sequencia;
        }
    }

    /// <summary>
    /// O client aplicou a posição forçada <paramref name="sequencia"/>: o movimento volta a
    /// valer, com o orçamento cheio. Falso (e nada muda) se a sequência não é a pendente.
    /// </summary>
    public bool ConfirmarPosicaoForcada(long personagemId, TSessao sessao, int sequencia)
    {
        lock (_trava)
        {
            if (
                !_avatares.TryGetValue(personagemId, out var entrada)
                || !ReferenceEquals(entrada.Sessao, sessao)
                || entrada.ForcadaPendente != sequencia
            )
            {
                return false;
            }

            _avatares[personagemId] = entrada with
            {
                ForcadaPendente = null,
                UltimoMs = relogioMs(),
                Orcamento = ValidadorDeMovimento.TetoDoOrcamento(LimiteDeVelocidade(entrada)),
            };
            return true;
        }
    }

    /// <summary>
    /// Quantos passos suspeitos o personagem teve nesta presença (para o GM e o log; <c>0</c> se
    /// ele não está na cidade).
    /// </summary>
    public int TotalDeSuspeitas(long personagemId)
    {
        lock (_trava)
        {
            return _avatares.TryGetValue(personagemId, out var entrada)
                ? entrada.TotalDeSuspeitas
                : 0;
        }
    }

    /// <summary>
    /// As velocidades que o client deve usar agora (a pendente, se houver, senão a confirmada),
    /// ou <c>null</c> se o personagem não está na cidade.
    /// </summary>
    public VelocidadesNaCidade? Velocidades(long personagemId)
    {
        lock (_trava)
        {
            return _avatares.TryGetValue(personagemId, out var entrada)
                ? entrada.Pendente ?? entrada.Confirmadas
                : null;
        }
    }

    /// <summary>
    /// Muda as velocidades do personagem. A nova fica pendente até o client confirmar com a
    /// sequência devolvida; enquanto isso, vale a maior entre a antiga e a nova (o client pode
    /// ainda não ter recebido o aviso). <c>null</c> se a sessão não está na cidade.
    /// </summary>
    public VelocidadesNaCidade? MudarVelocidades(
        long personagemId,
        TSessao sessao,
        float andar,
        float correr
    )
    {
        lock (_trava)
        {
            if (
                !_avatares.TryGetValue(personagemId, out var entrada)
                || !ReferenceEquals(entrada.Sessao, sessao)
            )
            {
                return null;
            }

            var ultima = (entrada.Pendente ?? entrada.Confirmadas).Sequencia;
            var nova = new VelocidadesNaCidade(andar, correr, ultima + 1);
            _avatares[personagemId] = entrada with { Pendente = nova };
            return nova;
        }
    }

    /// <summary>
    /// O client aplicou a mudança <paramref name="sequencia"/>. Falso (e nada muda) se não há
    /// mudança pendente com essa sequência.
    /// </summary>
    public bool ConfirmarVelocidades(long personagemId, TSessao sessao, int sequencia)
    {
        lock (_trava)
        {
            if (
                !_avatares.TryGetValue(personagemId, out var entrada)
                || !ReferenceEquals(entrada.Sessao, sessao)
                || entrada.Pendente is not { } pendente
                || pendente.Sequencia != sequencia
            )
            {
                return false;
            }

            _avatares[personagemId] = entrada with { Confirmadas = pendente, Pendente = null };
            return true;
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
    /// Limita a velocidade do personagem a <paramref name="fator"/> (0 a 1) da que ele tem, por
    /// <paramref name="duracaoSegundos"/> (veja <c>ILimiteDeVelocidadeDoMundo</c>). Um pedido
    /// novo substitui o anterior. Falso se ele não está na cidade ou se os números não servem.
    /// </summary>
    public bool LimitarVelocidade(long personagemId, double fator, double duracaoSegundos)
    {
        if (
            !double.IsFinite(fator)
            || !double.IsFinite(duracaoSegundos)
            || fator < 0
            || fator > 1
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

            // Fator 1 é "sem teto" (a guarda solta assim): volta o orçamento normal.
            var agora = relogioMs();
            var ateMs =
                agora
                + (long)Math.Ceiling(duracaoSegundos * 1000.0)
                + ValidadorDeMovimento.FolgaDaJanelaMs;
            _avatares[personagemId] = entrada with
            {
                Teto =
                    fator >= 1 ? null : new TetoDeVelocidade(entrada.Posicao, agora, ateMs, fator),
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
        new(id, e.Sessao, e.Posicao, e.SequenciaReplica, e.UltimoMs);

    // Até o client confirmar, vale a maior entre a confirmada e a pendente.
    private static bool PosicaoFinita(PosicaoNaCidade posicao) =>
        float.IsFinite(posicao.X)
        && float.IsFinite(posicao.Y)
        && float.IsFinite(posicao.Z)
        && float.IsFinite(posicao.RotacaoY);

    private static float LimiteDeVelocidade(Entrada e) =>
        e.Pendente is { } pendente
            ? Math.Max(e.Confirmadas.Maxima, pendente.Maxima)
            : e.Confirmadas.Maxima;

    private sealed record Entrada(
        TSessao Sessao,
        PosicaoNaCidade Posicao,
        long UltimoMs,
        VelocidadesNaCidade Confirmadas,
        double Orcamento,
        VelocidadesNaCidade? Pendente = null,
        JanelaDeDeslocamento? Janela = null,
        TetoDeVelocidade? Teto = null,
        double Suspeitas = 0,
        int TotalDeSuspeitas = 0,
        int SequenciaForcada = 0,
        int? ForcadaPendente = null,
        int SequenciaReplica = 0
    );

    // Onde o personagem estava quando a janela abriu, até quando ela vale e os limites dela.
    private sealed record JanelaDeDeslocamento(
        PosicaoNaCidade Origem,
        long AteMs,
        double DistanciaMaxima,
        double Velocidade
    );

    // Onde o personagem estava quando o teto começou, quando começou, até quando vale e a
    // fração da velocidade que sobra.
    private sealed record TetoDeVelocidade(
        PosicaoNaCidade Origem,
        long InicioMs,
        long AteMs,
        double Fator
    );
}
