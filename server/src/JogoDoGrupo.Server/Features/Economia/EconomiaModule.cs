using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed class EconomiaModule : IFeatureModule
{
    private IServiceScopeFactory? _scopeFactory;
    private ServidorTempoReal? _tempoReal;
    private MercadoDoServidor? _mercado;

    public string Nome => "Economia";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        var opcoesNpc = new OpcoesNpcEconomia();
        configuracao.GetSection("Economia:Npc").Bind(opcoesNpc);
        servicos.AddSingleton(opcoesNpc);
        servicos.AddScoped<JogoDoGrupo.Server.Contratos.Economia.ICarteiraCoroas, CarteiraCoroas>();
        servicos.AddScoped<ServicoDeEconomia>();
        servicos.AddSingleton<RelogioDoMercado>();
        servicos.AddScoped<ControleDeBarracasAtivas>();
        servicos.AddScoped<IResolvedorDeBarracas>(sp =>
            sp.GetRequiredService<ControleDeBarracasAtivas>()
        );
        servicos.AddScoped<ServicoDoMercado>();
        servicos.AddScoped<ServicoDeBarracas>();
        servicos.AddSingleton<MercadoDoServidor>();
        servicos.AddHostedService<VarreduraDoMercado>();
        servicos.AddScoped<
            JogoDoGrupo.Server.Contratos.Economia.IAjusteDeCoroasDeTeste,
            AjusteDeCoroasDeTeste
        >();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _scopeFactory = rotas.ServiceProvider.GetService<IServiceScopeFactory>();
        _mercado = rotas.ServiceProvider.GetRequiredService<MercadoDoServidor>();
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
            .Ao<RepararItem>(EconomiaMessageIds.RepararItem, AoRepararItem)
            .Ao<ConsultarMercado>(
                EconomiaMessageIds.ConsultarMercado,
                (sessao, pedido) => _mercado!.AoConsultar(sessao, pedido)
            )
            .Ao<OferecerVenda>(
                EconomiaMessageIds.OferecerVenda,
                (sessao, pedido) => _mercado!.AoOferecerVenda(sessao, pedido)
            )
            .Ao<OferecerCompra>(
                EconomiaMessageIds.OferecerCompra,
                (sessao, pedido) => _mercado!.AoOferecerCompra(sessao, pedido)
            )
            .Ao<ComprarOrdem>(
                EconomiaMessageIds.ComprarOrdem,
                (sessao, pedido) => _mercado!.AoComprarOrdem(sessao, pedido)
            )
            .Ao<CancelarOrdem>(
                EconomiaMessageIds.CancelarOrdem,
                (sessao, pedido) => _mercado!.AoCancelarOrdem(sessao, pedido)
            )
            .Ao<ReceberDevolucoes>(
                EconomiaMessageIds.ReceberDevolucoes,
                (sessao, pedido) => _mercado!.AoReceberDevolucoes(sessao, pedido)
            )
            .Ao<MontarBarraca>(EconomiaMessageIds.MontarBarraca, AoMontarBarraca)
            .Ao<DesmontarBarraca>(EconomiaMessageIds.DesmontarBarraca, AoDesmontarBarraca)
            .Ao<ContratarVendedorOffline>(
                EconomiaMessageIds.ContratarVendedorOffline,
                AoContratarVendedorOffline
            )
            .Ao<EncerrarVendedorOffline>(
                EconomiaMessageIds.EncerrarVendedorOffline,
                AoEncerrarVendedorOffline
            )
            .Ao<ConsultarBarraca>(EconomiaMessageIds.ConsultarBarraca, AoConsultarBarraca)
            .Ao<ComprarDaBarraca>(EconomiaMessageIds.ComprarDaBarraca, AoComprarDaBarraca);

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        contexto.Eventos.Assinar<CoroasMovimentadas>(AoCoroasMovimentadas);
        _mercado!.Ligar(contexto.TempoReal);
        contexto.TempoReal.SessaoAutenticada += sessao => _mercado.AoAutenticar(sessao);
        contexto.TempoReal.SessaoEncerrada += AoSessaoEncerrada;
    }

    /// <summary>Quem mexeu em coroas fora da Economia (troca direta) avisa; o client vê o saldo novo.</summary>
    private void AoCoroasMovimentadas(CoroasMovimentadas evento)
    {
        if (
            _tempoReal == null
            || !_tempoReal.TentarObterSessao(evento.PersonagemId, out var sessao)
            || sessao == null
        )
            return;

        AoConsultarSaldo(sessao, new ConsultarSaldoCoroas());
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

    private void AoSessaoEncerrada(SessaoTempoReal sessao)
    {
        if (_scopeFactory == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
                barracas.AoDesconectar(sessao.PersonagemId);
            }
            catch { }
        });
    }

    private void AoMontarBarraca(SessaoTempoReal sessao, MontarBarraca pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
                var diretorio =
                    escopo.ServiceProvider.GetRequiredService<IDiretorioDePersonagens>();
                diretorio.TentarObterNome(sessao.PersonagemId, out var nomeDono);

                var erro = barracas.MontarBarraca(
                    sessao.PersonagemId,
                    nomeDono,
                    pedido.Titulo,
                    pedido.PosicaoX,
                    pedido.PosicaoZ
                );

                if (erro != null)
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(erro)
                    );
                    return;
                }

                var dados = await barracas.ConsultarBarracaAsync(
                    sessao.PersonagemId,
                    CancellationToken.None
                );
                if (dados != null)
                {
                    _tempoReal.Enviar(sessao, EconomiaMessageIds.DadosDaBarraca, dados);
                }
            }
            catch { }
        });
    }

    private void AoDesmontarBarraca(SessaoTempoReal sessao, DesmontarBarraca pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
                barracas.DesmontarBarraca(sessao.PersonagemId);
            }
            catch { }
        });
    }

    private void AoContratarVendedorOffline(SessaoTempoReal sessao, ContratarVendedorOffline pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
                var diretorio =
                    escopo.ServiceProvider.GetRequiredService<IDiretorioDePersonagens>();
                diretorio.TentarObterNome(sessao.PersonagemId, out var nomeDono);

                var erro = await barracas.ContratarVendedorOfflineAsync(
                    sessao.PersonagemId,
                    nomeDono,
                    pedido.Titulo,
                    pedido.Dias,
                    pedido.PosicaoX,
                    pedido.PosicaoZ,
                    CancellationToken.None
                );

                if (erro != null)
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(erro)
                    );
                    return;
                }

                var dados = await barracas.ConsultarBarracaAsync(
                    sessao.PersonagemId,
                    CancellationToken.None
                );
                if (dados != null)
                {
                    _tempoReal.Enviar(sessao, EconomiaMessageIds.DadosDaBarraca, dados);
                }

                var saldo = await escopo
                    .ServiceProvider.GetRequiredService<ServicoDeEconomia>()
                    .ObterSaldoAsync(sessao.PersonagemId, CancellationToken.None);
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.SaldoCoroasAlterado,
                    new SaldoCoroasAlterado(saldo)
                );
            }
            catch { }
        });
    }

    private void AoEncerrarVendedorOffline(SessaoTempoReal sessao, EncerrarVendedorOffline pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
                var erro = await barracas.EncerrarVendedorOfflineAsync(
                    sessao.PersonagemId,
                    CancellationToken.None
                );
                if (erro != null)
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(erro)
                    );
                }
            }
            catch { }
        });
    }

    private void AoConsultarBarraca(SessaoTempoReal sessao, ConsultarBarraca pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
                if (pedido.PersonagemId == 0)
                {
                    var lista = await barracas.ListarBarracasAtivasAsync(CancellationToken.None);
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.BarracasNaArea,
                        new BarracasNaArea { Barracas = lista.ToArray() }
                    );
                }
                else
                {
                    var dados = await barracas.ConsultarBarracaAsync(
                        pedido.PersonagemId,
                        CancellationToken.None
                    );
                    if (dados == null)
                    {
                        _tempoReal.Enviar(
                            sessao,
                            EconomiaMessageIds.EconomiaRecusada,
                            new EconomiaRecusada(CodigosErroEconomia.BarracaNaoEncontrada)
                        );
                        return;
                    }

                    _tempoReal.Enviar(sessao, EconomiaMessageIds.DadosDaBarraca, dados);
                }
            }
            catch { }
        });
    }

    private void AoComprarDaBarraca(SessaoTempoReal sessao, ComprarDaBarraca pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var barracas = escopo.ServiceProvider.GetRequiredService<ServicoDeBarracas>();
                var resultado = await barracas.ComprarDaBarracaAsync(
                    sessao.PersonagemId,
                    pedido.OrdemId,
                    pedido.VendedorId,
                    CancellationToken.None
                );

                if (resultado.Erro != null)
                {
                    _tempoReal.Enviar(
                        sessao,
                        EconomiaMessageIds.EconomiaRecusada,
                        new EconomiaRecusada(resultado.Erro)
                    );
                    return;
                }

                _mercado?.Avisar(resultado);

                var dadosAtualizados = await barracas.ConsultarBarracaAsync(
                    pedido.VendedorId,
                    CancellationToken.None
                );
                if (dadosAtualizados != null)
                {
                    _tempoReal.Enviar(sessao, EconomiaMessageIds.DadosDaBarraca, dadosAtualizados);
                }

                var saldo = await escopo
                    .ServiceProvider.GetRequiredService<ServicoDeEconomia>()
                    .ObterSaldoAsync(sessao.PersonagemId, CancellationToken.None);
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.SaldoCoroasAlterado,
                    new SaldoCoroasAlterado(saldo)
                );
            }
            catch
            {
                _tempoReal.Enviar(
                    sessao,
                    EconomiaMessageIds.EconomiaRecusada,
                    new EconomiaRecusada(CodigosErroEconomia.OrdemIndisponivel)
                );
            }
        });
    }
}
