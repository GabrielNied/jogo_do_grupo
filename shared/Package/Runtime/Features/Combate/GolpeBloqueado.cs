namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa quem está na área que a guarda de um personagem segurou um golpe: o dano
    /// foi zero e o vigor caiu <see cref="VigorGasto"/>. O vigor novo chega ao dono em
    /// <see cref="RecursosMudaram"/>. Id: <see cref="CombateMessageIds.GolpeBloqueado"/>.
    /// </summary>
    [System.Serializable]
    public class GolpeBloqueado
    {
        public long PersonagemId = 0;
        public int VigorGasto = 0;
    }
}
