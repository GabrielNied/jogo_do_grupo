using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Montarias;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Montarias;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Montarias;

public sealed class MontariasModule : IFeatureModule
{
    private ServicoDeMontarias? _servico;
    private ServerContext? _contexto;

    public string Nome => "Montarias";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(sp => new ServicoDeMontarias(
            tempoReal: sp.GetService<ServidorTempoReal>(),
            controleVelocidade: sp.GetService<IControleDeVelocidadeDoMundo>(),
            combate: sp.GetService<IEstadoDeCombate>(),
            posicoes: sp.GetService<IPosicoesDoMundo>(),
            vizinhanca: sp.GetService<IVizinhanca>(),
            scopeFactory: sp.GetService<IServiceScopeFactory>(),
            logger: sp.GetService<ILogger<ServicoDeMontarias>>()
        ));
        servicos.AddSingleton<IMontariasConsulta>(sp =>
            sp.GetRequiredService<ServicoDeMontarias>()
        );
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _servico = rotas.ServiceProvider.GetRequiredService<ServicoDeMontarias>();
        MontariasRotas.Mapear(rotas);
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(MontariasMessageIds.Faixa)
            .Ao<ChamarMontaria>(MontariasMessageIds.ChamarMontaria, AoChamarMontaria)
            .Ao<Desmontar>(MontariasMessageIds.Desmontar, AoDesmontar)
            .Ao<EquiparMontaria>(MontariasMessageIds.EquiparMontaria, AoEquiparMontaria)
            .Ao<UsarItemMontaria>(MontariasMessageIds.UsarItemMontaria, AoUsarItemMontaria)
            .Ao<ConsultarEstabulo>(MontariasMessageIds.ConsultarEstabulo, AoConsultarEstabulo)
            .Ao<ComprarMontariaEstabulo>(
                MontariasMessageIds.ComprarMontariaEstabulo,
                AoComprarMontariaEstabulo
            );

    public void Inicializar(ServerContext contexto)
    {
        _contexto = contexto;
        contexto.TempoReal.SessaoEncerrada += AoEncerrar;
        contexto.Eventos.Assinar<PersonagemCaiu>(AoPersonagemCair);
        contexto.Eventos.Assinar<JogadorEntrouNaArea>(AoJogadorEntrarNaArea);
    }

    private void AoChamarMontaria(SessaoTempoReal sessao, ChamarMontaria pedido)
    {
        _servico?.ChamarMontaria(sessao.PersonagemId, sessao);
    }

    private void AoDesmontar(SessaoTempoReal sessao, Desmontar pedido)
    {
        _servico?.Desmontar(sessao.PersonagemId, sessao);
    }

    private void AoEquiparMontaria(SessaoTempoReal sessao, EquiparMontaria pedido)
    {
        _servico?.EquiparMontaria(sessao.PersonagemId, pedido.TipoMontaria, sessao);
    }

    private void AoUsarItemMontaria(SessaoTempoReal sessao, UsarItemMontaria pedido)
    {
        _ = _servico?.UsarItemMontariaAsync(
            sessao.ContaId,
            sessao.PersonagemId,
            pedido.InstanciaId,
            sessao
        );
    }

    private void AoConsultarEstabulo(SessaoTempoReal sessao, ConsultarEstabulo pedido)
    {
        _contexto?.TempoReal.Enviar(
            sessao,
            MontariasMessageIds.RespostaEstabulo,
            new RespostaEstabulo { Ofertas = ConfigEstabulo.Ofertas }
        );
    }

    private void AoComprarMontariaEstabulo(SessaoTempoReal sessao, ComprarMontariaEstabulo pedido)
    {
        _ = _servico?.ComprarMontariaEstabuloAsync(
            sessao.PersonagemId,
            pedido.TipoMontaria,
            sessao
        );
    }

    private void AoJogadorEntrarNaArea(JogadorEntrouNaArea evento)
    {
        if (_contexto == null || _servico == null)
        {
            return;
        }

        foreach (var sessao in _contexto.TempoReal.Sessoes)
        {
            if (sessao.PersonagemId == evento.PersonagemId)
            {
                _servico.EnviarColecao(sessao);
                break;
            }
        }
    }

    private void AoEncerrar(SessaoTempoReal sessao)
    {
        _servico?.Desconectar(sessao.PersonagemId);
    }

    private void AoPersonagemCair(PersonagemCaiu evento)
    {
        _servico?.Desmontar(evento.PersonagemId);
    }
}
