using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Npcs;
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
            .Ao<SolicitarNpcs>(NpcsMessageIds.SolicitarNpcs, AoSolicitarNpcs)
            .Ao<FalarComNpc>(NpcsMessageIds.FalarComNpc, AoFalarComNpc)
            .Ao<ExecutarOpcaoDeDialogo>(
                NpcsMessageIds.ExecutarOpcaoDeDialogo,
                AoExecutarOpcaoDeDialogo
            );

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

    private void AoFalarComNpc(SessaoTempoReal sessao, FalarComNpc pedido)
    {
        if (_contexto == null)
        {
            return;
        }

        var npc = CatalogoNpcs.Buscar(pedido.NpcId);
        if (npc == null)
        {
            _contexto.TempoReal.Enviar(
                sessao,
                NpcsMessageIds.DialogoRecusado,
                new DialogoRecusado(pedido.NpcId, MotivoRecusaDialogo.NpcNaoEncontrado)
            );
            return;
        }

        var posicaoConferida = false;
        if (_posicoes != null && _posicoes.TentarObter(sessao.PersonagemId, out var pos))
        {
            if (!MesmaArea(pos.Area, npc.Area))
            {
                _contexto.TempoReal.Enviar(
                    sessao,
                    NpcsMessageIds.DialogoRecusado,
                    new DialogoRecusado(pedido.NpcId, MotivoRecusaDialogo.LongeDemais)
                );
                return;
            }

            var dx = pos.X - npc.X;
            var dz = pos.Z - npc.Z;
            var distSq = dx * dx + dz * dz;
            var raioMax = npc.RaioInteracao > 0f ? npc.RaioInteracao : 4f;
            if (distSq > raioMax * raioMax)
            {
                _contexto.TempoReal.Enviar(
                    sessao,
                    NpcsMessageIds.DialogoRecusado,
                    new DialogoRecusado(pedido.NpcId, MotivoRecusaDialogo.LongeDemais)
                );
                return;
            }

            posicaoConferida = true;
        }

        var dialogo = CatalogoDialogosNpc.ObterDialogo(npc);
        _contexto.TempoReal.Enviar(sessao, NpcsMessageIds.DialogoDoNpc, dialogo);

        // Só conversa conferida com a posição do servidor conta para quem escuta (missões).
        if (posicaoConferida)
        {
            _contexto.Eventos.Publicar(new JogadorFalouComNpc(sessao.PersonagemId, npc.Id));
        }
    }

    // Os canais da cidade ("cidade:2"...) têm os mesmos NPCs da cidade do canal 1.
    private static bool MesmaArea(string? areaDoJogador, string areaDoNpc) =>
        string.Equals(areaDoJogador, areaDoNpc, StringComparison.OrdinalIgnoreCase)
        || (AreasDoMundo.EhCidade(areaDoJogador) && AreasDoMundo.EhCidade(areaDoNpc));

    private void AoExecutarOpcaoDeDialogo(SessaoTempoReal sessao, ExecutarOpcaoDeDialogo pedido)
    {
        if (_contexto == null)
        {
            return;
        }

        var npc = CatalogoNpcs.Buscar(pedido.NpcId);
        if (npc == null)
        {
            _contexto.TempoReal.Enviar(
                sessao,
                NpcsMessageIds.DialogoRecusado,
                new DialogoRecusado(pedido.NpcId, MotivoRecusaDialogo.NpcNaoEncontrado)
            );
            return;
        }

        if (_posicoes != null && _posicoes.TentarObter(sessao.PersonagemId, out var pos))
        {
            var dx = pos.X - npc.X;
            var dz = pos.Z - npc.Z;
            var distSq = dx * dx + dz * dz;
            var raioMax = npc.RaioInteracao > 0f ? npc.RaioInteracao : 4f;
            if (distSq > raioMax * raioMax)
            {
                _contexto.TempoReal.Enviar(
                    sessao,
                    NpcsMessageIds.DialogoRecusado,
                    new DialogoRecusado(pedido.NpcId, MotivoRecusaDialogo.LongeDemais)
                );
                return;
            }
        }

        var dialogo = CatalogoDialogosNpc.ObterDialogo(npc);
        var opcao = Array.Find(
            dialogo.Opcoes,
            o => string.Equals(o.Id, pedido.OpcaoId, StringComparison.OrdinalIgnoreCase)
        );

        if (opcao == null)
        {
            _contexto.TempoReal.Enviar(
                sessao,
                NpcsMessageIds.DialogoRecusado,
                new DialogoRecusado(pedido.NpcId, MotivoRecusaDialogo.OpcaoInvalida)
            );
            return;
        }

        _contexto.TempoReal.Enviar(
            sessao,
            NpcsMessageIds.DialogoRecusado,
            new DialogoRecusado(pedido.NpcId, MotivoRecusaDialogo.Nenhum)
        );
    }

    private void EnviarNpcsDaArea(SessaoTempoReal sessao, string area)
    {
        if (_contexto == null)
        {
            return;
        }

        var npcs = CatalogoNpcs.PorArea(AreasDoMundo.EhCidade(area) ? AreasDoMundo.Cidade : area);
        var mensagem = new NpcsDaArea { Area = area, Npcs = npcs.ToArray() };

        _contexto.TempoReal.Enviar(sessao, NpcsMessageIds.NpcsDaArea, mensagem);
    }
}
