using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Ângulos e posição da câmera que orbita o jogador, sem componente do Unity (testável no
    /// EditMode). O <see cref="CameraTerceiraPessoa"/> lê o mouse e aplica o resultado.
    /// </summary>
    public sealed class OrbitaCamera
    {
        /// <summary>Ângulo horizontal, em graus (0 = olhando para +Z), entre 0 e 360.</summary>
        public float Horizontal { get; private set; }

        /// <summary>Ângulo vertical, em graus (positivo = olhando de cima).</summary>
        public float Vertical { get; private set; } = 15f;

        /// <summary>Menor ângulo vertical (olhando de baixo), em graus.</summary>
        public float VerticalMinimo { get; set; } = -30f;

        /// <summary>Maior ângulo vertical (olhando de cima), em graus.</summary>
        public float VerticalMaximo { get; set; } = 70f;

        /// <summary>Distância do pivô (cabeça do jogador) até a câmera, em metros.</summary>
        public float Distancia { get; set; } = 4.5f;

        /// <summary>Altura do pivô acima dos pés do jogador, em metros.</summary>
        public float AlturaPivo { get; set; } = 1.6f;

        /// <summary>Define os ângulos (ex.: atrás do jogador ao nascer), respeitando os limites.</summary>
        public void Definir(float horizontal, float vertical)
        {
            Horizontal = Mathf.Repeat(horizontal, 360f);
            Vertical = Mathf.Clamp(vertical, VerticalMinimo, VerticalMaximo);
        }

        /// <summary>
        /// Gira a câmera: <paramref name="delta"/> em graus (x = horizontal, y = vertical; y
        /// positivo olha para cima, então a câmera desce).
        /// </summary>
        public void Girar(Vector2 delta)
        {
            Definir(Horizontal + delta.x, Vertical - delta.y);
        }

        /// <summary>Rotação da câmera para os ângulos atuais.</summary>
        public Quaternion Rotacao => Quaternion.Euler(Vertical, Horizontal, 0f);

        /// <summary>Ponto que a câmera olha, a partir dos pés do jogador.</summary>
        public Vector3 Pivo(Vector3 pesDoJogador)
        {
            return pesDoJogador + Vector3.up * AlturaPivo;
        }

        /// <summary>
        /// Posição da câmera a <paramref name="distancia"/> metros atrás do pivô, na direção dos
        /// ângulos atuais.
        /// </summary>
        public Vector3 Posicao(Vector3 pivo, float distancia)
        {
            return pivo - Rotacao * Vector3.forward * distancia;
        }
    }
}
