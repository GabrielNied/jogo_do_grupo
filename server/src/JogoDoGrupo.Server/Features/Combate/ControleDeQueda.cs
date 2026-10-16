using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>Como um pedido para começar a levantar terminou.</summary>
public enum ResultadoDeComecar
{
    Iniciado,

    /// <summary>O ajudante está caído (ou levantando).</summary>
    VoceEstaCaido,

    /// <summary>O alvo não está caído (vivo, já levantado, já voltou ou o tempo já acabou).</summary>
    AlvoNaoEstaCaido,

    /// <summary>O ajudante está a mais de 2 m do alvo.</summary>
    LongeDemais,

    /// <summary>Outro aliado (ou o próprio) já está levantando esse alvo.</summary>
    JaEstaSendoLevantado,
}

/// <summary>Como um pedido para voltar à cidade terminou.</summary>
public enum ResultadoDeVoltar
{
    Voltou,
    NaoEstaCaido,
    AindaNaoPodeVoltar,
}

/// <summary>Como o caído deixou o estado.</summary>
public enum TipoDeSaida
{
    /// <summary>Nada saiu.</summary>
    Nenhuma,

    /// <summary>Um aliado completou a interação: volta com 20% da vida.</summary>
    Levantou,

    /// <summary>Fim do tempo ou pedido dele: volta à cidade.</summary>
    Retorno,
}

/// <summary>
/// A saída de um caído. Cada caído tem <b>uma</b> saída por queda: quem a recebe (devolvida por
/// exatamente uma chamada ao controle) é quem deve aplicá-la.
/// </summary>
public readonly record struct SaidaDaQueda(TipoDeSaida Tipo, long PersonagemId, long AjudanteId)
{
    public static SaidaDaQueda Nenhuma => default;

    public bool Houve => Tipo != TipoDeSaida.Nenhuma;
}

/// <summary>
/// O que uma chamada ao controle causou além da resposta: uma interação que foi interrompida
/// (<see cref="AlvoInterrompido"/>, zero se nenhuma) e as saídas que estavam vencidas e foram
/// resolvidas agora (<see cref="Saidas"/>; cada uma é de um caído diferente).
/// </summary>
public readonly record struct EfeitoDaQueda(
    long AlvoInterrompido,
    IReadOnlyList<SaidaDaQueda> Saidas
)
{
    public static EfeitoDaQueda Nenhum => new(0, []);

    internal static EfeitoDaQueda De(long interrompido, params SaidaDaQueda[] saidas) =>
        new(interrompido, saidas.Where(s => s.Houve).ToArray());
}

/// <summary>Retrato de um caído num instante, para montar o <c>EstadoDeQueda</c>.</summary>
public readonly record struct SituacaoDaQueda(
    FaseDeQueda Fase,
    long RestanteMs,
    long AjudanteId,
    int ProgressoMs
);

/// <summary>
/// Quem está caído, quem o está levantando e quando cada coisa acaba. Só decide: não mexe em
/// rede, banco nem relógio (o tempo entra como parâmetro, em ms). Seguro para várias threads: uma
/// trava protege tudo, então eventos simultâneos viram uma ordem só.
/// <para>
/// Regras (docs/features/personagem.md, "Morte e caído"): a contagem de caído <b>para</b>
/// enquanto alguém levanta e volta a correr se interrompida; um ajudante por vez; levantar completa
/// 3 s depois de começar; ao levantar, 2 s de imunidade sem receber dano.
/// Toda chamada primeiro resolve o que já venceu até <c>agoraMs</c>, então nenhuma ordem de
/// chegada produz duas saídas.
/// </para>
/// </summary>
public sealed class ControleDeQueda
{
    private readonly Lock _trava = new();
    private readonly ConfigQueda _config;
    private readonly Dictionary<long, Caido> _caidos = [];
    private readonly Dictionary<long, long> _alvoDoAjudante = [];
    private readonly Dictionary<long, long> _imunidadeAposLevantar = [];

    private readonly Action<long>? _aoSair;

    /// <param name="config">Tempos; nulo usa os padrões de <see cref="RegrasQueda"/>.</param>
    /// <param name="aoSair">
    /// Chamado <b>dentro da trava</b>, no instante em que um caído sai (antes de qualquer outra
    /// thread ver que ele não está mais caído). Deve ser rápido e não chamar o controle.
    /// </param>
    public ControleDeQueda(ConfigQueda? config = null, Action<long>? aoSair = null)
    {
        _config = config ?? new ConfigQueda();
        _aoSair = aoSair;
    }

    /// <summary>Se o personagem está caído (ou sendo levantado) e ainda sem saída.</summary>
    public bool EstaCaido(long personagemId)
    {
        lock (_trava)
        {
            return _caidos.ContainsKey(personagemId);
        }
    }

