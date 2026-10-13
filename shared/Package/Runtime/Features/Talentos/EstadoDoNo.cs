namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Estado de um nó para o jogador (a interface mostra ícone e texto, não só cor).</summary>
    public enum EstadoDoNo
    {
        /// <summary>Falta cumprir algum requisito.</summary>
        Bloqueado = 0,

        /// <summary>Requisitos cumpridos e ainda há rank para comprar.</summary>
        Disponivel = 1,

        /// <summary>Tem pelo menos um rank, mas não o máximo.</summary>
        Comprado = 2,

        /// <summary>No rank máximo.</summary>
        NoMaximo = 3,

        /// <summary>Comprado, mas o efeito só vale para certas skills (nó condicional).</summary>
        CondicionalmenteAtivo = 4,
    }
}
