namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Client → Servidor: solicita ligar ou desligar a bandeira de PvP no mundo aberto.
    /// </summary>
    [System.Serializable]
    public class AlternarBandeiraPvp
    {
        public bool Ativar;
    }
}
