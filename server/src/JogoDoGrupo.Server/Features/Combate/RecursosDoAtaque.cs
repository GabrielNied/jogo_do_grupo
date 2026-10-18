using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Resultado de conferir os recursos no começo de um ataque: <see cref="Recusa"/> é um código
/// de <see cref="CodigosErroCombate"/> (nada foi gasto) ou nulo se o ataque vale;
/// <see cref="Recursos"/> é o novo estado, só quando o ataque gastou stamina. <see cref="Dano"/>
/// são as parcelas do dano lidas na mesma ida (o acerto não volta lá): dano-base da arma e bônus
/// do personagem e do equipamento, cada um na sua parcela.
/// </summary>
public sealed record InicioDoAtaque(string? Recusa, RecursosMudaram? Recursos, ParcelasDeDano Dano);

/// <summary>
/// A parte do ataque que mexe nos recursos: recusa quem está caído ou sem stamina e cobra a stamina
/// do pesado, uma vez só. Tudo em memória, sob a trava do personagem em
/// <see cref="RecursosEmMemoria"/>, então pedidos simultâneos rodam um depois do outro e a stamina
/// nunca é cobrado duas vezes; o golpe leve (custo 0) não toca no banco.
/// </summary>
internal static class RecursosDoAtaque
{
    public static Task<InicioDoAtaque> TentarIniciarAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        TipoDeAtaque tipo,
        CancellationToken cancelamento
    ) =>
        TentarGastarStaminaAsync(
            escopos,
            personagemId,
            RegrasAtaque.CustoDeStamina(tipo),
            cancelamento
        );

    /// <summary>
    /// A mesma conferência para qualquer ação que custa stamina (ataque ou esquiva): recusa quem
    /// está caído ou não tem <paramref name="custo"/> de stamina; senão cobra, uma vez só.
    /// </summary>
    public static Task<InicioDoAtaque> TentarGastarStaminaAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        int custo,
        CancellationToken cancelamento
    ) => Recursos(escopos).TentarGastarStaminaAsync(personagemId, custo, cancelamento);

    /// <summary>
    /// Tira <paramref name="dano"/> da vida do personagem (nunca abaixo de 0): golpes simultâneos
    /// rodam um depois do outro e somam. Devolve o novo estado para o dono.
    /// </summary>
    public static Task<RecursosMudaram> TirarVidaAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        int dano,
        CancellationToken cancelamento
    ) => Recursos(escopos).TirarVidaAsync(personagemId, dano, cancelamento);

    /// <summary>
    /// Cura <paramref name="cura"/> da vida do personagem (nunca acima do máximo): curas simultâneas
    /// rodam uma depois da outra e somam. Devolve o novo estado para o dono.
    /// </summary>
    public static Task<RecursosMudaram> CurarVidaAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        int cura,
        CancellationToken cancelamento
    ) => Recursos(escopos).CurarVidaAsync(personagemId, cura, cancelamento);

    /// <summary>
    /// Cobra da stamina o golpe contra a guarda segurada (ou quebra a guarda se não há stamina).
    /// </summary>
    public static Task<(bool Bloqueou, int StaminaGasto, RecursosMudaram Recursos)> BloquearAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        int dano,
        CancellationToken cancelamento
    ) => Recursos(escopos).BloquearAsync(personagemId, dano, cancelamento);

    // O dono dos recursos é um singleton: o escopo só serve para chegar nele.
    internal static RecursosEmMemoria Recursos(IServiceScopeFactory escopos)
    {
        using var escopo = escopos.CreateScope();
        return escopo.ServiceProvider.GetRequiredService<RecursosEmMemoria>();
    }
}
