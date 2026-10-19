using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Missoes;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Missoes;

/// <summary>
/// Módulo autoritativo de missões (#179, #1061).
/// </summary>
public sealed class MissoesModule : IFeatureModule
{
    private IServiceScopeFactory? _scopeFactory;
    private ServidorTempoReal? _tempoReal;

    public string Nome => "Missões";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddScoped<ServicoDeMissoes>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _scopeFactory = rotas.ServiceProvider.GetService<IServiceScopeFactory>();
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(MissoesMessageIds.Faixa)
            .Ao<ConsultarMissoes>(MissoesMessageIds.ConsultarMissoes, AoConsultarMissoes)
            .Ao<AceitarMissao>(MissoesMessageIds.AceitarMissao, AoAceitarMissao)
            .Ao<AbandonarMissao>(MissoesMessageIds.AbandonarMissao, AoAbandonarMissao)
            .Ao<EntregarMissao>(MissoesMessageIds.EntregarMissao, AoEntregarMissao)
            .Ao<AlternarAcompanhamento>(
                MissoesMessageIds.AlternarAcompanhamento,
                AoAlternarAcompanhamento
            )
            .Ao<CompartilharMissao>(MissoesMessageIds.CompartilharMissao, AoCompartilharMissao)
            .Ao<InteragirComNpcMissao>(
                MissoesMessageIds.InteragirComNpcMissao,
                AoInteragirComNpcMissao
            );

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        contexto.TempoReal.SessaoAutenticada += AoAutenticar;
        contexto.Eventos.Assinar<InimigoDerrotado>(AoInimigoDerrotado);
    }

    private void AoAutenticar(SessaoTempoReal sessao)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var estado = await servico.GarantirMissaoInicialAsync(sessao.PersonagemId);
                _tempoReal.Enviar(sessao, MissoesMessageIds.EstadoDasMissoes, estado);
            }
            catch { }
        });
    }

    private void AoConsultarMissoes(SessaoTempoReal sessao, ConsultarMissoes pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var estado = await servico.ObterEstadoDasMissoesAsync(sessao.PersonagemId);
                _tempoReal.Enviar(sessao, MissoesMessageIds.EstadoDasMissoes, estado);
            }
            catch { }
        });
    }

    private void AoAceitarMissao(SessaoTempoReal sessao, AceitarMissao pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var (sucesso, erro, dto) = await servico.AceitarMissaoAsync(
                    sessao.PersonagemId,
                    pedido.MissaoId
                );
                if (sucesso && dto != null)
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.MissaoAtualizada,
                        new MissaoAtualizada { Missao = dto }
                    );
                    var def = CatalogoDeMissoes.Obter(pedido.MissaoId);
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.AvisoDeMissao,
                        new AvisoDeMissao
                        {
                            MissaoId = pedido.MissaoId,
                            Titulo = def?.Titulo ?? "Nova Missão",
                            TipoAviso = "aceita",
                            Mensagem = "Missão iniciada!",
                        }
                    );
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.OperacaoDeMissaoRecusada,
                        new OperacaoDeMissaoRecusada(
                            erro ?? CodigosErroMissoes.MissaoNaoEncontrada,
                            pedido.MissaoId
                        )
                    );
                }
            }
            catch { }
        });
    }

    private void AoAbandonarMissao(SessaoTempoReal sessao, AbandonarMissao pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var (sucesso, erro) = await servico.AbandonarMissaoAsync(
                    sessao.PersonagemId,
                    pedido.MissaoId
                );
                if (sucesso)
                {
                    var estado = await servico.ObterEstadoDasMissoesAsync(sessao.PersonagemId);
                    _tempoReal.Enviar(sessao, MissoesMessageIds.EstadoDasMissoes, estado);
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.OperacaoDeMissaoRecusada,
                        new OperacaoDeMissaoRecusada(
                            erro ?? CodigosErroMissoes.MissaoNaoEncontrada,
                            pedido.MissaoId
                        )
                    );
                }
            }
            catch { }
        });
    }

    private void AoEntregarMissao(SessaoTempoReal sessao, EntregarMissao pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var (sucesso, erro, concluida, proxima) = await servico.EntregarMissaoAsync(
                    sessao.PersonagemId,
                    pedido.MissaoId,
                    pedido.ItemIdEscolhido > 0 ? pedido.ItemIdEscolhido : null
                );
                if (sucesso && concluida != null)
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.MissaoAtualizada,
                        new MissaoAtualizada { Missao = concluida }
                    );
                    var def = CatalogoDeMissoes.Obter(pedido.MissaoId);
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.AvisoDeMissao,
                        new AvisoDeMissao
                        {
                            MissaoId = pedido.MissaoId,
                            Titulo = def?.Titulo ?? "Missão",
                            TipoAviso = "concluida",
                            Mensagem = "Missão concluída com sucesso!",
                        }
                    );

                    if (proxima != null)
                    {
                        _tempoReal.Enviar(
                            sessao,
                            MissoesMessageIds.MissaoAtualizada,
                            new MissaoAtualizada { Missao = proxima }
                        );
                        var defProx = CatalogoDeMissoes.Obter(proxima.MissaoId);
                        _tempoReal.Enviar(
                            sessao,
                            MissoesMessageIds.AvisoDeMissao,
                            new AvisoDeMissao
                            {
                                MissaoId = proxima.MissaoId,
                                Titulo = defProx?.Titulo ?? "Nova Missão",
                                TipoAviso = "aceita",
                                Mensagem = "Nova missão principal disponível!",
                            }
                        );
                    }

                    var estado = await servico.ObterEstadoDasMissoesAsync(sessao.PersonagemId);
                    _tempoReal.Enviar(sessao, MissoesMessageIds.EstadoDasMissoes, estado);
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.OperacaoDeMissaoRecusada,
                        new OperacaoDeMissaoRecusada(
                            erro ?? CodigosErroMissoes.MissaoNaoEncontrada,
                            pedido.MissaoId
                        )
                    );
                }
            }
            catch { }
        });
    }

    private void AoAlternarAcompanhamento(SessaoTempoReal sessao, AlternarAcompanhamento pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var (sucesso, erro) = await servico.AlternarAcompanhamentoAsync(
                    sessao.PersonagemId,
                    pedido.MissaoId,
                    pedido.Acompanhada
                );
                if (sucesso)
                {
                    var estado = await servico.ObterEstadoDasMissoesAsync(sessao.PersonagemId);
                    _tempoReal.Enviar(sessao, MissoesMessageIds.EstadoDasMissoes, estado);
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.OperacaoDeMissaoRecusada,
                        new OperacaoDeMissaoRecusada(
                            erro ?? CodigosErroMissoes.LimiteRastreadorAtingido,
                            pedido.MissaoId
                        )
                    );
                }
            }
            catch { }
        });
    }

    private void AoCompartilharMissao(SessaoTempoReal sessao, CompartilharMissao pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var (sucesso, erro, destinatarios, titulo) =
                    await servico.CompartilharComGrupoAsync(sessao.PersonagemId, pedido.MissaoId);
                if (sucesso && destinatarios != null)
                {
                    var diretorio =
                        escopo.ServiceProvider.GetService<JogoDoGrupo.Server.Contratos.Personagem.IDiretorioDePersonagens>();
                    string? nomeLider = null;
                    diretorio?.TentarObterNome(sessao.PersonagemId, out nomeLider);
                    var avisoLider = nomeLider ?? "Líder";

                    foreach (var membroId in destinatarios)
                    {
                        if (
                            _tempoReal.TentarObterSessao(membroId, out var sessaoMembro)
                            && sessaoMembro != null
                        )
                        {
                            _tempoReal.Enviar(
                                sessaoMembro,
                                MissoesMessageIds.ConviteDeMissaoCompartilhada,
                                new ConviteDeMissaoCompartilhada
                                {
                                    MissaoId = pedido.MissaoId,
                                    NomeLider = avisoLider,
                                    TituloMissao = titulo ?? "",
                                }
                            );
                        }
                    }
                }
                else
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.OperacaoDeMissaoRecusada,
                        new OperacaoDeMissaoRecusada(
                            erro ?? CodigosErroMissoes.ApenasLiderCompartilha,
                            pedido.MissaoId
                        )
                    );
                }
            }
            catch { }
        });
    }

    private void AoInteragirComNpcMissao(SessaoTempoReal sessao, InteragirComNpcMissao pedido)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                var atualizadas = await servico.RegistrarInteracaoNpcAsync(
                    sessao.PersonagemId,
                    pedido.NpcId
                );
                foreach (var at in atualizadas)
                {
                    _tempoReal.Enviar(
                        sessao,
                        MissoesMessageIds.MissaoAtualizada,
                        new MissaoAtualizada { Missao = at }
                    );
                    if (at.Estado == EstadoDaMissao.ProntaParaEntregar)
                    {
                        var def = CatalogoDeMissoes.Obter(at.MissaoId);
                        _tempoReal.Enviar(
                            sessao,
                            MissoesMessageIds.AvisoDeMissao,
                            new AvisoDeMissao
                            {
                                MissaoId = at.MissaoId,
                                Titulo = def?.Titulo ?? "Missão",
                                TipoAviso = "pronta_para_entregar",
                                Mensagem = "Missão pronta para entrega!",
                            }
                        );
                    }
                }
            }
            catch { }
        });
    }

    private void AoInimigoDerrotado(InimigoDerrotado evento)
    {
        if (_scopeFactory == null || _tempoReal == null || evento.Participantes.Count == 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>();
                foreach (var participanteId in evento.Participantes)
                {
                    var atualizadas = await servico.RegistrarMorteInimigoAsync(
                        participanteId,
                        evento.CriaturaId
                    );
                    if (
                        atualizadas.Count > 0
                        && _tempoReal.TentarObterSessao(participanteId, out var sessao)
                        && sessao != null
                    )
                    {
                        foreach (var at in atualizadas)
                        {
                            _tempoReal.Enviar(
                                sessao,
                                MissoesMessageIds.MissaoAtualizada,
                                new MissaoAtualizada { Missao = at }
                            );
                            if (at.Estado == EstadoDaMissao.ProntaParaEntregar)
                            {
                                var def = CatalogoDeMissoes.Obter(at.MissaoId);
                                _tempoReal.Enviar(
                                    sessao,
                                    MissoesMessageIds.AvisoDeMissao,
                                    new AvisoDeMissao
                                    {
                                        MissaoId = at.MissaoId,
                                        Titulo = def?.Titulo ?? "Missão",
                                        TipoAviso = "pronta_para_entregar",
                                        Mensagem = "Objetivo concluído! Entregue a missão.",
                                    }
                                );
                            }
                        }
                    }
                }
            }
            catch { }
        });
    }
}
