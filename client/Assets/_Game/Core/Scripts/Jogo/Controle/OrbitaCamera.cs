using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Ângulos, zoom, distância suavizada e pivô filtrado da câmera que orbita o jogador, sem
    /// componente do Unity (testável no EditMode). O <see cref="CameraTerceiraPessoa"/> lê o
    /// mouse, faz o teste de obstáculo e aplica o resultado.
    /// </summary>
    public sealed class OrbitaCamera
    {
        /// <summary>Zoom mais perto pela roda, em metros.</summary>
        public const float ZoomMinimo = 2f;

        /// <summary>Zoom mais longe pela roda, em metros.</summary>
        public const float ZoomMaximo = 8f;

        /// <summary>Distância inicial, em metros.</summary>
        public const float ZoomPadrao = 4.5f;

        /// <summary>Metros por clique da roda.</summary>
        public const float PassoZoom = 0.75f;

        /// <summary>
        /// Rapidez (1/s) com que a câmera se aproxima quando algo fica entre ela e o jogador ou o
        /// zoom aproxima: alta, para não ficar dentro de pilar.
        /// </summary>
        public const float RapidezAproximar = 25f;

        /// <summary>Rapidez (1/s) com que a câmera recua quando o obstáculo sai: baixa, sem salto.</summary>
        public const float RapidezRecuar = 3f;

        /// <summary>
        /// Quanto a câmera pode ficar atrás do obstáculo enquanto se aproxima, em metros: menos que
        /// o raio do teste, para nunca entrar na parede.
        /// </summary>
        public const float FolgaAtrasDoObstaculo = 0.15f;

        /// <summary>Rapidez (1/s) com que o pivô segue o jogador na horizontal.</summary>
        public const float RapidezPivoHorizontal = 20f;

        /// <summary>Rapidez (1/s) com que o pivô segue o jogador na vertical (degrau, pulo, correção).</summary>
        public const float RapidezPivoVertical = 8f;

        /// <summary>Acima deste salto do jogador, em metros, o pivô vai direto (teleporte, renascer).</summary>
        public const float SaltoTeleporte = 6f;

        private bool _temPivo;
        private Vector3 _pesFiltrados;

        /// <summary>Ângulo horizontal, em graus (0 = olhando para +Z), entre 0 e 360.</summary>
        public float Horizontal { get; private set; }

        /// <summary>Ângulo vertical, em graus (positivo = olhando de cima).</summary>
        public float Vertical { get; private set; } = 15f;

        /// <summary>Menor ângulo vertical (olhando de baixo), em graus.</summary>
        public float VerticalMinimo { get; set; } = -30f;

        /// <summary>Maior ângulo vertical (olhando de cima), em graus.</summary>
        public float VerticalMaximo { get; set; } = 70f;

        /// <summary>
        /// Distância escolhida pelo zoom, do pivô até a câmera, em metros, entre
        /// <see cref="ZoomMinimo"/> e <see cref="ZoomMaximo"/>.
        /// </summary>
        public float Distancia { get; private set; } = ZoomPadrao;

        /// <summary>
        /// Distância em que a câmera está agora, suavizada: aproxima rápido e recua devagar até
        /// <see cref="Distancia"/> ou até o obstáculo.
        /// </summary>
        public float DistanciaAtual { get; private set; } = ZoomPadrao;

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

        /// <summary>
        /// Zoom pela roda: <paramref name="cliques"/> positivo aproxima, negativo afasta, dentro dos
        /// limites.
        /// </summary>
        public void Zoom(float cliques)
        {
            Distancia = Mathf.Clamp(Distancia - cliques * PassoZoom, ZoomMinimo, ZoomMaximo);
        }

        /// <summary>
        /// Avança <see cref="DistanciaAtual"/> por <paramref name="dt"/> segundos rumo ao zoom,
        /// limitado por <paramref name="distanciaLivre"/> (até o obstáculo; infinito se nada).
        /// Aproxima rápido sem ficar mais que <see cref="FolgaAtrasDoObstaculo"/> atrás do
        /// obstáculo e recua devagar.
        /// </summary>
        public float AtualizarDistancia(float distanciaLivre, float dt)
        {
            var alvo = Mathf.Min(Distancia, distanciaLivre);
            var rapidez = alvo < DistanciaAtual ? RapidezAproximar : RapidezRecuar;
            var atual = Mathf.Lerp(DistanciaAtual, alvo, Fator(rapidez, dt));
            if (distanciaLivre < atual)
            {
                atual = Mathf.Min(atual, distanciaLivre + FolgaAtrasDoObstaculo);
            }

            DistanciaAtual = atual;
            return atual;
        }

        /// <summary>Põe a câmera direto no zoom e no jogador, sem suavizar (ao nascer ou teleportar).</summary>
        public void Reiniciar(Vector3 pesDoJogador)
        {
            DistanciaAtual = Distancia;
            _pesFiltrados = pesDoJogador;
            _temPivo = true;
        }

        /// <summary>
        /// Pivô filtrado: segue os pés do jogador rápido na horizontal e mais devagar na vertical,
        /// para degrau e correção do servidor não darem tranco. Salto maior que
        /// <see cref="SaltoTeleporte"/> vai direto.
        /// </summary>
        public Vector3 PivoFiltrado(Vector3 pesDoJogador, float dt)
        {
            var salto = pesDoJogador - _pesFiltrados;
            if (!_temPivo || salto.sqrMagnitude > SaltoTeleporte * SaltoTeleporte)
            {
                _pesFiltrados = pesDoJogador;
                _temPivo = true;
            }
            else
            {
                var horizontal = Fator(RapidezPivoHorizontal, dt);
                _pesFiltrados = new Vector3(
                    Mathf.Lerp(_pesFiltrados.x, pesDoJogador.x, horizontal),
                    Mathf.Lerp(_pesFiltrados.y, pesDoJogador.y, Fator(RapidezPivoVertical, dt)),
                    Mathf.Lerp(_pesFiltrados.z, pesDoJogador.z, horizontal)
                );
            }

            return Pivo(_pesFiltrados);
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

        /// <summary>Fração do caminho andada em <paramref name="dt"/>, independente do FPS.</summary>
        private static float Fator(float rapidez, float dt)
        {
            return 1f - Mathf.Exp(-rapidez * Mathf.Max(0f, dt));
        }
    }
}
