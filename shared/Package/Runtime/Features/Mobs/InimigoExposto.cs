namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Servidor avisa quem está na área que um boss ficou exposto: durante a janela ele recebe
    /// mais dano. A janela acaba sozinha depois de <see cref="DuracaoMs"/>; não há mensagem de
    /// fim. Id: <see cref="MobsMessageIds.InimigoExposto"/>.
    /// </summary>
    [System.Serializable]
    public class InimigoExposto
    {
        public long InimigoId = 0;

        /// <summary>Milissegundos que a janela dura, a contar desta mensagem.</summary>
        public int DuracaoMs = 0;

        /// <summary>Por quanto o dano recebido é multiplicado enquanto ele está exposto.</summary>
        public float Multiplicador = 1f;
    }
}
