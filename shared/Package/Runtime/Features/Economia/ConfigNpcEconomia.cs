namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Onde ficam os comerciantes e o ferreiro na cidade e o raio que o servidor aceita. O client usa
    /// os mesmos pontos para os marcadores e dicas de interação (#173, #1066).
    /// </summary>
    public static class ConfigNpcEconomia
    {
        /// <summary>Área onde os NPCs de economia existem (a cidade).</summary>
        public const string Area = Npcs.CatalogoNpcs.AreaCidade;

        /// <summary>Vendedor de NPC legado: praça leste, perto do chão de pedra do mercado.</summary>
        public static float VendedorX => Npcs.CatalogoNpcs.Vendedor.X;

        /// <summary>Posição Z do vendedor na cidade.</summary>
        public static float VendedorZ => Npcs.CatalogoNpcs.Vendedor.Z;

        /// <summary>Ferreiro: lado oeste da praça, perto da forja da cidade.</summary>
        public static float FerreiroX => Npcs.CatalogoNpcs.Ferreiro.X;

        /// <summary>Posição Z do ferreiro na cidade.</summary>
        public static float FerreiroZ => Npcs.CatalogoNpcs.Ferreiro.Z;

        /// <summary>Armeiro: banca de armas na praça da cidade.</summary>
        public static float ArmeiroX => Npcs.CatalogoNpcs.Armeiro.X;
        public static float ArmeiroZ => Npcs.CatalogoNpcs.Armeiro.Z;

        /// <summary>Armadureiro: banca de armaduras na praça da cidade.</summary>
        public static float ArmadureiroX => Npcs.CatalogoNpcs.Armadureiro.X;
        public static float ArmadureiroZ => Npcs.CatalogoNpcs.Armadureiro.Z;

        /// <summary>Taverneiro: na taverna com a cozinha.</summary>
        public static float TaverneiroX => Npcs.CatalogoNpcs.Taverneiro.X;
        public static float TaverneiroZ => Npcs.CatalogoNpcs.Taverneiro.Z;

        /// <summary>Merceeiro: consumíveis e materiais básicos na praça da cidade.</summary>
        public static float MerceeiroX => Npcs.CatalogoNpcs.Merceeiro.X;
        public static float MerceeiroZ => Npcs.CatalogoNpcs.Merceeiro.Z;

        /// <summary>
        /// Distância horizontal máxima, em metros, que o servidor aceita entre o personagem e o
        /// NPC. Maior que a do client (3 m) para a posição atrasada da rede não recusar quem já
        /// vê a dica.
        /// </summary>
        public static float RaioParaInteragirMetros => Npcs.CatalogoNpcs.Vendedor.RaioInteracao;

        /// <summary>Se o ponto está ao alcance do armeiro.</summary>
        public static bool DentroDoRaioArmeiro(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdArmeiro, x, z);

        /// <summary>Se o ponto está ao alcance do armadureiro.</summary>
        public static bool DentroDoRaioArmadureiro(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdArmadureiro, x, z);

        /// <summary>Se o ponto está ao alcance do taverneiro.</summary>
        public static bool DentroDoRaioTaverneiro(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdTaverneiro, x, z);

        /// <summary>Se o ponto está ao alcance do merceeiro.</summary>
        public static bool DentroDoRaioMerceeiro(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdMerceeiro, x, z);

        /// <summary>Se o ponto está ao alcance do vendedor geral (legado).</summary>
        public static bool DentroDoRaioVendedor(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdVendedor, x, z)
            || DentroDoRaioMerceeiro(x, z);

        /// <summary>Se o ponto está ao alcance do ferreiro.</summary>
        public static bool DentroDoRaioFerreiro(float x, float z) =>
            Npcs.CatalogoNpcs.DentroDoAlcance(Npcs.CatalogoNpcs.IdFerreiro, x, z);

        /// <summary>Se o ponto está ao alcance da loja da categoria especificada.</summary>
        public static bool DentroDoRaioLoja(CategoriaLoja categoria, float x, float z) =>
            categoria switch
            {
                CategoriaLoja.Armeiro => DentroDoRaioArmeiro(x, z),
                CategoriaLoja.Armadureiro => DentroDoRaioArmadureiro(x, z),
                CategoriaLoja.Taverneiro => DentroDoRaioTaverneiro(x, z),
                CategoriaLoja.Merceeiro => DentroDoRaioMerceeiro(x, z),
                _ => DentroDoRaioQualquerLoja(x, z),
            };

        /// <summary>Se o ponto está ao alcance de qualquer uma das lojas de NPC.</summary>
        public static bool DentroDoRaioQualquerLoja(float x, float z) =>
            DentroDoRaioArmeiro(x, z)
            || DentroDoRaioArmadureiro(x, z)
            || DentroDoRaioTaverneiro(x, z)
            || DentroDoRaioMerceeiro(x, z)
            || DentroDoRaioVendedor(x, z);
    }
}