    /// <summary>Se o personagem está no período de 2 s de imunidade a dano após ser levantado.</summary>
    public bool EstaImuneAposLevantar(long personagemId, long agoraMs)
    {
        lock (_trava)
        {
            if (_imunidadeAposLevantar.TryGetValue(personagemId, out var fim))
            {
                if (agoraMs < fim)
                {
                    return true;
                }

                _imunidadeAposLevantar.Remove(personagemId);
            }

            return false;
        }
    }

    /// <summary>
    /// A vida do personagem chegou a 0. Falso se ele já estava caído. Se ele estava levantando
    /// alguém, essa interação é interrompida (<paramref name="efeito"/>).
    /// </summary>
    public bool Cair(long personagemId, long agoraMs, out EfeitoDaQueda efeito)
    {
        lock (_trava)
        {
            efeito = EfeitoDaQueda.Nenhum;
            if (_caidos.ContainsKey(personagemId))
            {
                return false;
            }

            _imunidadeAposLevantar.Remove(personagemId);
            var interrompido = Interromper(personagemId, agoraMs, out var saida);
            efeito = EfeitoDaQueda.De(interrompido, saida);
            _caidos[personagemId] = new Caido(agoraMs, _config);
            return true;
        }
    }

    /// <summary>
    /// Um aliado pede para começar a levantar <paramref name="alvoId"/>. A ordem dos testes é a
    /// dos códigos: ajudante caído, alvo não caído, longe demais (<paramref name="noAlcance"/>),
    /// já sendo levantado. Se o ajudante levantava outro caído, esse é interrompido.
    /// </summary>
    public ResultadoDeComecar Comecar(
        long ajudanteId,
        long alvoId,
        long agoraMs,
        Func<bool> noAlcance,
        out EfeitoDaQueda efeito
    )
    {
        lock (_trava)
        {
            var saida = Resolver(alvoId, agoraMs);
            efeito = EfeitoDaQueda.De(0, saida);

            if (_caidos.ContainsKey(ajudanteId))
            {
                return ResultadoDeComecar.VoceEstaCaido;
            }

            if (!_caidos.TryGetValue(alvoId, out var alvo) || alvo.RestanteMs(agoraMs) <= 0)
            {
                return ResultadoDeComecar.AlvoNaoEstaCaido;
            }

            if (!noAlcance())
            {
                return ResultadoDeComecar.LongeDemais;
            }

            if (alvo.AjudanteId != 0)
            {
                return ResultadoDeComecar.JaEstaSendoLevantado;
            }

            var interrompido = Interromper(ajudanteId, agoraMs, out var saidaDoOutro);
            efeito = EfeitoDaQueda.De(interrompido, saida, saidaDoOutro);
            alvo.Pausar(agoraMs);
            alvo.AjudanteId = ajudanteId;
            alvo.InicioDaAjudaMs = agoraMs;
            _alvoDoAjudante[ajudanteId] = alvoId;
            return ResultadoDeComecar.Iniciado;
        }
    }

    /// <summary>
    /// A interação do ajudante não vale mais (soltou, afastou-se, levou dano, atacou, esquivou,
    /// caiu ou saiu do jogo). Devolve o caído que ele levantava (zero se nenhum). Se a interação
    /// já tinha completado antes deste instante, quem vale é a saída em <paramref name="saida"/>.
    /// </summary>
    public long Interromper(long ajudanteId, long agoraMs, out SaidaDaQueda saida)
    {
        lock (_trava)
        {
            saida = default;
            if (!_alvoDoAjudante.TryGetValue(ajudanteId, out var alvoId))
            {
                return 0;
            }

            saida = Resolver(alvoId, agoraMs);
            if (!_alvoDoAjudante.ContainsKey(ajudanteId))
            {
                return 0; // Completou (ou o tempo acabou) antes da interrupção.
            }

            _alvoDoAjudante.Remove(ajudanteId);
            if (_caidos.TryGetValue(alvoId, out var alvo))
            {
                alvo.AjudanteId = 0;
                alvo.InicioDaAjudaMs = 0;
                alvo.Retomar(agoraMs);
            }

            return alvoId;
        }
    }

    /// <summary>
    /// O caído pede para voltar à cidade. Vale depois de <see cref="ConfigQueda.MinimoParaVoltarMs"/>
    /// caído. Se a interação do ajudante já tinha completado, a saída é a de levantar e o pedido
    /// é <see cref="ResultadoDeVoltar.NaoEstaCaido"/>.
    /// </summary>
    public ResultadoDeVoltar Voltar(long personagemId, long agoraMs, out SaidaDaQueda saida)
    {
        lock (_trava)
        {
            saida = Resolver(personagemId, agoraMs);
            if (!_caidos.TryGetValue(personagemId, out var caido))
            {
                return ResultadoDeVoltar.NaoEstaCaido;
            }

            if (agoraMs - caido.CaiuEmMs < _config.MinimoParaVoltarMs)
            {
                return ResultadoDeVoltar.AindaNaoPodeVoltar;
            }

            saida = Encerrar(personagemId, caido, TipoDeSaida.Retorno, agoraMs);
            return ResultadoDeVoltar.Voltou;
        }
    }

