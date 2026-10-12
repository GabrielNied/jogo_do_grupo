using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

public sealed class MundoModule : IFeatureModule
{
    /// <summary>
    /// Chave que libera o pedido de velocidade de teste. Sem ela, ou fora de Development, o
    /// servidor ignora o pedido (<see cref="PoliticaDeVelocidadeDeTeste"/>).
    /// </summary>
    public const string ChavePermitirVelocidadeDeTeste = PoliticaDeVelocidadeDeTeste.ChavePermitir;

    private readonly CidadeDoMundo<SessaoTempoReal> _cidade = new();
    private bool _permitirVelocidadeDeTeste;
    private ServidorTempoReal? _tempoReal;
    private EventosDoServidor? _eventos;

    // Criado pelo contêiner quando o app sobe (hosted service); o manipulador de rede só o usa
    // depois que uma sessão se autentica.
    private volatile ServicoDaHoraDoMundo? _hora;

    public string Nome => "Mundo";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IPosicoesDoMundo>(new PosicoesDaCidade(_cidade));
        servicos.AddSingleton<ITeleporteDeTeste>(sp => new TeleporteDeTeste(
            _cidade,
            sp.GetRequiredService<ServidorTempoReal>()
        ));
        servicos.AddSingleton<IDeslocamentosDoMundo>(new DeslocamentosDaCidade(_cidade));
        servicos.AddSingleton(sp => new RelogioDoMundo(sp.GetRequiredService<TimeProvider>()));
        servicos.AddSingleton(sp =>
        {
            var servico = new ServicoDaHoraDoMundo(
                sp.GetRequiredService<RelogioDoMundo>(),
                sp.GetRequiredService<ServidorTempoReal>(),
                () => _cidade.Presentes().Select(a => a.Sessao).ToArray(),
                sp.GetRequiredService<ILogger<ServicoDaHoraDoMundo>>()
            );
            _hora = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDaHoraDoMundo>());
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        var servicos = rotas.ServiceProvider;
        _permitirVelocidadeDeTeste = PoliticaDeVelocidadeDeTeste.Ligada(
            servicos.GetRequiredService<IConfiguration>(),
            servicos.GetRequiredService<IHostEnvironment>(),
            servicos.GetRequiredService<ILogger<MundoModule>>()
        );
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
    }

    private void AoEntrar(SessaoTempoReal sessao, EntrarNaCidade _)
    {
        var (novo, eu, outros) = _cidade.Entrar(sessao.PersonagemId, sessao);
        _tempoReal!.Enviar(
            sessao,
            MundoMessageIds.EstadoDaCidade,
            new EstadoDaCidade { Eu = Visivel(eu), Outros = outros.Select(Visivel).ToArray() }
        );
        if (_cidade.Velocidades(sessao.PersonagemId) is { } velocidades)
        {
            EnviarVelocidades(sessao, velocidades);
        }

        _hora?.Enviar(sessao);

        if (novo)
        {
            var aviso = new JogadorEntrou { Jogador = Visivel(eu) };
            foreach (var outro in outros)
            {
                _tempoReal.Enviar(outro.Sessao, MundoMessageIds.JogadorEntrou, aviso);
            }

            _eventos!.Publicar(new JogadorEntrouNaArea(sessao.PersonagemId, AreasDoMundo.Cidade));
        }
    }

    // O client é dono da própria posição (ADR 0024): o servidor valida e repassa aos outros,
    // sem devolver a posição ao dono. Só o caso grave vira PosicaoForcada, com confirmação.
    private void AoMover(SessaoTempoReal sessao, IntencaoDeMovimento intencao)
    {
        var desejada = new PosicaoNaCidade(intencao.X, intencao.Y, intencao.Z, intencao.RotacaoY);
        var (resultado, avatar) = _cidade.Mover(sessao.PersonagemId, sessao, desejada);
        if (avatar == null)
        {
            return;
        }

        switch (resultado)
        {
            case ResultadoMovimento.Recusado:
                if (_cidade.PosicaoForcadaPendente(sessao.PersonagemId) is { } sequencia)
                {
                    EnviarPosicaoForcada(sessao, avatar, sequencia);
                }

                return;
            case ResultadoMovimento.Aceito:
            case ResultadoMovimento.AceitoComSuspeita:
                Replicar(sessao, avatar);
                return;
            default:
                return;
        }
    }

    private void Replicar(SessaoTempoReal dono, AvatarNaCidade<SessaoTempoReal> avatar)
    {
        var replica = Posicao(avatar, 0);
        foreach (var outro in _cidade.Presentes())
        {
            if (!ReferenceEquals(outro.Sessao, dono))
            {
                _tempoReal!.Enviar(
                    outro.Sessao,
                    MundoMessageIds.PosicaoJogador,
                    replica,
                    EntregaTempoReal.Sequenciada
                );
            }
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
    ) => _cidade.ConfirmarPosicaoForcada(sessao.PersonagemId, sessao, confirmacao.Sequencia);

    private void AoConfirmarVelocidade(SessaoTempoReal sessao, ConfirmarVelocidade confirmacao) =>
        _cidade.ConfirmarVelocidades(sessao.PersonagemId, sessao, confirmacao.Sequencia);

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
            _cidade.MudarVelocidades(sessao.PersonagemId, sessao, pedido.Andar, pedido.Correr) is
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

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        if (!_cidade.Sair(sessao.PersonagemId, sessao))
        {
            return;
        }

        var aviso = new JogadorSaiu { PersonagemId = sessao.PersonagemId };
        foreach (var outro in _cidade.Presentes())
        {
            _tempoReal!.Enviar(outro.Sessao, MundoMessageIds.JogadorSaiu, aviso);
        }

        _eventos!.Publicar(new JogadorSaiuDaArea(sessao.PersonagemId, AreasDoMundo.Cidade));
    }

    private static JogadorVisivel Visivel(AvatarNaCidade<SessaoTempoReal> a) =>
        new()
        {
            PersonagemId = a.PersonagemId,
            X = a.Posicao.X,
            Y = a.Posicao.Y,
            Z = a.Posicao.Z,
            RotacaoY = a.Posicao.RotacaoY,
        };

    private static PosicaoJogador Posicao(AvatarNaCidade<SessaoTempoReal> a, int sequencia) =>
        new()
        {
            PersonagemId = a.PersonagemId,
            X = a.Posicao.X,
            Y = a.Posicao.Y,
            Z = a.Posicao.Z,
            RotacaoY = a.Posicao.RotacaoY,
            Sequencia = sequencia,
        };
}
