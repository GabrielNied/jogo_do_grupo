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
) : BackgroundService, ISpawnDeTeste, IInimigosDeTeste
{
    private readonly ControleInteresseDeInimigos _interesse = new();
    private readonly List<JogadorNoCampo> _jogadoresBuffer = new(32);
    private readonly Dictionary<long, (float X, float Z)> _posicoesInimigosBuffer = new(32);
    private readonly HashSet<long> _pertoIdsBuffer = new(32);
    private readonly List<SessaoTempoReal> _sessoesAlvoBuffer = new(32);

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

    private Dictionary<long, (float X, float Z)> PosicoesAtuais(
        IInimigosSimulados grupo,
        PassoDosInimigos passo
    )
    {
        _posicoesInimigosBuffer.Clear();
        foreach (var i in grupo.Vivos())
        {
            _posicoesInimigosBuffer[i.InimigoId] = (i.X, i.Z);
        }

        foreach (var mudanca in passo.Mudancas)
        {
            _posicoesInimigosBuffer[mudanca.Mensagem.InimigoId] = (
                mudanca.Mensagem.X,
                mudanca.Mensagem.Z
            );
        }

        return _posicoesInimigosBuffer;
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
            var definicao = CatalogoCriaturas.Obter(dados.CriaturaId);
            var ehChefeRaro = definicao != null && RegrasCriaturas.AvisaNaArea(definicao.Categoria);
            if (
                !ehChefeRaro
                && !AplicarInteresse(
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
        var ehChefeRaro = grupo is GrupoDeInimigos g && RegrasCriaturas.AvisaNaArea(g.Categoria);
        if (grupo.Area != AreasDoMundo.Cidade || ehChefeRaro)
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

    /// <summary>Folga no eixo X entre dois inimigos do mesmo pedido de spawn de teste.</summary>
    public const double FolgaEntreSpawnsDeTeste = 1.5;

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

        var grupo = GrupoParaSpawnDeTeste(posicao.Area);
        if (grupo == null)
        {
            return (false, "Não há inimigos nesta área.");
        }

        return SpawnarUmDeTeste(grupo, posicao, DistanciaDoSpawnDeTeste, null)
            ? (true, "Inimigo spawnado.")
            : (false, "Limite de spawn de teste.");
    }

    /// <summary>
    /// Põe <paramref name="quantidade"/> inimigos da espécie escolhida perto do personagem, no
    /// maior grupo da área, até o teto de spawns de teste do grupo.
    /// </summary>
    public (bool Aceito, string Texto) SpawnarEspecie(
        long personagemId,
        int especieId,
        int quantidade
    )
    {
        var especie = CatalogoCriaturas.Obter(especieId);
        if (especie == null)
        {
            return (false, "Espécie desconhecida.");
        }

        if (!posicoes.TentarObter(personagemId, out var posicao))
        {
            return (false, "Você não está no mundo.");
        }

        var grupo = GrupoParaSpawnDeTeste(posicao.Area);
        if (grupo == null)
        {
            return (false, "Não há inimigos nesta área.");
        }

        var criados = 0;
        for (var i = 0; i < quantidade; i++)
        {
            var deslocamento = DistanciaDoSpawnDeTeste + (i * FolgaEntreSpawnsDeTeste);
            if (!SpawnarUmDeTeste(grupo, posicao, deslocamento, especie))
            {
                break;
            }

            criados++;
        }

        if (criados == 0)
        {
            return (false, "Limite de spawn de teste.");
        }

        return (
            true,
            criados == quantidade
                ? $"{criados} × {especie.Nome} spawnado(s)."
                : $"{criados} de {quantidade} × {especie.Nome} spawnado(s) (limite de spawn de teste)."
        );
    }

    /// <summary>
    /// Mata os inimigos vivos dos grupos da área do personagem, sem experiência nem loot (eles
    /// voltam pela reposição de sempre). Só para a tela de teste.
    /// </summary>
    public (bool Aceito, string Texto) MatarDaArea(long personagemId)
    {
        if (!posicoes.TentarObter(personagemId, out var posicao))
        {
            return (false, "Você não está no mundo.");
        }

        var mortos = grupos
            .OfType<GrupoDeInimigos>()
            .Where(g => g.Area == posicao.Area)
            .Sum(g => g.MatarVivosSemRecompensa());
        return (
            true,
            mortos == 0 ? "Nenhum inimigo vivo na área." : $"{mortos} inimigo(s) morto(s)."
        );
    }

    private GrupoDeInimigos? GrupoParaSpawnDeTeste(string area) =>
        grupos
            .OfType<GrupoDeInimigos>()
            .Where(g => g.Area == area)
            .OrderByDescending(g => g.Capacidade)
            .FirstOrDefault();

    private bool SpawnarUmDeTeste(
        GrupoDeInimigos grupo,
        PosicaoNoMundo posicao,
        double deslocamentoX,
        DefinicaoDeCriatura? especie
    )
    {
        var inimigo = grupo.ForcarAparicao(posicao.X + deslocamentoX, posicao.Z, especie);
        if (inimigo == null)
        {
            return false;
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

        return true;
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

        var filtrados = _interesse
            .FiltrarEntrada(evento.PersonagemId, evento.Area, posicao.X, posicao.Z, vivos)
            .ToList();
        // Chefe raro e chefe de mundo avisam na área toda: quem entra depois da aparição também
        // os recebe (marca no mapa), mesmo longe do raio de interesse.
        foreach (var vivo in vivos)
        {
            if (
                CatalogoCriaturas.Obter(vivo.CriaturaId) is { } ficha
                && RegrasCriaturas.AvisaNaArea(ficha.Categoria)
                && !filtrados.Any(i => i.InimigoId == vivo.InimigoId)
            )
            {
                filtrados.Add(vivo);
            }
        }

        tempoReal.Enviar(
            sessao,
            MobsMessageIds.InimigosDaArea,
            new InimigosDaArea { Inimigos = [.. filtrados] }
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

    /// <summary>
    /// Quem cura um aliado em combate com um inimigo passa a participar da morte dele (#909).
    /// Pode vir de qualquer thread: cada grupo se protege com a sua trava.
    /// </summary>
    public void AoCurar(PersonagemCurouAliado evento)
    {
        foreach (var grupo in Simulados())
        {
            if (grupo.Area == evento.Area)
            {
                grupo.RegistrarCura(evento.CuradorId, evento.CuradoId);
            }
        }
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
            _pertoIdsBuffer.Clear();

            var temChefeRaro =
                (grupo is GrupoDeInimigos g && RegrasCriaturas.AvisaNaArea(g.Categoria))
                || passo.Aparicoes.Any(a =>
                    CatalogoCriaturas.Obter(a.Inimigo.CriaturaId) is { } ficha
                    && RegrasCriaturas.AvisaNaArea(ficha.Categoria)
                );

            if (temChefeRaro)
            {
                _pertoIdsBuffer.UnionWith(posicoes.PersonagensNaArea(grupo.Area));
            }
            else
            {
                foreach (var (inimigoId, (ix, iz)) in posicoesInimigos)
                {
                    _pertoIdsBuffer.UnionWith(
                        vizinhanca.PersonagensPerto(
                            grupo.Area,
                            ix,
                            iz,
                            (float)RegrasInteresseDeInimigos.SaidaMetros
                        )
                    );
                    _pertoIdsBuffer.UnionWith(_interesse.QuemJaRecebia(inimigoId));
                }

                foreach (var morte in passo.Mortes)
                {
                    _pertoIdsBuffer.UnionWith(_interesse.QuemJaRecebia(morte.InimigoId));
                }
            }

            _sessoesAlvoBuffer.Clear();
            foreach (var id in _pertoIdsBuffer)
            {
                if (tempoReal.TentarObterSessao(id, out var s) && s != null)
                {
                    _sessoesAlvoBuffer.Add(s);
                }
            }

            return _sessoesAlvoBuffer;
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
