using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Mundo.MarcasDeImpacto
{
    /// <summary>
    /// Qual marca um impacto deixa no chão, a partir do que o servidor já manda (ids de skill e
    /// tipo de golpe). Só apresentação. As skills são identificadas pelo id do catálogo, sem
    /// referenciar a funcionalidade Skills.
    /// </summary>
    internal static class DecisaoDeMarca
    {
        public const string MarteladaSismica = "martelo.martelada-sismica";
        public const string NovaDeFogo = "cajado.nova-de-fogo";
        public const string LinhaIncendiaria = "arco.linha-incendiaria";
        public const string ChuvaDeBrasas = "arco.chuva-de-brasas";

        /// <summary>A marca do acerto de uma skill, ou <c>false</c> se ela não deixa marca.</summary>
        public static bool DaSkill(string idDaSkill, out TipoDeMarca tipo)
        {
            switch (idDaSkill)
            {
                case MarteladaSismica:
                    tipo = TipoDeMarca.Martelada;
                    return true;
                case NovaDeFogo:
                case LinhaIncendiaria:
                case ChuvaDeBrasas:
                    tipo = TipoDeMarca.Fuligem;
                    return true;
                default:
                    tipo = default;
                    return false;
            }
        }

        /// <summary>Golpe pesado de martelo deixa marca de martelada; os demais golpes, não.</summary>
        public static bool DoGolpePesadoDeMartelo(bool pesado, TipoEquipamento arma) =>
            pesado && arma == TipoEquipamento.Martelo;
    }
}
