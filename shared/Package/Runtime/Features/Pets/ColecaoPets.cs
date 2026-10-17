namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Lista de espécies de pets desbloqueadas para a conta do jogador.
    /// Mensagem servidor -> client. Id: <see cref="PetsMessageIds.ColecaoPets"/>.
    /// </summary>
    [System.Serializable]
    public class ColecaoPets
    {
        public int[] EspeciesDesbloqueadas = System.Array.Empty<int>();
    }
}
