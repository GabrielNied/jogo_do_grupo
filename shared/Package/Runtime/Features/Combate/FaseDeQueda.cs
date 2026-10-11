namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Fase do personagem no estado caído. Os valores são estáveis: nunca mude nem reaproveite
    /// um número já publicado. Regras em docs/features/personagem.md ("Morte e caído").
    /// </summary>
    public enum FaseDeQueda
    {
        /// <summary>Em pé: acabou de ser levantado por um aliado.</summary>
        Vivo = 0,

        /// <summary>Caído, esperando ajuda ou o fim da contagem.</summary>
        Caido = 1,

        /// <summary>Caído, com um aliado segurando a interação.</summary>
        Levantando = 2,

        /// <summary>Saiu do estado caído voltando à cidade (fim da contagem ou pedido).</summary>
        Retorno = 3,
    }
}
