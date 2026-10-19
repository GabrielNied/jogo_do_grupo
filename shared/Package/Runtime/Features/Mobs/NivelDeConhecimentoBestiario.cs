namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Nível de conhecimento adquirido por um jogador sobre uma espécie (#922).
    /// </summary>
    public enum NivelDeConhecimentoBestiario
    {
        /// <summary>A criatura ainda não foi avistada ou derrotada. Exibe silhueta e pontos de interrogação.</summary>
        Desconhecido = 0,

        /// <summary>1ª derrota. Revela o nome, categoria, nível e região onde ela habita.</summary>
        Avistado = 1,

        /// <summary>5 derrotas (comuns) ou 3 (chefes). Revela vida base, dano base e sinais de ataque.</summary>
        Estudado = 2,

        /// <summary>20 derrotas (comuns) ou 5 (chefes). Revela fraquezas, drops conhecidos e frase de lore.</summary>
        Mestre = 3,
    }
}
