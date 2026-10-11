namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// O que já existe entre quem pede amizade e o alvo, visto por quem pede. Só o servidor sabe;
    /// serve de entrada para <see cref="RegrasAmigos.RecusaDoPedido"/>.
    /// </summary>
    public enum RelacaoDeAmizade
    {
        /// <summary>Nem amizade nem pedido entre os dois.</summary>
        Nenhuma = 0,

        /// <summary>Quem pede já tem um pedido sem resposta para o alvo.</summary>
        PedidoEnviado = 1,

        /// <summary>O alvo já tinha pedido amizade a quem pede.</summary>
        PedidoRecebido = 2,

        /// <summary>Os dois já são amigos.</summary>
        Amigos = 3,
    }
}
