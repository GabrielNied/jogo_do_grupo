using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

public sealed class MundoModule
    : IFeatureModule,
        ITransicoesDeAreaDoMundo,
        IControleDeVelocidadeDoMundo
{
    /// <summary>
    /// Chave que libera o pedido de velocidade de teste. Sem ela, ou fora de Development, o
    /// servidor ignora o pedido (<see cref="PoliticaDeVelocidadeDeTeste"/>).
    /// </summary>
    public const string ChavePermitirVelocidadeDeTeste = PoliticaDeVelocidadeDeTeste.ChavePermitir;

    private readonly EstadoDasAreasDoMundo<SessaoTempoReal> _areas = new(() =>
        Environment.TickCount64
    );
    private readonly GradeDeInteresse _grade = new();
    private readonly ConcurrentDictionary<long, (string Nome, int Raca)> _identidades = new();
    private IDiretorioDePersonagens? _diretorio;
    private bool _permitirVelocidadeDeTeste;
    private ServidorTempoReal? _tempoReal;
    private EventosDoServidor? _eventos;
    private IHistoricoDePosicoesDoMundo? _historico;
    private IEstadoDeQueda? _queda;

    public MundoModule()
        : this(null) { }

    public MundoModule(IEstadoDeQueda? queda)
    {
        _queda = queda;
    }

    // Criado pelo contêiner quando o app sobe (hosted service); o manipulador de rede só o usa
    // depois que uma sessão se autentica.
    private volatile ServicoDaHoraDoMundo? _hora;
    private volatile ServicoDePerigosDoMundo? _perigos;

    public string Nome => "Mundo";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IPosicoesDoMundo>(new PosicoesDoMundo<SessaoTempoReal>(_areas));
        servicos.AddSingleton<IVizinhanca>(new VizinhancaDoMundo<SessaoTempoReal>(_areas, _grade));
        servicos.AddSingleton<ITransicoesDeAreaDoMundo>(this);
        servicos.AddSingleton<IControleDeVelocidadeDoMundo>(this);
        servicos.AddSingleton<ITeleporteDeTeste>(sp => new TeleporteDeTeste(
            _areas,
            sp.GetRequiredService<ServidorTempoReal>(),
            (area, avatar) => Replicar(area, avatar.Sessao, avatar)
        ));
        servicos.AddSingleton<IDeslocamentosDoMundo>(new DeslocamentosDaCidade(_areas));
        servicos.AddSingleton<ILimiteDeVelocidadeDoMundo>(new LimiteDeVelocidadeDaCidade(_areas));
        servicos.AddSingleton(sp => new RelogioDoMundo(sp.GetRequiredService<TimeProvider>()));
        servicos.AddSingleton<IRelogioDoMundo>(sp => sp.GetRequiredService<RelogioDoMundo>());
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDaHoraDoMundo(
                sp.GetRequiredService<RelogioDoMundo>(),
                sp.GetRequiredService<ServidorTempoReal>(),
                () => _areas.Cidade.Presentes().Select(a => a.Sessao).ToArray(),
                sp.GetRequiredService<ILogger<ServicoDaHoraDoMundo>>()
            );
            _hora = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDaHoraDoMundo>());
        // Posição do grupo para o mapa (#789): o grupo vem do contrato do Social, se carregado.
        servicos.AddSingleton(sp => new ServicoDePosicaoDoGrupo(
            () => _areas.PresentesEmTodasAsAreas(),
            _grade.Vistos,
            () => sp.GetService<IConsultaDeGrupo>(),
            sp.GetRequiredService<IPosicoesDoMundo>(),
            sp.GetRequiredService<ServidorTempoReal>(),
            sp.GetRequiredService<ILogger<ServicoDePosicaoDoGrupo>>()
        ));
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDePosicaoDoGrupo>());
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDePerigosDoMundo(
                () => _areas.Cidade.Presentes(),
                sp.GetRequiredService<ILimiteDeVelocidadeDoMundo>(),
                sp.GetService<IDanoAoPersonagem>(),
                sp.GetService<EventosDoServidor>(),
                log: sp.GetService<ILogger<ServicoDePerigosDoMundo>>()
            );
            _perigos = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDePerigosDoMundo>());
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        var servicos = rotas.ServiceProvider;
        // Personagem pode não estar carregada (testes só do Mundo): sem ela, nome e raça vão vazios.
        _diretorio = servicos.GetService<IDiretorioDePersonagens>();
        _queda ??= servicos.GetService<IEstadoDeQueda>();
        _permitirVelocidadeDeTeste = PoliticaDeVelocidadeDeTeste.Ligada(
            servicos.GetRequiredService<IConfiguration>(),
            servicos.GetRequiredService<IHostEnvironment>(),
            servicos.GetRequiredService<ILogger<MundoModule>>()
        );
        _historico = servicos.GetRequiredService<IHistoricoDePosicoesDoMundo>();
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(MundoMessageIds.Faixa)
            .Ao<EntrarNaCidade>(MundoMessageIds.EntrarNaCidade, AoEntrar)
            .Ao<IntencaoDeMovimento>(MundoMessageIds.IntencaoDeMovimento, AoMover)
            .Ao<ConfirmarVelocidade>(MundoMessageIds.ConfirmarVelocidade, AoConfirmarVelocidade)
            .Ao<ConfirmarPosicaoForcada>(
                MundoMessageIds.ConfirmarPosicaoForcada,
                AoConfirmarPosicaoForcada
            )
            .Ao<PedirVelocidadeDeTeste>(
                MundoMessageIds.PedirVelocidadeDeTeste,
                AoPedirVelocidadeDeTeste
            );

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        _eventos = contexto.Eventos;
        _tempoReal.SessaoEncerrada += AoEncerrar;
        contexto.Eventos.Assinar<PersonagemVoltouParaACidade>(AoVoltarDaQueda);
    }

    /// <summary>
    /// Combate enche os recursos e avisa o retorno; o Mundo leva o personagem ao nascimento da
    /// cidade (docs/features/mundo.md, "Nascimento"; fatia-jogavel.md, seção 7).
    /// </summary>
    private void AoVoltarDaQueda(PersonagemVoltouParaACidade evento)
    {
        var nascimento = ValidadorDeMovimento.Nascimento;
        if (evento.Area != AreasDoMundo.Cidade)
        {
            TentarMoverParaArea(
                evento.PersonagemId,
                new PosicaoNoMundo(
                    AreasDoMundo.Cidade,
                    nascimento.X,
                    nascimento.Y,
                    nascimento.Z,
                    nascimento.RotacaoY
                )
            );
            return;
        }

        if (_areas.Cidade.ForcarPosicao(evento.PersonagemId, nascimento) is not { } sequencia)
        {
            return;
        }

        if (_areas.Cidade.Obter(evento.PersonagemId) is not { } avatar)
        {
            return;
        }

        EnviarPosicaoForcada(avatar.Sessao, avatar, sequencia);
        Replicar(AreasDoMundo.Cidade, avatar.Sessao, avatar);
    }

    private void AoEntrar(SessaoTempoReal sessao, EntrarNaCidade _)
    {
        if (_areas.Obter(sessao.PersonagemId) is { } atual && atual.Area != AreasDoMundo.Cidade)
        {
            // A reconexão pode trocar a sessão antes do pedido de entrada na cidade. Atualiza a
            // sessão mantendo a posição da área atual; a transição abaixo remove a presença velha.
            _areas.Localizar(sessao.PersonagemId)?.Estado.Entrar(sessao.PersonagemId, sessao);
            TentarMoverParaArea(
                sessao.PersonagemId,
                new PosicaoNoMundo(
                    AreasDoMundo.Cidade,
                    ValidadorDeMovimento.Nascimento.X,
                    ValidadorDeMovimento.Nascimento.Y,
                    ValidadorDeMovimento.Nascimento.Z,
                    ValidadorDeMovimento.Nascimento.RotacaoY
                )
            );
            return;
        }

        var (novo, eu, outros) = _areas.Entrar(
            sessao.PersonagemId,
            sessao,
            AreasDoMundo.Cidade,
            ValidadorDeMovimento.Nascimento
        );
        // Na cidade cada um recebe só quem está dentro do raio de visão (#500).
        var mudanca = _grade.Atualizar(eu.PersonagemId, eu.Posicao.X, eu.Posicao.Z);
        var vistos = _grade.Vistos(eu.PersonagemId);
        var proximos = outros.Where(o => vistos.Contains(o.PersonagemId)).ToArray();
        _tempoReal!.Enviar(
            sessao,
            MundoMessageIds.EstadoDaCidade,
            new EstadoDaCidade { Eu = Visivel(eu), Outros = proximos.Select(Visivel).ToArray() }
        );
        if (_areas.Cidade.Velocidades(sessao.PersonagemId) is { } velocidades)
        {
            EnviarVelocidades(sessao, velocidades);
        }

        _hora?.Enviar(sessao);

        // Quem passou a ver o recém-chegado (ou voltou a ver, numa sessão nova) recebe a entrada.
        var aviso = new JogadorEntrou { Jogador = Visivel(eu) };
        foreach (var outro in proximos.Where(o => mudanca.Entraram.Contains(o.PersonagemId)))
        {
            _tempoReal.Enviar(outro.Sessao, MundoMessageIds.JogadorEntrou, aviso);
        }

        if (novo)
        {
            _eventos!.Publicar(new JogadorEntrouNaArea(sessao.PersonagemId, AreasDoMundo.Cidade));
        }

        RegistrarPosicaoDoJogador(AreasDoMundo.Cidade, eu);
    }

    // O client é dono da própria posição (ADR 0024): o servidor valida e repassa aos outros,
    // sem devolver a posição ao dono. Só o caso grave vira PosicaoForcada, com confirmação.
    private void AoMover(SessaoTempoReal sessao, IntencaoDeMovimento intencao)
    {
        var desejada = new PosicaoNaCidade(intencao.X, intencao.Y, intencao.Z, intencao.RotacaoY);
        var localizado = _areas.Localizar(sessao.PersonagemId);
        if (localizado is null)
        {
            return;
        }

        var (area, estadoArea) = localizado.Value;

        // Enquanto o personagem está caído ou sendo levantado, não pode andar (personagem.md).
        if (_queda?.EstaCaido(sessao.PersonagemId) == true)
        {
            if (estadoArea.Obter(sessao.PersonagemId) is { } avatarCaido)
            {
                var distancia = ValidadorDeMovimento.DistanciaHorizontal(
                    avatarCaido.Posicao,
                    desejada
                );
                if (
                    distancia > 0.01f
                    && estadoArea.PosicaoForcadaPendente(sessao.PersonagemId) is not { }
                )
                {
                    if (
                        estadoArea.ForcarPosicao(sessao.PersonagemId, avatarCaido.Posicao) is
                        { } sequencia
                    )
                    {
                        EnviarPosicaoForcada(sessao, avatarCaido, sequencia);
                    }
                }
            }

            return;
        }

        var (resultado, avatar) = estadoArea.Mover(sessao.PersonagemId, sessao, desejada);
        if (avatar == null)
        {
            return;
        }

        switch (resultado)
        {
            case ResultadoMovimento.Recusado:
                if (estadoArea.PosicaoForcadaPendente(sessao.PersonagemId) is { } sequencia)
                {
                    EnviarPosicaoForcada(sessao, avatar, sequencia);
                }

                return;
            case ResultadoMovimento.Aceito:
            case ResultadoMovimento.AceitoComSuspeita:
                if (area == AreasDoMundo.Cidade)
                {
                    _perigos?.VerificarPasso(
                        sessao.PersonagemId,
                        avatar.Posicao.X,
                        avatar.Posicao.Z
                    );
                }

                Replicar(area, sessao, avatar);
                return;
            default:
                return;
        }
    }

    private void Replicar(string area, SessaoTempoReal dono, AvatarNaCidade<SessaoTempoReal> avatar)
    {
        if (area == AreasDoMundo.Cidade)
        {
            NotificarMudanca(
                avatar,
                _grade.Atualizar(avatar.PersonagemId, avatar.Posicao.X, avatar.Posicao.Z)
            );
        }

        var replica = Posicao(avatar, avatar.SequenciaReplica);
        var outros = Observadores(area, avatar.PersonagemId)
            .Where(outro => !ReferenceEquals(outro.Sessao, dono))
            .Select(outro => outro.Sessao)
            .ToArray();
        _tempoReal!.Enviar(
            outros,
            MundoMessageIds.PosicaoJogador,
            replica,
            EntregaTempoReal.Sequenciada,
            avatar.PersonagemId
        );
        RegistrarPosicaoDoJogador(area, avatar);
    }

    private void RegistrarPosicaoDoJogador(string area, AvatarNaCidade<SessaoTempoReal> avatar)
    {
        var p = avatar.Posicao;
        _historico?.Registrar(
            avatar.PersonagemId,
            new PosicaoNoMundo(area, p.X, p.Y, p.Z, p.RotacaoY),
            Environment.TickCount64
        );
    }

    /// <summary>
    /// Quem enxerga o personagem: na cidade, os que estão no raio de visão; nas instâncias
    /// (pequenas e isoladas), todos os outros da área.
    /// </summary>
    private AvatarNaCidade<SessaoTempoReal>[] Observadores(string area, long personagemId)
    {
        var presentes = _areas.ObterOuCriar(area).Presentes();
        if (area != AreasDoMundo.Cidade)
        {
            return presentes.Where(a => a.PersonagemId != personagemId).ToArray();
        }

        var vistos = _grade.Vistos(personagemId);
        return presentes.Where(a => vistos.Contains(a.PersonagemId)).ToArray();
    }

    // Quem passou a se ver recebe a entrada um do outro; quem deixou de se ver, a saída.
    private void NotificarMudanca(
        AvatarNaCidade<SessaoTempoReal> avatar,
        MudancaDeInteresse mudanca
    )
    {
        foreach (var id in mudanca.Entraram)
        {
            if (_areas.Cidade.Obter(id) is not { } outro)
            {
                continue;
            }

            _tempoReal!.Enviar(
                outro.Sessao,
                MundoMessageIds.JogadorEntrou,
                new JogadorEntrou { Jogador = Visivel(avatar) }
            );
            _tempoReal.Enviar(
                avatar.Sessao,
                MundoMessageIds.JogadorEntrou,
                new JogadorEntrou { Jogador = Visivel(outro) }
            );
        }

        foreach (var id in mudanca.Sairam)
        {
            if (_areas.Cidade.Obter(id) is { } outro)
            {
                _tempoReal!.Enviar(
                    outro.Sessao,
                    MundoMessageIds.JogadorSaiu,
                    new JogadorSaiu { PersonagemId = avatar.PersonagemId }
                );
            }

            _tempoReal!.Enviar(
                avatar.Sessao,
                MundoMessageIds.JogadorSaiu,
                new JogadorSaiu { PersonagemId = id }
            );
        }
    }

    private void EnviarPosicaoForcada(
        SessaoTempoReal sessao,
        AvatarNaCidade<SessaoTempoReal> avatar,
        int sequencia
    ) =>
        _tempoReal!.Enviar(
            sessao,
            MundoMessageIds.PosicaoForcada,
            new PosicaoForcada
            {
                X = avatar.Posicao.X,
                Y = avatar.Posicao.Y,
                Z = avatar.Posicao.Z,
                RotacaoY = avatar.Posicao.RotacaoY,
                Sequencia = sequencia,
            }
        );

    private void AoConfirmarPosicaoForcada(
        SessaoTempoReal sessao,
        ConfirmarPosicaoForcada confirmacao
    ) =>
        _areas
            .ParaPersonagem(sessao.PersonagemId)
            ?.ConfirmarPosicaoForcada(sessao.PersonagemId, sessao, confirmacao.Sequencia);

    private void AoConfirmarVelocidade(SessaoTempoReal sessao, ConfirmarVelocidade confirmacao) =>
        _areas
            .ParaPersonagem(sessao.PersonagemId)
            ?.ConfirmarVelocidades(sessao.PersonagemId, sessao, confirmacao.Sequencia);

    // Equivalente ao .modify speed de GM do WoW: só com a configuração de desenvolvimento.
    private void AoPedirVelocidadeDeTeste(SessaoTempoReal sessao, PedirVelocidadeDeTeste pedido)
    {
        if (
            !_permitirVelocidadeDeTeste
            || !RegrasVelocidade.TesteValido(pedido.Andar, pedido.Correr)
        )
        {
            return;
        }

        if (
            _areas
                .ParaPersonagem(sessao.PersonagemId)
                ?.MudarVelocidades(sessao.PersonagemId, sessao, pedido.Andar, pedido.Correr) is
            { } novas
        )
        {
            EnviarVelocidades(sessao, novas);
        }
    }

    private void EnviarVelocidades(SessaoTempoReal sessao, VelocidadesNaCidade v) =>
        _tempoReal!.Enviar(
            sessao,
            MundoMessageIds.VelocidadesDeMovimento,
            new VelocidadesDeMovimento
            {
                Andar = v.Andar,
                Correr = v.Correr,
                Sequencia = v.Sequencia,
            }
        );

    public bool DefinirVelocidades(long personagemId, float andar, float correr)
    {
        if (_areas.Obter(personagemId) is not { } localizado)
        {
            return false;
        }

        if (
            _areas
                .ParaPersonagem(personagemId)
                ?.MudarVelocidades(personagemId, localizado.Avatar.Sessao, andar, correr) is
            { } novas
        )
        {
            EnviarVelocidades(localizado.Avatar.Sessao, novas);
            return true;
        }

        return false;
    }

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        var localizado = _areas.Localizar(sessao.PersonagemId);
        if (localizado is null)
        {
            return;
        }

        var (area, estadoArea) = localizado.Value;
        if (!estadoArea.Sair(sessao.PersonagemId, sessao))
        {
            return;
        }

        _areas.RegistrarSaida(sessao.PersonagemId, area);
        _perigos?.RemoverJogador(sessao.PersonagemId);

        var aviso = new JogadorSaiu { PersonagemId = sessao.PersonagemId };
        foreach (var outro in AosQueViam(area, sessao.PersonagemId, estadoArea.Presentes()))
        {
            _tempoReal!.Enviar(outro.Sessao, MundoMessageIds.JogadorSaiu, aviso);
        }

        _eventos!.Publicar(new JogadorSaiuDaArea(sessao.PersonagemId, area));
    }

    public bool TentarMoverParaArea(long personagemId, PosicaoNoMundo destino)
    {
        if (
            !AreasDoMundo.EhValida(destino.Area)
            || !float.IsFinite(destino.X)
            || !float.IsFinite(destino.Y)
            || !float.IsFinite(destino.Z)
            || !float.IsFinite(destino.RotacaoY)
            || _tempoReal == null
            || _eventos == null
        )
        {
            return false;
        }

        var atual = _areas.Obter(personagemId);
        if (atual == null)
        {
            return false;
        }

        var transicao = _areas.Mover(
            personagemId,
            atual.Value.Avatar.Sessao,
            destino.Area,
            new PosicaoNaCidade(destino.X, destino.Y, destino.Z, destino.RotacaoY)
        );
        if (transicao == null)
        {
            return false;
        }

        var sessao = transicao.Eu.Sessao;
        var vizinhos = transicao.OutrosNoDestino;
        if (destino.Area == AreasDoMundo.Cidade)
        {
            // Entrar na cidade pelo portão ou pela queda: só quem está no raio de visão.
            var mudanca = _grade.Atualizar(
                personagemId,
                transicao.Eu.Posicao.X,
                transicao.Eu.Posicao.Z
            );
            vizinhos = vizinhos.Where(o => mudanca.Entraram.Contains(o.PersonagemId)).ToArray();
        }

        foreach (var outro in AosQueViam(transicao.Origem, personagemId, transicao.OutrosNaOrigem))
        {
            _tempoReal.Enviar(
                outro.Sessao,
                MundoMessageIds.JogadorSaiu,
                new JogadorSaiu { PersonagemId = personagemId }
            );
        }

        _eventos.Publicar(new JogadorSaiuDaArea(personagemId, transicao.Origem));

        if (destino.Area == AreasDoMundo.Cidade)
        {
            _tempoReal.Enviar(
                sessao,
                MundoMessageIds.EstadoDaCidade,
                new EstadoDaCidade
                {
                    Eu = Visivel(transicao.Eu),
                    Outros = vizinhos.Select(Visivel).ToArray(),
                }
            );
        }
        else
        {
            _tempoReal.Enviar(
                sessao,
                MundoMessageIds.EstadoDaArea,
                new EstadoDaArea
                {
                    Area = destino.Area,
                    Eu = Visivel(transicao.Eu),
                    Outros = vizinhos.Select(Visivel).ToArray(),
                }
            );
        }
        if (_areas.ParaPersonagem(personagemId)?.Velocidades(personagemId) is { } velocidades)
        {
            EnviarVelocidades(sessao, velocidades);
        }

        foreach (var outro in vizinhos)
        {
            _tempoReal.Enviar(
                outro.Sessao,
                MundoMessageIds.JogadorEntrou,
                new JogadorEntrou { Jogador = Visivel(transicao.Eu) }
            );
        }

        _eventos.Publicar(new JogadorEntrouNaArea(personagemId, destino.Area));
        if (destino.Area == AreasDoMundo.Cidade)
        {
            _hora?.Enviar(sessao);
        }

        return true;
    }

    // Na saída da área: quem via o personagem (a grade na cidade; todos nas instâncias).
    private AvatarNaCidade<SessaoTempoReal>[] AosQueViam(
        string area,
        long personagemId,
        AvatarNaCidade<SessaoTempoReal>[] presentes
    )
    {
        if (area != AreasDoMundo.Cidade)
        {
            return presentes.Where(a => a.PersonagemId != personagemId).ToArray();
        }

        var viam = _grade.Remover(personagemId);
        return presentes.Where(a => viam.Contains(a.PersonagemId)).ToArray();
    }

    private JogadorVisivel Visivel(AvatarNaCidade<SessaoTempoReal> a)
    {
        if (!_identidades.TryGetValue(a.PersonagemId, out var identidade))
        {
            identidade = Identidade(a.PersonagemId);
            if (identidade.Nome.Length > 0)
            {
                _identidades[a.PersonagemId] = identidade;
            }
        }

        var (nome, raca) = identidade;
        return new JogadorVisivel
        {
            PersonagemId = a.PersonagemId,
            X = a.Posicao.X,
            Y = a.Posicao.Y,
            Z = a.Posicao.Z,
            RotacaoY = a.Posicao.RotacaoY,
            Nome = nome,
            Raca = raca,
        };
    }

    // Nome e raça não mudam depois da criação: uma consulta ao banco por personagem.
    private (string Nome, int Raca) Identidade(long personagemId)
    {
        if (_diretorio == null || !_diretorio.TentarObterNome(personagemId, out var nome))
        {
            return ("", 0);
        }

        _diretorio.TentarObterRaca(personagemId, out var raca);
        return (nome, raca);
    }

    private static PosicaoJogador Posicao(AvatarNaCidade<SessaoTempoReal> a, int sequencia) =>
        new()
        {
            PersonagemId = a.PersonagemId,
            X = a.Posicao.X,
            Y = a.Posicao.Y,
            Z = a.Posicao.Z,
            RotacaoY = a.Posicao.RotacaoY,
            Sequencia = sequencia,
            TempoServidorMs = a.UltimoMs > 0 ? a.UltimoMs : Environment.TickCount64,
        };
}
