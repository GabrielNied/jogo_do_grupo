using JogoDoGrupo.Server.Contratos.Profissoes;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Profissoes;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Profissoes;

/// <summary>
/// Coleta nos pontos do mundo e produção na forja (#192). Tudo por mensagens de tempo real: o
/// client pede e o servidor responde com o resultado; recusa sempre sem efeito.
/// </summary>
public sealed class ProfissoesModule : IFeatureModule
{
    private IServiceScopeFactory? _scopeFactory;
    private ServidorTempoReal? _tempoReal;
    private EventosDoServidor? _eventos;

    public string Nome => "Profissões";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        var opcoes = new OpcoesProfissoes();
        configuracao.GetSection("Profissoes").Bind(opcoes);
        servicos.AddSingleton(opcoes);
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddSingleton<PontosDeColetaDoMundo>();
        servicos.AddScoped<ServicoDeProfissoes>();
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) =>
        _scopeFactory = rotas.ServiceProvider.GetService<IServiceScopeFactory>();

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(ProfissoesMessageIds.Faixa)
            .Ao<ConsultarProfissoes>(
                ProfissoesMessageIds.ConsultarProfissoes,
                (sessao, _) => Executar(sessao, s => AoConsultarProfissoes(sessao, s))
            )
            .Ao<ConsultarNos>(
                ProfissoesMessageIds.ConsultarNos,
                (sessao, _) => Executar(sessao, s => AoConsultarNos(sessao, s))
            )
            .Ao<AprenderProfissao>(
                ProfissoesMessageIds.AprenderProfissao,
                (sessao, pedido) => Executar(sessao, s => AoAprender(sessao, pedido, s))
            )
            .Ao<ColetarNo>(
                ProfissoesMessageIds.ColetarNo,
                (sessao, pedido) => Executar(sessao, s => AoColetar(sessao, pedido, s))
            )
            .Ao<ProduzirReceita>(
                ProfissoesMessageIds.ProduzirReceita,
                (sessao, pedido) => Executar(sessao, s => AoProduzir(sessao, pedido, s))
            );

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        _eventos = contexto.Eventos;
    }

    private void Executar(SessaoTempoReal sessao, Func<ServicoDeProfissoes, Task> acao)
    {
        if (_scopeFactory == null || _tempoReal == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ServicoDeProfissoes>();
                await acao(servico);
            }
            catch
            {
                // Falha de banco: o pedido não teve efeito; o client tenta de novo.
            }
        });
    }

    private async Task AoConsultarProfissoes(SessaoTempoReal sessao, ServicoDeProfissoes servico) =>
        EnviarEstado(sessao, await servico.ObterEstadoAsync(sessao.PersonagemId, default));

    private Task AoConsultarNos(SessaoTempoReal sessao, ServicoDeProfissoes servico)
    {
        _tempoReal!.Enviar(
            sessao,
            ProfissoesMessageIds.EstadoDosNos,
            servico.EstadoDosNos(servico.AreaDe(sessao.PersonagemId))
        );
        return Task.CompletedTask;
    }

    private async Task AoAprender(
        SessaoTempoReal sessao,
        AprenderProfissao pedido,
        ServicoDeProfissoes servico
    )
    {
        var erro = await servico.AprenderAsync(
            sessao.PersonagemId,
            pedido.Profissao,
            CancellationToken.None
        );
        if (erro != null)
            Recusar(sessao, erro);
        EnviarEstado(
            sessao,
            await servico.ObterEstadoAsync(sessao.PersonagemId, CancellationToken.None)
        );
    }

    private async Task AoColetar(
        SessaoTempoReal sessao,
        ColetarNo pedido,
        ServicoDeProfissoes servico
    )
    {
        var (erro, resultado, area) = await servico.ColetarAsync(
            sessao.PersonagemId,
            pedido.NoId,
            CancellationToken.None
        );
        if (erro != null || resultado == null)
        {
            Recusar(sessao, erro ?? CodigosErroProfissoes.NoInexistente);
            if (erro == CodigosErroProfissoes.NoEsgotado)
                _tempoReal!.Enviar(
                    sessao,
                    ProfissoesMessageIds.EstadoDosNos,
                    servico.EstadoDosNos(area)
                );
            return;
        }

        _tempoReal!.Enviar(sessao, ProfissoesMessageIds.ColetaConcluida, resultado);
        _eventos?.Publicar(
            new MaterialColetado(sessao.PersonagemId, resultado.MaterialId, resultado.Quantidade)
        );
        EnviarEstado(
            sessao,
            await servico.ObterEstadoAsync(sessao.PersonagemId, CancellationToken.None)
        );
        AvisarOsNos(area, sessao, servico);
    }

    private async Task AoProduzir(
        SessaoTempoReal sessao,
        ProduzirReceita pedido,
        ServicoDeProfissoes servico
    )
    {
        var (erro, resultado) = await servico.ProduzirAsync(
            sessao.PersonagemId,
            pedido.ReceitaId,
            CancellationToken.None
        );
        if (erro != null || resultado == null)
        {
            Recusar(sessao, erro ?? CodigosErroProfissoes.ReceitaInexistente);
            return;
        }

        _tempoReal!.Enviar(sessao, ProfissoesMessageIds.ProducaoConcluida, resultado);
        EnviarEstado(
            sessao,
            await servico.ObterEstadoAsync(sessao.PersonagemId, CancellationToken.None)
        );
    }

    private void EnviarEstado(SessaoTempoReal sessao, EstadoDasProfissoes estado) =>
        _tempoReal!.Enviar(sessao, ProfissoesMessageIds.EstadoDasProfissoes, estado);

    private void Recusar(SessaoTempoReal sessao, string codigo) =>
        _tempoReal!.Enviar(
            sessao,
            ProfissoesMessageIds.ProfissoesRecusada,
            new ProfissoesRecusada(codigo)
        );

    /// <summary>Quem coletou já recebeu a resposta; os outros da área veem o ponto sumir.</summary>
    private void AvisarOsNos(string area, SessaoTempoReal coletor, ServicoDeProfissoes servico)
    {
        var destinos = new List<SessaoTempoReal>();
        foreach (var id in servico.PersonagensNaArea(area))
        {
            if (
                id != coletor.PersonagemId
                && _tempoReal!.TentarObterSessao(id, out var sessao)
                && sessao != null
            )
                destinos.Add(sessao);
        }

        _tempoReal!.Enviar(destinos, ProfissoesMessageIds.EstadoDosNos, servico.EstadoDosNos(area));
    }
}
