using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Desenvolvimento;

namespace JogoDoGrupo.Server.Features.Desenvolvimento;

/// <summary>
/// Console de teste e tela de admin (F8) do editor e do build de desenvolvimento. Sem a chave,
/// fora de Development ou com a conta fora de <c>Desenvolvimento:Contas</c>, o pedido é
/// ignorado e o client não recebe resposta (<see cref="PoliticaDeComandos"/>). Não é a
/// ferramenta de GM de produção.
/// </summary>
public sealed class DesenvolvimentoModule : IFeatureModule
{
    /// <summary>Chave que libera os comandos. Veja <see cref="PoliticaDeComandos"/>.</summary>
    public const string ChavePermitirComandos = PoliticaDeComandos.ChavePermitirComandos;

    private readonly Lock _trava = new();
    private PoliticaDeComandos? _politica;
    private Task _fila = Task.CompletedTask;
    private ComandosDoServidor? _comandos;
    private ExecutorDeAcoesDeAdmin? _acoes;
    private ServidorTempoReal? _tempoReal;
    private ILogger? _log;

    public string Nome => "Desenvolvimento";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<ComandosDoServidor>();
        servicos.AddSingleton<ExecutorDeAcoesDeAdmin>();
    }

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
        _acoes = servicos.GetRequiredService<ExecutorDeAcoesDeAdmin>();
        _tempoReal = servicos.GetRequiredService<ServidorTempoReal>();
    }

    /// <summary>Liga as peças sem o host (testes).</summary>
    public void Configurar(
        PoliticaDeComandos politica,
        ComandosDoServidor comandos,
        ServidorTempoReal? tempoReal,
        ILogger log,
        ExecutorDeAcoesDeAdmin? acoes = null
    )
    {
        _politica = politica;
        _comandos = comandos;
        _acoes = acoes;
        _tempoReal = tempoReal;
        _log = log;
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(DesenvolvimentoMessageIds.Faixa)
            .Ao<PedirComandoDeTeste>(DesenvolvimentoMessageIds.PedirComandoDeTeste, AoPedir)
            .Ao<PedirAcaoDeAdmin>(DesenvolvimentoMessageIds.PedirAcaoDeAdmin, AoPedirAcao);

    public void Inicializar(ServerContext contexto) { }

    private void AoPedir(SessaoTempoReal sessao, PedirComandoDeTeste pedido) =>
        _ = Pedir(sessao.ContaId, sessao.PersonagemId, pedido.Texto);

    private void AoPedirAcao(SessaoTempoReal sessao, PedirAcaoDeAdmin pedido) =>
        _ = PedirAcao(sessao.ContaId, sessao.PersonagemId, pedido);

    /// <summary>
    /// Enfileira o comando. Desligado ou conta fora da lista: ignora sem responder. O Task
    /// devolvido termina quando o comando acaba (os testes esperam por ele).
    /// </summary>
    public Task Pedir(long contaId, long personagemId, string? texto)
    {
        if (_comandos == null || !Liberado(contaId))
        {
            return Task.CompletedTask;
        }

        var comando = texto ?? "";
        return Enfileirar(() => Processar(contaId, personagemId, comando));
    }

    /// <summary>
    /// Enfileira a ação da tela de admin, com a mesma trava e a mesma fila dos comandos de
    /// texto (uma de cada vez, na ordem em que chegaram).
    /// </summary>
    public Task PedirAcao(long contaId, long personagemId, PedirAcaoDeAdmin? pedido)
    {
        if (_acoes == null || pedido == null || !Liberado(contaId))
        {
            return Task.CompletedTask;
        }

        return Enfileirar(() => ProcessarAcao(contaId, personagemId, pedido));
    }

    private bool Liberado(long contaId)
    {
        if (_politica is not { Ligada: true })
        {
            return false;
        }

        if (!_politica.ContaPermitida(contaId))
        {
            _log!.LogWarning(
                "Comando de teste ignorado: conta {Conta} fora de {Chave}.",
                contaId,
                PoliticaDeComandos.ChaveContas
            );
            return false;
        }

        return true;
    }

    private Task Enfileirar(Func<Task> trabalho)
    {
        lock (_trava)
        {
            _fila = _fila
                .ContinueWith(
                    _ => trabalho(),
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

        if (_tempoReal == null || !_tempoReal.TentarObterSessao(personagemId, out var sessao))
        {
            return;
        }

        _tempoReal.Enviar(
            sessao,
            DesenvolvimentoMessageIds.ResultadoDeComandoDeTeste,
            new ResultadoDeComandoDeTeste { Aceito = resultado.Aceito, Texto = resultado.Texto }
        );
    }

    private async Task ProcessarAcao(long contaId, long personagemId, PedirAcaoDeAdmin pedido)
    {
        _log!.LogInformation(
            "Ação de admin da conta {Conta}, personagem {Personagem}: {Acao} modelo {Modelo} grau {Grau} origem {Origem} quantidade {Quantidade} valor {Valor} espécie {Especie}",
            contaId,
            personagemId,
            pedido.Acao,
            pedido.ModeloId,
            pedido.Grau,
            pedido.Origem,
            pedido.Quantidade,
            pedido.Valor,
            pedido.EspecieId
        );

        (bool Aceito, string Texto) resultado;
        try
        {
            resultado = await _acoes!.ExecutarAsync(personagemId, pedido, CancellationToken.None);
        }
        catch (Exception e)
        {
            _log.LogError(
                e,
                "Ação de admin {Acao} do personagem {Personagem} falhou.",
                pedido.Acao,
                personagemId
            );
            resultado = (false, "Ação falhou.");
        }

        if (_tempoReal == null || !_tempoReal.TentarObterSessao(personagemId, out var sessao))
        {
            return;
        }

        _tempoReal.Enviar(
            sessao,
            DesenvolvimentoMessageIds.ResultadoDeAcaoDeAdmin,
            new ResultadoDeAcaoDeAdmin
            {
                Acao = pedido.Acao,
                Aceito = resultado.Aceito,
                Texto = resultado.Texto,
            }
        );
    }
}
