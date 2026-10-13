using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// A parte do estado caído que mexe nos recursos guardados: a vida com que o personagem levanta
/// e a volta à cidade com tudo cheio. Em memória, na mesma trava do resto de Combate (<see cref="RecursosEmMemoria"/>).
/// Não mexe em item, XP, moeda nem nível: morrer não perde nada.
/// </summary>
internal static class RecursosDaQueda
{
    /// <summary>Vida em 20% do máximo (mínimo 1); stamina e mana como estavam.</summary>
    public static Task<RecursosMudaram> LevantarAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        CancellationToken cancelamento
    ) =>
        AtualizarAsync(
            escopos,
            personagemId,
            (recursos, maximos) => recursos.Vida = RegrasQueda.VidaAoLevantar(maximos.Vida),
            cancelamento
        );

    /// <summary>Vida, stamina e mana cheios (a volta à cidade).</summary>
    public static Task<RecursosMudaram> EncherTudoAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        CancellationToken cancelamento
    ) =>
        AtualizarAsync(
            escopos,
            personagemId,
            (recursos, maximos) =>
            {
                recursos.Vida = maximos.Vida;
                recursos.Stamina = maximos.Stamina;
                recursos.Mana = maximos.Mana;
            },
            cancelamento
        );

    private static Task<RecursosMudaram> AtualizarAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        Action<RecursosPersonagem, MaximosDeRecursos> mudar,
        CancellationToken cancelamento
    ) => RecursosDoAtaque.Recursos(escopos).AtualizarAsync(personagemId, mudar, cancelamento);
}
