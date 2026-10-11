namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// O que o inimigo está fazendo, decidido pelo servidor. Os valores são estáveis: nunca mude
    /// nem reaproveite um número já publicado. As passagens permitidas estão em
    /// <see cref="RegrasInimigo.TransicaoValida"/>.
    /// </summary>
    public enum EstadoDeInimigo
    {
        /// <summary>Parado no ponto de origem, sem alvo.</summary>
        Repouso = 0,

        /// <summary>Viu um jogador (ou alguém do grupo viu) e está prestes a perseguir.</summary>
        Alerta = 1,

        /// <summary>Indo até o alvo.</summary>
        Perseguicao = 2,

        /// <summary>No aviso, no golpe ou na recuperação de um ataque.</summary>
        Ataque = 3,

        /// <summary>Desistiu e volta ao ponto de origem; não escolhe alvo até chegar.</summary>
        Retorno = 4,

        /// <summary>Morto: não age nem leva dano.</summary>
        Morto = 5,
    }
}
