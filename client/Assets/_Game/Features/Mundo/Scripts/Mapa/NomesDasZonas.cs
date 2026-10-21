using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Client.Features.Mundo
{
    /// <summary>
    /// Nome da zona em que um ponto do mundo está, para o minimapa (como o nome da zona sobre o
    /// minimapa do World of Warcraft). Usa os nomes do Livro de Asterra adotados como rótulos dos
    /// biomas (mundo.md, "Livro de Asterra"); deserto e pântano ainda não têm nome do livro.
    /// </summary>
    public static class NomesDasZonas
    {
        public static string Nome(float x, float z) => Nome(MapaDoMundo.BiomaEm(x, z));

        public static string Nome(Bioma bioma)
        {
            switch (bioma)
            {
                case Bioma.Cidade:
                    return "Brasa Mansa";
                case Bioma.Campo:
                    return "Vale das Fogueiras";
                case Bioma.Mata:
                    return "Mata do Eco";
                case Bioma.Colinas:
                    return "Colinas do Vigia";
                case Bioma.Deserto:
                    return "Deserto";
                case Bioma.Neve:
                    return "Serra do Vidro Frio";
                case Bioma.Lava:
                    return "Caldeira Rubra";
                case Bioma.Pantano:
                    return "Pântano";
                default:
                    return string.Empty;
            }
        }
    }
}
