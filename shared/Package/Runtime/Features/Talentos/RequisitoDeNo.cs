namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Pré-requisito: outro nó com pelo menos esse rank. Todos os requisitos de um nó valem juntos.</summary>
    [System.Serializable]
    public class RequisitoDeNo
    {
        public string NoId = "";
        public int RankMinimo = 1;
    }
}
