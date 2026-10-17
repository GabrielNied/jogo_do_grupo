namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Catálogo fixo das primeiras criaturas (proposta de 2026-10-11, números a calibrar; veja
    /// docs/features/mobs.md). Os ids são estáveis: nunca mude nem reaproveite um id já
    /// publicado. Comuns ficam de 1 a 99, chefes de 100 a 199 e chefes raros de 200 a 299.
    /// </summary>
    public static class CatalogoCriaturas
    {
        public const int JavaliDoCampo = 1;
        public const int LoboDoCampo = 2;
        public const int TatuRochoso = 3;
        public const int CervoDeGalhadaOca = 4;
        public const int MariposaDeBreu = 5;
        public const int LoboDeGeada = 6;
        public const int CarapacaDeBrasa = 7;
        public const int JavaliFerrugem = 8;
        public const int MorcegoDasSombras = 9;
        public const int GuardiaoDoReflexo = 100;
        public const int PresaVelha = 200;

        /// <summary>Todas as criaturas do catálogo, em ordem crescente de id.</summary>
        public static readonly DefinicaoDeCriatura[] Todas =
        {
            new DefinicaoDeCriatura(
                id: JavaliDoCampo,
                nome: "Javali do Campo",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 3,
                vidaBase: 180,
                danoBase: 40,
                experienciaBase: 20,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 120
            ),
            new DefinicaoDeCriatura(
                id: LoboDoCampo,
                nome: "Lobo do Campo",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 4,
                vidaBase: 110,
                danoBase: 28,
                experienciaBase: 18,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 120
            ),
            new DefinicaoDeCriatura(
                id: TatuRochoso,
                nome: "Tatu Rochoso",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 5,
                vidaBase: 320,
                danoBase: 55,
                experienciaBase: 30,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 150
            ),
            new DefinicaoDeCriatura(
                id: GuardiaoDoReflexo,
                nome: "Guardião do Reflexo",
                categoria: CategoriaDeCriatura.Chefe,
                nivel: 5,
                vidaBase: 6000,
                danoBase: 120,
                experienciaBase: 500,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 0
            ),
            new DefinicaoDeCriatura(
                id: PresaVelha,
                nome: "Presa-Velha",
                categoria: CategoriaDeCriatura.ChefeRaro,
                nivel: 5,
                vidaBase: 1500,
                danoBase: 80,
                experienciaBase: 200,
                chanceDeAparecer: 0.2,
                intervaloDeAparicaoSegundos: 1800
            ),
        };

        /// <summary>
        /// Ficha da criatura, ou <c>null</c> se <paramref name="criaturaId"/> não está no
        /// catálogo.
        /// </summary>
        public static DefinicaoDeCriatura Obter(int criaturaId)
        {
            for (int i = 0; i < Todas.Length; i++)
            {
                if (Todas[i].Id == criaturaId)
                {
                    return Todas[i];
                }
            }
            return null;
        }
    }
}
