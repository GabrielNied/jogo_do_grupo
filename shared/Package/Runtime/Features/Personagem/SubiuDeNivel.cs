namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Servidor avisa quem está na área (inclusive o dono) que um personagem subiu de nível.
    /// Subir vários níveis de uma vez gera uma mensagem só, com o nível final.
    /// Id: <see cref="PersonagemMessageIds.SubiuDeNivel"/>.
    /// </summary>
    [System.Serializable]
    public class SubiuDeNivel
    {
        public long PersonagemId = 0;

        /// <summary>Nível novo do personagem.</summary>
        public int Nivel = 1;
    }
}
