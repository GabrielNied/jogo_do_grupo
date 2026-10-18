namespace JogoDoGrupo.Shared.Features.Pvp
{
    /// <summary>
    /// Servidor → Participantes: combatente foi derrotado dentro da arena.
    /// </summary>
    [System.Serializable]
    public class CombatenteDeArenaEliminado
    {
        public string IdPartida = "";
        public long PersonagemId;
        public int Equipe; // 0 = Azul, 1 = Vermelho
    }
}
