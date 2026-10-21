namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Servidor → client: lista dos canais e lotação atual.</summary>
    [System.Serializable]
    public sealed class RespostaCanais
    {
        public int CanalAtual;
        public InfoCanal[] Canais = new InfoCanal[0];
    }
}
