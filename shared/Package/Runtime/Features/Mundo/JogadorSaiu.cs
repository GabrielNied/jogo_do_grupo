namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor envia a todos os presentes quando um jogador sai da cidade.
    /// Id: <see cref="MundoMessageIds.JogadorSaiu"/>.
    /// </summary>
    [System.Serializable]
    public class JogadorSaiu
    {
        public long PersonagemId = 0;
    }
}
