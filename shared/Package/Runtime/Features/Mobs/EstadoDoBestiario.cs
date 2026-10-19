namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Resposta do servidor contendo o estado completo das entradas do bestiário do jogador (#922).
    /// </summary>
    public sealed class EstadoDoBestiario
    {
        public EstadoDoBestiario()
        {
            Entradas = System.Array.Empty<EntradaBestiarioDto>();
        }

        public EstadoDoBestiario(EntradaBestiarioDto[] entradas)
        {
            Entradas = entradas;
        }

        public EntradaBestiarioDto[] Entradas { get; set; }
    }
}
