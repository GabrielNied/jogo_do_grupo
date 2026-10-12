using System;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Gera os sons de combate como ondas simples (ruído e tons com envelope), sem arquivos de
    /// terceiros: não há licença a registrar. Puro e determinístico (mesmo som toda vez).
    /// Se o grupo escolher sons gravados, eles entram no lugar daqui.
    /// </summary>
    internal static class SintetizadorDeSons
    {
        public const int TaxaDeAmostragem = 22050;

        /// <summary>Amostras de -1 a 1; <see cref="SomDeCombate.Nenhum"/> devolve vazio.</summary>
        public static float[] Gerar(SomDeCombate som)
        {
            switch (som)
            {
                case SomDeCombate.Golpe:
                    return Ruido(0.16f, 6f, 0.5f, 1800f, 600f);
                case SomDeCombate.AcertoLeve:
                    return Impacto(0.12f, 150f, 0.55f, 0.35f);
                case SomDeCombate.AcertoPesado:
                    return Impacto(0.22f, 90f, 0.9f, 0.5f);
                case SomDeCombate.Esquiva:
                    return Ruido(0.2f, 4f, 0.35f, 600f, 2400f);
                case SomDeCombate.EsquivaBemSucedida:
                    return Tom(0.22f, 880f, 1320f, 0.4f);
                case SomDeCombate.GolpeRecebido:
                    return Impacto(0.26f, 70f, 0.8f, 0.7f);
                case SomDeCombate.GolpeNoVazio:
                    return Ruido(0.24f, 3.5f, 0.45f, 500f, 250f);
                case SomDeCombate.AlertaDeArea:
                    return Tom(0.14f, 440f, 440f, 0.3f);
                default:
                    return Array.Empty<float>();
            }
        }

        /// <summary>Ruído com envelope que decai, cuja "cor" (filtro simples) varia de um tom a outro.</summary>
        private static float[] Ruido(
            float segundos,
            float queda,
            float volume,
            float de,
            float para
        )
        {
            var amostras = new float[(int)(segundos * TaxaDeAmostragem)];
            uint semente = 12345u;
            var anterior = 0f;
            for (var i = 0; i < amostras.Length; i++)
            {
                var t = (float)i / amostras.Length;
                semente = unchecked(semente * 1664525u + 1013904223u);
                var bruto = (semente >> 8) / (float)(1 << 24) * 2f - 1f;
                var corte = de + (para - de) * t;
                var alfa = Math.Min(1f, corte / TaxaDeAmostragem * 6.283f);
                anterior += alfa * (bruto - anterior);
                amostras[i] = Limitar(anterior * (float)Math.Exp(-queda * t) * volume);
            }

            return amostras;
        }

        /// <summary>Pancada: tom grave que cai de frequência, misturado com um estalo de ruído.</summary>
        private static float[] Impacto(float segundos, float grave, float volume, float estalo)
        {
            var amostras = new float[(int)(segundos * TaxaDeAmostragem)];
            uint semente = 9876u;
            var fase = 0.0;
            for (var i = 0; i < amostras.Length; i++)
            {
                var t = (float)i / amostras.Length;
                semente = unchecked(semente * 1664525u + 1013904223u);
                var bruto = (semente >> 8) / (float)(1 << 24) * 2f - 1f;
                var frequencia = grave * (1f - 0.5f * t);
                fase += 2.0 * Math.PI * frequencia / TaxaDeAmostragem;
                var corpo = (float)Math.Sin(fase) * (float)Math.Exp(-5f * t);
                var ruido = bruto * (float)Math.Exp(-30f * t) * estalo;
                amostras[i] = Limitar((corpo + ruido) * volume);
            }

            return amostras;
        }

        private static float[] Tom(float segundos, float de, float para, float volume)
        {
            var amostras = new float[(int)(segundos * TaxaDeAmostragem)];
            var fase = 0.0;
            for (var i = 0; i < amostras.Length; i++)
            {
                var t = (float)i / amostras.Length;
                var frequencia = de + (para - de) * t;
                fase += 2.0 * Math.PI * frequencia / TaxaDeAmostragem;
                var ataque = Math.Min(1f, t * 20f);
                amostras[i] = Limitar((float)Math.Sin(fase) * ataque * (1f - t) * volume);
            }

            return amostras;
        }

        private static float Limitar(float valor) =>
            valor > 1f ? 1f
            : valor < -1f ? -1f
            : valor;
    }
}
