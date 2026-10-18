using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Moderacao;
using JogoDoGrupo.Shared.Core.TempoReal;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Core.Moderacao;

/// <summary>
/// Módulo de moderação e comandos de GM (#187).
/// </summary>
public sealed class ModeracaoModule : IFeatureModule
{
    private IServiceProvider? _servicos;
    private ServicoDeModeracao? _servico;
    private ServidorTempoReal? _tempoReal;

    public string Nome => "Moderacao";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<ServicoDeModeracao>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _servicos = rotas.ServiceProvider;
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens)
    {
        mensagens
            .FaixaCore()
            .Ao<PedirComandoGm>(TempoRealIds.PedirComandoGm, AoPedirComandoGm)
            .Ao<DenunciarJogador>(TempoRealIds.DenunciarJogador, AoDenunciarJogador);
    }

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        if (_servicos != null)
        {
            _servico = _servicos.GetService<ServicoDeModeracao>();
        }

        _servico ??= new ServicoDeModeracao(
            _servicos
                ?? throw new InvalidOperationException("Provedor de serviços não configurado."),
            contexto.TempoReal,
            _servicos?.GetService<ILogger<ServicoDeModeracao>>()
                ?? NullLogger<ServicoDeModeracao>.Instance
        );
    }

    private void AoPedirComandoGm(SessaoTempoReal sessao, PedirComandoGm pedido)
    {
        _ = Task.Run(async () =>
        {
            if (_servico != null && _tempoReal != null)
            {
                var resultado = await _servico.ProcessarComandoGmAsync(sessao, pedido);
                _tempoReal.Enviar(sessao, TempoRealIds.ResultadoComandoGm, resultado);
            }
        });
    }

    private void AoDenunciarJogador(SessaoTempoReal sessao, DenunciarJogador pedido)
    {
        _ = Task.Run(async () =>
        {
            if (_servico != null && _tempoReal != null)
            {
                var resultado = await _servico.ProcessarDenunciaAsync(sessao, pedido);
                _tempoReal.Enviar(sessao, TempoRealIds.ResultadoDenuncia, resultado);
            }
        });
    }
}
