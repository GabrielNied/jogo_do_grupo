using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Skills;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JogoDoGrupo.Server.Features.Skills;

public sealed class SkillsModule : IFeatureModule
{
    // Criado pelo contêiner quando o app sobe (hosted service); o manipulador de rede só o usa
    // depois que uma sessão se autentica.
    private volatile ServicoDeSkills? _servico;

    public string Nome => "Skills (magias)";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.TryAddSingleton<ISorteadorDeSkills, SorteadorDeSkills>();
        servicos.AddSingleton<IArmazemDeSkills, ArmazemDeSkills>();
        servicos.AddSingleton(sp =>
        {
            var relogio = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new MotorDePoderes(
                sp.GetRequiredService<IEfeitosDoEquipamento>(),
                () => relogio.GetUtcNow().ToUnixTimeMilliseconds(),
                sp.GetService<ILogger<MotorDePoderes>>()
            );
        });
        servicos.AddSingleton(sp =>
        {
            var relogio = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new NucleoDeSkills(
                sp.GetRequiredService<IArmaEquipada>(),
                sp.GetRequiredService<IArmazemDeSkills>(),
                sp.GetRequiredService<ICobrancaDeRecursos>(),
                sp.GetRequiredService<IAtributosDosEquipamentos>(),
                sp.GetRequiredService<IPosicoesDoMundo>(),
                sp.GetServices<IAlvosDeCombate>(),
                () => relogio.GetUtcNow().ToUnixTimeMilliseconds(),
                sp.GetService<ITrocaDeArma>(),
                sp.GetRequiredService<MotorDePoderes>(),
                sp.GetService<IApoioDeCombate>(),
                sp.GetService<IConsultaDeGrupo>()
            );
        });
        servicos.AddSingleton(sp =>
        {
            var servico = ActivatorUtilities.CreateInstance<ServicoDeSkills>(sp);
            _servico = servico;
            return servico;
        });
        servicos.AddHostedService(sp => sp.GetRequiredService<ServicoDeSkills>());
    }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(SkillsMessageIds.Faixa)
            .Ao<UsarSkill>(
                SkillsMessageIds.UsarSkill,
                (sessao, pedido) => _servico?.AoUsar(sessao, pedido)
            )
            .Ao<CancelarSkill>(
                SkillsMessageIds.CancelarSkill,
                (sessao, _) => _servico?.AoCancelar(sessao)
            )
            .Ao<PedirBarraDeSkills>(
                SkillsMessageIds.PedirBarraDeSkills,
                (sessao, _) => _servico?.AoPedirBarra(sessao)
            );

    public void Inicializar(ServerContext contexto)
    {
        contexto.Eventos.Assinar<EquipamentoMudou>(evento => _servico?.AoEquipamentoMudar(evento));
        // Combate publica; sem ele publicando, os poderes de golpe comum e de esquiva ficam parados.
        contexto.Eventos.Assinar<GolpeComumAcertou>(evento => _servico?.AoGolpeComum(evento));
        contexto.Eventos.Assinar<PersonagemEsquivou>(evento => _servico?.AoEsquivar(evento));
    }
}
