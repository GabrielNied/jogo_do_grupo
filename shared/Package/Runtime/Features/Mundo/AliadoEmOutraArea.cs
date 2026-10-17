namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Membro do grupo em outra área ou instância: só o rótulo no mapa-múndi (#903), sem posição no
    /// mundo aberto.
    /// </summary>
    [System.Serializable]
    public class AliadoEmOutraArea
    {
        public long PersonagemId = 0;

        /// <summary>Ex.: Cidade, Campo, Dungeon (<see cref="RotulosDeArea"/>).</summary>
        public string Rotulo = "";
    }
}
