using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Montarias;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Montarias;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Montarias;

public sealed class MontariasModule : IFeatureModule
{
    private ServicoDeMontarias? _servico;

    public string Nome => "Montarias";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(sp => new ServicoDeMontarias(
            tempoReal: sp.GetService<ServidorTempoReal>(),
            controleVelocidade: sp.GetService<IControleDeVelocidadeDoMundo>(),
            combate: sp.GetService<IEstadoDeCombate>(),
            posicoes: sp.GetService<IPosicoesDoMundo>(),
            vizinhanca: sp.GetService<IVizinhanca>(),
            logger: sp.GetService<ILogger<ServicoDeMontarias>>()
        ));
        servicos.AddSingleton<IMontariasConsulta>(sp =>
            sp.GetRequiredService<ServicoDeMontarias>()
        );
    }

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        _servico = rotas.ServiceProvider.GetRequiredService<ServicoDeMontarias>();
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(MontariasMessageIds.Faixa)
            .Ao<ChamarMontaria>(MontariasMessageIds.ChamarMontaria, AoChamarMontaria)
            .Ao<Desmontar>(MontariasMessageIds.Desmontar, AoDesmontar);

    public void Inicializar(ServerContext contexto)
    {
        contexto.TempoReal.SessaoEncerrada += AoEncerrar;
        contexto.Eventos.Assinar<PersonagemCaiu>(AoPersonagemCair);
    }

    private void AoChamarMontaria(SessaoTempoReal sessao, ChamarMontaria pedido)
    {
        _servico?.ChamarMontaria(sessao.PersonagemId, sessao);
    }

    private void AoDesmontar(SessaoTempoReal sessao, Desmontar pedido)
    {
        _servico?.Desmontar(sessao.PersonagemId, sessao);
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
