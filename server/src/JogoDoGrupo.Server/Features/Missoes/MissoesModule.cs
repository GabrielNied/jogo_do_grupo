using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Npcs;
using JogoDoGrupo.Server.Contratos.Profissoes;
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
    /// <summary>De quanto em quanto tempo o laço confere se alguém chegou a um objetivo IrA.</summary>
    internal static readonly TimeSpan IntervaloDasChegadas = TimeSpan.FromSeconds(0.5);

    private IServiceScopeFactory? _scopeFactory;
    private ServidorTempoReal? _tempoReal;
    private IPosicoesDoMundo? _posicoes;

    // Objetivos de chegada (IrA) ainda abertos de quem está no jogo. O passo do laço só lê daqui
    // e só vai ao banco quando a posição entra no raio de um deles; o índice é refeito a cada
    // estado ou atualização de missão enviada ao jogador.
    private readonly ConcurrentDictionary<long, ChegadaPendente[]> _chegadas = new();
    private readonly ConcurrentDictionary<long, byte> _chegadasEmVoo = new();

    // Uma conta de progresso por personagem de cada vez: duas mortes ou coletas seguidas leem e
    // gravam a mesma linha, e em paralelo uma apagaria a outra.
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _travas = new();

    private readonly record struct ChegadaPendente(int MissaoId, float X, float Z, float Raio);

    public string Nome => "Missões";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddScoped<ServicoDeMissoes>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _scopeFactory = rotas.ServiceProvider.GetService<IServiceScopeFactory>();
        _posicoes ??= rotas.ServiceProvider.GetService<IPosicoesDoMundo>();
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
            .Ao<CompartilharMissao>(MissoesMessageIds.CompartilharMissao, AoCompartilharMissao);

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        contexto.TempoReal.SessaoAutenticada += AoAutenticar;
        contexto.TempoReal.SessaoEncerrada += sessao =>
            _chegadas.TryRemove(sessao.PersonagemId, out _);
        contexto.Eventos.Assinar<InimigoDerrotado>(AoInimigoDerrotado);
        contexto.Eventos.Assinar<JogadorFalouComNpc>(AoJogadorFalouComNpc);
        contexto.Eventos.Assinar<MaterialColetado>(AoMaterialColetado);
        contexto.AoPasso(IntervaloDasChegadas, _ => ConferirChegadas(), "Missoes");
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
                EnviarEstado(sessao, estado);
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
                EnviarEstado(sessao, estado);
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
                    EnviarAtualizacao(sessao, dto);
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
                    EnviarEstado(sessao, estado);
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
                    EnviarAtualizacao(sessao, concluida);
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
                        EnviarAtualizacao(sessao, proxima);
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
                    EnviarEstado(sessao, estado);
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
                    EnviarEstado(sessao, estado);
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

    private void AoJogadorFalouComNpc(JogadorFalouComNpc evento) =>
        RegistrarProgresso(async servico =>
            EnviarProgresso(
                evento.PersonagemId,
                await EmSerieAsync(
                    evento.PersonagemId,
                    () => servico.RegistrarInteracaoNpcAsync(evento.PersonagemId, evento.NpcId)
                )
            )
        );

    private void AoMaterialColetado(MaterialColetado evento) =>
        RegistrarProgresso(async servico =>
            EnviarProgresso(
                evento.PersonagemId,
                await EmSerieAsync(
                    evento.PersonagemId,
                    () =>
                        servico.RegistrarColetaItemAsync(
                            evento.PersonagemId,
                            evento.MaterialId,
                            evento.Quantidade
                        )
                )
            )
        );

    private void AoInimigoDerrotado(InimigoDerrotado evento)
    {
        if (evento.Participantes.Count == 0)
            return;

        RegistrarProgresso(async servico =>
        {
            foreach (var participanteId in evento.Participantes)
            {
                EnviarProgresso(
                    participanteId,
                    await EmSerieAsync(
                        participanteId,
                        () => servico.RegistrarMorteInimigoAsync(participanteId, evento.CriaturaId)
                    )
                );
            }
        });
    }

    /// <summary>
    /// Passo do laço: quem está no mundo aberto (cidade e terreno, em qualquer canal) dentro do
    /// raio de um objetivo IrA aberto vai ao banco uma vez; enquanto a conta corre, a mesma
    /// pessoa não é conferida de novo.
    /// </summary>
    internal void ConferirChegadas()
    {
        if (_posicoes == null || _scopeFactory == null || _chegadas.IsEmpty)
            return;

        foreach (var (personagemId, pendentes) in _chegadas)
        {
            if (_chegadasEmVoo.ContainsKey(personagemId))
                continue;
            if (
                !_posicoes.TentarObter(personagemId, out var posicao)
                || !AreasDoMundo.EhCidade(posicao.Area)
            )
                continue;
            if (!Array.Exists(pendentes, c => Dentro(c, posicao.X, posicao.Z)))
                continue;
            if (!_chegadasEmVoo.TryAdd(personagemId, 0))
                continue;

            var (x, z) = (posicao.X, posicao.Z);
            RegistrarProgresso(
                async servico =>
                {
                    var atualizadas = await EmSerieAsync(
                        personagemId,
                        () => servico.RegistrarChegadaLocalAsync(personagemId, x, z)
                    );
                    // O banco é a fonte: o que estava no raio e não mudou já estava cumprido.
                    Esquecer(personagemId, c => Dentro(c, x, z));
                    EnviarProgresso(personagemId, atualizadas);
                },
                () => _chegadasEmVoo.TryRemove(personagemId, out _)
            );
        }
    }

    private async Task<List<ProgressoDeMissaoDto>> EmSerieAsync(
        long personagemId,
        Func<Task<List<ProgressoDeMissaoDto>>> conta
    )
    {
        var trava = _travas.GetOrAdd(personagemId, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync();
        try
        {
            return await conta();
        }
        finally
        {
            trava.Release();
        }
    }

    private static bool Dentro(ChegadaPendente chegada, float x, float z)
    {
        var dx = x - chegada.X;
        var dz = z - chegada.Z;
        return dx * dx + dz * dz <= chegada.Raio * chegada.Raio;
    }

    private void EnviarEstado(SessaoTempoReal sessao, EstadoDasMissoes estado)
    {
        LembrarChegadas(sessao.PersonagemId, estado.Ativas);
        _tempoReal?.Enviar(sessao, MissoesMessageIds.EstadoDasMissoes, estado);
    }

    private void EnviarAtualizacao(SessaoTempoReal sessao, ProgressoDeMissaoDto missao)
    {
        LembrarChegada(sessao.PersonagemId, missao);
        _tempoReal?.Enviar(
            sessao,
            MissoesMessageIds.MissaoAtualizada,
            new MissaoAtualizada { Missao = missao }
        );
    }

    // Progresso que não veio de um pedido do jogador (morte, conversa, chegada, coleta): manda
    // cada missão que mudou e avisa quando ela fica pronta para entregar.
    private void EnviarProgresso(long personagemId, IReadOnlyList<ProgressoDeMissaoDto> atualizadas)
    {
        if (
            atualizadas.Count == 0
            || _tempoReal == null
            || !_tempoReal.TentarObterSessao(personagemId, out var sessao)
            || sessao == null
        )
            return;

        foreach (var missao in atualizadas)
        {
            EnviarAtualizacao(sessao, missao);
            if (missao.Estado != EstadoDaMissao.ProntaParaEntregar)
                continue;

            var def = CatalogoDeMissoes.Obter(missao.MissaoId);
            _tempoReal.Enviar(
                sessao,
                MissoesMessageIds.AvisoDeMissao,
                new AvisoDeMissao
                {
                    MissaoId = missao.MissaoId,
                    Titulo = def?.Titulo ?? "Missão",
                    TipoAviso = "pronta_para_entregar",
                    Mensagem = "Objetivo concluído! Entregue a missão.",
                }
            );
        }
    }

    // Trabalho de banco fora do laço, num escopo próprio; a falha não tem efeito, como nos pedidos.
    private void RegistrarProgresso(Func<ServicoDeMissoes, Task> acao, Action? depois = null)
    {
        if (_scopeFactory == null)
        {
            depois?.Invoke();
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>());
            }
            catch { }
            finally
            {
                depois?.Invoke();
            }
        });
    }

    private void LembrarChegadas(long personagemId, IEnumerable<ProgressoDeMissaoDto>? ativas)
    {
        var pendentes = new List<ChegadaPendente>();
        foreach (var missao in ativas ?? [])
            pendentes.AddRange(ChegadasAbertas(missao));
        Guardar(personagemId, pendentes);
    }

    private void LembrarChegada(long personagemId, ProgressoDeMissaoDto missao)
    {
        var pendentes = new List<ChegadaPendente>();
        if (_chegadas.TryGetValue(personagemId, out var atuais))
            pendentes.AddRange(Array.FindAll(atuais, c => c.MissaoId != missao.MissaoId));
        pendentes.AddRange(ChegadasAbertas(missao));
        Guardar(personagemId, pendentes);
    }

    private void Esquecer(long personagemId, Predicate<ChegadaPendente> criterio)
    {
        if (_chegadas.TryGetValue(personagemId, out var atuais))
            Guardar(personagemId, Array.FindAll(atuais, c => !criterio(c)));
    }

    private void Guardar(long personagemId, IReadOnlyCollection<ChegadaPendente> pendentes)
    {
        if (pendentes.Count == 0)
            _chegadas.TryRemove(personagemId, out _);
        else
            _chegadas[personagemId] = [.. pendentes];
    }

    private static IEnumerable<ChegadaPendente> ChegadasAbertas(ProgressoDeMissaoDto missao)
    {
        if (missao.Estado != EstadoDaMissao.EmAndamento)
            yield break;

        var def = CatalogoDeMissoes.Obter(missao.MissaoId);
        if (def == null)
            yield break;

        foreach (var objetivo in def.Objetivos)
        {
            if (objetivo.Tipo != TipoDeObjetivo.IrA)
                continue;

            var progresso = Array.Find(missao.Objetivos ?? [], o => o.ObjetivoId == objetivo.Id);
            if (progresso != null && progresso.Concluido)
                continue;

            yield return new ChegadaPendente(
                missao.MissaoId,
                objetivo.CoordenadaX,
                objetivo.CoordenadaZ,
                objetivo.RaioAreaMetros > 0f ? objetivo.RaioAreaMetros : 5f
            );
        }
    }
}
