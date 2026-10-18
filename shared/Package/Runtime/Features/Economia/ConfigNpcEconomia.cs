namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Onde ficam o vendedor e o ferreiro na cidade e o raio que o servidor aceita. O client usa
    /// os mesmos pontos para o marcador e a dica de interação (#173).
    /// </summary>
    public static class ConfigNpcEconomia
    {
        /// <summary>Área onde os NPCs de economia existem (a cidade).</summary>
        public const string Area = Npcs.CatalogoNpcs.AreaCidade;

        /// <summary>Vendedor de NPC: praça leste, perto do chão de pedra do mercado.</summary>
        public static float VendedorX => Npcs.CatalogoNpcs.Vendedor.X;

        /// <summary>Posição Z do vendedor na cidade.</summary>
        public static float VendedorZ => Npcs.CatalogoNpcs.Vendedor.Z;

        /// <summary>Ferreiro: lado oeste da praça, perto da forja da cidade.</summary>
        public static float FerreiroX => Npcs.CatalogoNpcs.Ferreiro.X;

        /// <summary>Posição Z do ferreiro na cidade.</summary>
        public static float FerreiroZ => Npcs.CatalogoNpcs.Ferreiro.Z;

        /// <summary>
        /// Distância horizontal máxima, em metros, que o servidor aceita entre o personagem e o
        /// NPC. Maior que a do client (3 m) para a posição atrasada da rede não recusar quem já
        /// vê a dica.
        /// </summary>
        public static float RaioParaInteragirMetros => Npcs.CatalogoNpcs.Vendedor.RaioInteracao;

        /// <summary>Se o ponto está ao alcance do vendedor.</summary>
        public static bool DentroDoRaioVendedor(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdVendedor, x, z);

        /// <summary>Se o ponto está ao alcance do ferreiro.</summary>
        public static bool DentroDoRaioFerreiro(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdFerreiro, x, z);
    }
}
