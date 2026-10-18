using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Npcs;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Npcs;

public sealed class NpcsModule : IFeatureModule
{
    private ServerContext? _contexto;
    private IPosicoesDoMundo? _posicoes;

    public NpcsModule()
        : this(null) { }

    public NpcsModule(IPosicoesDoMundo? posicoes)
    {
        _posicoes = posicoes;
    }

    public string Nome => "NPCs";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) { }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _posicoes ??= rotas.ServiceProvider.GetService<IPosicoesDoMundo>();
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(NpcsMessageIds.Faixa)
            .Ao<SolicitarNpcs>(NpcsMessageIds.SolicitarNpcs, AoSolicitarNpcs);

    public void Inicializar(ServerContext contexto)
    {
        _contexto = contexto;
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(AoJogadorEntrarNaArea);
    }

    private void AoJogadorEntrarNaArea(JogadorEntrouNaArea evento)
    {
        if (_contexto == null)
        {
            return;
        }

        foreach (var sessao in _contexto.TempoReal.Sessoes)
        {
            if (sessao.PersonagemId == evento.PersonagemId)
            {
                EnviarNpcsDaArea(sessao, evento.Area);
                break;
            }
        }
    }

    private void AoSolicitarNpcs(SessaoTempoReal sessao, SolicitarNpcs pedido)
    {
        var area = "cidade";
        if (
            _posicoes != null
            && _posicoes.TentarObter(sessao.PersonagemId, out var pos)
            && !string.IsNullOrWhiteSpace(pos.Area)
        )
        {
            area = pos.Area;
        }

        EnviarNpcsDaArea(sessao, area);
    }

    private void EnviarNpcsDaArea(SessaoTempoReal sessao, string area)
    {
        if (_contexto == null)
        {
            return;
        }

        var npcs = CatalogoNpcs.PorArea(area);
        var mensagem = new NpcsDaArea { Area = area, Npcs = npcs.ToArray() };

        _contexto.TempoReal.Enviar(sessao, NpcsMessageIds.NpcsDaArea, mensagem);
    }
}
