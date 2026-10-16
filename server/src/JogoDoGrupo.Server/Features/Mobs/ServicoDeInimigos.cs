using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// O laço de IA dos inimigos do campo: <see cref="RegrasInimigo.PassosPorSegundo"/> vezes por
/// segundo lê do Mundo onde estão os jogadores da área, avança o <see cref="GrupoDeInimigos"/>,
/// manda o que mudou a quem está na área e publica os eventos de Mobs
/// (<c>Contratos/Mobs/</c>). A decisão é toda do grupo; aqui só há rede e eventos.
/// </summary>
public sealed class ServicoDeInimigos(
    IReadOnlyList<IInimigosSimulados> grupos,
    ServidorTempoReal tempoReal,
    IPosicoesDoMundo posicoes,
    EventosDoServidor eventos,
    ILogger<ServicoDeInimigos> log,
    IEstadoDeQueda queda,
    ChefesDaDungeon? chefes = null,
    InimigosPorInstancia? instancias = null,
    IVizinhanca? vizinhanca = null
) : BackgroundService, ISpawnDeTeste
{
    private readonly ControleInteresseDeInimigos _interesse = new();
    private readonly List<JogadorNoCampo> _jogadoresBuffer = new(32);

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, parada);
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>Um passo do laço: avança a IA, envia e publica. Os testes chamam direto.</summary>
    public void Avancar()
    {
        foreach (var grupo in Simulados())
        {
            try
            {
                Avancar(grupo);
            }
            catch (Exception erro)
            {
                log.LogError(erro, "Erro no passo da IA do grupo na área {Area}.", grupo.Area);
            }
        }
    }

    // Os grupos fixos, os bosses de dungeon e os salões de instância (listas que mudam).
    private IEnumerable<IInimigosSimulados> Simulados()
    {
        IEnumerable<IInimigosSimulados> todos = grupos;
        if (chefes != null)
        {
            todos = todos.Concat(chefes.Ativos());
        }

        if (instancias != null)
        {
            todos = todos.Concat(instancias.Simulados());
        }

        return todos;
    }

    private void Avancar(IInimigosSimulados grupo)
    {
        PassoDosInimigos passo;
        lock (_jogadoresBuffer)
        {
            _jogadoresBuffer.Clear();
            foreach (var personagemId in posicoes.PersonagensNaArea(grupo.Area))
            {
                if (
                    queda.EstaCaido(personagemId)
                    || !posicoes.TentarObter(personagemId, out var posicao)
                    || posicao.Area != grupo.Area
                )
                {
                    continue;
                }

                _jogadoresBuffer.Add(new JogadorNoCampo(personagemId, posicao.X, posicao.Z));
            }

            passo = grupo.Passo(_jogadoresBuffer);
        }

        var posicoesInimigos = PosicoesAtuais(grupo, passo);

        var sessoesAlvo = ObterSessoesAlvo(grupo, passo, posicoesInimigos);
        foreach (var sessao in sessoesAlvo)
        {
            if (
                !posicoes.TentarObter(sessao.PersonagemId, out var posicaoJogador)
                || posicaoJogador.Area != grupo.Area
                || queda.EstaCaido(sessao.PersonagemId)
            )
            {
                continue;
            }

            SincronizarNovosNoRaio(sessao, grupo, posicaoJogador.X, posicaoJogador.Z);

            if (!passo.Vazio)
            {
                EnviarPasso(
                    sessao,
                    grupo,
                    passo,
                    posicoesInimigos,
                    posicaoJogador.X,
                    posicaoJogador.Z
                );
            }
        }

        if (passo.Vazio)
        {
            return;
        }

        foreach (var golpe in passo.Golpes)
        {
            Publicar(golpe);
        }

        foreach (var derrota in passo.Derrotas)
        {
            Publicar(derrota);
        }
    }

    /// <summary>
    /// Quem se aproxima de inimigos parados não recebia nada: o passo podia vir vazio e só
    /// <see cref="InimigoMudou"/> não cria modelo no client.
    /// </summary>
    private void SincronizarNovosNoRaio(
        SessaoTempoReal sessao,
        IInimigosSimulados grupo,
        double jogadorX,
        double jogadorZ
    )
    {
        foreach (var inimigo in grupo.Vivos())
        {
            AplicarInteresse(
                sessao,
                grupo,
                inimigo.InimigoId,
                inimigo.X,
                inimigo.Z,
                jogadorX,
                jogadorZ,
                retratoSeEntrou: inimigo
            );
        }
    }

    private static Dictionary<long, (float X, float Z)> PosicoesAtuais(
        IInimigosSimulados grupo,
        PassoDosInimigos passo
    )
    {
        var map = grupo.Vivos().ToDictionary(i => i.InimigoId, i => (i.X, i.Z));
        foreach (var mudanca in passo.Mudancas)
        {
            map[mudanca.Mensagem.InimigoId] = (mudanca.Mensagem.X, mudanca.Mensagem.Z);
        }

        return map;
    }

    private void EnviarPasso(
        SessaoTempoReal sessao,
        IInimigosSimulados grupo,
        PassoDosInimigos passo,
        Dictionary<long, (float X, float Z)> posicoesInimigos,
        double jogadorX,
        double jogadorZ
    )
    {
        foreach (var mudanca in passo.Mudancas)
        {
            var id = mudanca.Mensagem.InimigoId;
            if (
                !AplicarInteresse(
                    sessao,
                    grupo,
                    id,
                    mudanca.Mensagem.X,
                    mudanca.Mensagem.Z,
                    jogadorX,
                    jogadorZ,
                    retratoSeEntrou: null
                )
            )
            {
                continue;
            }

            tempoReal.Enviar(
                sessao,
                MobsMessageIds.InimigoMudou,
                mudanca.Mensagem,
                mudanca.SoPosicao ? EntregaTempoReal.Sequenciada : EntregaTempoReal.Confiavel,
                mudanca.SoPosicao ? mudanca.Mensagem.InimigoId : null
            );
        }

        foreach (var preparo in passo.Preparos)
        {
            if (
                !PosicaoDoInimigo(
                    preparo.InimigoId,
                    posicoesInimigos,
                    preparo.CentroX,
                    preparo.CentroZ,
                    out var ix,
                    out var iz
                )
            )
            {
                continue;
            }

            if (
                !AplicarInteresse(
                    sessao,
                    grupo,
                    preparo.InimigoId,
                    ix,
                    iz,
                    jogadorX,
                    jogadorZ,
                    retratoSeEntrou: null
                )
            )
            {
                continue;
            }

            tempoReal.Enviar(sessao, MobsMessageIds.InimigoPreparaAtaque, preparo);
        }

        foreach (var acerto in passo.Acertos)
        {
            if (!PosicaoDoInimigo(acerto.InimigoId, posicoesInimigos, 0, 0, out var ix, out var iz))
            {
                continue;
            }

            if (
                !AplicarInteresse(
                    sessao,
                    grupo,
                    acerto.InimigoId,
                    ix,
                    iz,
                    jogadorX,
                    jogadorZ,
                    retratoSeEntrou: null
                )
            )
            {
                continue;
            }

            tempoReal.Enviar(sessao, MobsMessageIds.InimigoAcertou, acerto);
        }

        foreach (var morte in passo.Mortes)
        {
            EnviarMorte(sessao, morte, posicoesInimigos, jogadorX, jogadorZ, grupo);
        }

        foreach (var aparicao in passo.Aparicoes)
        {
            var dados = aparicao.Inimigo;
            if (
                !AplicarInteresse(
                    sessao,
                    grupo,
                    dados.InimigoId,
                    dados.X,
                    dados.Z,
                    jogadorX,
                    jogadorZ,
                    retratoSeEntrou: dados
                )
            )
            {
                continue;
            }

            tempoReal.Enviar(sessao, MobsMessageIds.InimigoApareceu, aparicao);
        }

        foreach (var exposto in passo.Expostos)
        {
            if (
                !PosicaoDoInimigo(exposto.InimigoId, posicoesInimigos, 0, 0, out var ix, out var iz)
            )
            {
                continue;
            }

            if (
                !AplicarInteresse(
                    sessao,
                    grupo,
                    exposto.InimigoId,
                    ix,
                    iz,
                    jogadorX,
                    jogadorZ,
                    retratoSeEntrou: null
                )
            )
            {
                continue;
            }

            tempoReal.Enviar(sessao, MobsMessageIds.InimigoExposto, exposto);
        }
    }

    /// <summary>
    /// Morte tira o inimigo de <see cref="IInimigosSimulados.Vivos"/>; sem posição no mapa do
    /// passo, quem já recebia o retrato ainda precisa do <see cref="InimigoMorreu"/>.
    /// </summary>
    private void EnviarMorte(
        SessaoTempoReal sessao,
        InimigoMorreu morte,
        Dictionary<long, (float X, float Z)> posicoesInimigos,
        double jogadorX,
        double jogadorZ,
        IInimigosSimulados grupo
    )
    {
        var podeEnviar = false;
        if (grupo.Area != AreasDoMundo.Cidade)
        {
            podeEnviar = true;
        }
        else if (
            PosicaoDoInimigo(morte.InimigoId, posicoesInimigos, 0, 0, out var ix, out var iz)
            && AplicarInteresse(
                sessao,
                grupo,
                morte.InimigoId,
                ix,
                iz,
                jogadorX,
                jogadorZ,
                retratoSeEntrou: null
            )
        )
        {
            podeEnviar = true;
        }
        else if (_interesse.JaRecebia(sessao.PersonagemId, morte.InimigoId))
        {
            podeEnviar = true;
        }

        if (!podeEnviar)
        {
            return;
        }

        _interesse.EsquecerInimigo(sessao.PersonagemId, morte.InimigoId);
        tempoReal.Enviar(sessao, MobsMessageIds.InimigoMorreu, morte);
    }

    /// <summary>
    /// Raio de interesse. Quem acaba de entrar no raio recebe <see cref="InimigoApareceu"/> aqui;
    /// o caller só manda a mensagem específica (mudou, preparo…) se devolver true.
    /// </summary>
    private bool AplicarInteresse(
        SessaoTempoReal sessao,
        IInimigosSimulados grupo,
        long inimigoId,
        float inimigoX,
        float inimigoZ,
        double jogadorX,
        double jogadorZ,
        InimigoNoMundo? retratoSeEntrou
    )
    {
        if (grupo.Area != AreasDoMundo.Cidade)
        {
            return true;
        }

        var avaliacao = _interesse.Avaliar(
            sessao.PersonagemId,
            inimigoId,
            inimigoX,
            inimigoZ,
            jogadorX,
            jogadorZ
        );
        switch (avaliacao.Acao)
        {
            case ControleInteresseDeInimigos.AcaoInteresse.EnviarSaida:
                tempoReal.Enviar(
                    sessao,
                    MobsMessageIds.InimigoSaiuDoInteresse,
                    new InimigoSaiuDoInteresse { InimigoId = inimigoId }
                );
                return false;
            case ControleInteresseDeInimigos.AcaoInteresse.Enviar:
                if (avaliacao.EntrouNoRaio)
                {
                    var retrato = retratoSeEntrou ?? BuscarRetrato(grupo, inimigoId);
                    if (retrato != null)
                    {
                        tempoReal.Enviar(
                            sessao,
                            MobsMessageIds.InimigoApareceu,
                            new InimigoApareceu { Inimigo = retrato }
                        );
                    }

                    return false;
                }

                return true;
            default:
                return false;
        }
    }

    private static InimigoNoMundo? BuscarRetrato(IInimigosSimulados grupo, long inimigoId)
    {
        foreach (var inimigo in grupo.Vivos())
        {
            if (inimigo.InimigoId == inimigoId)
            {
                return inimigo;
            }
        }

        return null;
    }

    private static bool PosicaoDoInimigo(
        long inimigoId,
        Dictionary<long, (float X, float Z)> mapa,
        float fallbackX,
        float fallbackZ,
        out float x,
        out float z
    )
    {
        if (mapa.TryGetValue(inimigoId, out var pos))
        {
            x = pos.X;
            z = pos.Z;
            return true;
        }

        if (fallbackX != 0 || fallbackZ != 0)
        {
            x = fallbackX;
            z = fallbackZ;
            return true;
        }

        x = 0;
        z = 0;
        return false;
    }

    /// <summary>A que distância, no eixo X, o spawn de teste nasce do personagem.</summary>
    public const double DistanciaDoSpawnDeTeste = 2;

    /// <summary>
    /// Põe um inimigo a mais no maior grupo da área do personagem (o comum, não o chefe raro
    /// de um só) e avisa quem está na área.
    /// </summary>
    public (bool Aceito, string Texto) SpawnarPerto(long personagemId)
    {
        if (!posicoes.TentarObter(personagemId, out var posicao))
        {
            return (false, "Você não está no mundo.");
        }

        var grupo = grupos
            .OfType<GrupoDeInimigos>()
            .Where(g => g.Area == posicao.Area)
            .OrderByDescending(g => g.Capacidade)
            .FirstOrDefault();
        if (grupo == null)
        {
            return (false, "Não há inimigos nesta área.");
        }

        var inimigo = grupo.ForcarAparicao(posicao.X + DistanciaDoSpawnDeTeste, posicao.Z);
        if (inimigo == null)
        {
            return (false, "Limite de spawn de teste.");
        }

        var mensagem = new InimigoApareceu { Inimigo = inimigo };
        var sessoesCandidatas = ObterSessoesPerto(
            posicao.Area,
            inimigo.X,
            inimigo.Z,
            (float)RegrasInteresseDeInimigos.SaidaMetros
        );
        foreach (var sessao in sessoesCandidatas)
        {
            if (
                !posicoes.TentarObter(sessao.PersonagemId, out var posicaoJogador)
                || posicaoJogador.Area != posicao.Area
            )
            {
                continue;
            }

            if (
                AplicarInteresse(
                    sessao,
                    grupo,
                    inimigo.InimigoId,
                    inimigo.X,
                    inimigo.Z,
                    posicaoJogador.X,
                    posicaoJogador.Z,
                    retratoSeEntrou: inimigo
                )
            )
            {
                tempoReal.Enviar(sessao, MobsMessageIds.InimigoApareceu, mensagem);
            }
        }

        return (true, "Inimigo spawnado.");
    }

    /// <summary>Quem entra na área do grupo recebe os inimigos vivos no raio de interesse.</summary>
    public void AoEntrarNaArea(JogadorEntrouNaArea evento)
    {
        if (!tempoReal.TentarObterSessao(evento.PersonagemId, out var sessao) || sessao is null)
        {
            return;
        }

        if (!posicoes.TentarObter(evento.PersonagemId, out var posicao))
        {
            return;
        }

        var vivos = VivosDaArea(evento.Area);
        if (vivos.Count == 0)
        {
            return;
        }

        var filtrados = _interesse.FiltrarEntrada(
            evento.PersonagemId,
            evento.Area,
            posicao.X,
            posicao.Z,
            vivos
        );
        tempoReal.Enviar(
            sessao,
            MobsMessageIds.InimigosDaArea,
            new InimigosDaArea { Inimigos = filtrados }
        );
    }

    private List<InimigoNoMundo> VivosDaArea(string area)
    {
        var vivos = new List<InimigoNoMundo>();
        foreach (var grupo in Simulados())
        {
            if (grupo.Area == area)
            {
                vivos.AddRange(grupo.Vivos());
            }
        }

        return vivos;
    }

    /// <summary>Quem sai da área limpa o estado de interesse de inimigos.</summary>
    public void AoSairDaArea(JogadorSaiuDaArea evento)
    {
        _interesse.RemoverJogador(evento.PersonagemId);
    }

    private IEnumerable<SessaoTempoReal> ObterSessoesAlvo(
        IInimigosSimulados grupo,
        PassoDosInimigos passo,
        Dictionary<long, (float X, float Z)> posicoesInimigos
    )
    {
        if (vizinhanca != null && grupo.Area == AreasDoMundo.Cidade)
        {
            var pertoIds = new HashSet<long>();
            foreach (var (inimigoId, (ix, iz)) in posicoesInimigos)
            {
                pertoIds.UnionWith(
                    vizinhanca.PersonagensPerto(
                        grupo.Area,
                        ix,
                        iz,
                        (float)RegrasInteresseDeInimigos.SaidaMetros
                    )
                );
                pertoIds.UnionWith(_interesse.QuemJaRecebia(inimigoId));
            }

            foreach (var morte in passo.Mortes)
            {
                pertoIds.UnionWith(_interesse.QuemJaRecebia(morte.InimigoId));
            }

            var lista = new List<SessaoTempoReal>(pertoIds.Count);
            foreach (var id in pertoIds)
            {
                if (tempoReal.TentarObterSessao(id, out var s) && s != null)
                {
                    lista.Add(s);
                }
            }

            return lista;
        }

        return tempoReal.Sessoes;
    }

    private IEnumerable<SessaoTempoReal> ObterSessoesPerto(
        string area,
        float x,
        float z,
        float raio
    )
    {
        if (vizinhanca != null && area == AreasDoMundo.Cidade)
        {
            var perto = vizinhanca.PersonagensPerto(area, x, z, raio);
            var lista = new List<SessaoTempoReal>(perto.Count);
            foreach (var id in perto)
            {
                if (tempoReal.TentarObterSessao(id, out var s) && s != null)
                {
                    lista.Add(s);
                }
            }

            return lista;
        }

        return tempoReal.Sessoes;
    }

    // A falha de um assinante não pode parar a IA nem os outros eventos do passo.
    private void Publicar<T>(T evento)
        where T : class
    {
        try
        {
            eventos.Publicar(evento);
        }
        catch (AggregateException erro)
        {
            log.LogError(erro, "Assinante de {Evento} falhou.", typeof(T).Name);
        }
    }
}
