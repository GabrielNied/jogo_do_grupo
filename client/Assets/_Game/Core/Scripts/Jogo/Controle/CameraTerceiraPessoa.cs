using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// Câmera que orbita e segue o jogador: o mouse (ou o analógico direito) gira, e ela se
    /// aproxima quando há uma parede entre ela e o jogador. O cálculo fica em
    /// <see cref="OrbitaCamera"/>. Criada pela <see cref="EntradaNoJogo"/>.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraTerceiraPessoa : MonoBehaviour
    {
        /// <summary>Graus por pixel de movimento do mouse.</summary>
        [SerializeField]
        private float _sensibilidadeMouse = 0.12f;

        /// <summary>Graus por segundo com o analógico no máximo.</summary>
        [SerializeField]
        private float _velocidadeControle = 150f;

        /// <summary>Raio usado para detectar paredes atrás do jogador, em metros.</summary>
        [SerializeField]
        private float _raioColisao = 0.2f;

        /// <summary>Distância mínima até o pivô quando encosta numa parede, em metros.</summary>
        [SerializeField]
        private float _distanciaMinima = 0.5f;

        private readonly OrbitaCamera _orbita = new OrbitaCamera();

        private AcoesJogador _acoes;
        private Transform _alvo;

        /// <summary>Se lê o input (falso com o menu aberto).</summary>
        public bool InputAtivo { get; set; } = true;

        /// <summary>Ângulo horizontal da câmera, em graus: a "frente" do movimento.</summary>
        public float Horizontal => _orbita.Horizontal;

        /// <summary>Ângulos, distância e altura do pivô.</summary>
        public OrbitaCamera Orbita => _orbita;

        internal void Configurar(AcoesJogador acoes, Transform alvo)
        {
            _acoes = acoes;
            _alvo = alvo;
            if (alvo != null)
            {
                _orbita.Definir(alvo.eulerAngles.y, _orbita.Vertical);
                Posicionar();
            }
        }

        private void LateUpdate()
        {
            if (_alvo == null)
            {
                return;
            }

            if (InputAtivo && _acoes != null)
            {
                var delta =
                    _acoes.OlharMouse * _sensibilidadeMouse * OpcoesLocais.Atual.Sensibilidade
                    + _acoes.OlharControle * (_velocidadeControle * Time.deltaTime);
                _orbita.Girar(delta);
            }

            Posicionar();
        }

        private void Posicionar()
        {
            var pivo = _orbita.Pivo(_alvo.position);
            var distancia = _orbita.Distancia;
            var paraTras = _orbita.Rotacao * Vector3.back;

            // O próprio jogador não conta: o SphereCast ignora colisores em que já começa dentro.
            if (
                Physics.SphereCast(
                    pivo,
                    _raioColisao,
                    paraTras,
                    out var acerto,
                    distancia,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                distancia = Mathf.Max(_distanciaMinima, acerto.distance);
            }

            transform.SetPositionAndRotation(_orbita.Posicao(pivo, distancia), _orbita.Rotacao);
        }
    }
}
