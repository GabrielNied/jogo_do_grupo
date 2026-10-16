using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// O arco que a lâmina percorre num golpe (combate.md, "Golpe realista"): um leque no plano
    /// do chão a partir do atacante, do <see cref="AnguloInicialGraus"/> ao
    /// <see cref="AnguloFinalGraus"/>, com o <see cref="AlcanceMetros"/> do comprimento da arma, e
    /// uma faixa de altura. Os ângulos são relativos à direção do golpe (0 grau é à frente e
    /// cresce para +X, como <see cref="IntencaoDeAtaque.DirecaoY"/>). A janela de tempo diz
    /// quando a lâmina está no arco, em milissegundos a partir do contato (negativo é antes).
    /// O servidor testa o arco contra a cápsula do corpo do alvo; o client desenha e anima com
    /// os mesmos dados.
    /// </summary>
    public readonly struct ArcoDoGolpe
    {
        public ArcoDoGolpe(
            double anguloInicialGraus,
            double anguloFinalGraus,
            double alcanceMetros,
            double alturaMinimaMetros,
            double alturaMaximaMetros,
            int inicioMs,
            int fimMs
        )
        {
            AnguloInicialGraus = anguloInicialGraus;
            AnguloFinalGraus = anguloFinalGraus;
            AlcanceMetros = alcanceMetros;
            AlturaMinimaMetros = alturaMinimaMetros;
            AlturaMaximaMetros = alturaMaximaMetros;
            InicioMs = inicioMs;
            FimMs = fimMs;
        }

        /// <summary>Onde a lâmina começa, em graus à frente da direção do golpe.</summary>
        public double AnguloInicialGraus { get; }

        /// <summary>Onde a lâmina termina, em graus à frente da direção do golpe.</summary>
        public double AnguloFinalGraus { get; }

        /// <summary>Comprimento da arma mais o braço: até onde a ponta chega, em metros.</summary>
        public double AlcanceMetros { get; }

        /// <summary>Altura da lâmina acima dos pés, em metros: de baixo.</summary>
        public double AlturaMinimaMetros { get; }

        /// <summary>Altura da lâmina acima dos pés, em metros: de cima.</summary>
        public double AlturaMaximaMetros { get; }

        /// <summary>Quando a lâmina entra no arco, em ms a partir do contato (negativo é antes).</summary>
        public int InicioMs { get; }

        /// <summary>Quando a lâmina sai do arco, em ms a partir do contato.</summary>
        public int FimMs { get; }

        /// <summary>O menor dos dois ângulos (o lado esquerdo do leque).</summary>
        public double AnguloMinimoGraus => Math.Min(AnguloInicialGraus, AnguloFinalGraus);

        /// <summary>O maior dos dois ângulos (o lado direito do leque).</summary>
        public double AnguloMaximoGraus => Math.Max(AnguloInicialGraus, AnguloFinalGraus);

        /// <summary>
        /// Se o arco passa pela cápsula do corpo do alvo: um círculo de
        /// <paramref name="raioDoAlvo"/> no chão (centro em <paramref name="alvoX"/>,
        /// <paramref name="alvoZ"/>) que sobe até <paramref name="alturaDoAlvo"/>. Vale se a faixa
        /// de altura da lâmina cruza a do corpo e o leque toca o círculo (o centro do alvo no leque
        /// ou uma das duas bordas do leque a menos que o raio do alvo). Alvo sobre o atacante
        /// sempre é acertado.
        /// </summary>
        public bool Alcanca(
            double atacanteX,
            double atacanteZ,
            double direcaoYGraus,
            double alvoX,
            double alvoZ,
            double raioDoAlvo,
            double alturaDoAlvo
        )
        {
            if (AlturaMaximaMetros < 0.0 || AlturaMinimaMetros > alturaDoAlvo)
            {
                return false;
            }

            double raio = Math.Max(0.0, raioDoAlvo);
            double dx = alvoX - atacanteX;
            double dz = alvoZ - atacanteZ;
            double distancia = Math.Sqrt(dx * dx + dz * dz);
            if (distancia <= raio)
            {
                return true;
            }

            double angulo = RegrasAtaque.DiferencaDeAngulo(
                Math.Atan2(dx, dz) * 180.0 / Math.PI,
                direcaoYGraus
            );
            if (angulo >= AnguloMinimoGraus && angulo <= AnguloMaximoGraus)
            {
                return distancia - raio <= AlcanceMetros;
            }

            return TocaABorda(distancia, angulo, AnguloMinimoGraus, raio)
                || TocaABorda(distancia, angulo, AnguloMaximoGraus, raio);
        }

        // A borda do leque é um segmento da origem até o alcance, no ângulo dado.
        private bool TocaABorda(double distancia, double angulo, double anguloDaBorda, double raio)
        {
            double entre = RegrasAtaque.DiferencaDeAngulo(angulo, anguloDaBorda) * Math.PI / 180.0;
            double ao = Math.Cos(entre);
            double t = Math.Max(0.0, Math.Min(AlcanceMetros, distancia * ao));
            double quadrado = distancia * distancia + t * t - 2.0 * distancia * t * ao;
            return quadrado <= raio * raio;
        }
    }

    /// <summary>
    /// Os dados do arco de cada golpe da espada (leve 1, 2 e 3 da cadeia, e o pesado). Substitui o
    /// cone de <see cref="RegrasAtaque.EstaNoVolume"/> para o golpe comum. Valores da proposta, a
    /// calibrar com o gizmo do arco no F10; martelo, arco e cajado entram com os seus dados na
    /// tarefa de cada arma.
    /// </summary>
    public static class ArcosDosGolpes
    {
        /// <summary>Altura do corpo (cápsula) quando o alvo não informa a sua, em metros.</summary>
        public const double AlturaPadraoDoCorpoMetros = 1.8;

        /// <summary>Primeiro leve: corte da direita para a esquerda.</summary>
        public static readonly ArcoDoGolpe Leve1 = new ArcoDoGolpe(
            70.0,
            -70.0,
            2.2,
            0.3,
            1.7,
            -100,
            80
        );

        /// <summary>Segundo leve: corte de volta, da esquerda para a direita.</summary>
        public static readonly ArcoDoGolpe Leve2 = new ArcoDoGolpe(
            -70.0,
            70.0,
            2.2,
            0.3,
            1.7,
            -100,
            80
        );

        /// <summary>Terceiro leve: golpe de cima, estreito e mais longo.</summary>
        public static readonly ArcoDoGolpe Leve3 = new ArcoDoGolpe(
            35.0,
            -35.0,
            2.6,
            0.2,
            2.0,
            -120,
            80
        );

        /// <summary>Pesado: corte largo, da direita para a esquerda.</summary>
        public static readonly ArcoDoGolpe Pesado = new ArcoDoGolpe(
            110.0,
            -110.0,
            2.4,
            0.2,
            1.9,
            -200,
            120
        );

        /// <summary>O arco do golpe de <paramref name="tipo"/> no <paramref name="passo"/> da cadeia.</summary>
        public static ArcoDoGolpe Obter(TipoDeAtaque tipo, int passo)
        {
            if (tipo == TipoDeAtaque.Pesado)
            {
                return Pesado;
            }

            switch (passo)
            {
                case 1:
                    return Leve2;
                case 2:
                    return Leve3;
                default:
                    return Leve1;
            }
        }
    }
}
