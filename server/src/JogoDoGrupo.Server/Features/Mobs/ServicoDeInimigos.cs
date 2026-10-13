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
    ChefesDaDungeon? chefes = null
) : BackgroundService, ISpawnDeTeste
{
    private readonly ControleInteresseDeInimigos _interesse = new();

    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var relogio = new PeriodicTimer(TimeSpan.FromSeconds(RegrasInimigo.PassoSegundos));
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                Avancar();
            }
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

    // Os grupos fixos e os bosses de dungeon que existem agora (a lista deles muda).
    private IEnumerable<IInimigosSimulados> Simulados() =>
        chefes == null ? grupos : grupos.Concat(chefes.Ativos());

    private void Avancar(IInimigosSimulados grupo)
    {
        var jogadores = new List<JogadorNoCampo>();
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

            jogadores.Add(new JogadorNoCampo(personagemId, posicao.X, posicao.Z));
        }

        var passo = grupo.Passo(jogadores);
        if (passo.Vazio)
        {
            return;
        }

        var posicoesInimigos = PosicoesAtuais(grupo, passo);

        foreach (var sessao in tempoReal.Sessoes)
        {
            if (
                !posicoes.TentarObter(sessao.PersonagemId, out var posicaoJogador)
                || posicaoJogador.Area != grupo.Area
                || queda.EstaCaido(sessao.PersonagemId)
            )
            {
                continue;
            }

            EnviarPasso(sessao, passo, posicoesInimigos, posicaoJogador.X, posicaoJogador.Z);
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
                !Filtrar(
                    sessao.PersonagemId,
                    id,
                    mudanca.Mensagem.X,
                    mudanca.Mensagem.Z,
                    jogadorX,
                    jogadorZ,
                    sessao
                )
            )
            {
                continue;
            }

            tempoReal.Enviar(
                sessao,
                MobsMessageIds.InimigoMudou,
                mudanca.Mensagem,
                mudanca.SoPosicao ? EntregaTempoReal.Sequenciada : EntregaTempoReal.Confiavel
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
                !Filtrar(sessao.PersonagemId, preparo.InimigoId, ix, iz, jogadorX, jogadorZ, sessao)
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

            if (!Filtrar(sessao.PersonagemId, acerto.InimigoId, ix, iz, jogadorX, jogadorZ, sessao))
            {
                continue;
            }

            tempoReal.Enviar(sessao, MobsMessageIds.InimigoAcertou, acerto);
        }

        foreach (var morte in passo.Mortes)
        {
            if (!PosicaoDoInimigo(morte.InimigoId, posicoesInimigos, 0, 0, out var ix, out var iz))
            {
                continue;
            }

            if (!Filtrar(sessao.PersonagemId, morte.InimigoId, ix, iz, jogadorX, jogadorZ, sessao))
            {
                continue;
            }

            tempoReal.Enviar(sessao, MobsMessageIds.InimigoMorreu, morte);
        }

        foreach (var aparicao in passo.Aparicoes)
        {
            var dados = aparicao.Inimigo;
            if (
                !Filtrar(
                    sessao.PersonagemId,
                    dados.InimigoId,
                    dados.X,
                    dados.Z,
                    jogadorX,
                    jogadorZ,
                    sessao
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
                !Filtrar(sessao.PersonagemId, exposto.InimigoId, ix, iz, jogadorX, jogadorZ, sessao)
            )
            {
                continue;
            }

            tempoReal.Enviar(sessao, MobsMessageIds.InimigoExposto, exposto);
        }
    }

    private bool Filtrar(
        long personagemId,
        long inimigoId,
        float inimigoX,
        float inimigoZ,
        double jogadorX,
        double jogadorZ,
        SessaoTempoReal sessao
    )
    {
        var acao = _interesse.Avaliar(
            personagemId,
            inimigoId,
            inimigoX,
            inimigoZ,
            jogadorX,
            jogadorZ
        );
        switch (acao)
        {
            case ControleInteresseDeInimigos.AcaoInteresse.Enviar:
                return true;
            case ControleInteresseDeInimigos.AcaoInteresse.EnviarSaida:
                tempoReal.Enviar(
                    sessao,
                    MobsMessageIds.InimigoSaiuDoInteresse,
                    new InimigoSaiuDoInteresse { InimigoId = inimigoId }
                );
                return false;
            default:
                return false;
        }
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
        foreach (var sessao in tempoReal.Sessoes)
        {
            if (
                !posicoes.TentarObter(sessao.PersonagemId, out var posicaoJogador)
                || posicaoJogador.Area != posicao.Area
            )
            {
                continue;
            }

            if (
                Filtrar(
                    sessao.PersonagemId,
                    inimigo.InimigoId,
                    inimigo.X,
                    inimigo.Z,
                    posicaoJogador.X,
                    posicaoJogador.Z,
                    sessao
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
        var daArea = Simulados().Where(g => g.Area == evento.Area).ToArray();
        if (daArea.Length == 0)
        {
            return;
        }

        var sessao = tempoReal.Sessoes.FirstOrDefault(s => s.PersonagemId == evento.PersonagemId);
        if (sessao == null)
        {
            return;
        }

        if (!posicoes.TentarObter(evento.PersonagemId, out var posicao))
        {
            return;
        }

        var vivos = daArea.SelectMany(g => g.Vivos());
        var filtrados = _interesse.FiltrarEntrada(evento.PersonagemId, posicao.X, posicao.Z, vivos);
        tempoReal.Enviar(
            sessao,
            MobsMessageIds.InimigosDaArea,
            new InimigosDaArea { Inimigos = filtrados }
        );
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
