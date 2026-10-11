using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

public sealed class MundoModule : IFeatureModule
{
    /// <summary>
    /// Chave de configuração que libera o pedido de velocidade de teste (só em
    /// appsettings.Development.json). Sem ela, o servidor ignora o pedido.
    /// </summary>
    public const string ChavePermitirVelocidadeDeTeste = "Mundo:PermitirVelocidadeDeTeste";

    private readonly CidadeDoMundo<SessaoTempoReal> _cidade = new();
    private bool _permitirVelocidadeDeTeste;
    private ServidorTempoReal? _tempoReal;
    private EventosDoServidor? _eventos;

    public string Nome => "Mundo";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        _permitirVelocidadeDeTeste = configuracao.GetValue<bool>(ChavePermitirVelocidadeDeTeste);
        servicos.AddSingleton<IPosicoesDoMundo>(new PosicoesDaCidade(_cidade));
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(MundoMessageIds.Faixa)
            .Ao<EntrarNaCidade>(MundoMessageIds.EntrarNaCidade, AoEntrar)
            .Ao<IntencaoDeMovimento>(MundoMessageIds.IntencaoDeMovimento, AoMover)
            .Ao<ConfirmarVelocidade>(MundoMessageIds.ConfirmarVelocidade, AoConfirmarVelocidade)
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

    private void AoMover(SessaoTempoReal sessao, IntencaoDeMovimento intencao)
    {
        var desejada = new PosicaoNaCidade(intencao.X, intencao.Y, intencao.Z, intencao.RotacaoY);
        var (resultado, avatar) = _cidade.Mover(sessao.PersonagemId, sessao, desejada);
        if (avatar == null)
        {
            return;
        }

        // O dono sempre recebe a posição autoritativa com a sequência (corrige a predição se recusada).
        _tempoReal!.Enviar(
            sessao,
            MundoMessageIds.PosicaoJogador,
            Posicao(avatar, intencao.Sequencia),
            EntregaTempoReal.Sequenciada
        );
        if (resultado != ResultadoMovimento.Aceito)
        {
            return;
        }

        var replica = Posicao(avatar, 0);
        foreach (var outro in _cidade.Presentes())
        {
            if (!ReferenceEquals(outro.Sessao, sessao))
            {
                _tempoReal.Enviar(
                    outro.Sessao,
                    MundoMessageIds.PosicaoJogador,
                    replica,
                    EntregaTempoReal.Sequenciada
                );
            }
        }
    }

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
