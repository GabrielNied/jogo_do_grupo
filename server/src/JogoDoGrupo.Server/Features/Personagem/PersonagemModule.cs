using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Features.Personagem;

public sealed class PersonagemModule : IFeatureModule
{
    // O contêiner cria o serviço quando o app sobe; a morte só chega depois, no laço de IA.
    private volatile ExperienciaDoPersonagem? _experiencia;
    private volatile ServicoDeMovimentoEspecial? _movimento;

    public string Nome => "Personagem";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddScoped<IVerificadorPersonagem, VerificadorPersonagem>();
        servicos.AddScoped<IPontosDeNivel, PontosDeNivel>();
        servicos.AddSingleton<IDiretorioDePersonagens, DiretorioDePersonagens>();
        servicos.AddSingleton(sp =>
        {
            var servico = new ExperienciaDoPersonagem(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ServidorTempoReal>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetRequiredService<ILogger<ExperienciaDoPersonagem>>()
            );
            _experiencia = servico;
            return servico;
        });
        servicos.AddSingleton<IAjusteDeExperienciaDeTeste>(sp =>
            sp.GetRequiredService<ExperienciaDoPersonagem>()
        );
        servicos.AddHostedService<IniciarExperiencia>();
        servicos.AddSingleton(sp =>
        {
            var tempoReal = sp.GetRequiredService<ServidorTempoReal>();
            var servico = new ServicoDeMovimentoEspecial(
                sp.GetRequiredService<IPosicoesDoMundo>(),
                () => tempoReal.Sessoes.Select(s => s.PersonagemId).Where(id => id > 0).ToList(),
                (id, estado) =>
                    EnviarAoDono(tempoReal, id, PersonagemMessageIds.EstadoDoMovimento, estado),
                (id, dano) => EnviarAoDono(tempoReal, id, PersonagemMessageIds.DanoDeQueda, dano),
                (id, recursos) =>
                    EnviarAoDono(tempoReal, id, CombateMessageIds.RecursosMudaram, recursos),
                sp.GetService<ICobrancaDeRecursos>(),
                sp.GetService<IDanoAoPersonagem>(),
                sp.GetService<ILimiteDeVelocidadeDoMundo>(),
                sp.GetService<IEstadoDeQueda>(),
                log: sp.GetService<ILogger<ServicoDeMovimentoEspecial>>()
            );
            _movimento = servico;
            tempoReal.SessaoEncerrada += sessao => servico.Remover(sessao.PersonagemId);
            return servico;
        });
        servicos.AddSingleton<IEstadoDeMovimento>(sp =>
            sp.GetRequiredService<ServicoDeMovimentoEspecial>()
        );
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeMovimentoEspecial>());
    }

    public void MapearRotas(IEndpointRouteBuilder rotas) => PersonagensRotas.Mapear(rotas);

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(PersonagemMessageIds.Faixa)
            .Ao<IntencaoDeEscalada>(
                PersonagemMessageIds.IntencaoDeEscalada,
                (sessao, pedido) => _movimento?.AoEscalar(sessao.PersonagemId, pedido)
            );

    private static void EnviarAoDono<T>(
        ServidorTempoReal tempoReal,
        long personagemId,
        ushort id,
        T mensagem
    )
    {
        foreach (var sessao in tempoReal.Sessoes)
        {
            if (sessao.PersonagemId == personagemId)
            {
                tempoReal.Enviar(sessao, id, mensagem);
            }
        }
    }

    public void Inicializar(ServerContext contexto) =>
        contexto.Eventos.Assinar<InimigoDerrotado>(derrota => _experiencia?.Agendar(derrota));
}

/// <summary>
/// Garante que a experiência existe antes de o servidor aceitar sessão. O construtor resolve
/// o serviço e preenche o campo que o evento de morte lê.
/// </summary>
internal sealed class IniciarExperiencia : IHostedService
{
    public IniciarExperiencia(ExperienciaDoPersonagem experiencia) => _ = experiencia;

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento) => Task.CompletedTask;
}
