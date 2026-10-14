using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// A cobrança de custo de stamina ou mana para quem não é o ataque (skills). Usa a operação
/// atômica de <see cref="RecursosEmMemoria.AtualizarAsync"/>: regenera até agora, confere e
/// desconta sob a trava do personagem, então pedidos simultâneos nunca cobram duas vezes.
/// Gastar reinicia a espera da regeneração do recurso, como o gasto do ataque.
/// </summary>
internal sealed class CobrancaDeRecursos(RecursosEmMemoria recursos, TimeProvider relogio)
    : ICobrancaDeRecursos
{
    public async Task<(ResultadoDaCobranca Resultado, RecursosMudaram? Recursos)> TentarCobrarAsync(
        long personagemId,
        RecursoCobravel recurso,
        int custo,
        CancellationToken cancelamento
    )
    {
        var resultado = ResultadoDaCobranca.Cobrado;
        var estado = await recursos.AtualizarAsync(
            personagemId,
            (r, _) =>
            {
                if (r.Vida <= 0)
                {
                    resultado = ResultadoDaCobranca.Caido;
                    return;
                }

                var agora = relogio.GetUtcNow().UtcDateTime;
                if (recurso == RecursoCobravel.Mana)
                {
                    if (r.Mana < custo)
                    {
                        resultado = ResultadoDaCobranca.Insuficiente;
                        return;
                    }

                    r.Mana -= custo;
                    r.UltimoGastoManaEm = agora;
                }
                else
                {
                    if (r.Stamina < custo)
                    {
                        resultado = ResultadoDaCobranca.Insuficiente;
                        return;
                    }

                    r.Stamina -= custo;
                    r.UltimoGastoStaminaEm = agora;
                }
            },
            cancelamento
        );
        return (resultado, resultado == ResultadoDaCobranca.Cobrado ? estado : null);
    }
}

/// <summary>
/// Registra a cobrança de recursos de Combate para as outras funcionalidades. Módulo à parte
/// para não mexer no <c>CombateModule</c>; os dois são descobertos sozinhos.
/// </summary>
public sealed class CobrancaDeRecursosModule : IFeatureModule
{
    public string Nome => "Combate (cobrança de recursos)";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) =>
        servicos.AddSingleton<ICobrancaDeRecursos>(sp => new CobrancaDeRecursos(
            sp.GetRequiredService<RecursosEmMemoria>(),
            sp.GetService<TimeProvider>() ?? TimeProvider.System
        ));

    public void Inicializar(ServerContext contexto) { }
}
