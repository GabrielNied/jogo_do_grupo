using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.Extensions.Hosting;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Liga o mercado (#176) ao tempo real: confere a distância do leiloeiro, chama o
/// <see cref="ServicoDoMercado"/> e entrega ao client o estado novo, os avisos de venda e o
/// saldo. As regras vivem no serviço; aqui só há tradução de mensagens.
/// </summary>
public sealed class MercadoDoServidor(
    IServiceScopeFactory escopos,
    OpcoesNpcEconomia opcoes,
    ILogger<MercadoDoServidor> log
)
{
    private ServidorTempoReal? _tempoReal;

    public void Ligar(ServidorTempoReal tempoReal) => _tempoReal = tempoReal;

    /// <summary>Roda o pedido numa tarefa própria (o laço de rede não espera o banco).</summary>
    public void Pedido(SessaoTempoReal sessao, Func<IServiceProvider, Task> trabalho) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                if (!PertoDoLeiloeiro(sessao.PersonagemId, escopo.ServiceProvider))
                {
                    Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(CodigosErroEconomia.LongeDoLeiloeiro)
                    );
                    return;
                }

                await trabalho(escopo.ServiceProvider);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Falha num pedido do mercado");
                Enviar(
                    sessao,
                    EconomiaMessageIds.EconomiaRecusada,
                    new EconomiaRecusada(CodigosErroEconomia.OrdemIndisponivel)
                );
            }
        });

    public void AoConsultar(SessaoTempoReal sessao, ConsultarMercado pedido) =>
        Pedido(
            sessao,
            async sp =>
            {
                var servico = sp.GetRequiredService<ServicoDoMercado>();
                await EnviarEstadoAsync(servico, sessao, pedido.ItemId, pedido.Filtro);
            }
        );

    public void AoOferecerVenda(SessaoTempoReal sessao, OferecerVenda pedido) =>
        Operar(
            sessao,
            (servico, c) =>
                servico.OferecerVendaAsync(
                    sessao.PersonagemId,
                    pedido.InstanciaId,
                    pedido.PrecoCoroas,
                    c
                )
        );

    public void AoOferecerCompra(SessaoTempoReal sessao, OferecerCompra pedido) =>
        Operar(
            sessao,
            (servico, c) =>
                servico.OferecerCompraAsync(
                    sessao.PersonagemId,
                    pedido.ItemId,
                    pedido.Quantidade,
                    pedido.PrecoUnitario,
                    c
                )
        );

    public void AoComprarOrdem(SessaoTempoReal sessao, ComprarOrdem pedido) =>
        Operar(
            sessao,
            (servico, c) => servico.ComprarOrdemAsync(sessao.PersonagemId, pedido.OrdemId, c)
        );

    public void AoCancelarOrdem(SessaoTempoReal sessao, CancelarOrdem pedido) =>
        Operar(
            sessao,
            (servico, c) => servico.CancelarAsync(sessao.PersonagemId, pedido.OrdemId, c)
        );

    public void AoReceberDevolucoes(SessaoTempoReal sessao, ReceberDevolucoes pedido) =>
        Operar(sessao, (servico, c) => servico.ReceberDevolucoesAsync(sessao.PersonagemId, c));

    /// <summary>No login, tenta entregar o que não coube na bolsa quando a ordem venceu.</summary>
    public void AoAutenticar(SessaoTempoReal sessao) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDoMercado>();
                var resultado = await servico.ReceberDevolucoesAsync(
                    sessao.PersonagemId,
                    CancellationToken.None
                );
                Avisar(resultado);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Falha ao devolver itens do mercado no login");
            }
        });

    /// <summary>Uma varredura: vence as ordens vencidas e avisa quem estiver online.</summary>
    public async Task VarrerAsync(CancellationToken c)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var servico = escopo.ServiceProvider.GetRequiredService<ServicoDoMercado>();
        Avisar(await servico.ExpirarVencidasAsync(c));
    }

    private void Operar(
        SessaoTempoReal sessao,
        Func<ServicoDoMercado, CancellationToken, Task<ResultadoDoMercado>> operacao
    ) =>
        Pedido(
            sessao,
            async sp =>
            {
                var servico = sp.GetRequiredService<ServicoDoMercado>();
                var resultado = await operacao(servico, CancellationToken.None);
                if (resultado.Erro != null)
                {
                    Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(resultado.Erro)
                    );
                    return;
                }

                Avisar(resultado);
                await EnviarEstadoAsync(servico, sessao, 0, "");
            }
        );

    private async Task EnviarEstadoAsync(
        ServicoDoMercado servico,
        SessaoTempoReal sessao,
        int itemId,
        string? filtro
    )
    {
        var estado = await servico.ConsultarAsync(
            sessao.PersonagemId,
            itemId,
            filtro,
            CancellationToken.None
        );
        Enviar(sessao, EconomiaMessageIds.EstadoDoMercado, estado);
        Enviar(
            sessao,
            EconomiaMessageIds.SaldoCoroasAlterado,
            new SaldoCoroasAlterado(estado.Saldo)
        );
    }

    /// <summary>
    /// Avisa as pessoas online do que mudou (venda, compra, vencimento, devolução) e reenvia o
    /// saldo de quem recebeu ou pagou coroas.
    /// </summary>
    private void Avisar(ResultadoDoMercado resultado)
    {
        foreach (var negocio in resultado.Negocios)
        {
            var nome = ConfigMercado.NomeDoItem(negocio.ItemId) ?? "";
            AvisarPersonagem(
                negocio.VendedorId,
                new AvisoDoMercado
                {
                    Tipo = TiposAvisoDoMercado.Vendeu,
                    ItemId = negocio.ItemId,
                    Nome = nome,
                    PrecoUnitario = negocio.PrecoUnitario,
                    Taxa = negocio.Taxa,
                }
            );
            AvisarPersonagem(
                negocio.CompradorId,
                new AvisoDoMercado
                {
                    Tipo = TiposAvisoDoMercado.Comprou,
                    ItemId = negocio.ItemId,
                    Nome = nome,
                    PrecoUnitario = negocio.PrecoUnitario,
                }
            );
        }

        foreach (var encerrada in resultado.Encerradas.Where(e => e.Expirou))
        {
            AvisarPersonagem(
                encerrada.PersonagemId,
                new AvisoDoMercado
                {
                    Tipo = TiposAvisoDoMercado.Expirou,
                    ItemId = encerrada.ItemId,
                    Nome = ConfigMercado.NomeDoItem(encerrada.ItemId) ?? "",
                }
            );
        }

        foreach (var (personagemId, itemId) in resultado.Devolvidos)
        {
            AvisarPersonagem(
                personagemId,
                new AvisoDoMercado
                {
                    Tipo = TiposAvisoDoMercado.Devolvido,
                    ItemId = itemId,
                    Nome = ConfigMercado.NomeDoItem(itemId) ?? "",
                }
            );
        }
    }

    private void AvisarPersonagem(long personagemId, AvisoDoMercado aviso)
    {
        if (
            _tempoReal == null
            || !_tempoReal.TentarObterSessao(personagemId, out var sessao)
            || sessao == null
        )
            return;

        Enviar(sessao, EconomiaMessageIds.AvisoDoMercado, aviso);
        // O saldo mudou (recebeu a venda, ou o reembolso, ou a compra venceu): o client atualiza.
        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = escopos.CreateAsyncScope();
                var saldo = await escopo
                    .ServiceProvider.GetRequiredService<ServicoDeEconomia>()
                    .ObterSaldoAsync(personagemId, CancellationToken.None);
                Enviar(
                    sessao,
                    EconomiaMessageIds.SaldoCoroasAlterado,
                    new SaldoCoroasAlterado(saldo)
                );
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Falha ao reenviar o saldo depois do mercado");
            }
        });
    }

    private void Enviar<T>(SessaoTempoReal sessao, ushort id, T mensagem) =>
        _tempoReal?.Enviar(sessao, id, mensagem);

    private bool PertoDoLeiloeiro(long personagemId, IServiceProvider servicos)
    {
        if (!opcoes.ExigirProximidade)
            return true;

        return servicos.GetRequiredService<IPosicoesDoMundo>().TentarObter(personagemId, out var p)
            && p.Area == ConfigMercado.Area
            && ConfigMercado.DentroDoRaioLeiloeiro(p.X, p.Z);
    }
}

/// <summary>Chama a varredura do mercado a cada <see cref="ConfigMercado.IntervaloDaVarreduraSegundos"/>.</summary>
public sealed class VarreduraDoMercado(MercadoDoServidor mercado, ILogger<VarreduraDoMercado> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var relogio = new PeriodicTimer(
            TimeSpan.FromSeconds(ConfigMercado.IntervaloDaVarreduraSegundos)
        );
        try
        {
            while (await relogio.WaitForNextTickAsync(parada))
            {
                try
                {
                    await mercado.VarrerAsync(parada);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogWarning(ex, "Falha na varredura do mercado");
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
