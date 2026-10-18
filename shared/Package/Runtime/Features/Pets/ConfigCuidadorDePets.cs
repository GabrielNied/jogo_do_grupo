using System;

namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>
    /// Configuração e preços dos pets vendidos no Cuidador de Pets da Brasa Mansa (#1068).
    /// </summary>
    public static class ConfigCuidadorDePets
    {
        public const long PrecoGalinhaComum = 50;
        public const long PrecoGatoComum = 75;
        public const long PrecoCachorroComum = 100;

        public static readonly PetCuidadorDto[] Ofertas =
        {
            new PetCuidadorDto(
                (int)EspecieDePet.GalinhaComum,
                RegrasPets.ObterNome(EspecieDePet.GalinhaComum),
                PrecoGalinhaComum,
                RegrasPets.ObterDescricao(EspecieDePet.GalinhaComum),
                RegrasPets.ObterOrigem(EspecieDePet.GalinhaComum)
            ),
            new PetCuidadorDto(
                (int)EspecieDePet.GatoComum,
                RegrasPets.ObterNome(EspecieDePet.GatoComum),
                PrecoGatoComum,
                RegrasPets.ObterDescricao(EspecieDePet.GatoComum),
                RegrasPets.ObterOrigem(EspecieDePet.GatoComum)
            ),
            new PetCuidadorDto(
                (int)EspecieDePet.CachorroComum,
                RegrasPets.ObterNome(EspecieDePet.CachorroComum),
                PrecoCachorroComum,
                RegrasPets.ObterDescricao(EspecieDePet.CachorroComum),
                RegrasPets.ObterOrigem(EspecieDePet.CachorroComum)
            ),
        };

        public static bool EstaAVenda(int especie) => ObterPreco(especie) > 0;

        public static long ObterPreco(int especie) =>
            (EspecieDePet)especie switch
            {
                EspecieDePet.GalinhaComum => PrecoGalinhaComum,
                EspecieDePet.GatoComum => PrecoGatoComum,
                EspecieDePet.CachorroComum => PrecoCachorroComum,
                _ => 0,
            };
    }
}
