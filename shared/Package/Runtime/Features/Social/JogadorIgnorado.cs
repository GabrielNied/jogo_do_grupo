namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>Um jogador em <see cref="ListaDeIgnorados.Ignorados"/>.</summary>
    [System.Serializable]
    public class JogadorIgnorado
    {
        /// <summary>Gerado pelo servidor; não é o id da conta ignorada.</summary>
        public long IgnoradoId = 0;

        /// <summary>Nome do personagem pelo qual o jogador foi ignorado.</summary>
        public string Nome = "";
    }
}
