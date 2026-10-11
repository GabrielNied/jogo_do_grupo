using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Desenvolvimento;

namespace JogoDoGrupo.Server.Features.Desenvolvimento;

/// <summary>
/// Console de teste do editor e do build de desenvolvimento. Sem
/// <see cref="ChavePermitirComandos"/>, o pedido é ignorado e o client não recebe resposta.
/// Não é a ferramenta de GM de produção.
/// </summary>
public sealed class DesenvolvimentoModule : IFeatureModule
{
    /// <summary>
    /// Chave de configuração que libera os comandos (só em appsettings.Development.json).
    /// Sem ela, o servidor ignora o pedido.
    /// </summary>
    public const string ChavePermitirComandos = "Desenvolvimento:PermitirComandos";

    private readonly Lock _trava = new();
    private bool _permitir;
    private Task _fila = Task.CompletedTask;
    private ComandosDoServidor? _comandos;
    private ServidorTempoReal? _tempoReal;
    private ILogger<DesenvolvimentoModule>? _log;

    public string Nome => "Desenvolvimento";

    /// <summary>Verdadeiro só quando a configuração ligou os comandos.</summary>
    public static bool Ligado(IConfiguration configuracao) =>
        configuracao.GetValue<bool>(ChavePermitirComandos);

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        _permitir = Ligado(configuracao);
        servicos.AddSingleton<ComandosDoServidor>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _comandos = rotas.ServiceProvider.GetRequiredService<ComandosDoServidor>();
        _tempoReal = rotas.ServiceProvider.GetRequiredService<ServidorTempoReal>();
        _log = rotas.ServiceProvider.GetRequiredService<ILogger<DesenvolvimentoModule>>();
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(DesenvolvimentoMessageIds.Faixa)
            .Ao<PedirComandoDeTeste>(DesenvolvimentoMessageIds.PedirComandoDeTeste, AoPedir);

    public void Inicializar(ServerContext contexto) { }

    private void AoPedir(SessaoTempoReal sessao, PedirComandoDeTeste pedido)
    {
        if (!_permitir || _comandos == null)
        {
            return;
        }

        var personagemId = sessao.PersonagemId;
        var texto = pedido.Texto ?? "";
        lock (_trava)
        {
            _fila = _fila
                .ContinueWith(
                    _ => Processar(personagemId, texto),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default
                )
                .Unwrap();
        }
    }

    private async Task Processar(long personagemId, string texto)
    {
        (bool Aceito, string Texto) resultado;
        try
        {
            resultado = await _comandos!.ExecutarAsync(personagemId, texto, CancellationToken.None);
        }
        catch (Exception e)
        {
            _log!.LogError(e, "Comando de teste do personagem {Personagem} falhou.", personagemId);
            resultado = (false, "Comando falhou.");
        }

        var sessao = _tempoReal!.Sessoes.FirstOrDefault(s => s.PersonagemId == personagemId);
        if (sessao == null)
        {
            return;
        }

        _tempoReal.Enviar(
            sessao,
            DesenvolvimentoMessageIds.ResultadoDeComandoDeTeste,
            new ResultadoDeComandoDeTeste { Aceito = resultado.Aceito, Texto = resultado.Texto }
        );
    }
}
