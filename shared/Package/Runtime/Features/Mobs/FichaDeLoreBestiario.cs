namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Informações descritivas, de lore e pistas de combate de uma criatura no bestiário (#922).
    /// </summary>
    public sealed class FichaDeLoreBestiario
    {
        public FichaDeLoreBestiario(
            int criaturaId,
            string regiao,
            string descricaoGeral,
            string sinaisDeAtaque,
            string fraquezas,
            string dropsConhecidos,
            string fraseBestiario
        )
        {
            CriaturaId = criaturaId;
            Regiao = regiao;
            DescricaoGeral = descricaoGeral;
            SinaisDeAtaque = sinaisDeAtaque;
            Fraquezas = fraquezas;
            DropsConhecidos = dropsConhecidos;
            FraseBestiario = fraseBestiario;
        }

        public int CriaturaId { get; }
        public string Regiao { get; }
        public string DescricaoGeral { get; }
        public string SinaisDeAtaque { get; }
        public string Fraquezas { get; }
        public string DropsConhecidos { get; }
        public string FraseBestiario { get; }
    }
}
