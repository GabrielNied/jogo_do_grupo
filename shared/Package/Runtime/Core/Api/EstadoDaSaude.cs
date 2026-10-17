namespace JogoDoGrupo.Shared.Core.Api
{
    /// <summary>Resposta pública de disponibilidade e compatibilidade da API.</summary>
    [System.Serializable]
    public class EstadoDaSaude
    {
        public const int VersaoAtual = 1;

        public string Estado = "";
        public int Versao;
        public string Banco = "";
    }
}
