namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Servidor → client: recusa a solicitação de troca de canal com o motivo.</summary>
    public sealed class RecusaTrocaDeCanal
    {
        public string Motivo { get; set; }
        public int CanalAtual { get; set; }
    }
}
