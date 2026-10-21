using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Trata as mensagens de rede de correio (#212) entre o tempo real UDP e o <see cref="ServicoDeCorreio"/>.
/// </summary>
public sealed class CorreioDoServidor(
    IServiceScopeFactory escopos,
    OpcoesNpcEconomia opcoes,
    ILogger<CorreioDoServidor> log
)
{
    private ServidorTempoReal? _tempoReal;

    public void Ligar(ServidorTempoReal tempoReal) => _tempoReal = tempoReal;

    private void Pedido(
        SessaoTempoReal sessao,
        Func<IServiceProvider, CancellationToken, Task> trabalho
    ) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                await trabalho(escopo.ServiceProvider, CancellationToken.None);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Falha ao processar pedido de correio");
                Enviar(
                    sessao,
                    EconomiaMessageIds.EconomiaRecusada,
                    new EconomiaRecusada(CodigosErroEconomia.CartaNaoEncontrada)
                );
            }
        });

    public void AoConsultar(SessaoTempoReal sessao, ConsultarCorreio _) =>
        Pedido(
            sessao,
            async (sp, c) =>
            {
                var servico = sp.GetRequiredService<ServicoDeCorreio>();
                var estado = await servico.ConsultarAsync(sessao.PersonagemId, c);
                Enviar(sessao, EconomiaMessageIds.EstadoDoCorreio, estado);
            }
        );

    public void AoLer(SessaoTempoReal sessao, LerCarta pedido) =>
        Pedido(
            sessao,
            async (sp, c) =>
            {
                var servico = sp.GetRequiredService<ServicoDeCorreio>();
                var (erro, detalhes) = await servico.LerCartaAsync(
                    sessao.PersonagemId,
                    pedido.CartaId,
                    c
                );
                if (erro != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EconomiaRecusada, new EconomiaRecusada(erro));
                    return;
                }

                if (detalhes != null)
                {
                    Enviar(sessao, EconomiaMessageIds.DetalhesDaCarta, detalhes);
                }
            }
        );

    public void AoEnviar(SessaoTempoReal sessao, EnviarCarta pedido) =>
        Pedido(
            sessao,
            async (sp, c) =>
            {
                var servico = sp.GetRequiredService<ServicoDeCorreio>();
                var posicoes = sp.GetService<IPosicoesDoMundo>();
                var x = 0f;
                var z = 0f;
                if (posicoes != null && posicoes.TentarObter(sessao.PersonagemId, out var pos))
                {
                    x = pos.X;
                    z = pos.Z;
                }

                var (erro, cartaId, destId) = await servico.EnviarCartaAsync(
                    sessao.PersonagemId,
                    pedido.Destinatario,
                    pedido.Assunto,
                    pedido.Mensagem,
                    pedido.Coroas,
                    pedido.InstanciasItens,
                    opcoes.ExigirProximidade,
                    x,
                    z,
                    c
                );

                if (erro != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EconomiaRecusada, new EconomiaRecusada(erro));
                    return;
                }

                Enviar(
                    sessao,
                    EconomiaMessageIds.CartaEnviada,
                    new CartaEnviada { CartaId = cartaId }
                );

                // Notifica destinatário se online
                if (_tempoReal != null && _tempoReal.TentarObterSessao(destId, out var sessaoDest))
                {
                    var diretorio = sp.GetRequiredService<IDiretorioDePersonagens>();
                    var nomeRemetente = "Desconhecido";
                    if (diretorio.TentarObterNome(sessao.PersonagemId, out var nome))
                        nomeRemetente = nome;

                    Enviar(
                        sessaoDest,
                        EconomiaMessageIds.AvisoNovoCorreio,
                        new AvisoNovoCorreio
                        {
                            CartaId = cartaId,
                            Remetente = nomeRemetente,
                            Assunto = pedido.Assunto,
                        }
                    );
                }
            }
        );

    public void AoColetar(SessaoTempoReal sessao, ColetarAnexos pedido) =>
        Pedido(
            sessao,
            async (sp, c) =>
            {
                var servico = sp.GetRequiredService<ServicoDeCorreio>();
                var posicoes = sp.GetService<IPosicoesDoMundo>();
                var x = 0f;
                var z = 0f;
                if (posicoes != null && posicoes.TentarObter(sessao.PersonagemId, out var pos))
                {
                    x = pos.X;
                    z = pos.Z;
                }

                var (erro, coroas, itens) = await servico.ColetarAnexosAsync(
                    sessao.PersonagemId,
                    pedido.CartaId,
                    opcoes.ExigirProximidade,
                    x,
                    z,
                    c
                );

                if (erro != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EconomiaRecusada, new EconomiaRecusada(erro));
                    return;
                }

                Enviar(
                    sessao,
                    EconomiaMessageIds.AnexosColetados,
                    new AnexosColetados
                    {
                        CartaId = pedido.CartaId,
                        Coroas = coroas,
                        TotalItens = itens,
                    }
                );

                // Envia estado atualizado
                var estado = await servico.ConsultarAsync(sessao.PersonagemId, c);
                Enviar(sessao, EconomiaMessageIds.EstadoDoCorreio, estado);
            }
        );

    public void AoExcluir(SessaoTempoReal sessao, ExcluirCarta pedido) =>
        Pedido(
            sessao,
            async (sp, c) =>
            {
                var servico = sp.GetRequiredService<ServicoDeCorreio>();
                var erro = await servico.ExcluirCartaAsync(sessao.PersonagemId, pedido.CartaId, c);
                if (erro != null)
                {
                    Enviar(sessao, EconomiaMessageIds.EconomiaRecusada, new EconomiaRecusada(erro));
                    return;
                }

                Enviar(
                    sessao,
                    EconomiaMessageIds.CartaExcluida,
                    new CartaExcluida { CartaId = pedido.CartaId }
                );

                // Envia estado atualizado
                var estado = await servico.ConsultarAsync(sessao.PersonagemId, c);
                Enviar(sessao, EconomiaMessageIds.EstadoDoCorreio, estado);
            }
        );

    public async Task VarrerAsync(CancellationToken c)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeCorreio>();
        var processadas = await servico.ProcessarExpiracoesAsync(c);
        if (processadas > 0)
        {
            log.LogInformation(
                "Varredura de correio: {Quantidade} cartas expiradas processadas",
                processadas
            );
        }
    }

    private void Enviar(SessaoTempoReal sessao, ushort id, object mensagem) =>
        _tempoReal?.Enviar(sessao, id, mensagem);
}

/// <summary>
/// Executa a varredura periódica de cartas expiradas para devolução.
/// </summary>
public sealed class VarreduraDoCorreio(CorreioDoServidor correio, ILogger<VarreduraDoCorreio> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var relogio = new PeriodicTimer(TimeSpan.FromMinutes(10));
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                try
                {
                    await correio.VarrerAsync(parada);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogWarning(ex, "Falha na varredura periódica de correio");
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
