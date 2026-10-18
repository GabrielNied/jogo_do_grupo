namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Onde ficam o vendedor e o ferreiro na cidade e o raio que o servidor aceita. O client usa
    /// os mesmos pontos para o marcador e a dica de interação (#173).
    /// </summary>
    public static class ConfigNpcEconomia
    {
        /// <summary>Área onde os NPCs de economia existem (a cidade).</summary>
        public const string Area = "cidade";

        /// <summary>Vendedor de NPC: praça leste, perto do chão de pedra do mercado.</summary>
        public const float VendedorX = 12f;

        /// <summary>Posição Z do vendedor na cidade.</summary>
        public const float VendedorZ = -8f;

        /// <summary>Ferreiro: lado oeste da praça, perto da forja da cidade.</summary>
        public const float FerreiroX = -12f;

        /// <summary>Posição Z do ferreiro na cidade.</summary>
        public const float FerreiroZ = -6f;

        /// <summary>
        /// Distância horizontal máxima, em metros, que o servidor aceita entre o personagem e o
        /// NPC. Maior que a do client (3 m) para a posição atrasada da rede não recusar quem já
        /// vê a dica.
        /// </summary>
        public const float RaioParaInteragirMetros = 4f;

        /// <summary>Se o ponto está ao alcance do vendedor.</summary>
        public static bool DentroDoRaioVendedor(float x, float z)
        {
            var dx = x - VendedorX;
            var dz = z - VendedorZ;
            return dx * dx + dz * dz <= RaioParaInteragirMetros * RaioParaInteragirMetros;
        }

        /// <summary>Se o ponto está ao alcance do ferreiro.</summary>
        public static bool DentroDoRaioFerreiro(float x, float z)
        {
            var dx = x - FerreiroX;
            var dz = z - FerreiroZ;
            return dx * dx + dz * dz <= RaioParaInteragirMetros * RaioParaInteragirMetros;
        }
    }
}
