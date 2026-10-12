using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// A parte do estado caído que mexe nos recursos guardados: a vida com que o personagem levanta
/// e a volta à cidade com tudo cheio. Em memória, na mesma trava do resto de Combate (<see cref="RecursosEmMemoria"/>).
/// Não mexe em item, XP, moeda nem nível: morrer não perde nada.
/// </summary>
internal static class RecursosDaQueda
{
    /// <summary>Vida em 20% do máximo (mínimo 1); vigor e energia como estavam.</summary>
    public static Task<RecursosMudaram> LevantarAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        CancellationToken cancelamento
    ) =>
        AtualizarAsync(
            escopos,
            personagemId,
            (recursos, atributos) =>
                recursos.Vida = RegrasQueda.VidaAoLevantar(
                    RegrasRecursos.VidaMaxima(atributos.Vitalidade)
                ),
            cancelamento
        );

    /// <summary>Vida, vigor e energia cheios (a volta à cidade).</summary>
    public static Task<RecursosMudaram> EncherTudoAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        CancellationToken cancelamento
    ) =>
        AtualizarAsync(
            escopos,
            personagemId,
            (recursos, atributos) =>
            {
                recursos.Vida = RegrasRecursos.VidaMaxima(atributos.Vitalidade);
                recursos.Vigor = RegrasRecursos.VigorMaximo();
                recursos.Energia = RegrasRecursos.EnergiaMaxima(atributos.Espirito);
            },
            cancelamento
        );

    private static Task<RecursosMudaram> AtualizarAsync(
        IServiceScopeFactory escopos,
        long personagemId,
        Action<RecursosPersonagem, AtributosCombate> mudar,
        CancellationToken cancelamento
    ) => RecursosDoAtaque.Recursos(escopos).AtualizarAsync(personagemId, mudar, cancelamento);
}
