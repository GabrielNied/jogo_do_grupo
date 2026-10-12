using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>
    /// Valores ajustáveis da luz de dia do mundo (issue #162). Só aparência: o jogo não decide
    /// nada com eles. A cor, a força e a névoa por bioma ficam no <c>AmbienteDosBiomas</c>;
    /// aqui moram o ângulo e a sombra do sol, a luz de preenchimento e as lanternas.
    /// </summary>
    public static class ConfigIluminacao
    {
        // ----- Sol -----

        /// <summary>Altura do sol no céu, em graus. Mais baixo alonga as sombras e mostra o relevo.</summary>
        public const float AlturaDoSol = 42f;

        /// <summary>Para onde o sol gira, em graus (a sombra das casas cai para o lado oposto).</summary>
        public const float GiroDoSol = -35f;

        /// <summary>Quanto a sombra do sol escurece (0 = some, 1 = preta). Abaixo de 1 para o chão claro e a mata não ficarem pretos.</summary>
        public const float ForcaDaSombra = 0.55f;

        // ----- Luz de preenchimento (rosto do personagem fora do sol) -----

        /// <summary>Altura da luz de preenchimento, em graus.</summary>
        public const float AlturaDoPreenchimento = 25f;

        /// <summary>Giro da luz de preenchimento: vem do lado oposto ao do sol.</summary>
        public const float GiroDoPreenchimento = GiroDoSol + 180f;

        /// <summary>Força do preenchimento em relação à do sol. Tem de ser bem menor que 1: o sol continua sendo a luz principal.</summary>
        public const float FracaoDoPreenchimento = 0.4f;

        /// <summary>Quanto da cor do sol entra na cor do preenchimento (o resto é a cor do céu).</summary>
        public const float MisturaDoSolNoPreenchimento = 0.25f;

        // ----- Lanternas (módulo de 4 m, poste de uns 6 m) -----

        /// <summary>Cor da chama, quente.</summary>
        public static readonly Color CorDaLanterna = new Color(1f, 0.72f, 0.42f);

        /// <summary>
        /// Força da luz de ponto. A luz cai com o quadrado da distância: a 6 m (a base do poste)
        /// dá cerca de 0,7, parecido com o sol do dia; a 12 m, cerca de 0,17.
        /// </summary>
        public const float IntensidadeDaLanterna = 26f;

        /// <summary>Alcance em metros: cinco módulos de 4 m, para as luzes da praça (22 m entre uma e outra) se encontrarem.</summary>
        public const float AlcanceDaLanterna = 20f;

        /// <summary>Altura da luz acima do chão quando o prefab da lanterna não traz uma.</summary>
        public const float AlturaDaLuzDaLanterna = 5.8f;

        // ----- Caverna (sem sol: só ambiente fraco e lanternas) -----

        /// <summary>Luz ambiente de cima dentro da caverna: quase preta.</summary>
        public static readonly Color CeuDaCaverna = new Color(0.07f, 0.065f, 0.07f);

        /// <summary>Luz ambiente dos lados dentro da caverna.</summary>
        public static readonly Color HorizonteDaCaverna = new Color(0.06f, 0.055f, 0.055f);

        /// <summary>Luz ambiente de baixo dentro da caverna.</summary>
        public static readonly Color SoloDaCaverna = new Color(0.04f, 0.035f, 0.035f);

        /// <summary>Cor da névoa dentro da caverna (escura, para o fundo do túnel sumir).</summary>
        public static readonly Color NevoaDaCaverna = new Color(0.03f, 0.028f, 0.03f);

        /// <summary>Densidade da névoa dentro da caverna (exponencial ao quadrado).</summary>
        public const float DensidadeDaNevoaDaCaverna = 0.018f;

        /// <summary>Cor das lanternas da caverna, mais quente que as da cidade.</summary>
        public static readonly Color CorDaLuzDaCaverna = new Color(1f, 0.62f, 0.32f);

        /// <summary>Força de cada luz de ponto da caverna.</summary>
        public const float IntensidadeDaLuzDaCaverna = 12f;

        /// <summary>Alcance de cada luz da caverna, em metros.</summary>
        public const float AlcanceDaLuzDaCaverna = 13f;

        /// <summary>Altura da luz da caverna acima do piso, em metros.</summary>
        public const float AlturaDaLuzDaCaverna = 2.4f;

        /// <summary>Distância entre luzes ao longo do percurso da caverna, em metros.</summary>
        public const float DistanciaEntreLuzesDaCaverna = 11f;
    }
}
