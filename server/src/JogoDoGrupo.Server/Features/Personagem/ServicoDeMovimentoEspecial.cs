using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>
/// Natação, queda e escalada no servidor (#349). A cada passo lê a posição que o Mundo aceitou
/// (nunca a que o client diz) e decide: quem nada gasta stamina e fica sem atacar; quem cai de
/// alto leva dano autoritativo (altura medida pelo servidor, água funda amortece); quem escala só
/// pode estar dentro de uma via marcada. O dano e a stamina passam por Combate (dono da vida e
/// dos recursos) pelos contratos. Instâncias ainda não têm natação, queda nem escalada: o
/// serviço só age na cidade e no mundo aberto (docs/features/personagem.md).
/// </summary>
public sealed class ServicoDeMovimentoEspecial : BackgroundService, IEstadoDeMovimento
{
    /// <summary>Intervalo entre passos (ms). Tem de ser curto para ver a queda em amostras seguidas.</summary>
    public const int IntervaloDoPassoMs = 100;

    /// <summary>Deslocamento horizontal (m) de um passo acima do qual é teleporte ou volta à cidade, não queda.</summary>
    public const float SaltoQueReiniciaAMedicao = 12f;

    private readonly object _trava = new();
    private readonly Dictionary<long, Estado> _estados = [];
    private readonly IPosicoesDoMundo _posicoes;
    private readonly Func<IReadOnlyCollection<long>> _online;
    private readonly Action<long, EstadoDoMovimento> _enviarEstado;
    private readonly Action<long, DanoDeQueda> _enviarDano;
    private readonly Action<long, RecursosMudaram> _enviarRecursos;
    private readonly ICobrancaDeRecursos? _cobranca;
    private readonly IDanoAoPersonagem? _dano;
    private readonly ILimiteDeVelocidadeDoMundo? _limiteDeVelocidade;
    private readonly IEstadoDeQueda? _queda;
    private readonly IReadOnlyList<VolumeMarcado> _volumes;
    private readonly Func<long> _relogioMs;
    private readonly ILogger<ServicoDeMovimentoEspecial>? _log;

    public ServicoDeMovimentoEspecial(
        IPosicoesDoMundo posicoes,
        Func<IReadOnlyCollection<long>> online,
        Action<long, EstadoDoMovimento> enviarEstado,
        Action<long, DanoDeQueda> enviarDano,
        Action<long, RecursosMudaram> enviarRecursos,
        ICobrancaDeRecursos? cobranca = null,
        IDanoAoPersonagem? dano = null,
        ILimiteDeVelocidadeDoMundo? limiteDeVelocidade = null,
        IEstadoDeQueda? queda = null,
        IReadOnlyList<VolumeMarcado>? volumes = null,
        Func<long>? relogioMs = null,
        ILogger<ServicoDeMovimentoEspecial>? log = null
    )
    {
        _posicoes = posicoes;
        _online = online;
        _enviarEstado = enviarEstado;
        _enviarDano = enviarDano;
        _enviarRecursos = enviarRecursos;
        _cobranca = cobranca;
        _dano = dano;
        _limiteDeVelocidade = limiteDeVelocidade;
        _queda = queda;
        _volumes = volumes ?? VolumesMarcados.Todos;
        _relogioMs = relogioMs ?? (() => Environment.TickCount64);
        _log = log;
    }

    public bool EstaNadando(long personagemId)
    {
        lock (_trava)
        {
            return _estados.TryGetValue(personagemId, out var e) && e.Nadando;
        }
    }

