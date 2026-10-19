using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>De qual recurso do personagem um custo é cobrado.</summary>
public enum RecursoCobravel
{
    Stamina,
    Mana,
}

/// <summary>O resultado de <see cref="ICobrancaDeRecursos.TentarCobrarAsync"/>.</summary>
public enum ResultadoDaCobranca
{
    /// <summary>O custo foi tirado do recurso.</summary>
    Cobrado,

    /// <summary>O personagem está caído (vida zero): nada foi tirado.</summary>
    Caido,

    /// <summary>O recurso não cobre o custo: nada foi tirado.</summary>
    Insuficiente,
}

/// <summary>
/// Cobrança de custo de stamina ou mana, oferecida por Combate (dono dos recursos) às outras
/// funcionalidades (ADR 0016), para ações que gastam recurso fora do ataque (skills). A conferência
/// e o desconto são uma operação só, sob a trava do personagem: pedidos simultâneos rodam um depois
/// do outro e o custo nunca é cobrado duas vezes. Pode ser chamada de qualquer thread.
/// </summary>
public interface ICobrancaDeRecursos
{
    /// <summary>
    /// Cobra <paramref name="custo"/> do recurso. Devolve o novo estado dos recursos quando cobrou
    /// (para o dono ver as barras), e <c>null</c> numa recusa.
    /// </summary>
    Task<(ResultadoDaCobranca Resultado, RecursosMudaram? Recursos)> TentarCobrarAsync(
        long personagemId,
        RecursoCobravel recurso,
        int custo,
        CancellationToken cancelamento
    );

    /// <summary>
    /// Confere sem cobrar se <paramref name="custo"/> cabe no recurso agora (preparo de skill forte:
    /// recusa cedo; a cobrança só vem ao concluir ou cancelar). Devolve <see cref="ResultadoDaCobranca.Cobrado"/>
    /// se cabe, sem descontar nada.
    /// </summary>
    Task<ResultadoDaCobranca> PodeCobrarAsync(
        long personagemId,
        RecursoCobravel recurso,
        int custo,
        CancellationToken cancelamento
    );
}
