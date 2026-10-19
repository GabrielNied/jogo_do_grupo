using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Conquistas
{
    public static class CatalogoDeConquistas
    {
        public const int PrimeirosPassos = 1;
        public const int DesbravadorDeAsterra = 2;
        public const int MatadorDeMobs = 3;
        public const int OFimDoPredador = 4;
        public const int FortunaAcumulada = 5;
        public const int MembroDeGuilda = 6;

        public static readonly IReadOnlyList<DefinicaoDeConquista> Todas =
            new DefinicaoDeConquista[]
            {
                new DefinicaoDeConquista(
                    PrimeirosPassos,
                    "Primeiros Passos",
                    "Alcançar nível 2 com qualquer personagem",
                    CategoriaConquista.Geral,
                    meta: 2,
                    pontos: 10,
                    tituloRecompensa: "O Iniciante"
                ),
                new DefinicaoDeConquista(
                    DesbravadorDeAsterra,
                    "Desbravador de Asterra",
                    "Visitar 3 áreas distintas do mundo",
                    CategoriaConquista.Exploracao,
                    meta: 3,
                    pontos: 15,
                    tituloRecompensa: "O Explorador"
                ),
                new DefinicaoDeConquista(
                    MatadorDeMobs,
                    "Matador de Mobs",
                    "Derrotar 10 monstros",
                    CategoriaConquista.Combate,
                    meta: 10,
                    pontos: 15,
                    tituloRecompensa: "O Caçador"
                ),
                new DefinicaoDeConquista(
                    OFimDoPredador,
                    "O Fim do Predador",
                    "Derrotar o temido chefe Presa-Velha",
                    CategoriaConquista.Combate,
                    meta: 1,
                    pontos: 25,
                    tituloRecompensa: "Caçador de Lobos"
                ),
                new DefinicaoDeConquista(
                    FortunaAcumulada,
                    "Fortuna Acumulada",
                    "Acumular 1.000 coroas na carteira",
                    CategoriaConquista.Economia,
                    meta: 1000,
                    pontos: 20,
                    tituloRecompensa: "O Próspero"
                ),
                new DefinicaoDeConquista(
                    MembroDeGuilda,
                    "Membro de Guilda",
                    "Fundar ou ingressar em uma guilda",
                    CategoriaConquista.Geral,
                    meta: 1,
                    pontos: 15,
                    tituloRecompensa: "O Companheiro"
                ),
            };

        private static readonly Dictionary<int, DefinicaoDeConquista> PorId =
            CriarDicionarioPorId();

        private static Dictionary<int, DefinicaoDeConquista> CriarDicionarioPorId()
        {
            var dic = new Dictionary<int, DefinicaoDeConquista>();
            for (int i = 0; i < Todas.Count; i++)
            {
                dic[Todas[i].Id] = Todas[i];
            }
            return dic;
        }

        public static bool TentarObter(int id, out DefinicaoDeConquista definicao)
        {
            return PorId.TryGetValue(id, out definicao);
        }
    }
}
