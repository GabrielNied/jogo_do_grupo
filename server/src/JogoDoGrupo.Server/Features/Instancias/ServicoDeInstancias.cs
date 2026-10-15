using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Instancias;

namespace JogoDoGrupo.Server.Features.Instancias;

/// <summary>Cria e mantém corridas isoladas de dungeon dentro do processo do servidor.</summary>
public sealed class ServicoDeInstancias(
    IConsultaDeGrupo grupos,
    IPosicoesDoMundo posicoes,
    ITransicoesDeAreaDoMundo transicoes,
    ServidorTempoReal tempoReal,
    TimeProvider relogio
)
{
    public static readonly TimeSpan PrazoDeReconexao = TimeSpan.FromSeconds(120);
    public static readonly TimeSpan PrazoVazia = TimeSpan.FromMinutes(5);

    private const double DistanciaMaximaInicial = 10;

    private readonly Lock _trava = new();
    private readonly Dictionary<string, Corrida> _corridas = new(StringComparer.Ordinal);
    private readonly Dictionary<long, Corrida> _corridaDoPersonagem = [];
    private readonly Dictionary<long, Corrida> _corridaDoGrupo = [];

    /// <summary>Quantidade de corridas que ainda aceitam participantes.</summary>
    public int CorridasAtivas
    {
        get
        {
            lock (_trava)
            {
                return _corridas.Count;
            }
        }
    }

    /// <summary>Trata o pedido de um jogador solo, líder ou membro do grupo.</summary>
    public void AoEntrar(SessaoTempoReal sessao)
    {
        var grupo = grupos.Consultar(sessao.PersonagemId);
        var membros = grupo?.Membros.Distinct().ToArray() ?? [sessao.PersonagemId];
        Corrida? corridaExistente;
        lock (_trava)
        {
            if (_corridaDoPersonagem.TryGetValue(sessao.PersonagemId, out corridaExistente))
            {
                if (
                    !Participa(corridaExistente, sessao.PersonagemId)
                    || corridaExistente.GrupoId != grupo?.GrupoId
                )
                {
                    Recusar(sessao, CodigosErroInstancias.GrupoEmOutraDungeon);
                    return;
                }
            }
            else if (
                grupo is not null
                && _corridaDoGrupo.TryGetValue(grupo.GrupoId, out corridaExistente)
            )
            {
                if (
                    !Participa(corridaExistente, sessao.PersonagemId)
                    || corridaExistente.GrupoId != grupo.GrupoId
                )
                {
                    Recusar(sessao, CodigosErroInstancias.GrupoEmOutraDungeon);
                    return;
                }
            }
            else
            {
                corridaExistente = null;
            }
        }

        if (corridaExistente is not null)
        {
            EntrarNaCorrida(sessao, corridaExistente);
            return;
        }

        if (grupo is not null && grupo.LiderId != sessao.PersonagemId)
        {
            Recusar(sessao, CodigosErroInstancias.NaoELider);
            return;
        }

        var posicaoDoSolicitante = ObterPosicao(sessao.PersonagemId);
        if (posicaoDoSolicitante is not { } posicao || posicao.Area != AreasDoMundo.Cidade)
        {
            Recusar(sessao, CodigosErroInstancias.LongeDemais);
            return;
        }

        if (
            !float.IsFinite(posicao.X)
            || !float.IsFinite(posicao.Y)
            || !float.IsFinite(posicao.Z)
            || membros.Length == 0
            || GrupoTemMembroEmOutraCorrida(membros, grupo?.GrupoId)
        )
        {
            Recusar(sessao, CodigosErroInstancias.GrupoEmOutraDungeon);
            return;
        }

        var id = Guid.NewGuid().ToString("N");
        var corrida = new Corrida(id, grupo?.GrupoId, membros);
        lock (_trava)
        {
            if (
                _corridaDoPersonagem.ContainsKey(sessao.PersonagemId)
                || (grupo is not null && _corridaDoGrupo.ContainsKey(grupo.GrupoId))
            )
            {
                Recusar(sessao, CodigosErroInstancias.GrupoEmOutraDungeon);
                return;
            }

            _corridas.Add(id, corrida);
            if (grupo is not null)
            {
                _corridaDoGrupo.Add(grupo.GrupoId, corrida);
            }

            foreach (var membro in membros)
            {
                _corridaDoPersonagem[membro] = corrida;
            }
        }

        var area = AreasDoMundo.Instancia(id);
        foreach (var membro in membros)
        {
            if (
                !posicoes.TentarObter(membro, out var posicaoMembro)
                || posicaoMembro.Area != AreasDoMundo.Cidade
                || Distancia(posicao, posicaoMembro) > DistanciaMaximaInicial
            )
            {
                continue;
            }

            if (membro == sessao.PersonagemId)
            {
                corrida.PosicoesDeRetorno[membro] = posicaoMembro;
            }

            if (!transicoes.TentarMoverParaArea(membro, new PosicaoNoMundo(area, 0, 0, 0, 0)))
            {
                continue;
            }

            if (membro != sessao.PersonagemId)
            {
                corrida.PosicoesDeRetorno[membro] = posicaoMembro;
            }

            if (tempoReal.TentarObterSessao(membro, out var participante) && participante != null)
            {
                EnviarInicio(participante, corrida);
            }
        }

        if (!corrida.PosicoesDeRetorno.ContainsKey(sessao.PersonagemId))
        {
            EncerrarNoLaco(id, ResultadoDungeon.InstanciaVazia);
            Recusar(sessao, CodigosErroInstancias.LongeDemais);
        }
    }

    /// <summary>
    /// Solicita o encerramento por vitória ou derrota. A alteração de estado e os envios rodam
    /// no laço principal do tempo real.
    /// </summary>
    public void Encerrar(string idInstancia, ResultadoDungeon resultado) =>
        tempoReal.NoLaco(() => EncerrarNoLaco(idInstancia, resultado));

    /// <summary>Marca uma sessão desconectada sem perder sua vaga durante o prazo de retorno.</summary>
    public void AoDesconectar(SessaoTempoReal sessao)
    {
        lock (_trava)
        {
            if (_corridaDoPersonagem.TryGetValue(sessao.PersonagemId, out var corrida))
            {
                corrida.DesconectadosDesde.TryAdd(sessao.PersonagemId, AgoraMs());
            }
        }
    }

    /// <summary>Expira vagas desconectadas e fecha instâncias vazias após cinco minutos.</summary>
    public void Varrer()
    {
        var agora = AgoraMs();
        List<(string Id, ResultadoDungeon Resultado)> encerramentos = [];
        lock (_trava)
        {
            foreach (var corrida in _corridas.Values.ToArray())
            {
                foreach (
                    var personagemId in corrida
                        .DesconectadosDesde.Where(par =>
                            agora - par.Value >= PrazoDeReconexao.TotalMilliseconds
                        )
                        .Select(par => par.Key)
                        .ToArray()
                )
                {
                    corrida.DesconectadosDesde.Remove(personagemId);
                    corrida.Participantes.Remove(personagemId);
                    corrida.PosicoesDeRetorno.Remove(personagemId);
                    _corridaDoPersonagem.Remove(personagemId);
                }

                if (posicoes.PersonagensNaArea(corrida.Area).Count == 0)
                {
                    corrida.VaziaDesde ??= agora;
                    if (agora - corrida.VaziaDesde.Value >= PrazoVazia.TotalMilliseconds)
                    {
                        encerramentos.Add((corrida.Id, ResultadoDungeon.InstanciaVazia));
                    }
                }
                else
                {
                    corrida.VaziaDesde = null;
                }
            }
        }

        foreach (var (id, resultado) in encerramentos)
        {
            EncerrarNoLaco(id, resultado);
        }
    }

    private void EntrarNaCorrida(SessaoTempoReal sessao, Corrida corrida)
    {
        var personagemId = sessao.PersonagemId;
        lock (_trava)
        {
            if (!Participa(corrida, personagemId))
            {
                Recusar(sessao, CodigosErroInstancias.GrupoEmOutraDungeon);
                return;
            }

            if (
                corrida.DesconectadosDesde.TryGetValue(personagemId, out var desconectado)
                && AgoraMs() - desconectado >= PrazoDeReconexao.TotalMilliseconds
            )
            {
                Recusar(sessao, CodigosErroInstancias.LongeDemais);
                return;
            }
        }

        if (posicoes.TentarObter(personagemId, out var atual) && atual.Area == corrida.Area)
        {
            EnviarInicio(sessao, corrida);
            return;
        }

        if (!posicoes.TentarObter(personagemId, out atual) || atual.Area != AreasDoMundo.Cidade)
        {
            Recusar(sessao, CodigosErroInstancias.LongeDemais);
            return;
        }

        lock (_trava)
        {
            corrida.PosicoesDeRetorno.TryAdd(personagemId, atual);
            corrida.DesconectadosDesde.Remove(personagemId);
            corrida.VaziaDesde = null;
        }

        if (
            !transicoes.TentarMoverParaArea(
                personagemId,
                new PosicaoNoMundo(corrida.Area, 0, 0, 0, 0)
            )
        )
        {
            Recusar(sessao, CodigosErroInstancias.LongeDemais);
            return;
        }

        EnviarInicio(sessao, corrida);
    }

    private void EncerrarNoLaco(string idInstancia, ResultadoDungeon resultado)
    {
        Corrida? corrida;
        lock (_trava)
        {
            if (!_corridas.Remove(idInstancia, out corrida))
            {
                return;
            }

            if (corrida.GrupoId is { } grupoId)
            {
                _corridaDoGrupo.Remove(grupoId);
            }

            foreach (var membro in corrida.Participantes)
            {
                _corridaDoPersonagem.Remove(membro);
            }
        }

        foreach (var personagemId in corrida.Participantes)
        {
            if (!tempoReal.TentarObterSessao(personagemId, out var sessao) || sessao == null)
            {
                continue;
            }

            tempoReal.Enviar(
                sessao,
                InstanciasMessageIds.DungeonEncerrada,
                new DungeonEncerrada { IdInstancia = corrida.Id, Resultado = resultado }
            );

            if (
                posicoes.TentarObter(personagemId, out var atual)
                && atual.Area == corrida.Area
                && corrida.PosicoesDeRetorno.TryGetValue(personagemId, out var retorno)
            )
            {
                transicoes.TentarMoverParaArea(personagemId, retorno);
            }
        }
    }

    private void EnviarInicio(SessaoTempoReal sessao, Corrida corrida) =>
        tempoReal.Enviar(
            sessao,
            InstanciasMessageIds.DungeonIniciada,
            new DungeonIniciada { IdInstancia = corrida.Id, Area = corrida.Area }
        );

    private void Recusar(SessaoTempoReal sessao, string codigo) =>
        tempoReal.Enviar(
            sessao,
            InstanciasMessageIds.EntradaRecusada,
            new EntradaRecusada { Codigo = codigo }
        );

    private PosicaoNoMundo? ObterPosicao(long personagemId) =>
        posicoes.TentarObter(personagemId, out var posicao) ? posicao : null;

    private bool GrupoTemMembroEmOutraCorrida(long[] membros, long? grupoId)
    {
        lock (_trava)
        {
            return membros.Any(membro =>
                _corridaDoPersonagem.TryGetValue(membro, out var ativa) && ativa.GrupoId != grupoId
            );
        }
    }

    private static bool Participa(Corrida corrida, long personagemId) =>
        corrida.Participantes.Contains(personagemId);

    private static double Distancia(PosicaoNoMundo a, PosicaoNoMundo b)
    {
        var x = a.X - b.X;
        var y = a.Y - b.Y;
        var z = a.Z - b.Z;
        return Math.Sqrt(x * x + y * y + z * z);
    }

    private long AgoraMs() => relogio.GetUtcNow().ToUnixTimeMilliseconds();

    private sealed class Corrida(string id, long? grupoId, IEnumerable<long> participantes)
    {
        public string Id { get; } = id;
        public long? GrupoId { get; } = grupoId;
        public string Area { get; } = AreasDoMundo.Instancia(id);
        public HashSet<long> Participantes { get; } = participantes.ToHashSet();
        public Dictionary<long, PosicaoNoMundo> PosicoesDeRetorno { get; } = [];
        public Dictionary<long, long> DesconectadosDesde { get; } = [];
        public long? VaziaDesde { get; set; }
    }
}
