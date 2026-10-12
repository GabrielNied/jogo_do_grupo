using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Desenvolvimento;

namespace JogoDoGrupo.Server.Features.Desenvolvimento;

/// <summary>
/// Console de teste do editor e do build de desenvolvimento. Sem a chave, fora de Development
/// ou com a conta fora de <c>Desenvolvimento:Contas</c>, o pedido é ignorado e o client não
/// recebe resposta (<see cref="PoliticaDeComandos"/>). Não é a ferramenta de GM de produção.
/// </summary>
public sealed class DesenvolvimentoModule : IFeatureModule
{
    /// <summary>Chave que libera os comandos. Veja <see cref="PoliticaDeComandos"/>.</summary>
    public const string ChavePermitirComandos = PoliticaDeComandos.ChavePermitirComandos;

    private readonly Lock _trava = new();
    private PoliticaDeComandos? _politica;
    private Task _fila = Task.CompletedTask;
    private ComandosDoServidor? _comandos;
    private ServidorTempoReal? _tempoReal;
    private ILogger? _log;

    public string Nome => "Desenvolvimento";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) =>
        servicos.AddSingleton<ComandosDoServidor>();

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        var servicos = rotas.ServiceProvider;
        _log = servicos.GetRequiredService<ILogger<DesenvolvimentoModule>>();
        _politica = PoliticaDeComandos.Ler(
            servicos.GetRequiredService<IConfiguration>(),
            servicos.GetRequiredService<IHostEnvironment>(),
            _log
        );
        _comandos = servicos.GetRequiredService<ComandosDoServidor>();
        _tempoReal = servicos.GetRequiredService<ServidorTempoReal>();
    }

    /// <summary>Liga as peças sem o host (testes).</summary>
    public void Configurar(
        PoliticaDeComandos politica,
        ComandosDoServidor comandos,
        ServidorTempoReal? tempoReal,
        ILogger log
    )
    {
        _politica = politica;
        _comandos = comandos;
        _tempoReal = tempoReal;
        _log = log;
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(DesenvolvimentoMessageIds.Faixa)
            .Ao<PedirComandoDeTeste>(DesenvolvimentoMessageIds.PedirComandoDeTeste, AoPedir);

    public void Inicializar(ServerContext contexto) { }

    private void AoPedir(SessaoTempoReal sessao, PedirComandoDeTeste pedido) =>
        _ = Pedir(sessao.ContaId, sessao.PersonagemId, pedido.Texto);

    /// <summary>
    /// Enfileira o comando. Desligado ou conta fora da lista: ignora sem responder. O Task
    /// devolvido termina quando o comando acaba (os testes esperam por ele).
    /// </summary>
    public Task Pedir(long contaId, long personagemId, string? texto)
    {
        if (_politica is not { Ligada: true } || _comandos == null)
        {
            return Task.CompletedTask;
        }

        if (!_politica.ContaPermitida(contaId))
        {
            _log!.LogWarning(
                "Comando de teste ignorado: conta {Conta} fora de {Chave}.",
                contaId,
                PoliticaDeComandos.ChaveContas
            );
            return Task.CompletedTask;
        }

        var comando = texto ?? "";
        lock (_trava)
        {
            _fila = _fila
                .ContinueWith(
                    _ => Processar(contaId, personagemId, comando),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default
                )
                .Unwrap();
            return _fila;
        }
    }

    private async Task Processar(long contaId, long personagemId, string texto)
    {
        _log!.LogInformation(
            "Comando de teste da conta {Conta}, personagem {Personagem}: {Comando}",
            contaId,
            personagemId,
            texto
        );

        (bool Aceito, string Texto) resultado;
        try
        {
            resultado = await _comandos!.ExecutarAsync(personagemId, texto, CancellationToken.None);
        }
        catch (Exception e)
        {
            _log.LogError(e, "Comando de teste do personagem {Personagem} falhou.", personagemId);
            resultado = (false, "Comando falhou.");
        }

        var sessao = _tempoReal?.Sessoes.FirstOrDefault(s => s.PersonagemId == personagemId);
        if (sessao == null || _tempoReal == null)
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
