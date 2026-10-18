using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed class EconomiaModule : IFeatureModule
{
    private IServiceScopeFactory? _scopeFactory;
    private ServidorTempoReal? _tempoReal;

    public string Nome => "Economia";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        var opcoesNpc = new OpcoesNpcEconomia();
        configuracao.GetSection("Economia:Npc").Bind(opcoesNpc);
        servicos.AddSingleton(opcoesNpc);
        servicos.AddScoped<JogoDoGrupo.Server.Contratos.Economia.ICarteiraCoroas, CarteiraCoroas>();
        servicos.AddScoped<ServicoDeEconomia>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _scopeFactory = rotas.ServiceProvider.GetService<IServiceScopeFactory>();
        EconomiaRotas.Mapear(rotas);
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(EconomiaMessageIds.Faixa)
            .Ao<ConsultarSaldoCoroas>(EconomiaMessageIds.ConsultarSaldoCoroas, AoConsultarSaldo)
            .Ao<ConsultarCatalogoLoja>(
                EconomiaMessageIds.ConsultarCatalogoLoja,
                AoConsultarCatalogo
            )
            .Ao<ComprarItemDaLoja>(EconomiaMessageIds.ComprarItemDaLoja, AoComprarItem)
            .Ao<VenderItemParaNpc>(EconomiaMessageIds.VenderItemParaNpc, AoVenderItem)
            .Ao<RepararItem>(EconomiaMessageIds.RepararItem, AoRepararItem);

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
    }

    private void AoConsultarSaldo(SessaoTempoReal sessao, ConsultarSaldoCoroas pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeEconomia>();
                var saldo = await servico.ObterSaldoAsync(
                    sessao.PersonagemId,
                    CancellationToken.None
                );
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.SaldoCoroasAlterado,
                    new SaldoCoroasAlterado(saldo)
                );
            }
            catch { }
        });
    }

    private void AoConsultarCatalogo(SessaoTempoReal sessao, ConsultarCatalogoLoja pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        var cat = pedido.Categoria;
        var nid = pedido.NpcId;
        if (nid > 0 && cat == CategoriaLoja.Nenhuma)
        {
            cat = ConfigEconomia.ObterCategoria(nid);
        }

        using (var escopo = _scopeFactory.CreateScope())
        {
            if (!PodeUsarLoja(sessao.PersonagemId, cat, nid, escopo.ServiceProvider))
            {
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.EconomiaRecusada,
                    new EconomiaRecusada(CodigosErroEconomia.LongeDoNpc)
                );
                return;
            }
        }

        var catalogo = ConfigEconomia.ObterCatalogo(cat);
        _tempoReal.Enviar(
            sessao,
            EconomiaMessageIds.RespostaCatalogoLoja,
            new RespostaCatalogoLoja(catalogo, cat, nid)
        );
    }

    private void AoComprarItem(SessaoTempoReal sessao, ComprarItemDaLoja pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        var cat = pedido.Categoria;
        var nid = pedido.NpcId;
        if (nid > 0 && cat == CategoriaLoja.Nenhuma)
        {
            cat = ConfigEconomia.ObterCategoria(nid);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                if (!PodeUsarLoja(sessao.PersonagemId, cat, nid, escopo.ServiceProvider))
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(CodigosErroEconomia.LongeDoNpc)
                    );
                    return;
                }

                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeEconomia>();
                var (sucesso, erro, resultado) = await servico.ComprarItemAsync(
                    sessao.PersonagemId,
                    pedido.ItemId,
                    pedido.Quantidade,
                    cat,
                    CancellationToken.None
                );

                if (sucesso && resultado != null)
                {
                    _tempoReal.Enviar(sessao, EconomiaMessageIds.ItemCompradoDaLoja, resultado);
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.SaldoCoroasAlterado,
                        new SaldoCoroasAlterado(resultado.SaldoAtualizado)
                    );
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(erro ?? CodigosErroEconomia.ItemNaoAVenda)
                    );
                }
            }
            catch (Exception ex)
            {
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.EconomiaRecusada,
                    new EconomiaRecusada(ex.Message)
                );
            }
        });
    }

    private void AoVenderItem(SessaoTempoReal sessao, VenderItemParaNpc pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                if (!PodeVenderEmLoja(sessao.PersonagemId, escopo.ServiceProvider))
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(CodigosErroEconomia.LongeDoNpc)
                    );
                    return;
                }

                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeEconomia>();
                var (sucesso, erro, resultado) = await servico.VenderItemAsync(
                    sessao.PersonagemId,
                    pedido.InstanciaId,
                    CancellationToken.None
                );

                if (sucesso && resultado != null)
                {
                    _tempoReal.Enviar(sessao, EconomiaMessageIds.ItemVendidoParaNpc, resultado);
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.SaldoCoroasAlterado,
                        new SaldoCoroasAlterado(resultado.SaldoAtualizado)
                    );
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(erro ?? CodigosErroEconomia.ItemNaoEncontrado)
                    );
                }
            }
            catch (Exception ex)
            {
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.EconomiaRecusada,
                    new EconomiaRecusada(ex.Message)
                );
            }
        });
    }

    private void AoRepararItem(SessaoTempoReal sessao, RepararItem pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                if (!PodeUsarFerreiro(sessao.PersonagemId, escopo.ServiceProvider))
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(CodigosErroEconomia.LongeDoNpc)
                    );
                    return;
                }

                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeEconomia>();
                var (sucesso, erro, resultado) = await servico.RepararItemAsync(
                    sessao.PersonagemId,
                    pedido.InstanciaId,
                    CancellationToken.None
                );

                if (sucesso && resultado != null)
                {
                    _tempoReal.Enviar(sessao, EconomiaMessageIds.ItemReparado, resultado);
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.SaldoCoroasAlterado,
                        new SaldoCoroasAlterado(resultado.SaldoAtualizado)
                    );
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(erro ?? CodigosErroEconomia.ItemNaoReparavel)
                    );
                }
            }
            catch (Exception ex)
            {
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.EconomiaRecusada,
                    new EconomiaRecusada(ex.Message)
                );
            }
        });
    }

    private static bool PodeUsarLoja(
        long personagemId,
        CategoriaLoja categoria,
        int npcId,
        IServiceProvider servicos
    ) =>
        ValidacaoNpcEconomia.EstaPertoDaLoja(
            servicos.GetRequiredService<IPosicoesDoMundo>(),
            personagemId,
            categoria,
            npcId,
            servicos.GetRequiredService<OpcoesNpcEconomia>()
        );

    private static bool PodeVenderEmLoja(long personagemId, IServiceProvider servicos) =>
        ValidacaoNpcEconomia.EstaPertoDaLoja(
            servicos.GetRequiredService<IPosicoesDoMundo>(),
            personagemId,
            CategoriaLoja.Nenhuma,
            0,
            servicos.GetRequiredService<OpcoesNpcEconomia>()
        );

    private static bool PodeUsarFerreiro(long personagemId, IServiceProvider servicos) =>
        ValidacaoNpcEconomia.EstaPerto(
            servicos.GetRequiredService<IPosicoesDoMundo>(),
            personagemId,
            PontoNpcEconomia.Ferreiro,
            servicos.GetRequiredService<OpcoesNpcEconomia>()
        );
}
