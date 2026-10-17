namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Servidor → client: lista dos canais e lotação atual.</summary>
    public sealed class RespostaCanais
    {
        public int CanalAtual { get; set; }
        public InfoCanal[] Canais { get; set; }
    }
}
