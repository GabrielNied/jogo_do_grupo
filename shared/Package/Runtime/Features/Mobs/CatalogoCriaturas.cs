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
        public const int ArqueiroDoCampo = 10;
        public const int GuardiaoDoReflexo = 100;
        public const int PresaVelha = 200;
        public const int AnciaoTrovejante = 300;
        public const int ColossoBasaltico = 301;

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
                id: CervoDeGalhadaOca,
                nome: "Cervo de Galhada Oca",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 12,
                vidaBase: 650,
                danoBase: 22,
                experienciaBase: 40,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 120
            ),
            new DefinicaoDeCriatura(
                id: MariposaDeBreu,
                nome: "Mariposa de Breu",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 10,
                vidaBase: 90,
                danoBase: 14,
                experienciaBase: 16,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 90
            ),
            new DefinicaoDeCriatura(
                id: LoboDeGeada,
                nome: "Lobo de Geada",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 22,
                vidaBase: 1400,
                danoBase: 95,
                experienciaBase: 160,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 150
            ),
            new DefinicaoDeCriatura(
                id: CarapacaDeBrasa,
                nome: "Carapaça de Brasa",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 30,
                vidaBase: 3200,
                danoBase: 180,
                experienciaBase: 320,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 180
            ),
            new DefinicaoDeCriatura(
                id: JavaliFerrugem,
                nome: "Javali Ferrugem",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 3,
                vidaBase: 180,
                danoBase: 18,
                experienciaBase: 12,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 120
            ),
            new DefinicaoDeCriatura(
                id: ArqueiroDoCampo,
                nome: "Arqueiro do Campo",
                categoria: CategoriaDeCriatura.Comum,
                nivel: 4,
                vidaBase: 120,
                danoBase: 35,
                experienciaBase: 22,
                chanceDeAparecer: 1.0,
                intervaloDeAparicaoSegundos: 120
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
            new DefinicaoDeCriatura(
                id: AnciaoTrovejante,
                nome: "Ancião Trovejante",
                categoria: CategoriaDeCriatura.ChefeDeMundo,
                nivel: 45,
                vidaBase: 250000,
                danoBase: 900,
                experienciaBase: 20000,
                chanceDeAparecer: 0.25,
                intervaloDeAparicaoSegundos: 3600
            ),
            new DefinicaoDeCriatura(
                id: ColossoBasaltico,
                nome: "Colosso Basáltico",
                categoria: CategoriaDeCriatura.ChefeDeMundo,
                nivel: 50,
                vidaBase: 350000,
                danoBase: 1200,
                experienciaBase: 30000,
                chanceDeAparecer: 0.25,
                intervaloDeAparicaoSegundos: 3600
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
