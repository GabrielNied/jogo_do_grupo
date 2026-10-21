using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// Guardas e moradores ambiente da Brasa Mansa, em dados (#1069). As rotas seguem os corredores
    /// livres do <c>docs/features/mundo.md</c> (estradas em cruz e rua em anel) e a praça central
    /// em volta da fonte, para não exigir mudança no mapa. Ids a partir de <see cref="PrimeiroId"/>,
    /// fora do <see cref="CatalogoNpcs"/>: ninguém aqui abre diálogo.
    /// </summary>
    public static class PopulacaoDaCidade
    {
        public const int PrimeiroId = 1001;

        private static readonly string[] FalasDeGuarda =
        {
            "Pode passar, viajante.",
            "A cidade está tranquila hoje.",
            "Fique longe dos lobos além do portão.",
            "Sem encrenca na Brasa Mansa.",
        };

        private static readonly string[] FalasDeMorador =
        {
            "Bom dia! O mercado está cheio hoje.",
            "A forja do Brom nunca descansa.",
            "Dizem que há lobos perto da estrada.",
            "A sopa do Barnabé está quente.",
            "Ouvi falar de um sol que nasceu na estrada.",
            "Cuidado na serra, forasteiro.",
        };

        private static readonly string[] FalasDeConversaA =
        {
            "Viu a Capitã passar hoje cedo?",
            "O preço do ferro subiu de novo.",
            "Minha filha quer ser guarda.",
        };

        private static readonly string[] FalasDeConversaB =
        {
            "Vi sim, estava com pressa.",
            "É a serra, tudo fica mais caro.",
            "Ela tem o jeito, pode crer.",
        };

        public static readonly HabitanteDaCidade[] Todos = Montar();

        public static HabitanteDaCidade Obter(int id)
        {
            for (var i = 0; i < Todos.Length; i++)
            {
                if (Todos[i].Id == id)
                {
                    return Todos[i];
                }
            }

            return null;
        }

        private static HabitanteDaCidade[] Montar()
        {
            const float vg = ConfigPopulacaoDaCidade.VelocidadeGuarda;
            const float vm = ConfigPopulacaoDaCidade.VelocidadeMorador;

            return new[]
            {
                // Guardas parados nos portões (além da Capitã Valéria e do Guarda Kael, do catálogo).
                Guarda(1001, "Guarda Teodoro", Parado(3.5f, 32f, 3.5f, 0f), 0f),
                Guarda(1002, "Guarda Inês", Parado(-3.5f, -32f, -3.5f, 0f), 0f),
                // Patrulhas: anel em sentidos opostos e um trecho de cada estrada do portão.
                Guarda(
                    1003,
                    "Guarda Orlando",
                    new RotaDeNpc(
                        new[]
                        {
                            Ponto(22f, 22f, 5f),
                            Ponto(-22f, 22f, 5f),
                            Ponto(-22f, -22f, 5f),
                            Ponto(22f, -22f, 5f),
                        },
                        ModoDeRota.Ciclo,
                        vg
                    ),
                    0f
                ),
                Guarda(
                    1004,
                    "Guarda Maren",
                    new RotaDeNpc(
                        new[]
                        {
                            Ponto(-22f, -22f, 5f),
                            Ponto(-22f, 22f, 5f),
                            Ponto(22f, 22f, 5f),
                            Ponto(22f, -22f, 5f),
                        },
                        ModoDeRota.Ciclo,
                        vg
                    ),
                    37f
                ),
                Guarda(
                    1005,
                    "Guarda Bento",
                    new RotaDeNpc(
                        new[] { Ponto(2.5f, 14f, 6f), Ponto(2.5f, 30f, 6f) },
                        ModoDeRota.VaiEVolta,
                        vg
                    ),
                    0f
                ),
                Guarda(
                    1006,
                    "Guarda Selma",
                    new RotaDeNpc(
                        new[] { Ponto(-2.5f, -14f, 6f), Ponto(-2.5f, -30f, 6f) },
                        ModoDeRota.VaiEVolta,
                        vg
                    ),
                    11f
                ),
                // Pares que passeiam pela praça e param para conversar (rotas espelhadas: mesmo ciclo).
                Morador(1011, "Morador Tiago", 11, ParNorte(-1, vm), FalasDeConversaA, 1012, 0),
                Morador(1012, "Moradora Marta", 12, ParNorte(1, vm), FalasDeConversaB, 1011, 1),
                Morador(1013, "Morador Joaquim", 13, ParSul(-1, vm), FalasDeConversaA, 1014, 0),
                Morador(1014, "Moradora Dora", 14, ParSul(1, vm), FalasDeConversaB, 1013, 1),
                // Moradores sozinhos, pela rua em anel.
                Morador(
                    1015,
                    "Morador Heitor",
                    15,
                    new RotaDeNpc(
                        new[] { Ponto(-22f, -12f, 8f), Ponto(-22f, 12f, 8f) },
                        ModoDeRota.VaiEVolta,
                        vm
                    ),
                    FalasDeMorador,
                    0,
                    0,
                    9f
                ),
                Morador(
                    1016,
                    "Moradora Cecília",
                    16,
                    new RotaDeNpc(
                        new[] { Ponto(22f, -14f, 10f), Ponto(22f, 14f, 10f) },
                        ModoDeRota.VaiEVolta,
                        vm
                    ),
                    FalasDeMorador,
                    0,
                    0,
                    23f
                ),
            };
        }

        // Par da praça ao norte (z = 9): lado -1 à esquerda da fonte, 1 à direita.
        private static RotaDeNpc ParNorte(int lado, float velocidade) =>
            Par(lado, 9f, 5f, velocidade);

        private static RotaDeNpc ParSul(int lado, float velocidade) =>
            Par(lado, -9f, -5f, velocidade);

        private static RotaDeNpc Par(int lado, float zConversa, float zVolta, float velocidade) =>
            new RotaDeNpc(
                new[]
                {
                    new PontoDeRota(
                        1.2f * lado,
                        zConversa,
                        12f,
                        true,
                        true,
                        -1.2f * lado,
                        zConversa
                    ),
                    Ponto(7f * lado, zConversa, 4f),
                    Ponto(7f * lado, zVolta, 4f),
                },
                ModoDeRota.Ciclo,
                velocidade
            );

        private static PontoDeRota Ponto(float x, float z, float pausa) =>
            new PontoDeRota(x, z, pausa);

        private static RotaDeNpc Parado(float x, float z, float olharX, float olharZ) =>
            new RotaDeNpc(
                new[] { new PontoDeRota(x, z, 0f, false, true, olharX, olharZ) },
                ModoDeRota.Ciclo,
                1f
            );

        private static HabitanteDaCidade Guarda(int id, string nome, RotaDeNpc rota, float fase) =>
            new HabitanteDaCidade(id, nome, PapelNpc.Guarda, id, rota, FalasDeGuarda, fase);

        private static HabitanteDaCidade Morador(
            int id,
            string nome,
            int semente,
            RotaDeNpc rota,
            string[] falas,
            int parceiro,
            int lado,
            float fase = 0f
        ) =>
            new HabitanteDaCidade(
                id,
                nome,
                PapelNpc.Morador,
                semente,
                rota,
                falas,
                fase,
                parceiro,
                lado
            );
    }
}
