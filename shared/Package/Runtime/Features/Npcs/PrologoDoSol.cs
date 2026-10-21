#nullable enable annotations
using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>Uma fala da cena "O sol que nasceu na estrada" (#327).</summary>
    public sealed class FalaDoPrologo
    {
        public const int Narrador = -1;
        public const int Viajante = -2;

        public readonly float Inicio;
        public readonly float Duracao;

        /// <summary>Índice do mago que fala (0 é o líder), <see cref="Narrador"/> ou <see cref="Viajante"/>.</summary>
        public readonly int Falante;

        public readonly string Texto;

        public FalaDoPrologo(float inicio, float duracao, int falante, string texto)
        {
            Inicio = inicio;
            Duracao = duracao;
            Falante = falante;
            Texto = texto;
        }

        public float Fim => Inicio + Duracao;
    }

    /// <summary>
    /// Roteiro e linha do tempo do prólogo opcional "O sol que nasceu na estrada" (#327). É só
    /// encenação do client: os magos são NPCs de cena, não há loot, dano nem estado no servidor, e
    /// quem assiste e quem pula terminam no mesmo ponto, na cidade, com a mesma missão.
    /// Texto do livro de ideias (FigJam, "Prólogo"), no <c>docs/features/missoes.md</c>.
    /// </summary>
    public static class PrologoDoSol
    {
        public const string Titulo = "O sol que nasceu na estrada";
        public const int QuantidadeDeMagos = 4;
        public const int QuantidadeDeSombras = 9;

        /// <summary>Instante do clarão: a esfera se solta e a manada some.</summary>
        public const float TempoDoClarao = 28f;

        /// <summary>Duração do clarão e da dissolução da manada, em segundos.</summary>
        public const float DuracaoDoClarao = 4f;

        public static readonly string[] NomesDosMagos =
        {
            "Arquimaga Veyra",
            "Mago Ilso",
            "Maga Oren",
            "Mago Tavi",
        };

        public static readonly FalaDoPrologo[] Falas =
        {
            new FalaDoPrologo(
                0f,
                7f,
                FalaDoPrologo.Narrador,
                "Você veio de longe. Talvez tenha ouvido falar das cidades, dos tesouros..."
            ),
            new FalaDoPrologo(
                7f,
                6f,
                FalaDoPrologo.Narrador,
                "Mas aqui, poder é algo que se vê no horizonte."
            ),
            new FalaDoPrologo(13f, 5f, 1, "A manada está perto! Sustentem a esfera!"),
            new FalaDoPrologo(18f, 5f, 2, "Os fios aguentam. Ela cresce sobre as árvores!"),
            new FalaDoPrologo(23f, 5f, 3, "Mais um pouco. Todos juntos!"),
            new FalaDoPrologo(28f, 5f, 0, "Agora! Que o pequeno sol nasça!"),
            new FalaDoPrologo(
                33f,
                5f,
                FalaDoPrologo.Narrador,
                "O clarão cobre a estrada. A manada some como poeira."
            ),
            new FalaDoPrologo(
                38f,
                6f,
                FalaDoPrologo.Viajante,
                "Vamos, forasteiro. A Brasa Mansa fica logo ali, e a Capitã Valéria espera por você."
            ),
        };

        public static float DuracaoTotal => Falas[Falas.Length - 1].Fim;

        /// <summary>A fala do instante <paramref name="tempo"/>, ou <c>null</c> depois do fim.</summary>
        public static FalaDoPrologo? FalaEm(float tempo)
        {
            for (var i = 0; i < Falas.Length; i++)
            {
                if (tempo >= Falas[i].Inicio && tempo < Falas[i].Fim)
                {
                    return Falas[i];
                }
            }

            return null;
        }

        /// <summary>0 a 1: a manada vem da estrada até perto da clareira, antes do clarão.</summary>
        public static float AproximacaoEm(float tempo) => Limitar(tempo / TempoDoClarao);

        /// <summary>0 a 1: o tamanho da esfera (cresce até o clarão e depois fica grande).</summary>
        public static float BrilhoEm(float tempo)
        {
            var t = Limitar((tempo - 8f) / (TempoDoClarao - 8f));
            return t * t * (3f - 2f * t);
        }

        /// <summary>0 a 1: intensidade do clarão, com pico no meio da janela do clarão.</summary>
        public static float ClaraoEm(float tempo)
        {
            var t = (tempo - TempoDoClarao) / DuracaoDoClarao;
            if (t < 0f || t > 1f)
            {
                return 0f;
            }

            return 1f - Math.Abs(2f * t - 1f);
        }

        /// <summary>0 a 1: o quanto da manada já se desfez (0 antes do clarão, 1 depois dele).</summary>
        public static float DissolucaoEm(float tempo) =>
            Limitar((tempo - TempoDoClarao - 0.5f * DuracaoDoClarao) / (0.5f * DuracaoDoClarao));

        private static float Limitar(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
    }
}
