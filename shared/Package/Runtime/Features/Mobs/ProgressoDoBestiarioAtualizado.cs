namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Notificação em tempo real do servidor quando o progresso de uma criatura avança (#922).
    /// </summary>
    [System.Serializable]
    public sealed class ProgressoDoBestiarioAtualizado
    {
        public ProgressoDoBestiarioAtualizado()
        {
            Entrada = new EntradaBestiarioDto();
        }

        public ProgressoDoBestiarioAtualizado(EntradaBestiarioDto entrada)
        {
            Entrada = entrada;
        }

        public EntradaBestiarioDto Entrada;
    }
}
