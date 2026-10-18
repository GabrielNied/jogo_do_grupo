namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Clientes da área: avisa que a bandeira PvP de um personagem foi alterada.
    /// </summary>
    [System.Serializable]
    public class BandeiraPvpAlterada
    {
        public long PersonagemId;
        public bool Ativa;
    }
}
