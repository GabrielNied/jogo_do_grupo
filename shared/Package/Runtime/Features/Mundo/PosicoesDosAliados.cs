namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor → client, cerca de 1 vez por segundo: onde estão os membros do grupo na cidade
    /// que o client não recebe pela presença (fora da área de interesse). Serve só ao mapa e ao
    /// minimapa; o modelo 3D continua só perto. Substitui a lista anterior: quem não vem mais
    /// (chegou perto, saiu do grupo ou da cidade) some do mapa. Lista vazia é válida.
    /// Id: <see cref="MundoMessageIds.PosicoesDosAliados"/>.
    /// </summary>
    [System.Serializable]
    public class PosicoesDosAliados
    {
        public AliadoLonge[] Aliados = System.Array.Empty<AliadoLonge>();
    }
}
