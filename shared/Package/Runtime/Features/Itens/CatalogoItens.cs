namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Catálogo fixo de itens de teste (recorte de 2026-10-11). Os ids são estáveis: nunca mude
    /// nem reaproveite um id já publicado. O nome exibido vem daqui, tanto no servidor quanto
    /// no client.
    /// </summary>
    public static class CatalogoItens
    {
        public const int EspadaDeTeste = 1;
        public const int EscudoDeTeste = 2;
        public const int PocaoDeTeste = 3;
        public const int MoedaDeTeste = 4;
        public const int RedeasDoCorcelNoturno = 10;
        public const int SinoDoCervoJovem = 11;
        public const int SinoDoFilhoteDeJavali = 12;
        public const int SinoDoLoboFilhote = 13;
        public const int SinoDoTatuPequeno = 14;
        public const int SinoDaCorujaDaClareira = 15;
        public const int FrascoDaMariposaLuz = 16;
        public const int SinoDoCoelhoDoPasto = 17;
        public const int SinoDoLoboDeGeadaFilhote = 18;
        public const int ColarDaCarapacaJovem = 19;
        public const int FrascoDoMosquitoDoPantano = 20;
        public const int RedeasDoLoboDePresa = 21;
        public const int RedeasDoCorcelDeCristal = 22;
        public const int RedeasDoCorcelDaGeada = 23;
        public const int RedeasDoBasaltoVivente = 24;
        public const int RedeasDoTempestario = 25;
        public const int RedeasDaMulaDeCarga = 26;

        /// <summary>Todos os ids do catálogo, em ordem crescente.</summary>
        public static readonly int[] Todos =
        {
            EspadaDeTeste,
            EscudoDeTeste,
            PocaoDeTeste,
            MoedaDeTeste,
            RedeasDoCorcelNoturno,
            SinoDoCervoJovem,
            SinoDoFilhoteDeJavali,
            SinoDoLoboFilhote,
            SinoDoTatuPequeno,
            SinoDaCorujaDaClareira,
            FrascoDaMariposaLuz,
            SinoDoCoelhoDoPasto,
            SinoDoLoboDeGeadaFilhote,
            ColarDaCarapacaJovem,
            FrascoDoMosquitoDoPantano,
            RedeasDoLoboDePresa,
            RedeasDoCorcelDeCristal,
            RedeasDoCorcelDaGeada,
            RedeasDoBasaltoVivente,
            RedeasDoTempestario,
            RedeasDaMulaDeCarga,
        };

        /// <summary>
        /// Nome exibido do item, ou <c>null</c> se <paramref name="itemId"/> não está no catálogo.
        /// </summary>
        public static string ObterNome(int itemId)
        {
            switch (itemId)
            {
                case EspadaDeTeste:
                    return "Espada de teste";
                case EscudoDeTeste:
                    return "Escudo de teste";
                case PocaoDeTeste:
                    return "Poção de teste";
                case MoedaDeTeste:
                    return "Moeda de teste";
                case RedeasDoCorcelNoturno:
                    return "Rédeas do Corcel Noturno";
                case SinoDoCervoJovem:
                    return "Sino do Cervo Jovem";
                case SinoDoFilhoteDeJavali:
                    return "Sino do Filhote de Javali";
                case SinoDoLoboFilhote:
                    return "Sino do Lobo Filhote";
                case SinoDoTatuPequeno:
                    return "Sino do Tatu Pequeno";
                case SinoDaCorujaDaClareira:
                    return "Sino da Coruja da Clareira";
                case FrascoDaMariposaLuz:
                    return "Frasco da Mariposa-luz";
                case SinoDoCoelhoDoPasto:
                    return "Sino do Coelho do Pasto";
                case SinoDoLoboDeGeadaFilhote:
                    return "Sino do Lobo de Geada Filhote";
                case ColarDaCarapacaJovem:
                    return "Colar da Carapaça Jovem";
                case FrascoDoMosquitoDoPantano:
                    return "Frasco do Mosquito do Pântano";
                case RedeasDoLoboDePresa:
                    return "Rédeas do Lobo de Presa";
                case RedeasDoCorcelDeCristal:
                    return "Rédeas do Corcel de Cristal";
                case RedeasDoCorcelDaGeada:
                    return "Rédeas do Corcel da Geada";
                case RedeasDoBasaltoVivente:
                    return "Rédeas do Basalto Vivente";
                case RedeasDoTempestario:
                    return "Rédeas do Tempestário";
                case RedeasDaMulaDeCarga:
                    return "Rédeas da Mula de Carga";
                default:
                    return null;
            }
        }
    }
}
