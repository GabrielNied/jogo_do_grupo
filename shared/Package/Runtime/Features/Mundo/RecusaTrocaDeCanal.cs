namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Servidor → client: recusa a solicitação de troca de canal com o motivo.</summary>
    [System.Serializable]
    public sealed class RecusaTrocaDeCanal
    {
        public string Motivo = "";
        public int CanalAtual;
    }
}
