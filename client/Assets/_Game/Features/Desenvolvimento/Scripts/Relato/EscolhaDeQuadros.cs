using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>Quais quadros guardados entram em cada folha do relato.</summary>
    internal static class EscolhaDeQuadros
    {
        /// <summary>
        /// <paramref name="quantos"/> índices espalhados de 0 a <paramref name="total"/> - 1,
        /// incluindo o primeiro e o último. Com poucos quadros, devolve todos.
        /// </summary>
        public static int[] Espalhados(int total, int quantos)
        {
            if (total <= 0 || quantos <= 0)
            {
                return Array.Empty<int>();
            }

            if (total <= quantos)
            {
                return Sequencia(0, total);
            }

            if (quantos == 1)
            {
                return new[] { total - 1 };
            }

            var indices = new int[quantos];
            for (var i = 0; i < quantos; i++)
            {
                indices[i] = (int)Math.Round(i * (total - 1) / (double)(quantos - 1));
            }

            return indices;
        }

        /// <summary>
        /// <paramref name="quantos"/> índices seguidos com <paramref name="antes"/> deles antes de
        /// <paramref name="centro"/>, deslizando para dentro quando encosta numa ponta.
        /// </summary>
        public static int[] EmVolta(int total, int centro, int quantos, int antes)
        {
            if (total <= 0 || quantos <= 0)
            {
                return Array.Empty<int>();
            }

            if (total <= quantos)
            {
                return Sequencia(0, total);
            }

            var inicio = centro - antes;
            if (inicio < 0)
            {
                inicio = 0;
            }

            if (inicio > total - quantos)
            {
                inicio = total - quantos;
            }

            return Sequencia(inicio, quantos);
        }

        /// <summary>
        /// O último quadro com tempo até <paramref name="tempo"/> (o que estava na tela quando a
        /// mensagem chegou). Antes do primeiro, o primeiro.
        /// </summary>
        public static int QuadroNoTempo(IReadOnlyList<AmostraDeQuadro> quadros, double tempo)
        {
            var escolhido = 0;
            for (var i = 0; i < quadros.Count; i++)
            {
                if (quadros[i].Tempo > tempo)
                {
                    break;
                }

                escolhido = i;
            }

            return escolhido;
        }

        /// <summary>
        /// Centro da folha de impacto: o quadro da última mensagem de um dos
        /// <paramref name="idsDeAcerto"/>; sem nenhuma, o último quadro.
        /// </summary>
        public static int CentroDoImpacto(
            IReadOnlyList<AmostraDeQuadro> quadros,
            IReadOnlyList<EventoDeRede> eventos,
            ICollection<ushort> idsDeAcerto
        )
        {
            for (var i = eventos.Count - 1; i >= 0; i--)
            {
                if (idsDeAcerto.Contains(eventos[i].Id))
                {
                    return QuadroNoTempo(quadros, eventos[i].Tempo);
                }
            }

            return Math.Max(0, quadros.Count - 1);
        }

        private static int[] Sequencia(int inicio, int quantos)
        {
            var indices = new int[quantos];
            for (var i = 0; i < quantos; i++)
            {
                indices[i] = inicio + i;
            }

            return indices;
        }
    }
}
