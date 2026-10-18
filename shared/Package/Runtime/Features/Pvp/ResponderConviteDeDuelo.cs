namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Client → Servidor: o desafiado aceita ou recusa o convite de duelo.
    /// </summary>
    [System.Serializable]
    public class ResponderConviteDeDuelo
    {
        public long DesafianteId;
        public bool Aceito;
    }
}
