namespace JogoDoGrupo.Shared.Core.TempoReal
{
    /// <summary>
    /// Configuração do transporte UDP que servidor e client precisam ter igual.
    /// </summary>
    public static class TransporteTempoReal
    {
        /// <summary>
        /// Canais da LiteNetLib (<c>NetManager.ChannelsCount</c>). O servidor manda as mensagens
        /// sequenciadas de cada entidade no canal <c>id % Canais</c>, para uma entidade não
        /// descartar a posição de outra; a LiteNetLib descarta em silêncio pacote de canal maior
        /// que os canais de quem recebe, então os dois lados usam este valor.
        /// </summary>
        public const byte Canais = 32;
    }
}
