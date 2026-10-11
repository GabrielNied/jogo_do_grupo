using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Textos de cada raça na tela de criação. A fonte é a seção "Raças propostas" de
    /// <c>docs/features/personagem.md</c> e o livro de ideias (<c>docs/brainstorm/figjam.md</c>);
    /// o servidor não depende deles (só valida o <see cref="Raca"/>).
    /// </summary>
    internal sealed class DadosDaRaca
    {
        public Raca Raca;
        public string Nome;
        public string Povo;
        public string Aparencia;
        public string Traco;
        public string SkillRacial;
        public string Texto;
    }

    internal static class DadosDasRacas
    {
        private static readonly DadosDaRaca[] _todas =
        {
            new DadosDaRaca
            {
                Raca = Raca.Valdren,
                Nome = "Valdren",
                Povo = "Os povos da fronteira",
                Aparencia = "Humanos de tecidos grossos, couro gasto e marcas de viagem.",
                Traco =
                    "Espírito de Comunidade: comer perto de outro jogador compartilha 10 de saciedade com ele, uma vez por refeição.",
                SkillRacial =
                    "Estandarte dos Errantes: aliados em 25 m recuperam vigor mais depressa e resistem a interrupções por 20 s.",
                Texto = "Onde outros enxergam ruínas, os Valdren enxergam o próximo lar.",
            },
            new DadosDaRaca
            {
                Raca = Raca.Aurideo,
                Nome = "Aurídeos",
                Povo = "Portadores da alvorada",
                Aparencia =
                    "Pele em tons quentes ou azulados, olhos claros e marcas douradas que acendem.",
                Traco = "Olhar Crepuscular: reconhecem contornos próximos com pouca luz.",
                SkillRacial =
                    "Aurora Errante: um orbe ilumina 35 m por 30 s e pode ser lançado a até 100 m.",
                Texto = "Leve a alvorada com você. Revele o caminho dos seus amigos.",
            },
            new DadosDaRaca
            {
                Raca = Raca.Granitico,
                Nome = "Graníticos",
                Povo = "Filhos da montanha",
                Aparencia = "Corpo robusto, pele mineral e fissuras discretamente luminosas.",
                Traco =
                    "Raiz de Pedra: parados preparando uma ação, sofrem 20% menos deslocamento por empurrões.",
                SkillRacial =
                    "Ressonância Ancestral: três pulsos em 6 s, num raio de 30 m, desequilibram inimigos comuns.",
                Texto = "A montanha não se apressa. Ela espera o instante certo.",
            },
            new DadosDaRaca
            {
                Raca = Raca.Cinereo,
                Nome = "Cinéreos",
                Povo = "Herdeiros da brasa",
                Aparencia =
                    "Pele cinza, cobre ou carvão, olhos de brasa e pequenas placas minerais.",
                Traco = "Corpo de Fornalha: 15% menos dano de queimadura.",
                SkillRacial =
                    "Sopro de Cinzas: faixa de 25 m por 8 m, por 8 s, que o fogo inflama.",
                Texto = "Uma brasa basta para começar.",
            },
            new DadosDaRaca
            {
                Raca = Raca.Silvano,
                Nome = "Silvanos",
                Povo = "Guardiões das trilhas",
                Aparencia = "Esguios, de orelhas alongadas e cabelos em tons de folhas.",
                Traco = "Leitor de Rastros: pegadas de criaturas estudadas ficam mais visíveis.",
                SkillRacial =
                    "Chamado da Clareira: uma figura sonora e luminosa a até 60 m, por 12 s, atrai criaturas.",
                Texto = "A floresta sempre avisa. Aprenda a ouvir.",
            },
        };

        /// <summary>As raças, na ordem em que a tela de criação as mostra.</summary>
        public static DadosDaRaca[] Todas => _todas;

        /// <summary>Os dados da raça, ou <c>null</c> se ela ainda não tem texto.</summary>
        public static DadosDaRaca Procurar(Raca raca)
        {
            foreach (var dados in _todas)
            {
                if (dados.Raca == raca)
                {
                    return dados;
                }
            }

            return null;
        }
    }
}