    /// <summary>
    /// Resolve o que venceu até <paramref name="agoraMs"/>: a interação completa ou o tempo
    /// acaba. Devolve a saída (e só uma vez por queda) ou <see cref="SaidaDaQueda.Nenhuma"/>.
    /// </summary>
    public SaidaDaQueda Resolver(long personagemId, long agoraMs)
    {
        lock (_trava)
        {
            if (!_caidos.TryGetValue(personagemId, out var caido))
            {
                return SaidaDaQueda.Nenhuma;
            }

            if (
                caido.AjudanteId != 0
                && agoraMs >= caido.InicioDaAjudaMs + _config.TempoParaLevantarMs
            )
            {
                return Encerrar(personagemId, caido, TipoDeSaida.Levantou, agoraMs);
            }

            if (caido.AjudanteId == 0 && caido.RestanteMs(agoraMs) <= 0)
            {
                return Encerrar(personagemId, caido, TipoDeSaida.Retorno, agoraMs);
            }

            return SaidaDaQueda.Nenhuma;
        }
    }

    /// <summary>Quem está levantando o caído (zero se ninguém ou se ele não está caído).</summary>
    public long AjudanteDe(long personagemId)
    {
        lock (_trava)
        {
            return _caidos.TryGetValue(personagemId, out var caido) ? caido.AjudanteId : 0;
        }
    }

    /// <summary>O retrato do caído agora. Fase <c>Vivo</c> se ele não está caído.</summary>
    public SituacaoDaQueda Situacao(long personagemId, long agoraMs)
    {
        lock (_trava)
        {
            if (!_caidos.TryGetValue(personagemId, out var caido))
            {
                return new SituacaoDaQueda(FaseDeQueda.Vivo, 0, 0, 0);
            }

            var restante = caido.RestanteMs(agoraMs);
            if (caido.AjudanteId == 0)
            {
                return new SituacaoDaQueda(FaseDeQueda.Caido, restante, 0, 0);
            }

            var progresso = (int)
                Math.Clamp(agoraMs - caido.InicioDaAjudaMs, 0, _config.TempoParaLevantarMs);
            return new SituacaoDaQueda(
                FaseDeQueda.Levantando,
                restante,
                caido.AjudanteId,
                progresso
            );
        }
    }

    /// <summary>
    /// Em quantos ms o caído tem algo a resolver (a interação completa ou o tempo acaba), ou
    /// -1 se ele não está caído. Serve para o laço dormir até a hora certa.
    /// </summary>
    public long MsAteOProximoEvento(long personagemId, long agoraMs)
    {
        lock (_trava)
        {
            if (!_caidos.TryGetValue(personagemId, out var caido))
            {
                return -1;
            }

            if (caido.AjudanteId != 0)
            {
                return Math.Max(0, (caido.InicioDaAjudaMs + _config.TempoParaLevantarMs) - agoraMs);
            }

            return Math.Max(0, caido.RestanteMs(agoraMs));
        }
    }

    // Chamar com a trava.
    private SaidaDaQueda Encerrar(long personagemId, Caido caido, TipoDeSaida tipo, long agoraMs)
    {
        var ajudante = caido.AjudanteId;
        _aoSair?.Invoke(personagemId);
        _caidos.Remove(personagemId);
        if (ajudante != 0)
        {
            _alvoDoAjudante.Remove(ajudante);
        }

        if (tipo == TipoDeSaida.Levantou)
        {
            _imunidadeAposLevantar[personagemId] = agoraMs + _config.ImunidadeAposLevantarMs;
        }

        return new SaidaDaQueda(tipo, personagemId, ajudante);
    }

    private sealed class Caido
    {
        public long CaiuEmMs { get; }
        public long TempoRestanteMs { get; private set; }
        public long UltimoInicioContagemMs { get; private set; }
        public long AjudanteId { get; set; }
        public long InicioDaAjudaMs { get; set; }

        public Caido(long caiuEmMs, ConfigQueda config)
        {
            CaiuEmMs = caiuEmMs;
            TempoRestanteMs = config.TempoCaidoMs;
            UltimoInicioContagemMs = caiuEmMs;
        }

        public long RestanteMs(long agoraMs)
        {
            if (AjudanteId != 0)
            {
                return TempoRestanteMs;
            }

            var decorrido = Math.Max(0, agoraMs - UltimoInicioContagemMs);
            return Math.Max(0, TempoRestanteMs - decorrido);
        }

        public void Pausar(long agoraMs)
        {
            TempoRestanteMs = RestanteMs(agoraMs);
            UltimoInicioContagemMs = agoraMs;
        }

        public void Retomar(long agoraMs)
        {
            UltimoInicioContagemMs = agoraMs;
        }
    }
}
