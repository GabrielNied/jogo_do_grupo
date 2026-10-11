using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Onde o jogador nasce: posição dos pés (Y do chão) e rotação (frente em +Z da rotação).
    /// Devolvida pelo construtor de mundo registrado em
    /// <see cref="EntradaNoJogo.DefinirConstrutorDeMundo"/>.
    /// </summary>
    public readonly struct PosicaoInicial
    {
        public PosicaoInicial(Vector3 posicao, Quaternion rotacao)
        {
            Posicao = posicao;
            Rotacao = rotacao;
        }

        /// <summary>Posição dos pés do jogador, em metros.</summary>
        public Vector3 Posicao { get; }

        /// <summary>Para onde o jogador olha ao nascer.</summary>
        public Quaternion Rotacao { get; }

        /// <summary>Origem, olhando para +Z.</summary>
        public static PosicaoInicial Origem =>
            new PosicaoInicial(Vector3.zero, Quaternion.identity);
    }
}
