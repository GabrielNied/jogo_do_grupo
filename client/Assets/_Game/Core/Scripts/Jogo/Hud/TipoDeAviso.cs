namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>Tipo de uma mensagem de sistema (<see cref="AvisosDoJogo.Mostrar"/>); muda a cor.</summary>
    public enum TipoDeAviso
    {
        /// <summary>Informação (branco). Ex.: "Ana entrou no grupo".</summary>
        Informacao,

        /// <summary>Aviso (amarelo). Ex.: "Inventário cheio".</summary>
        Aviso,

        /// <summary>Erro (vermelho). Ex.: "Vigor insuficiente".</summary>
        Erro,
    }
}