    public bool EstaEscalando(long personagemId)
    {
        lock (_trava)
        {
            return _estados.TryGetValue(personagemId, out var e) && e.Escalando;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(IntervaloDoPassoMs));
        try
        {
            while (await timer.WaitForNextTickAsync(parada))
            {
                try
                {
                    await PassoAsync();
                }
                catch (Exception erro)
                {
                    _log?.LogError(erro, "Erro ao processar o movimento especial.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>Trata o pedido de começar ou parar de escalar (laço de rede).</summary>
    public void AoEscalar(long personagemId, IntencaoDeEscalada pedido) =>
        _ = AoEscalarAsync(personagemId, pedido);

    /// <summary>
    /// Começar a escalar só vale dentro de uma via marcada, sem nadar nem estar caído e com
    /// stamina para o primeiro intervalo. Parar vale sempre. A resposta é o estado novo.
    /// </summary>
    public async Task AoEscalarAsync(long personagemId, IntencaoDeEscalada pedido)
    {
        try
        {
            if (pedido == null || !pedido.Escalar)
            {
                bool mudou;
                lock (_trava)
                {
                    mudou = _estados.TryGetValue(personagemId, out var atual) && atual.Escalando;
                    if (mudou)
                    {
                        _estados[personagemId].Escalando = false;
                    }
                }

                if (mudou)
                {
                    NotificarSeMudou(personagemId);
                }

                return;
            }

            var aceita = await PodeComecarAEscalarAsync(personagemId);
            lock (_trava)
            {
                var estado = ObterEstado(personagemId);
                estado.Escalando = aceita;
                estado.SemStamina = false;
                estado.ProximaCobrancaMs =
                    _relogioMs() + ConfigMovimentoEspecial.IntervaloDeCobrancaMs;
                estado.UltimoEnviado = null; // Responde sempre, mesmo sem mudança (recusa).
            }

            NotificarSeMudou(personagemId);
        }
        catch (Exception erro)
        {
            _log?.LogError(
                erro,
                "Erro ao tratar o pedido de escalada de {Personagem}.",
                personagemId
            );
        }
    }

    private async Task<bool> PodeComecarAEscalarAsync(long personagemId)
    {
        if (
            !_posicoes.TentarObter(personagemId, out var posicao)
            || !AreasDoMundo.EhCidade(posicao.Area)
            || (_queda?.EstaCaido(personagemId) ?? false)
            || RegrasMovimentoEspecial.EstaNadando(posicao.X, posicao.Y, posicao.Z, _volumes)
            || RegrasMovimentoEspecial.ProcurarVia(posicao.X, posicao.Y, posicao.Z, _volumes)
                == null
        )
        {
            return false;
        }

        if (_cobranca == null)
        {
            return true;
        }

        var cabe = await _cobranca.PodeCobrarAsync(
            personagemId,
            RecursoCobravel.Stamina,
            ConfigMovimentoEspecial.StaminaDaEscaladaPorSegundo,
            CancellationToken.None
        );
        return cabe == ResultadoDaCobranca.Cobrado;
    }

    /// <summary>Esquece o personagem quando a sessão acaba.</summary>
    public void Remover(long personagemId)
    {
        lock (_trava)
        {
            _estados.Remove(personagemId);
        }
    }

    /// <summary>
    /// Um passo: avalia a posição de todos os personagens online. O laço em segundo plano chama a
    /// cada <see cref="IntervaloDoPassoMs"/>; os testes chamam direto.
    /// </summary>
    public async Task PassoAsync()
    {
        var agora = _relogioMs();
        var online = _online();
        var cobrancas = new List<(long Id, bool Escalada)>();
        var quedas = new List<(long Id, string Area, float Altura, int Dano)>();
        var mudaram = new List<long>();

        lock (_trava)
        {
            foreach (var id in online)
            {
                if (!_posicoes.TentarObter(id, out var posicao))
                {
                    _estados.Remove(id);
                    continue;
                }

                AvaliarPasso(id, posicao, agora, cobrancas, quedas);
                mudaram.Add(id);
            }

            var presentes = new HashSet<long>(online);
            var sumiram = _estados.Keys.Where(id => !presentes.Contains(id)).ToList();
            foreach (var id in sumiram)
            {
                _estados.Remove(id);
            }
        }

        foreach (var (id, area, altura, dano) in quedas)
        {
            AplicarQueda(id, area, altura, dano);
        }

        foreach (var (id, escalada) in cobrancas)
        {
            await CobrarAsync(id, escalada);
        }

        foreach (var id in mudaram)
        {
            NotificarSeMudou(id);
            AjustarVelocidade(id);
        }
    }

    // Chamado sob a trava. Atualiza o estado de um personagem com a posição nova.
    private void AvaliarPasso(
        long id,
        PosicaoNoMundo posicao,
        long agora,
        List<(long Id, bool Escalada)> cobrancas,
        List<(long Id, string Area, float Altura, int Dano)> quedas
    )
    {
        var estado = ObterEstado(id);
        var estavaAtivo = estado.Nadando || estado.Escalando;
        var naCidade = AreasDoMundo.EhCidade(posicao.Area);
        var caido = _queda?.EstaCaido(id) ?? false;

        var reinicia =
            !estado.Medido
            || estado.Area != posicao.Area
            || !naCidade
            || caido
            || Horizontal(estado, posicao) > SaltoQueReiniciaAMedicao;

        var descida = estado.Y - posicao.Y;
        var avanco = Horizontal(estado, posicao);
        estado.Area = posicao.Area;
        estado.X = posicao.X;
        estado.Y = posicao.Y;
        estado.Z = posicao.Z;
        estado.Medido = true;

        if (!naCidade || caido)
        {
            estado.Nadando = false;
            estado.Escalando = false;
            estado.SemStamina = false;
            estado.Queda = 0f;
            return;
        }

        var nadando = RegrasMovimentoEspecial.EstaNadando(
            posicao.X,
            posicao.Y,
            posicao.Z,
            _volumes
        );

        if (estado.Escalando)
        {
            var via = RegrasMovimentoEspecial.ProcurarVia(
                posicao.X,
                posicao.Y,
                posicao.Z,
                _volumes
            );
            if (via == null || nadando)
            {
                estado.Escalando = false;
            }
        }

        estado.Nadando = nadando;
        if (!estado.Nadando && !estado.Escalando)
        {
            estado.SemStamina = false;
        }

        // Queda: só mede com a posição anterior confiável, e nunca enquanto escala.
        if (estado.Escalando || reinicia)
        {
            estado.Queda = 0f;
        }
        else if (!nadando && RegrasMovimentoEspecial.EhQuedaLivre(descida, avanco))
        {
            estado.Queda += descida;
        }
        else
        {
            if (estado.Queda > 0f)
            {
                var altura = estado.Queda;
                var amortecida = RegrasMovimentoEspecial.AguaAmortece(
                    posicao.X,
                    posicao.Y,
                    posicao.Z,
                    _volumes
                );
                var dano = amortecida ? 0 : RegrasMovimentoEspecial.DanoDaQueda(altura);
                if (dano > 0)
                {
                    quedas.Add((id, posicao.Area, altura, dano));
                }
            }

            estado.Queda = 0f;
        }

        var ativo = estado.Nadando || estado.Escalando;
        if (!ativo || !estavaAtivo)
        {
            // A primeira cobrança vem um intervalo depois de começar a nadar ou escalar.
            estado.ProximaCobrancaMs = agora + ConfigMovimentoEspecial.IntervaloDeCobrancaMs;
        }
        else if (_cobranca != null && agora >= estado.ProximaCobrancaMs)
        {
            estado.ProximaCobrancaMs = agora + ConfigMovimentoEspecial.IntervaloDeCobrancaMs;
            cobrancas.Add((id, estado.Escalando));
        }
    }

    private async Task CobrarAsync(long id, bool escalada)
    {
        if (_cobranca == null)
        {
            return;
        }

        try
        {
            var custo = escalada
                ? ConfigMovimentoEspecial.StaminaDaEscaladaPorSegundo
                : ConfigMovimentoEspecial.StaminaDoNadoPorSegundo;
            var (resultado, recursos) = await _cobranca.TentarCobrarAsync(
                id,
                RecursoCobravel.Stamina,
                custo,
                CancellationToken.None
            );

            lock (_trava)
            {
                if (!_estados.TryGetValue(id, out var estado))
                {
                    return;
                }

                switch (resultado)
                {
                    case ResultadoDaCobranca.Cobrado:
                        estado.SemStamina = false;
                        break;
                    case ResultadoDaCobranca.Insuficiente:
                        // Sem stamina: quem escala solta a via (e cai); quem nada fica lento.
                        estado.SemStamina = true;
                        if (escalada)
                        {
                            estado.Escalando = false;
                        }

                        break;
                    default:
                        break;
                }
            }

            if (recursos != null)
            {
                _enviarRecursos(id, recursos);
            }
        }
        catch (Exception erro)
        {
            _log?.LogError(erro, "Erro ao cobrar a stamina de {Personagem}.", id);
        }
    }

    private void AplicarQueda(long id, string area, float altura, int dano)
    {
        try
        {
            _dano?.AplicarDanoDeAmbiente(id, dano, "Queda", area);
            _enviarDano(id, new DanoDeQueda { Dano = dano, Altura = altura });
        }
        catch (Exception erro)
        {
            _log?.LogError(erro, "Erro ao aplicar o dano de queda de {Personagem}.", id);
        }
    }

    // Quem nada sem stamina fica lento; ao recuperar, a velocidade volta.
    private void AjustarVelocidade(long id)
    {
        if (_limiteDeVelocidade == null)
        {
            return;
        }

        bool lento;
        bool estavaLento;
        lock (_trava)
        {
            if (!_estados.TryGetValue(id, out var estado))
            {
                return;
            }

            lento = estado.Nadando && estado.SemStamina;
            estavaLento = estado.Lento;
            estado.Lento = lento;
        }

        if (lento)
        {
            _limiteDeVelocidade.LimitarVelocidade(
                id,
                ConfigMovimentoEspecial.FatorSemStamina,
                (ConfigMovimentoEspecial.IntervaloDeCobrancaMs / 1000.0) * 2
            );
        }
        else if (estavaLento)
        {
            _limiteDeVelocidade.LimitarVelocidade(id, 1.0, 1.0);
        }
    }

    private void NotificarSeMudou(long id)
    {
        EstadoDoMovimento? aviso = null;
        lock (_trava)
        {
            if (!_estados.TryGetValue(id, out var estado))
            {
                return;
            }

            var atual = (estado.Nadando, estado.Escalando, estado.SemStamina);
            if (estado.UltimoEnviado != atual)
            {
                estado.UltimoEnviado = atual;
                aviso = new EstadoDoMovimento
                {
                    Nadando = atual.Nadando,
                    Escalando = atual.Escalando,
                    SemStamina = atual.SemStamina,
                };
            }
        }

        if (aviso != null)
        {
            _enviarEstado(id, aviso);
        }
    }

    private Estado ObterEstado(long id)
    {
        if (!_estados.TryGetValue(id, out var estado))
        {
            estado = new Estado();
            _estados[id] = estado;
        }

        return estado;
    }

    private static float Horizontal(Estado de, PosicaoNoMundo para)
    {
        var dx = para.X - de.X;
        var dz = para.Z - de.Z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }

    private sealed class Estado
    {
        public bool Medido;
        public string Area = "";
        public float X;
        public float Y;
        public float Z;

        /// <summary>Metros descidos em queda livre sem tocar o chão.</summary>
        public float Queda;

        public bool Nadando;
        public bool Escalando;
        public bool SemStamina;
        public bool Lento;
        public long ProximaCobrancaMs;
        public (bool, bool, bool)? UltimoEnviado = (false, false, false);
    }
}
